using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using LoginWeb.Models;
using LoginWeb.Data;
using System.Linq;
public class PdfReportService
{
    private readonly AppDbContext _context;
    public PdfReportService(AppDbContext context)
    {
        _context = context;
    }
    public static byte[] GenerateDeviceReport(string title,List<Device> devices, AppDbContext context)
    {
        try
        {

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    // 🔹 Report Header
                    page.Header()
                        .AlignCenter()
                        .Text(title)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    // 🔹 Table of Device Data
                    page.Content()
                        .Table(table =>
                        {
                            // ✅ Define Columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);  // ID
                                columns.RelativeColumn(1);   // Name
                                columns.RelativeColumn(1);   // IP
                                columns.RelativeColumn(1);   // Status
                                columns.RelativeColumn(1);   // CPU Usage
                                columns.RelativeColumn(1);   // Memory Usage
                                columns.RelativeColumn(1);   // Last Updated
                            });

                            // ✅ Table Header Row
                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).Padding(5).Text("ID").Bold();
                                header.Cell().BorderBottom(1).Padding(5).Text("Name").Bold();
                                header.Cell().BorderBottom(1).Padding(5).Text("IP Address").Bold();
                                header.Cell().BorderBottom(1).Padding(5).Text("Status").Bold();
                                header.Cell().BorderBottom(1).Padding(5).Text("CPU Usage").Bold();
                                header.Cell().BorderBottom(1).Padding(5).Text("Memory Usage").Bold();
                                header.Cell().BorderBottom(1).Padding(5).Text("Last Updated").Bold();
                            });

                            // ✅ Add Device Data Rows
                            foreach (var device in devices)
                            {
                                DeviceHistory? latestHistory = null;
                                if (context != null && context.DeviceHistories != null)
                                {
                                    latestHistory = context.DeviceHistories
                                                        .Where(h => h.DeviceId == device.Id)
                                                        .OrderByDescending(h => h.Timestamp)
                                                        .FirstOrDefault();
                                }

                                table.Cell().Padding(5).Text(device.Id.ToString());
                                table.Cell().Padding(5).Text(device.Name);
                                table.Cell().Padding(5).Text(device.IPAddress);
                                table.Cell().Padding(5).Text(device.LastStatus ?? "Unknown");
                                table.Cell().Padding(5).Text(latestHistory?.CpuLoadPercentage.HasValue == true ? $"{latestHistory.CpuLoadPercentage}%" : "N/A");
                                table.Cell().Padding(5).Text(latestHistory?.MemoryUsagePercentage.HasValue == true ? $"{latestHistory.MemoryUsagePercentage}%" : "N/A");
                                string metricsTimestamp = latestHistory?.Timestamp.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
                                table.Cell().Padding(5).Text(metricsTimestamp);
                            }
                        });

                    // 🔹 Footer with Timestamp
                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
                });
            })
            .GeneratePdf();

            Console.WriteLine("✅ PDF Created Successfully.");
            return pdfBytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PDF Generation Failed: {ex.Message}");
            throw;
        }
    }
}
