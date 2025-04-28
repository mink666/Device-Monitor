using LoginWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;
using LoginWeb.Data;
using LoginWeb.Models;
using System; 
using System.Collections.Generic; 
using System.Linq;
using System.Security.Claims;

namespace LoginWeb.Controllers
{
    [ApiController]
    [Route("api/auth")] // Base route
    public class AuthController : ControllerBase //use API controller
    {
        private readonly EmailService _emailService; // For sending emails
        private readonly IConfiguration _configuration; // For reading app settings
        private readonly AppDbContext _context; // For database operations
        private readonly ILogger<AuthController> _logger; // Inject Logger

        public AuthController(AppDbContext context, EmailService emailService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Username and Email are required." });
            }
            // 1a. Check if Username already exists
            bool usernameExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (usernameExists)
            {
                // Return specific error for username
                return BadRequest(new { message = $"Username '{request.Username}' is already taken. Please choose a different username." });
            }

            // 1b. Check if Email already exists
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                // Return specific error for email
                return BadRequest(new { message = $"Email '{request.Email}' is already registered. Please use a different email or log in." });
            }

            // 2. Generate the plain text password to send via email
            string generatedPassword = GenerateSecurePassword();

            // 3. Hash the generated password using MD5 (INSECURE!)
            string passwordHash = ComputeMd5Hash(generatedPassword);

