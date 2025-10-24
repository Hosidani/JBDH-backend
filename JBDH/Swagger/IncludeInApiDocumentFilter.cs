using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace JBDH.Swagger
{
    /// <summary>
    /// Adds OpenAPI paths for controllers or actions annotated with [IncludeInApi]
    /// when ApiExplorer did not produce entries (conventional MVC actions).
    /// This is a conservative fallback that generates paths using the default
    /// MVC route pattern: /{controller}/{action}/{id?}.
    /// </summary>
    public class IncludeInApiDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var controllerTypes = asm.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(Microsoft.AspNetCore.Mvc.Controller).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttributes(typeof(IncludeInApiAttribute), true).Any())
                .ToList();

            foreach (var ctrl in controllerTypes)
            {
                var controllerName = ctrl.Name;
                if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                    controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);

                var methods = ctrl.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName && !m.GetCustomAttributes(typeof(NonActionAttribute), true).Any())
                    .ToList();

                foreach (var m in methods)
                {
                    // Skip if already provided by ApiExplorer (avoid duplicates)
                    var existing = swaggerDoc.Paths.Keys.Any(p => p.IndexOf($"/{controllerName}/", StringComparison.OrdinalIgnoreCase) >= 0 && p.IndexOf($"/{m.Name}", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (existing) continue;

                    // Skip actions with explicit Route attributes (they are handled by ApiExplorer)
                    if (m.GetCustomAttributes().Any(a => a.GetType().Name.StartsWith("Route", StringComparison.OrdinalIgnoreCase) || a.GetType().Name.StartsWith("Http", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // Determine HTTP method
                    string httpMethod = "get";
                    if (m.GetCustomAttributes().Any(a => a.GetType().Name.StartsWith("HttpPost", StringComparison.OrdinalIgnoreCase)))
                        httpMethod = "post";
                    else if (m.GetCustomAttributes().Any(a => a.GetType().Name.StartsWith("HttpPut", StringComparison.OrdinalIgnoreCase)))
                        httpMethod = "put";
                    else if (m.GetCustomAttributes().Any(a => a.GetType().Name.StartsWith("HttpDelete", StringComparison.OrdinalIgnoreCase)))
                        httpMethod = "delete";

                    var path = $"/{controllerName}/{m.Name}";

                    // Add optional id parameter for common patterns
                    var parameters = new System.Collections.Generic.List<OpenApiParameter>();
                    // If method has a parameter named 'id' allow it as route parameter
                    if (m.GetParameters().Any(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase)))
                    {
                        parameters.Add(new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = false,
                            Schema = new OpenApiSchema { Type = "string" }
                        });
                        path += "/{id}";
                    }

                    // Create a minimal OpenApiOperation describing an HTML response
                    var operation = new OpenApiOperation
                    {
                        Summary = $"{m.Name} (MVC view)",
                        Responses = new OpenApiResponses()
                    };
                    operation.Responses.Add("200", new OpenApiResponse
                    {
                        Description = "OK (HTML)",
                        Content = new System.Collections.Generic.Dictionary<string, OpenApiMediaType>
                        {
                            ["text/html"] = new OpenApiMediaType()
                        }
                    });
                    if (parameters.Count > 0)
                        operation.Parameters = parameters;

                    // Add to swaggerDoc paths
                    if (!swaggerDoc.Paths.ContainsKey(path))
                        swaggerDoc.Paths.Add(path, new OpenApiPathItem());
                    swaggerDoc.Paths[path].AddOperation(ParseOperationType(httpMethod), operation);
                }
            }
        }

        private OperationType ParseOperationType(string method)
        {
            return method.ToLowerInvariant() switch
            {
                "get" => OperationType.Get,
                "post" => OperationType.Post,
                "put" => OperationType.Put,
                "delete" => OperationType.Delete,
                _ => OperationType.Get,
            };
        }
    }
}
