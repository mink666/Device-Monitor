using LoginWeb.Data; 
using LoginWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LoginWeb.Controllers
{
    public class AccountController : Controller
    {

        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet] 
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Account/Register
        [HttpGet] 
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost] 
        [ValidateAntiForgeryToken] 
        public IActionResult Logout()
        {
            // Clear relevant session variables
            HttpContext.Session.Remove("isLogin");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("isAdmin"); 

            return RedirectToAction("Login", "Account"); 
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int userId) 
        {
            var model = new ConfirmEmailViewModel(); 

            if (userId <= 0)
            {
                model.Message = "Invalid user ID provided."; 
                return View("ConfirmEmail", model); 
            }

            // Find the user by primary key
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                model.Message = "User not found.";
                return View("ConfirmEmail", model); 
            }

            if (user.EmailConfirmed)
            {
                model.Message = "Your email address has already been confirmed."; 
                return View("ConfirmEmail", model);
            }

            user.EmailConfirmed = true;

            try
            {
                // Save the changes to the database
                await _context.SaveChangesAsync();
                model.Message = "Email confirmed successfully. You can now log in.";
            }
            catch (DbUpdateException ex)
            {
                model.Message = "An error occurred while confirming your email. Please try again later."; 
            }
            return View("ConfirmEmail", model); 
        }
    }
}