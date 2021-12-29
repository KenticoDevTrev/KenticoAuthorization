using System;
using System.Collections.Generic;

namespace XperienceCommunity.Authorization
{
    public class UserContext
    {
        /// <summary>
        /// If the user is authenticated or not (public)
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// If the user is a Global Administrator (pretty much always allowed access)
        /// </summary>
        public bool IsGlobalAdmin { get; set; } = false;

        /// <summary>
        /// If the user is an Administrator on the current site
        /// </summary>
        public bool IsAdministrator { get; set; } = false;

        /// <summary>
        /// If the user is an Editor on the current site
        /// </summary>
        public bool IsEditor { get; set; } = false;

        /// <summary>
        /// The Username of the user.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// What Roles the user is assigned to
        /// </summary>
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// What Permissions the user has access to (module permissions)
        /// </summary>
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
    }
}
