using JBDH.Controllers;
using JBDH.Models;
using JBDH.Services.Configuration;
using JBDH.Services.Core;
using JBDH.Services.JobManagement;
using JBDH.Services.UserManagement;
using JBDH.Services.Friendship;
using JBDH.Services.AICommunications;
using JBDH.Services.Helpers;
using JBDH.Services.Notifications;
using JBDH.Services.Organisation;
using JBDH.Services.Authentication;
using JBDH.Services.Reporting;
using JBDH.Services.DataFlow;
using JBDH.Services.Workflows;
using Microsoft.AspNetCore.Authentication.Cookies; // For cookie authentication
using Microsoft.AspNetCore.Builder; // For WebApplication
using Microsoft.Extensions.DependencyInjection; // For DI container setup
using Microsoft.Extensions.Hosting; // For environment setup
using Microsoft.AspNetCore.Http; // For IHttpContextAccessor
using Microsoft.EntityFrameworkCore; // For DbContext and DbContextOptionsBuilder
using MySql.Data.MySqlClient; // For MySQL connection
using MySql.EntityFrameworkCore.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Get connection string ONCE at the top
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new Exception("Database connection string 'DefaultConnection' is missing or empty.");

// Create the database if it doesn't exist (development only)
if (builder.Environment.IsDevelopment())
{
    var builderWithoutDb = new MySqlConnectionStringBuilder(connectionString)
    {
        Database = "" // Remove the database part
    };
    using (var connection = new MySqlConnection(builderWithoutDb.ConnectionString))
    {
        connection.Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE DATABASE IF NOT EXISTS testing_purposes;";
            cmd.ExecuteNonQuery();
        }
    }
}

// Run PlatsbankenApi.Main asynchronously before starting the web application
await Task.Run(async () =>
{
    try
    {
        await JobStreamService.RunJobStreamAsync(args); // Asynchronous job stream service
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
builder.Services.AddHttpClient<IAIApiClient, AIApiClient>();
builder.Services.AddScoped<IManageIOService, ManageIOService>();
builder.Services.AddScoped<IAIRequestBuilderService, AIRequestBuilderService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<IUserAIResponseService, UserAIResponseService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IRegisterOrganisationService, RegisterOrganisationService>();
builder.Services.AddScoped<IUserVerificationService, UserVerificationService>();
builder.Services.AddScoped<ISearchJobsService, SearchJobsService>();
builder.Services.AddScoped<IJobAIResponseService, JobAIResponseService>();
builder.Services.AddScoped<IJobAIAnalysisService, JobAIAnalysisService>();
builder.Services.AddScoped<ISearchCandidateService, SearchCandidateService>();
builder.Services.AddScoped<IRapportUserService, RapportUserService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>(); 
builder.Services.AddHttpContextAccessor(); 
builder.Services.AddScoped<DataFlowService>(); 
builder.Services.AddScoped<ICVAnalysisService, CVAnalysisService>();
builder.Services.AddScoped<ISearchWorkflowService, SearchWorkflowService>();


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
    options.UseMySQL(connectionString)
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));

// Register SMTP config
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Validate SMTP settings
var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
if (smtpSettings == null ||
    string.IsNullOrWhiteSpace(smtpSettings.Username) ||
    string.IsNullOrWhiteSpace(smtpSettings.Password))
{
    throw new Exception("SMTP configuration is invalid. Please check your appsettings.json.");
}

// Usual stuff
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<UserBanBackgroundService>(); // Register the background service

// Build the application
var app = builder.Build();

// Ensure database is created on startup (development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }
}

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
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Drop the database (development only)
            if (app.Environment.IsDevelopment())
            {
                dbContext.Database.EnsureDeleted();
            }

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
