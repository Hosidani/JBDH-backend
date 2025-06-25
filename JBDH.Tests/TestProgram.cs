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
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;

namespace JBDH.Tests
{
    public static class TestProgram
    {
        public static WebApplication CreateTestApplication()
        {
            var builder = WebApplication.CreateBuilder();

            // Get connection string for tests
            var connectionString = "Server=localhost;Database=testing_purposes;User=root;Password=;";

            // Add services to the container for tests (skip startup operations)
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<ILoginService, LoginService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IUserPropertiesService, UserPropertiesService>();
            builder.Services.AddScoped<IFriendService, FriendService>();
            builder.Services.AddSingleton<AIConfig>();
            builder.Services.AddHttpClient<IAIApiClient, AIApiClient>();
            builder.Services.AddScoped<IManageInputService, ManageInputService>();
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
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
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

            // Register application services with in-memory database for tests
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));

            // Register SMTP config for tests
            builder.Services.Configure<SmtpSettings>(options =>
            {
                options.Host = "smtp.test.com";
                options.Port = 587;
                options.Username = "test@test.com";
                options.Password = "testpassword";
            });

            // Build the application
            var app = builder.Build();

            // Configure the HTTP request pipeline for tests
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseCors();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            return app;
        }
    }
} 