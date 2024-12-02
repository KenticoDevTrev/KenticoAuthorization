namespace XperienceCommunity.Authorization
{
    public class AuthorizationConfiguration
    {
        /// <summary>
        /// True by default, if no other attributes specified and this is true, then will authorize any logged in user.  Set to false to  
        /// </summary>
        public bool UserAuthenticationRequired { get; set; } = true;

        /// <summary>
        /// Set to true to leverage Member Roles's Page Security, must be able to find the node for this check to run
        /// </summary>
        public bool CheckPageACL { get; set; } = false;

        /// <summary>
        /// Custom redirect path, useful if you want to direct users to a specific unauthorized page or perhaps a JsonResult action for AJAX calls.
        /// </summary>
        public string? CustomUnauthorizedRedirect { get; set; } = null;

        /// <summary>
        /// Roles in Kentico you wish to check against
        /// </summary>
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// List of usernames you wish to check again
        /// </summary>
        public IEnumerable<string> Users { get; set; } = Array.Empty<string>();

        /// <summary>
        /// <param name="CustomAuthorization">Type of the custom authorization, must use <see cref="IAuthorization"/>IAuthorization interface.</param>
        /// </summary>
        public Type? CustomAuthorization { get; set; } = null;

        /// <summary>
        /// True by default, this will cache authentication requests using Kentico's CacheHelper.
        /// </summary>
        public bool CacheAuthenticationResults { get; set; } = true;

    }
}
