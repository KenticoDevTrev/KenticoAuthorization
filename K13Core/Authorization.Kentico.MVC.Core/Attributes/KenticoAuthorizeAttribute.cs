using CMS.DocumentEngine;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Authorization.Kentico.Filters;

namespace Authorization.Kentico.MVC
{
    [Obsolete("Use Authorization.Kentico.ControllerActionAuthorization attribute instead.", true)]
    public class KenticoAuthorizeAttribute : TypeFilterAttribute
    {
        private AuthorizationConfiguration config;

        [Obsolete("Use Authorization.Kentico.ControllerActionAuthorization attribute instead.", true)]
        public KenticoAuthorizeAttribute(bool CacheAuthenticationResults = true,
            bool CheckPageACL = false,
            string CustomUnauthorizedRedirect = null,
            NodePermissionsEnum NodePermissionToCheck = NodePermissionsEnum.Read,
            string ResourceAndPermissionNames = null,
            string Roles = null,
            bool UserAuthenticationRequired = true,
            string Users = null
            ) : base(typeof(ControllerActionAuthorizationFilter))
        {
            // Build Configuration
            config = new AuthorizationConfiguration()
            {
                CacheAuthenticationResults = CacheAuthenticationResults,
                CustomUnauthorizedRedirect = CustomUnauthorizedRedirect,
                CheckPageACL = CheckPageACL,
                NodePermissionToCheck = NodePermissionToCheck,
                ResourceAndPermissionNames = !string.IsNullOrWhiteSpace(ResourceAndPermissionNames) ? ResourceAndPermissionNames.Split(";,|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()) : Array.Empty<string>(),
                Roles = !string.IsNullOrWhiteSpace(Roles) ? Roles.Split(";,|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()) : Array.Empty<string>(),
                UserAuthenticationRequired = UserAuthenticationRequired,
                Users = !string.IsNullOrWhiteSpace(Users) ? Roles.Split(";,|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()) : Array.Empty<string>()
            };
        }
    }
}
