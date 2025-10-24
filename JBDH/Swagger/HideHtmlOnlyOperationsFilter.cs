using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JBDH.Swagger
{
    /// <summary>
    /// Hides operations that only produce HTML (text/html) responses so they do not appear in Swagger UI.
    /// </summary>
    public class HideHtmlOnlyOperationsFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDesc = context.ApiDescription;

            var supported = apiDesc.SupportedResponseTypes;
            if (supported == null || supported.Count == 0)
                return;

            // If the action or controller was explicitly opted-in, do not hide it.
            // Check endpoint metadata first
            var optedIn = apiDesc.ActionDescriptor.EndpointMetadata
                .OfType<JBDH.Swagger.IncludeInApiAttribute>()
                .Any();

            // If not found there, try reflection on the action method and declaring type (controller)
            if (!optedIn && context.MethodInfo != null)
            {
                // Method-level attribute
                optedIn = context.MethodInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any();

                // Controller-level attribute
                if (!optedIn && context.MethodInfo.DeclaringType != null)
                {
                    optedIn = context.MethodInfo.DeclaringType.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any();
                }
            }
            if (optedIn)
                return;

            // If any supported response format is not HTML, keep the operation.
            var hasNonHtml = supported.Any(rt => rt.ApiResponseFormats.Any(f =>
                !(f.MediaType?.Contains("html", System.StringComparison.OrdinalIgnoreCase) ?? false)));

            if (!hasNonHtml)
            {
                // Clearing responses causes Swashbuckle to omit the operation from the UI.
                operation.Responses.Clear();
            }
        }
    }
}