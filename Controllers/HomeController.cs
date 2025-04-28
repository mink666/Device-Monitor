using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LoginWeb.Models;
using Microsoft.EntityFrameworkCore;
using LoginWeb.Data;
using Microsoft.AspNetCore.Authorization;

namespace LoginWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("isLogin") == null)
        {
            return RedirectToAction("Login", "Account");
        }
        var devices = _context.Devices.ToList();
        return View(devices);
    }   
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
