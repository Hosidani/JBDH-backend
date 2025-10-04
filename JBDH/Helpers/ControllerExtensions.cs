using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using JBDH.Services.Configuration;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.Core;
using JBDH.Models.Common;
using JBDH.Models.ViewModels;

namespace JBDH.Helpers
{
    public static class ControllerAdvancedExtensions
    {
        public static async Task<IActionResult> ValidateModelOrReturnViewAsync(this Controller controller, object model, string errorMessage, Func<Task<IActionResult>> successFunc)
        {
            if (!controller.ModelState.IsValid)
            {
                controller.SetModelError(string.Empty, errorMessage);
                return controller.View(model);
            }
            return await successFunc();
        }

        public static async Task<TUser?> GetAuthenticatedUserAsync<TUser>(this Controller controller, DbContext context) where TUser : class
        {
            var authResult = controller.EnsureAuthenticatedUserOrRedirect();
            if (authResult.Value.HasValue)
            {
                return await context.Set<TUser>().FindAsync(authResult.Value.Value);
            }
            return null;
        }
    }
    public static class EntityExtensions
    {
        public static void SetTimestamps(this object entity)
        {
            var type = entity.GetType();
            var createdAtProp = type.GetProperty("CreatedAt");
            var updatedAtProp = type.GetProperty("UpdatedAt");
            var now = DateTime.UtcNow;
            if (createdAtProp != null && createdAtProp.CanWrite)
                createdAtProp.SetValue(entity, now);
            if (updatedAtProp != null && updatedAtProp.CanWrite)
                updatedAtProp.SetValue(entity, now);
        }
    }

    public static class ControllerEntityExtensions
    {
        public static async Task<IActionResult> GetOrError<T>(this Controller controller, Func<Task<T>> getter, string errorMessage, Func<T, IActionResult> onSuccess)
        {
            var entity = await getter();
            if (entity == null)
                return controller.ErrorPage(errorMessage);
            return onSuccess(entity);
        }
    }

    public static class ControllerExtensions
    {
        public static void SetTempDataMessage(this Controller controller, string key, string message)
        {
            controller.TempData[key] = message;
            controller.LogAction($"{key} set: {message}");
        }

        public static void SetSuccessMessage(this Controller controller, string message)
            => controller.SetTempDataMessage("SuccessMessage", message);

        public static void SetErrorMessage(this Controller controller, string message)
            => controller.SetTempDataMessage("ErrorMessage", message);

        public static void SetModelError(this Controller controller, string key, string message)
        {
            controller.SetErrorMessage(message);
            controller.ModelState.AddModelError(key, message);
            controller.LogAction($"Model error on '{key}': {message}");
        }

        public static void SetModelInvalidError(this Controller controller, string message)
            => controller.SetModelError(string.Empty, message);

        public static void SetEmptyFieldError(this Controller controller, string fieldName)
            => controller.SetModelError(fieldName, $"{fieldName} cannot be empty.");

        public static int? GetAuthenticatedUserId(this Controller controller)
        {
            var userIdStr = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
                return userId;
            return null;
        }

        public static bool IsAuthenticated(this Controller controller)
            => controller.User?.Identity != null && controller.User.Identity.IsAuthenticated;

        public static void LogAction(this Controller controller, string message)
            => LoggingConfigService.ReturnInfo($"[Controller:{controller.GetType().Name}] {message}");

        public static IActionResult RedirectTo(this Controller controller, string action, string? controllerName = null, object? routeValues = null)
        {
            controller.LogAction($"Redirecting to {action} in {controllerName ?? controller.GetType().Name}");
            if (controllerName == null)
                return controller.RedirectToAction(action, routeValues);
            else
                return controller.RedirectToAction(action, controllerName, routeValues);
        }

        public static IActionResult RedirectToLogin(this Controller controller)
            => controller.RedirectTo("Login", "Account");

        public static IActionResult RedirectToHome(this Controller controller)
            => controller.RedirectTo("Index");

        public static IActionResult RedirectToMyProfile(this Controller controller)
            => controller.RedirectTo("MyProfile");

        public static async Task<IActionResult> SignOutAndRedirectToHomeAsync(this Controller controller, string scheme = "CookieAuth")
        {
            controller.LogAction("Signing out and redirecting to Home.");
            await controller.HttpContext.SignOutAsync(scheme);
            return controller.RedirectToHome();
        }

