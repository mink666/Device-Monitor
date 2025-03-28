using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LoginWeb.Controllers
{
    [ApiController] 
    [Route("api/auth")] // Base route
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager; // For user management
        private readonly SignInManager<IdentityUser> _signInManager; // For sign-in and sign-out

        public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")] //Use IdentityUser to register a new user
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = new IdentityUser { UserName = request.Username };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(new { message = "Registration failed", errors = result.Errors });

            return Ok(new { message = "Registration successful" });
        }

        [HttpPost("login")] //use SignInManager to login
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _signInManager.PasswordSignInAsync(request.Username, request.Password, false, false);
                
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid username or password." });

            HttpContext.Session.SetString("Username", request.Username);
            HttpContext.Session.SetString("isLogin", "true");
            return Ok(new { message = "Login successful", redirectUrl = "/Home/Index" });
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear session data
            return RedirectToAction("Login", "Auth");
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
                return Unauthorized(new { message = "Google login failed." });

            // Get user info from Google claims
            var claims = result.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Google login failed: No email received." });

            // Store user info in session
            HttpContext.Session.SetString("Username", email);
            HttpContext.Session.SetString("isLogin", "true");

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
                return Unauthorized(new { message = "Microsoft login failed." });
            var claims = result.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Microsoft login failed: No email received." });
            HttpContext.Session.SetString("Username", email);
            HttpContext.Session.SetString("isLogin", "true");
            return Redirect("/Home/Index");
        }
    }
    
    public class LoginRequest // Request model for login
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest //Request model for registration
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}