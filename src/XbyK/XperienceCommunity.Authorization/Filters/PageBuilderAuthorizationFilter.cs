using CMS.Core;
using CMS.Helpers;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XperienceCommunity.Authorization
{
    internal class PageBuilderAuthorizationFilter : IAsyncResourceFilter
    {
        private readonly IProgressiveCache _progressiveCache;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly IAuthorization _authorization;

        public PageBuilderAuthorizationFilter(IProgressiveCache progressiveCache,
            IAuthorizationContext authorizationContext,
            IAuthorization authorization)
        {
            _progressiveCache = progressiveCache;
            _authorizationContext = authorizationContext;
            _authorization = authorization;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            // Do not do processes edit mode requests.
            if (context.HttpContext.Kentico().PageBuilder().EditMode)
            {
                await next();
                return;
            }

            // get context
            var page = await _authorizationContext.GetCurrentPageAsync();
            var templateIdentifier = await _authorizationContext.GetCurrentPageTemplateIdentifierAsync(page);
            var user = await _authorizationContext.GetCurrentUserAsync();

            bool authorized;

            // if page template found, look for PageTemplateAuthorizations
            if (page != null && !string.IsNullOrWhiteSpace(templateIdentifier))
            {
                var className = page.ClassName;

                var attributes = GetRegisteredTemplateAuthorizationAttributes();
                foreach (var attribute in attributes)
                {
                    if (attribute.PageBuilderConfiguration.Applies(templateIdentifier, className))
                    {
                        var config = attribute.AuthorizationConfiguration;
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
            await next();
        }

        private IEnumerable<RegisterPageBuilderAuthorizationAttribute> GetRegisteredTemplateAuthorizationAttributes()
        {
            return _progressiveCache.Load(cs =>
            {
                List<RegisterPageBuilderAuthorizationAttribute> attributes = new List<RegisterPageBuilderAuthorizationAttribute>();
                // Find filters that apply
                var registerPageTemplateAuthorizationAttributeType = typeof(RegisterPageBuilderAuthorizationAttribute);
                foreach (var assembly in AssemblyDiscoveryHelper.GetAssemblies(true))
                {
                    attributes.AddRange(assembly.GetCustomAttributes(registerPageTemplateAuthorizationAttributeType, true).Select(x => (RegisterPageBuilderAuthorizationAttribute)x));
                }
                return attributes;
            }, new CacheSettings(60, "GetRegisteredTemplateAuthorizationAttributesAsync"));
        }
    }
}