        public static async Task<bool> UserExistsAsync(this Controller controller, ApplicationDbContext context, int userId)
        {
            controller.LogAction($"Checking if user exists: {userId}");
            return await context.Users.AnyAsync(u => u.Id == userId);
        }

        public static IActionResult SetErrorAndRedirectToLogin(this Controller controller, string message)
        {
            controller.SetErrorMessage(message);
            return controller.RedirectToLogin();
        }

        public static IActionResult HandleServiceResult(this Controller controller, IServiceResult result, string redirectAction = "Index")
        {
            var msg = result.Success ? result.SuccessMessage : result.ErrorMessage;
            controller.LogAction($"Handling service result for redirect to {redirectAction}: Success={result.Success}, Message={msg}");
            if (!result.Success)
                controller.SetErrorMessage(result.ErrorMessage ?? "An error occurred.");
            else
                controller.SetSuccessMessage(result.SuccessMessage ?? "Operation completed successfully.");
            return controller.RedirectTo(redirectAction);
        }

        public static int EnsureAuthenticatedUser(this Controller controller)
        {
            var userId = controller.GetAuthenticatedUserId();
            if (!userId.HasValue)
            {
                controller.LogAction("Unauthorized access attempt.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userId.Value;
        }

        public class ActionResultWithValue<T>
        {
            public T? Value { get; set; }
            public IActionResult? ErrorResult { get; set; }
        }

        public static ActionResultWithValue<int?> EnsureAuthenticatedUserOrRedirect(this Controller controller, string errorMessage = "Invalid user context.")
        {
            var userId = controller.GetAuthenticatedUserId();
            if (!userId.HasValue)
            {
                controller.SetErrorMessage(errorMessage);
                return new ActionResultWithValue<int?> { Value = null, ErrorResult = controller.RedirectToLogin() };
            }
            return new ActionResultWithValue<int?> { Value = userId, ErrorResult = null };
        }

        public static IActionResult ErrorPage(this Controller controller, string message)
        {
            controller.SetErrorMessage(message);
            controller.LogAction($"Error page: {message}");
            return controller.View("ErrorPage", new ErrorViewModel { Message = message });
        }

        public static ActionResultWithValue<int?> ValidateAndHandleParseError(this Controller controller, string input, string errorMessage = "Invalid input.")
        {
            if (!int.TryParse(input, out int value))
            {
                controller.SetErrorMessage(errorMessage);
                controller.LogAction($"Parse error: {errorMessage}");
                return new ActionResultWithValue<int?> { Value = null, ErrorResult = controller.ErrorPage(errorMessage) };
            }
            return new ActionResultWithValue<int?> { Value = value, ErrorResult = null };
        }

        public static IActionResult BadRequestMessage(this Controller controller, string message)
        {
            controller.SetErrorMessage(message);
            controller.LogAction($"BadRequest: {message}");
            return controller.BadRequest(message);
        }

        public static IActionResult ForbidWithMessage(this Controller controller, string message)
        {
            controller.SetErrorMessage(message);
            controller.LogAction($"Forbid: {message}");
            return controller.Forbid();
        }

        public static IActionResult SetSuccessAndRedirect(this Controller controller, string message, string redirectAction = "Index", object routeValues = null)
        {
            controller.SetSuccessMessage(message);
            controller.LogAction($"Success: {message}, redirecting to {redirectAction}");
            return controller.RedirectTo(redirectAction, null, routeValues);
        }

        public static IActionResult SetErrorAndRedirect(this Controller controller, string message, string redirectAction = "Index", object routeValues = null)
        {
            controller.SetErrorMessage(message);
            controller.LogAction($"Error: {message}, redirecting to {redirectAction}");
            return controller.RedirectTo(redirectAction, null, routeValues);
        }

        public static async Task<string> RenderViewAsync<TModel>(
            this Controller controller,
            string viewName,
            TModel model,
            bool partial = false)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                if (viewEngine == null)
                    throw new InvalidOperationException("View engine not found.");
                var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !partial);

                if (!viewResult.Success || viewResult.View == null)
                    throw new FileNotFoundException($"View {viewName} not found.");

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}