            // 4. Create the new custom User entity
            var user = new User 
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash, 
                IsAdmin = false,
                EmailConfirmed = false,
                AccountStatus = true
            };
            // 5. Add user to context and save
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User {user.Username} created successfully with ID {user.Id}.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during user registration.");
                return StatusCode(500, new { message = "Registration failed due to a database error." });
            }

            // 6. Prepare and send confirmation email (link points to AccountController)
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id }, Request.Scheme);
            string emailBody = $@"
                <p>Thank you for registering!</p>
                <p>Your login details:</p>
                <p><strong>Username:</strong> {user.Username}</p>
                <p><strong>Password:</strong> {generatedPassword}</p> 
                <p>Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.</p>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Confirm Your Account & Your Login Credentials", emailBody);
                return Ok(new { message = "Registration successful. Please check your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send registration email to {user.Email}.");
                return Ok(new { message = "Registration successful, but the confirmation email could not be sent. Please contact support." });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and Password are required." });
            }
            // 1. Find the user by username using AppDbContext
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            // 2. Check if user exists
            if (user == null)
            {
                _logger.LogWarning($"Login failed: User '{request.Username}' not found.");
                return Unauthorized(new { message = "Invalid username or password." });
            }

            // 3. Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"Login failed for user '{user.Username}': Email not confirmed.");
                return Unauthorized(new { message = "Please confirm your email before logging in." });
            }
            if (!user.AccountStatus)
            {
                _logger.LogWarning($"Login failed for user '{user.Username}': Account is deactivated.");
                return Unauthorized(new { message = "Your account has been deactivated. Please contact an administrator." });
            }
            // 4. Hash the incoming password using MD5 (INSECURE!)
            string enteredPasswordHash = ComputeMd5Hash(request.Password);

            // 5. Compare the generated hash with the stored hash
            if (string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash != enteredPasswordHash)
            {
                _logger.LogWarning($"Login failed for user '{user.Username}': Password hash mismatch.");
                // _logger.LogDebug($"Stored Hash: {user.PasswordHash}, Entered Hash: {enteredPasswordHash}");
                return Unauthorized(new { message = "Invalid username or password." }); // Generic error
            }

            // 6. Login successful! Set session variables
            _logger.LogInformation($"Login successful for user '{user.Username}'. IsAdmin: {user.IsAdmin}");
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("isLogin", "true");
            HttpContext.Session.SetString("isAdmin", user.IsAdmin ? "true" : "false");

            return Ok(new { message = "Login successful", redirectUrl = "/Home/Index" });
        }

        [HttpGet("login-google")] // Redirect to Google login page
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth", null, Request.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        [HttpGet("google-response")] // Handle Google response
        public async Task<IActionResult> GoogleResponse()
        {
            // Authenticate the user using the "Cookies" scheme
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Google login failed. Authentication did not succeed.");
                HttpContext.Session.SetString("ErrorMessage", "Google login failed. Please try again.");
                return Redirect("/Account/Login");
            }

            var claims = result.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning($"Google login failed: No email received.");
                HttpContext.Session.SetString("ErrorMessage", "Google login failed: No email received.");
                return Redirect("/Account/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                HttpContext.Session.SetString("ErrorMessage", "Please create an account first.");
                return Redirect("/Account/Register");
            }
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"Login failed for user '{user.Username}': Email not confirmed.");
                return Unauthorized(new { message = "Please confirm your email before logging in." });
            }
            if (!user.AccountStatus)
            {
                _logger.LogWarning($"Login failed for user '{user.Username}': Account is deactivated.");
                return Unauthorized(new { message = "Your account has been deactivated. Please contact an administrator." });
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("isLogin", "true");
            HttpContext.Session.SetString("isAdmin", user.IsAdmin ? "true" : "false");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to home page
            return Redirect("/Home/Index");
        }

        [HttpGet("login-microsoft")] // Redirect to Microsoft login page
        public IActionResult LoginWithMicrosoft()
        {
            var redirectUrl = Url.Action("MicrosoftResponse", "Auth", null, Request.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Microsoft");
        }

        [HttpGet("microsoft-response")] // Handle Microsoft response
        public async Task<IActionResult> MicrosoftResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Microsoft login failed. Authentication did not succeed.");
                HttpContext.Session.SetString("ErrorMessage", "Microsoft login failed. Please try again.");
                return Redirect("/Account/Login");
            }

            var claims = result.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning($"Microsoft login failed: No email received.");
                HttpContext.Session.SetString("ErrorMessage", "Microsoft login failed: No email received.");
                return Redirect("/Account/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                HttpContext.Session.SetString("ErrorMessage", "Please create an account first.");
                return Redirect("/Account/Register");
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("isLogin", "true");
            HttpContext.Session.SetString("isAdmin", user.IsAdmin ? "true" : "false");

            return Redirect("/Home/Index");
        }
        private string GenerateSecurePassword(int length = 12)
        {
            const string lowerCaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string upperCaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digitChars = "0123456789";
            const string nonAlphanumericChars = "!@#$%^&*()_-+=<>?";

            string allValidChars = lowerCaseChars + upperCaseChars + digitChars + nonAlphanumericChars;

            // Use a list to build the password before shuffling
            List<char> passwordChars = new List<char>(length);
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[4]; 

            passwordChars.Add(GetRandomChar(lowerCaseChars, rng, randomBytes));
            passwordChars.Add(GetRandomChar(upperCaseChars, rng, randomBytes));
            passwordChars.Add(GetRandomChar(digitChars, rng, randomBytes));
            passwordChars.Add(GetRandomChar(nonAlphanumericChars, rng, randomBytes));

            int remainingLength = length - passwordChars.Count;
            for (int i = 0; i < remainingLength; i++)
            {
                passwordChars.Add(GetRandomChar(allValidChars, rng, randomBytes));
            }

            // Fisher-Yates shuffle algorithm using cryptographic RNG
            for (int i = passwordChars.Count - 1; i > 0; i--)
            {
                rng.GetBytes(randomBytes);
                uint randomIndex = BitConverter.ToUInt32(randomBytes, 0) % (uint)(i + 1);
                int j = (int)randomIndex;

                (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
            }

            rng.Dispose();

            return new string(passwordChars.ToArray());
        }

        // Helper function to get a single random char from a given string using RNG
        private char GetRandomChar(string characterSet, RandomNumberGenerator rng, byte[] buffer)
        {
            rng.GetBytes(buffer);
                                 
            uint randomIndex = BitConverter.ToUInt32(buffer, 0) % (uint)characterSet.Length;
            return characterSet[(int)randomIndex];
        }

        // --- MD5 Hashing Helper (INSECURE - Replace with PBKDF2) ---
        private static string ComputeMd5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert byte array to hex string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // Lowercase hex format
                }
                return sb.ToString();
            }
        }
    }


    public class LoginRequest // Request model for login
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Username { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Password { get; set; }
    }

    public class RegisterRequest //Request model for registration
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Username { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; }
    }
}