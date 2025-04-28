using LoginWeb.Data;
using LoginWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LoginWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }
        private bool IsAdminUser()
        {
            bool isLoggedIn = HttpContext.Session.GetString("isLogin") == "true";
            bool isAdmin = HttpContext.Session.GetString("isAdmin") == "true";
            return isLoggedIn && isAdmin;
        }
        public IActionResult Index()
        {
            if (!IsAdminUser())
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have permission";
                return RedirectToAction("Index", "Home");
            }
            var users = _context.Users.OrderBy(u => u.Username).ToList();
            return View(users);
        }
        [HttpPost]
        [ValidateAntiForgeryToken] // Protect against CSRF attacks
        public async Task<IActionResult> ToggleAccountStatus(int id)
        {
            // 2. Find the user to toggle by their ID
            var userToToggle = await _context.Users.FindAsync(id);

            // 3. Handle case where user doesn't exist
            if (userToToggle == null)
            {
                TempData["AdminMessageError"] = "User not found.";
                return RedirectToAction("Index"); // Go back to the user list
            }

            // 4. Prevent admin from deactivating their own account
            var currentAdminUsername = HttpContext.Session.GetString("Username"); // Get current admin's identifier
            if (userToToggle.Username == currentAdminUsername) // Compare with the target user's identifier
            {
                TempData["AdminMessageError"] = "Error: Cannot change the status of your own account.";
                return RedirectToAction("Index"); // Go back to the user list
            }

            // 5. Toggle the AccountStatus property
            userToToggle.AccountStatus = !userToToggle.AccountStatus; // Flip the boolean value
            string newStatus = userToToggle.AccountStatus ? "Activated" : "Deactivated"; // For logging/message

            // 6. Save the change to the database
            try
            {
                await _context.SaveChangesAsync(); // Persist the change
                _logger.LogInformation($"AccountStatus toggled for user {userToToggle.Username} (ID: {id}) by admin {currentAdminUsername}. New status: {newStatus}");
                // Set success message for the view
                TempData["AdminMessageSuccess"] = $"User '{userToToggle.Username}' has been successfully {newStatus.ToLower()}.";
            }
            catch (DbUpdateException ex)
            {
                // Log error if database save fails
                _logger.LogError(ex, $"Database error toggling status for user ID: {id}");
                // Set error message for the view
                TempData["AdminMessageError"] = "A database error occurred while updating the user status.";
            }

            // 7. Redirect back to the Admin Index page to show the updated list
            return RedirectToAction("Index");
        }

    }
}
