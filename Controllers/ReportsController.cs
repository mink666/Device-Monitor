using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using LoginWeb.Data;
using LoginWeb.Models;
using Microsoft.AspNetCore.Authorization;

public class ReportsController : Controller
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context) 
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Generate(string title)
    {
        // Check if the user is logged in using session
        if (HttpContext.Session.GetString("isLogin") == null)
        {
            return Unauthorized("You must be logged in to access reports.");
        }

        List<Device> devices = _context.Devices.ToList();

        if (devices == null || devices.Count == 0)
        {
            return BadRequest("No devices available.");
        }
        byte[] pdfBytes = PdfReportService.GenerateDeviceReport(title, devices, _context);
        return File(pdfBytes, "application/pdf", $"{title}.pdf");
    }
}
