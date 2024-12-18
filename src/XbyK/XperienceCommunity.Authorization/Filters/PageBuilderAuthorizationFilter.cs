using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Kentico.Web.Mvc.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace XperienceCommunity.Authorization
{
    internal class PageBuilderAuthorizationFilter(IProgressiveCache progressiveCache,
        IAuthorizationContext authorizationContext,
        IAuthorization authorization,
        IAdminPathRetriever adminPathRetriever) : IAsyncResourceFilter
    {
        private readonly IProgressiveCache _progressiveCache = progressiveCache;
        private readonly IAuthorizationContext _authorizationContext = authorizationContext;
        private readonly IAuthorization _authorization = authorization;
        private readonly IAdminPathRetriever _adminPathRetriever = adminPathRetriever;

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            // Ignore Admin requests
            var path = context.HttpContext.Request.Path.Value ?? "/admin";
            if (path.StartsWith(_adminPathRetriever.GetApiPrefix(), StringComparison.OrdinalIgnoreCase)
            ||
                path.StartsWith(_adminPathRetriever.GetAdminPrefix(), StringComparison.OrdinalIgnoreCase)
                ||
                path.StartsWith("/Kentico.", StringComparison.OrdinalIgnoreCase)
                ) {
                await next();
                return;
            }

            // Do not do processes if the IPageBuilderFeature is not present, calling the extension normally throws an exception so doing this way.
            var feature = context.HttpContext.Kentico().GetFeature<IPageBuilderFeature>();
            if (feature == null) {
                await next();
                return;
            }

            // get context, could try to use the PageBuilderDataContext, but that would not work with the customization logic.
            var page = await _authorizationContext.GetCurrentPageAsync();
            var templateIdentifier = page != null ? await _authorizationContext.GetCurrentPageTemplateIdentifierAsync(page) : null;
            var user = await _authorizationContext.GetCurrentUserAsync();

            var classIdToName = await _progressiveCache.LoadAsync(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{DataClassInfo.OBJECT_TYPE}|all");
                }
                return (await DataClassInfoProvider.GetClasses()
                    .Columns(nameof(DataClassInfo.ClassID), nameof(DataClassInfo.ClassName))
                    .GetEnumerableTypedResultAsync())
                    .ToDictionary(key => key.ClassID, value => value.ClassName);
            }, new CacheSettings(1440, "Authorization_ClassIdToClassName"));

            // if page template found, look for PageTemplateAuthorizations
            if (page != null && !string.IsNullOrWhiteSpace(templateIdentifier) && classIdToName.TryGetValue(page.SystemFields.ContentItemContentTypeID, out var className)) {
                var attributes = GetRegisteredTemplateAuthorizationAttributes();
                foreach (var attribute in attributes) {
                    if (attribute.PageBuilderConfiguration.Applies(templateIdentifier, className)) {
                        if (user != null) {
                            bool authorized;
                            var config = attribute.AuthorizationConfiguration;

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


                    }
                }
            }
            await next();
        }

        private List<RegisterPageBuilderAuthorizationAttribute> GetRegisteredTemplateAuthorizationAttributes() => _progressiveCache.Load(cs => {
            var attributes = new List<RegisterPageBuilderAuthorizationAttribute>();
            // Find filters that apply
            var registerPageTemplateAuthorizationAttributeType = typeof(RegisterPageBuilderAuthorizationAttribute);
            foreach (var assembly in AssemblyDiscoveryHelper.GetAssemblies(true)) {
                attributes.AddRange(assembly.GetCustomAttributes(registerPageTemplateAuthorizationAttributeType, true).Select(x => (RegisterPageBuilderAuthorizationAttribute)x));
            }
            return attributes;
        }, new CacheSettings(60, "Authorization_GetRegisteredTemplateAuthorizationAttributesAsync"));
    }
}
