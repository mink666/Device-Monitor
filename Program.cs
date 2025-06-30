using LoginWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LoginWeb.Models;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LoginWeb.Services;
using System.Security.Cryptography; 
using System.Text; 
using System;
using System.Linq; 

var builder = WebApplication.CreateBuilder(args);
// Register the AppDbContext service using your connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<EmailService>();

// Add services to the container.
builder.Services.AddRazorPages();

// Register API controllers
builder.Services.AddControllers();

// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

// Enable Google Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.SaveTokens = true;
})
.AddMicrosoftAccount(microsoftOptions =>
{
    microsoftOptions.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
    microsoftOptions.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    microsoftOptions.SaveTokens = true;
});


// Add logging services
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddHostedService<SnmpPollingService>();
//builder.Services.AddHostedService<DeviceHealthAnalyzerService>();
builder.Services.AddSignalR();
// Add services to the container.
var app = builder.Build();

SeedAdminUser(app); 


// Seed default admin account
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseHsts();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapHub<NotificationHub>("/notificationHub");

QuestPDF.Settings.License = LicenseType.Community;
app.Run();

static void SeedAdminUser(WebApplication app) // Made static
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();

        // --- Admin User Definition ---
        string adminUsername = "admin"; 
        string adminEmail = "admin@gmail.com"; 
        string adminPassword = "123456789";

        // --- Check if admin exists (using synchronous Any()) ---
        if (!context.Users.Any(u => u.Username == adminUsername || u.Email == adminEmail))
        {
            string adminPasswordHash = ComputeMd5Hash(adminPassword);
            if (adminPasswordHash == null)
            {
                Console.WriteLine("ERROR: Failed to hash admin password during seeding.");
                return; 
            }

            // --- Create the admin user ---
            var adminUser = new User 
            {
                Username = adminUsername,
                Email = adminEmail,
                PasswordHash = adminPasswordHash,
                IsAdmin = true,         
                EmailConfirmed = true,
                AccountStatus = true
            };

            // --- Add and Save (using synchronous SaveChanges) ---
            context.Users.Add(adminUser);
            int changes = context.SaveChanges();

            if (changes > 0)
            {
                Console.WriteLine($"Admin user '{adminUsername}' created successfully.");
            }
            else
            {
                Console.WriteLine($"ERROR: Failed to save admin user '{adminUsername}' during seeding.");
            }
        }
        else
        {
            Console.WriteLine($"Admin user '{adminUsername}' or email '{adminEmail}' already exists. Seeding skipped.");
        }
    }
}
static string ComputeMd5Hash(string input)
{
    if (input == null) return null;
    using (MD5 md5 = MD5.Create())
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }
        return sb.ToString();
    }
}