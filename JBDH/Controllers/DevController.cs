using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Linq;
using System.Reflection;

namespace JBDH.Controllers
{
    // Development-only controller to dump ApiDescription info for debugging Swagger/document generation.
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly IApiDescriptionGroupCollectionProvider _provider;

        public DevController(IApiDescriptionGroupCollectionProvider provider)
        {
            _provider = provider;
        }

        [HttpGet("apidescriptions")]
        public IActionResult GetApiDescriptions()
        {
            var groups = _provider.ApiDescriptionGroups.Items;
            var list = groups.SelectMany(g => g.Items).Select(d => new
            {
                RelativePath = d.RelativePath,
                HttpMethod = d.HttpMethod,
                ActionDisplayName = d.ActionDescriptor?.DisplayName,
                EndpointMetadata = d.ActionDescriptor?.EndpointMetadata?.Select(m => m.GetType().FullName).ToList(),
                SupportedResponseMediaTypes = d.SupportedResponseTypes.SelectMany(rt => rt.ApiResponseFormats.Select(f => f.MediaType)).ToList(),
                HasIncludeInApiAttribute = HasIncludeInApi(d)
            }).ToList();

            return Ok(list);
        }

        private bool HasIncludeInApi(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription d)
        {
            // Check endpoint metadata
            if (d.ActionDescriptor?.EndpointMetadata?.OfType<JBDH.Swagger.IncludeInApiAttribute>().Any() == true)
                return true;

            // Try ControllerActionDescriptor reflection
            if (d.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
            {
                if (cad.MethodInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any())
                    return true;
                if (cad.ControllerTypeInfo.GetCustomAttributes(typeof(JBDH.Swagger.IncludeInApiAttribute), true).Any())
                    return true;
            }

            return false;
        }
    }
}
