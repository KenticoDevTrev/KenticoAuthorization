using CMS.Membership;
using CMS.Websites;
using XperienceCommunity.Authorization.Events;

namespace XperienceCommunity.Authorization.Implementations
{
    public class AuthorizationContextCustomizer : IAuthorizationContextCustomizer
    {
        public Task<string?> GetCustomCultureAsync(GetCultureEventArgs cultureArgs, AuthorizationEventType eventType) => Task.FromResult<string?>(null);

        public Task<IWebPageFieldsSource?> GetCustomPageAsync(GetPageEventArgs pageArgs, AuthorizationEventType eventType) => Task.FromResult<IWebPageFieldsSource?>(null);

        public Task<MemberInfo?> GetCustomUserAsync(GetUserEventArgs userArgs, AuthorizationEventType eventType) => Task.FromResult<MemberInfo?>(null);

        public Task<UserContext?> GetCustomUserContextAsync(GetUserEventArgs userArgs, AuthorizationEventType eventType) => Task.FromResult<UserContext?>(null);
    }
}
