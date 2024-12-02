using CMS.Websites;
using XperienceCommunity.MemberRoles.Repositories;

namespace XperienceCommunity.Authorization.Implementations
{
    public class Authorization(IMemberPermissionSummaryRepository memberPermissionSummaryRepository) : IAuthorization
    {
        private readonly IMemberPermissionSummaryRepository _memberPermissionSummaryRepository = memberPermissionSummaryRepository;

        public async Task<bool> IsAuthorizedAsync(UserContext user, AuthorizationConfiguration authConfig, IWebPageFieldsSource? currentPage = null, string? pageTemplateIdentifier = null)
        {
            bool authorized = false;

            // Will remain true only if no other higher priority authorization items were specified
            bool onlyAuthenticatedCheck = true;

            // Roles
            if (!authorized && authConfig.Roles.Any()) {
                onlyAuthenticatedCheck = false;
                authorized = (user.Roles.Intersect(authConfig.Roles, StringComparer.InvariantCultureIgnoreCase).Any());
            }

            // Users no longer there
            if (!authorized && authConfig.Users.Any()) {
                onlyAuthenticatedCheck = false;
                authorized = authConfig.Users.Contains(user.UserName, StringComparer.InvariantCultureIgnoreCase);
            }

            // Check page level security
            if (!authorized && authConfig.CheckPageACL && currentPage != null && currentPage.SystemFields.WebPageItemID > 0) {
                var permissions = await _memberPermissionSummaryRepository.GetMemberRolePermissionSummaryByWebPageItem(currentPage.SystemFields.WebPageItemID);

                // At this point, if no other check exists and the page ACL does not require authentication, then return authorized.
                if (onlyAuthenticatedCheck && !permissions.RequiresAuthentication) {
                    return true;
                } else if (permissions.RequiresAuthentication && !user.IsAuthenticated) {
                    return false;
                }

                // Check member Roles logic
                if (!authorized && permissions.MemberRoles.Length == 0) {
                    onlyAuthenticatedCheck = false;
                    authorized = (user.Roles.Intersect(permissions.MemberRoles, StringComparer.InvariantCultureIgnoreCase).Any());
                }
            }

            // If there were no other authentication properties, check if this is purely an "just requires authentication" area
            if (onlyAuthenticatedCheck && (!authConfig.UserAuthenticationRequired || user.IsAuthenticated)) {
                authorized = true;
            }

            return authorized;
        }
    }
}
