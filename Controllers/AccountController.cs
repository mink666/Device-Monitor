using LoginWeb.Data;
using LoginWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LoginWeb.Controllers
{
        public class AccountController : Controller
        {
            private readonly AppDbContext _context;
            private readonly UserManager<IdentityUser> _userManager;

        public AccountController(AppDbContext context, UserManager<IdentityUser> userManager)
            {
                _context = context;
                _userManager = userManager;
        }

        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Register()
            {
                return View();
            }
            public IActionResult Logout()
            {
            HttpContext.Session.Remove("isLogin");
            return RedirectToAction("Login"); 
            }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var model = new ConfirmEmailViewModel();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                model.Message = "Invalid confirmation link.";
                return View("ConfirmEmail", model);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                model.Message = "User not found.";
                return View("ConfirmEmail", model);
            }
            string decodedToken = WebUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                model.Message = $"Email confirmation failed: {errors}";
            }
            else
            {
                model.Message = "Email confirmed successfully.";
            }
            return View("ConfirmEmail", model);
        }

    }
}
