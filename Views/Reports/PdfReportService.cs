using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using LoginWeb.Models; 
public class PdfReportService
{
    public static byte[] GenerateDeviceReport(string title,List<Device> devices)
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
                                table.Cell().Padding(5).Text(device.Id.ToString());
                                table.Cell().Padding(5).Text(device.Name);
                                table.Cell().Padding(5).Text(device.IPAddress);
                                table.Cell().Padding(5).Text(device.Status);
                                table.Cell().Padding(5).Text(device.CPUUsage.HasValue ? $"{device.CPUUsage}%" : "N/A");
                                table.Cell().Padding(5).Text(device.MemoryUsage.HasValue ? $"{device.MemoryUsage} MB" : "N/A");
                                table.Cell().Padding(5).Text(device.LastUpdated?.ToString("yyyy-MM-dd HH:mm") ?? "N/A");
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
