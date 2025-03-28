using LoginWeb.Data;
using Microsoft.AspNetCore.Mvc;

namespace LoginWeb.Controllers
{
        public class AccountController : Controller
        {
            private readonly AppDbContext _context;

            public AccountController(AppDbContext context)
            {
                _context = context;
            }

        public IActionResult Login()
        {
            // Generate a random nonce
            string nonce = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("LoginNonce", nonce);
            ViewData["Nonce"] = nonce; // Pass to client
            return View();
        }
        public IActionResult Register()
            {
                return View();
            }
            public IActionResult Logout()
            {
            HttpContext.Session.Remove("isLogin"); // ✅ Clear session
            return RedirectToAction("Login"); // ✅ Redirect to login page
            }

    }
}
