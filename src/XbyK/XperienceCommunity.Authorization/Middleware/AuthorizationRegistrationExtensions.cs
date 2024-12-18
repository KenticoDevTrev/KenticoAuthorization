using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.Authorization;
using XperienceCommunity.Authorization.Implementations;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthorizationRegistrationExtensions
    {
        /// <summary>
        /// Registers the default Authorization interfaces (IAuthorizationAttributeRetriever, IAuthorization, IAuthorizationContext, and IAuthorizationContextCustomizer)
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKenticoAuthorization(this IServiceCollection services)
        {
            return services
                    .AddSingleton<IAuthorizationAttributeRetriever, AuthorizationAttributeRetriever>()
                    .AddScoped<IAuthorization, Authorization>()
                    .AddScoped<IAuthorizationContext, AuthorizationContext>()
                    .AddScoped<IAuthorizationContextCustomizer, AuthorizationContextCustomizer>();
        }

        /// <summary>
        /// Adds the Authorization filters to the Filter Collection
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public static FilterCollection AddKenticoAuthorizationFilters(this FilterCollection filters)
        {
            filters.Add<PageBuilderAuthorizationFilter>();
            filters.Add<ControllerActionAuthorizationFilter>();
            return filters;
        }
    }
}
