using JBDH.Controllers;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Hosting;
using System.IO;
using DbHandler = JBDH.Controllers.DbHandler;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using JBDH.Services;


var builder = WebApplication.CreateBuilder(args);

// Run PlatsbankenApi.Main asynchronously before starting the web application
await Task.Run(async () =>
{
    try
    {
        await JobStreamService.Main(args); // Fetch or process job data
    }
    catch (Exception ex)
    {
        LoggingConfigService.ReturnError($"Error in PlatsbankenApi.Main: {ex.Message}");
    }
});

// Run DbHandler.Main synchronously before starting the web application
await Task.Run(() =>
{
    try
    {
        DatabaseHandlerService.Main(args); // Initialize database or populate tables
    }
    catch (Exception ex)
    {
        LoggingConfigService.ReturnError($"Error in DbHandler.Main: {ex.Message}");
    }
});

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/User/Login"; // Redirect to login page if not authenticated
    });

builder.Services.AddAuthorization();
builder.Services.AddSession(); // Add session services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));



var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession(); // Enable session middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapGet("/Settings/Test", () => "Settings route is working!");
// Add a shutdown hook to flush the database
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    LoggingConfigService.ReturnInfo("Application is shutting down. Flushing the database...");
    using var connection = new MySqlConnection(DbHandler.ConnectionString);
    connection.Open();
    DbHandler.FlushDatabase(connection);
    LoggingConfigService.ReturnInfo("Database flush completed.");

    // Delete all files in the uploads folder
    LoggingConfigService.ReturnInfo("Deleting files in the uploads folder...");
    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
    if (Directory.Exists(uploadsPath))
    {
        var files = Directory.GetFiles(uploadsPath);
        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
                LoggingConfigService.ReturnInfo($"Deleted file: {file}");
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error deleting file {file}: {ex.Message}");
            }
        }
    }
    else
    {
        LoggingConfigService.ReturnInfo("Uploads folder does not exist. No files to delete.");
    }

    // Delete the timestamp file on application shutdown
    var timestampFilePath = "timestamp.txt";
    if (File.Exists(timestampFilePath))
    {
        File.Delete(timestampFilePath);
        LoggingConfigService.ReturnInfo("Deleted timestamp.txt on application shutdown.");
    }
});
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    if (endpoint != null)
    {
        Console.WriteLine($"Endpoint: {endpoint.DisplayName}");
    }
    await next();
});
app.Run();
