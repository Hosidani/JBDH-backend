using JBDH.Controllers;
using JBDH.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using DbHandler = JBDH.Controllers.DbHandler;
var builder = WebApplication.CreateBuilder(args);
await Task.Run(async () =>
{
    try
    {
        await PlatsbankenApi.Main(args);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in PlatsbankenApi.Main: {ex.Message}");
    }
});

// Run DbHandler.Main synchronously before starting the web application
await Task.Run(() =>
{
    try
    {
        DbHandler.Main(args);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in DbHandler.Main: {ex.Message}");
    }
});

// Add services to the container
builder.Services.AddControllersWithViews();
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
app.UseRouting();

app.UseAuthorization();
app.UseCors();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add a shutdown hook to flush the database
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application is shutting down. Flushing the database...");
    using var connection = new MySqlConnection(DbHandler.ConnectionString);
    connection.Open();
    DbHandler.FlushDatabase(connection);
    Console.WriteLine("Database flushed successfully.");
});

app.Run();
