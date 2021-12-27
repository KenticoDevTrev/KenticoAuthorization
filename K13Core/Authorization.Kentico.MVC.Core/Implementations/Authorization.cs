using Authorization.Kentico.Interfaces;
using Authorization.Kentico.MVC.Events;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Authorization.Kentico.Implementations
{
    public class Authorization : IAuthorization
    {
        private readonly IProgressiveCache _progressiveCache;
        private readonly IUserInfoProvider _userInfoProvider;

        public Authorization(IProgressiveCache progressiveCache,
            IUserInfoProvider userInfoProvider)
        {
            _progressiveCache = progressiveCache;
            _userInfoProvider = userInfoProvider;
        }
        public async Task<bool> IsAuthorizedAsync(UserContext user, AuthorizationConfiguration authConfig, TreeNode currentPage = null, string pageTemplateIdentifier = null)
        {
            var site = SiteContextSafe();

            bool authorized = false;

            // Will remain true only if no other higher priority authorization items were specified
            bool OnlyAuthenticatedCheck = true;

            // Global admin
            if (!authorized && user.IsGlobalAdmin)
            {
                authorized = true;
                OnlyAuthenticatedCheck = false;
            }

            // Roles
            if (!authorized && authConfig.Roles.Any())
            {
                OnlyAuthenticatedCheck = false;
                authorized = (user.Roles.Intersect(authConfig.Roles, StringComparer.InvariantCultureIgnoreCase).Any());
            }

            // Users no longer there
            if (!authorized && authConfig.Users.Any())
            {
                OnlyAuthenticatedCheck = false;
                authorized = authConfig.Users.Contains(user.UserName, StringComparer.InvariantCultureIgnoreCase);
            }

            // Explicit Permissions
            if (!authorized && authConfig.ResourceAndPermissionNames.Any())
            {
                OnlyAuthenticatedCheck = false;
                authorized = authConfig.ResourceAndPermissionNames.Intersect(user.Permissions, StringComparer.InvariantCultureIgnoreCase).Any();
            }

            // Check page level security
            if (!authorized && authConfig.CheckPageACL && currentPage != null)
            {
                // Need basic user from username
                var userInfo = await _progressiveCache.LoadAsync(async cs =>
                {
                    return await _userInfoProvider.GetAsync(user.UserName);
                }, new CacheSettings(30, "GetUserInfoForAuthorization", user.UserName));

                // Kentico has own caching so okay to call uncached
                if (TreeSecurityProvider.IsAuthorizedPerNode(currentPage, authConfig.NodePermissionToCheck, userInfo) != AuthorizationResultEnum.Denied)
                {
                    authorized = true;
                }
            }

            // If there were no other authentication properties, check if this is purely an "just requires authentication" area
            if (OnlyAuthenticatedCheck && (!authConfig.UserAuthenticationRequired || user.IsAuthenticated))
            {
                authorized = true;
            }

            return authorized;
        }
        private SiteInfo SiteContextSafe()
        {
            return SiteContext.CurrentSite ?? _progressiveCache.Load(cs =>
            {
                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency("cms.site|all");
                }
                return SiteInfo.Provider.Get().TopN(1).FirstOrDefault();
            }, new CacheSettings(1440, "KenticoAuthorizationGetSiteContextSafe"));
        }
    }
}
