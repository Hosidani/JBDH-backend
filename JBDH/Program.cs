using JBDH.Controllers;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // For cookie authentication
using Microsoft.AspNetCore.Builder; // For WebApplication
using Microsoft.Extensions.DependencyInjection; // For DI container setup
using Microsoft.Extensions.Hosting; // For environment setup
using Microsoft.AspNetCore.Http; // For IHttpContextAccessor
using Microsoft.EntityFrameworkCore; // For DbContext and DbContextOptionsBuilder
using MySql.Data.MySqlClient; // For MySQL connection
using MySql.EntityFrameworkCore.Extensions; // For UseMySQL


var builder = WebApplication.CreateBuilder(args);

// Run PlatsbankenApi.Main asynchronously before starting the web application
await Task.Run(async () =>
{
    try
    {
        await JobStreamService.Main(args); // Async job service
        DatabaseHandlerService.databaseHandler(args); // Synchronous DB handler
    }
    catch (Exception ex)
    {
        LoggingConfigService.ReturnError($"Startup error: {ex.Message}");
        throw; // Optional: rethrow if you want the app to fail on critical startup errors
    }

// Run PlatsbankenApi.Main asynchronously before starting the web application

});

// Add services to the container **before** calling builder.Build
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IUserPropertiesService, UserPropertiesService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddSingleton<AIConfig>();
builder.Services.AddHttpClient<IAIService, AIService>();
builder.Services.AddScoped<IManageInputService, ManageInputService>();
builder.Services.AddScoped<IAIHelperSwitchService, AIHelperSwitchService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddHttpContextAccessor(); // Needed for IHttpContextAccessor
builder.Services.AddScoped<DataFlowService>(); // Register DataFlowService
builder.Services.AddScoped<IAIResponseService, AIResponseService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IJobAIAnalysisService, JobAIAnalysisService>();
builder.Services.AddScoped<ISearchCandidateService, SearchCandidateService>();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set the session timeout
    options.Cookie.HttpOnly = true;  // Ensures cookies are sent only through HTTP requests
    options.Cookie.IsEssential = true;  // Makes the session cookie essential for the app
});

// Configure services
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "MyCookieName";
        options.LoginPath = "/Login/Login";
        options.LogoutPath = "/Logout/Logout";
    });

// Add authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Add memory cache for session
builder.Services.AddDistributedMemoryCache();

// Register application services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register SMTP config
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Validate SMTP settings
var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
if (string.IsNullOrWhiteSpace(smtpSettings.Username) || string.IsNullOrWhiteSpace(smtpSettings.Password))
{
    throw new Exception("SMTP configuration is invalid. Please check your appsettings.json.");
}

// Usual stuff
builder.Services.AddControllersWithViews();

// Build the application
var app = builder.Build();

// Access IHostApplicationLifetime to hook into shutdown events
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
// Register a function to run when the application is stopping

// SHUTDOWN HOOK:
lifetime.ApplicationStopping.Register(() =>
{
    var filePath = Path.Combine(AppContext.BaseDirectory, "timestamp.txt");

    if (File.Exists(filePath))
    {
        try
        {
            File.Delete(filePath);
            Console.WriteLine("timestamp.txt deleted on shutdown.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete timestamp.txt: {ex.Message}");
        }
    }
    try
    {
        // Create a new scope to resolve scoped services
        using (var scope = app.Services.CreateScope())
        {
            // Resolve ApplicationDbContext from the scope
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Call your database flushing service
            FlushDatabaseService.FlushDatabase();
            DeleteJsonFileService.DeleteJsonFile(); // Delete the JSON file
            JobStreamService.DeleteTimestampFile(); // Delete the timestamp file
            
            LoggingConfigService.ReturnInfo("Database flushed, json file deleted and application stopped.");
        }
    }
    catch (Exception ex)
    {
        LoggingConfigService.ReturnError($"Error during application shutdown: {ex.Message}");
    }
});
// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors(); // This should come after UseRouting() and before endpoints

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Default route pattern

// Start the application
app.Run();
