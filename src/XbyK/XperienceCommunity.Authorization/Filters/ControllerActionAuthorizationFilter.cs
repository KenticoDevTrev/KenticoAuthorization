using Kentico.Web.Mvc.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace XperienceCommunity.Authorization
{
    internal class ControllerActionAuthorizationFilter(IAuthorizationContext authorizationContext,
        IAuthorization authorization,
        IAdminPathRetriever adminPathRetriever) : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationContext _authorizationContext = authorizationContext;
        private readonly IAuthorization _authorization = authorization;
        private readonly IAdminPathRetriever _adminPathRetriever = adminPathRetriever;

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Ignore Admin requests
            var path = context.HttpContext.Request.Path.Value ?? "/admin";
            if (path.StartsWith(_adminPathRetriever.GetApiPrefix(), StringComparison.OrdinalIgnoreCase)
                ||
                path.StartsWith(_adminPathRetriever.GetAdminPrefix(), StringComparison.OrdinalIgnoreCase)
                || 
                path.StartsWith("/Kentico.", StringComparison.OrdinalIgnoreCase)
                ) {
                return;
            }

            try {
                var attribute = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo.GetCustomAttribute<ControllerActionAuthorizationAttribute>(true);

                if (attribute != null) {
                    // Restore config
                    var config = attribute.AuthorizationConfiguration;

                    // get context
                    var page = await _authorizationContext.GetCurrentPageAsync();
                    var user = await _authorizationContext.GetCurrentUserAsync();

                    bool authorized;
                    if (config.CustomAuthorization != null && config.CustomAuthorization.GetInterfaces().Contains(typeof(IAuthorization))) {
                        var service = context.HttpContext.RequestServices.GetService(config.CustomAuthorization) ?? throw new Exception($"There is no service of type {config.CustomAuthorization.FullName}, please register it on the service container before using this.");
                        authorized = await ((IAuthorization)service).IsAuthorizedAsync(user, config, page, null);
                    } else {
                        authorized = await _authorization.IsAuthorizedAsync(user, config, page, null);
                    }
                    
                    if (!authorized) {
                        // Custom provided redirect
                        if (!string.IsNullOrWhiteSpace(config.CustomUnauthorizedRedirect)) {
                            context.HttpContext.Response.StatusCode = (!user?.IsAuthenticated ?? false ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden);
                            context.Result = new RedirectResult(config.CustomUnauthorizedRedirect);
                            return;
                        } else if ((user?.UserName ?? "public").Equals("public", StringComparison.InvariantCultureIgnoreCase)) {
                            // Needs to log in, this uses ConfigureApplicationCookie's LoginPath
                            context.Result = new ChallengeResult();
                            return;
                        } else {
                            // Logged in, but forbidden, this uses ConfigureApplicationCookie's AccessDeniedPath
                            context.Result = new ForbidResult();
                            return;
                        }
                    }
                }
            } catch(InvalidOperationException) {
                // ignore
            }
        }
    }
}
