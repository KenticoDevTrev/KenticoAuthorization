using Authorization.Kentico.Filters;
using Authorization.Kentico.Implementations;
using Authorization.Kentico.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Kentico
{
    public static class AuthorizationRegistrationExtensions
    {
        public static IServiceCollection AddKenticoAuthorization(this IServiceCollection services)
        {
            return services
                    .AddSingleton<IAuthorizationAttributeRetriever, AuthorizationAttributeRetriever>()
                    .AddScoped<IAuthorization, Implementations.Authorization>()
                    .AddScoped<IAuthorizationContext, AuthorizationContext>()
                    .AddScoped<IAuthorizationContextCustomizer, AuthorizationContextCustomizer>();
        }

        public static FilterCollection AddKenticoAuthorization(this FilterCollection filters)
        {
            filters.Add<PageBuilderAuthorizationFilter>();
            return filters;
        }
    }
}
