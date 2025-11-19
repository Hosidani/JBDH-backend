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
using JBDH.Models.Core;
using JBDH.Models.AI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using JBDH.Services.BoardDataService;
using JBDH.Services.ListService;

// Ensure logging is configured for the entire application
JBDH.Services.Configuration.LoggingConfigService.ConfigLogging();

var builder = WebApplication.CreateBuilder(args);

// Log the connection string for debugging
var debugConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[DEBUG] Using connection string: {debugConnectionString}");

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

// Run PlatsbankenApi.Main asynchronously in background (non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        LoggingConfigService.ConfigLogging(); // Configure logging first
        LoggingConfigService.ReturnInfo("Starting background job data fetch...");
        var jobData = await JobStreamService.RunJobStreamAsync(args);
        DatabaseHandlerService.databaseHandler(jobData);
        LoggingConfigService.ReturnInfo("Background job data fetch completed.");
    }
    catch (Exception ex)
    {
        LoggingConfigService.ReturnError($"Background startup error: {ex.Message}");
        // Don't rethrow - let the app continue even if job fetching fails
    }
});

// Add services to the container **before** calling builder.Build
builder.Services.AddControllersWithViews();
// Swagger / OpenAPI (only register in Development)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        // Include ApiDescriptions only if they have at least one non-HTML response format,
        // or if the action has a custom [IncludeInApi] attribute (optional).
        c.DocInclusionPredicate((docName, apiDesc) =>
        {
            // Skip endpoints explicitly ignored
            var ignore = apiDesc.ActionDescriptor.EndpointMetadata
                        .OfType<Microsoft.AspNetCore.Mvc.ApiExplorerSettingsAttribute>()
                        .Any(a => a.IgnoreApi);
            if (ignore) return false;

            // Helper: check endpoint metadata, action attributes, and controller type for IncludeInApi
            bool HasIncludeInApi()
            {
                // Endpoint metadata (works for many cases)
                if (apiDesc.ActionDescriptor.EndpointMetadata
                    .OfType<JBDH.Swagger.IncludeInApiAttribute>().Any())
                    return true;

                // Try to inspect the action/controller via ControllerActionDescriptor
                if (apiDesc.ActionDescriptor is ControllerActionDescriptor cad)
                {
                    // Action-level attribute
                    if (cad.MethodInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any())
                        return true;

                    // Controller-level attribute
                    if (cad.ControllerTypeInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any())
                        return true;
                }

                return false;
            }

            if (HasIncludeInApi()) return true;

            // Otherwise include only if any supported response format is NOT text/html
            var produces = apiDesc.SupportedResponseTypes;
            if (produces == null || produces.Count == 0) return false;
            return produces.Any(rt => rt.ApiResponseFormats.Any(f => !(f.MediaType?.Contains("html", System.StringComparison.OrdinalIgnoreCase) ?? false)));
        });

        // Optional: Add an operation filter to remove any remaining operations that only produce HTML
        c.OperationFilter<JBDH.Swagger.HideHtmlOnlyOperationsFilter>();
        // Document filter: add paths for controllers/actions marked with [IncludeInApi] when ApiExplorer didn't produce them
        c.DocumentFilter<JBDH.Swagger.IncludeInApiDocumentFilter>();
    });
}
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
builder.Services.AddScoped<IBoardDataService, BoardDataService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IListService, ListService>();
builder.Services.AddScoped<JBDH.Services.Messaging.IMessageService, JBDH.Services.Messaging.MessageService>();
builder.Services.AddScoped<IMatchingAlgorithmCVJobService, MatchingAlgorithmCVJobService>();
builder.Services.AddScoped<IAIUserCourseProposalService, AIUserCourseProposalService>();
builder.Services.AddScoped<IJobService, JobService>();

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
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout/Logout";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Add authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://victorious-bay-00a810703.3.azurestaticapps.net"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
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

// Build the application
var app = builder.Build();

// Add request logging middleware for production debugging
app.Use(async (context, next) =>
{
    try
    {
        LoggingConfigService.ReturnInfo($"Request: {context.Request.Method} {context.Request.Path}");
        await next();
        LoggingConfigService.ReturnInfo($"Response: {context.Response.StatusCode}");
    }
    catch (Exception ex)
    {
        LoggingConfigService.ReturnError($"Request failed: {ex.Message}");
        LoggingConfigService.ReturnError($"Stack trace: {ex.StackTrace}");
        throw;
    }
});

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
app.UseCors("AllowLocalFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseStaticFiles();
app.UseHttpsRedirection();

// Enable Swagger middleware only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JBDH API V1");
        options.RoutePrefix = "swagger"; // Serve at /swagger
    });
}

// Add health check endpoint (before MapControllerRoute)
app.MapGet("/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName 
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Default route pattern

// Start the application
app.Run();