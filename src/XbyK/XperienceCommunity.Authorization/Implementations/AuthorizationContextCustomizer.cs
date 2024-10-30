using CMS.DocumentEngine;
using CMS.Membership;
using System.Threading.Tasks;
using XperienceCommunity.Authorization.Events;

namespace XperienceCommunity.Authorization.Implementations
{
    public class AuthorizationContextCustomizer : IAuthorizationContextCustomizer
    {
        public Task<string> GetCustomCultureAsync(GetCultureEventArgs cultureArgs, AuthorizationEventType eventType)
        {
            return Task.FromResult<string>(null);
        }

        public Task<TreeNode> GetCustomPageAsync(GetPageEventArgs pageArgs, AuthorizationEventType eventType)
        {
            return Task.FromResult<TreeNode>(null);
        }

        public Task<UserInfo> GetCustomUserAsync(GetUserEventArgs userArgs, AuthorizationEventType eventType)
        {
            return Task.FromResult<UserInfo>(null);
        }

        public Task<UserContext> GetCustomUserContextAsync(GetUserEventArgs userArgs, AuthorizationEventType eventType)
        {
            return Task.FromResult<UserContext>(null);
        }
    }
}
