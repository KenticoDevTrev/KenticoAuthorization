using Authorization.Kentico.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using System.Reflection;

namespace Authorization.Kentico.Filters
{
    public class ControllerActionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private IAuthorizationContext _authorizationContext;
        private IAuthorization _authorization;

        public ControllerActionAuthorizationFilter(IAuthorizationContext authorizationContext,
            IAuthorization authorization)
        {
            _authorizationContext = authorizationContext;
            _authorization = authorization;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var attribute = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo.GetCustomAttribute<ControllerActionAuthorizationAttribute>(true);

            if(attribute != null)
            {
                // Restore config
                var config = attribute.AuthorizationConfiguration;

                // get context
                var page = await _authorizationContext.GetCurrentPageAsync();
                var user = await _authorizationContext.GetCurrentUserAsync();

                bool authorized;
                if (config.CustomAuthorization != null && config.CustomAuthorization.IsAssignableFrom(typeof(IAuthorization)))
                {
                    if (!(context.HttpContext.RequestServices.GetService(config.CustomAuthorization) is IAuthorization authService))
                    {
                        throw new Exception($"There is no service of type {config.CustomAuthorization.FullName}, please register it on the service container before using this.");
                    }
                    authorized = await authService.IsAuthorizedAsync(user, config, page, null);
                }
                else
                {
                    authorized = await _authorization.IsAuthorizedAsync(user, config, page, null);
                }

                if (!authorized)
                {
                    // Custom provided redirect
                    if (!string.IsNullOrWhiteSpace(config.CustomUnauthorizedRedirect))
                    {
                        context.HttpContext.Response.StatusCode = (!user?.IsAuthenticated ?? false ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden);
                        context.Result = new RedirectResult(config.CustomUnauthorizedRedirect);
                    }
                    else if ((user?.UserName ?? "public").Equals("public", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Needs to log in, this uses ConfigureApplicationCookie's LoginPath
                        await context.HttpContext.ChallengeAsync();
                    }
                    else
                    {
                        // Logged in, but forbidden, this uses ConfigureApplicationCookie's AccessDeniedPath
                        await context.HttpContext.ForbidAsync();
                    }
                }
            }
        }
    }
}
