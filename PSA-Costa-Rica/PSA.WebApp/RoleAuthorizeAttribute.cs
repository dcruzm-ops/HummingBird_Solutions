using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PSA.WebApp.Filters
{
    public class RoleAuthorizeAttribute : Attribute, IAsyncPageFilter
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var role = context.HttpContext.Session.GetString("rol");

            if (string.IsNullOrWhiteSpace(role))
            {
                context.Result = new RedirectToPageResult("/Auth/Login");
                return Task.CompletedTask;
            }

            if (!_allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToPageResult("/AccessDenied");
                return Task.CompletedTask;
            }

            return next();
        }
    }
}