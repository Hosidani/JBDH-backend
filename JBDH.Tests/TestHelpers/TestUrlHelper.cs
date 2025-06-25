using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace JBDH.Tests.TestHelpers
{
    public class TestUrlHelper : IUrlHelper
    {
        private readonly string _returnUrl;

        public TestUrlHelper(string returnUrl = "http://test.com/verify")
        {
            _returnUrl = returnUrl;
        }

        public string? Action(string? action = null, string? controller = null, object? values = null, string? protocol = null, string? host = null, string? fragment = null)
        {
            return _returnUrl;
        }

        public string? Action(UrlActionContext actionContext)
        {
            return _returnUrl;
        }

        public string? ActionLink(string? action = null, string? controller = null, object? values = null, string? protocol = null, string? host = null, string? fragment = null)
        {
            return _returnUrl;
        }

        public string? Content(string? contentPath)
        {
            return contentPath;
        }

        public bool IsLocalUrl(string? url)
        {
            return true;
        }

        public string? Link(string? routeName, object? values)
        {
            return _returnUrl;
        }

        public string? RouteUrl(object? values)
        {
            return _returnUrl;
        }

        public string? RouteUrl(string? routeName, object? values)
        {
            return _returnUrl;
        }

        public string? RouteUrl(string? routeName, object? values, string? protocol)
        {
            return _returnUrl;
        }

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            return _returnUrl;
        }

        public ActionContext ActionContext { get; } = new ActionContext();
    }
} 