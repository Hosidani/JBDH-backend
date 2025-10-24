using System;

namespace JBDH.Swagger
{
    /// <summary>
    /// Marks an MVC action or controller to be included in the generated Swagger/OpenAPI docs.
    /// Use this to opt-in endpoints that would otherwise be filtered out as HTML-only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IncludeInApiAttribute : Attribute
    {
    }
}
