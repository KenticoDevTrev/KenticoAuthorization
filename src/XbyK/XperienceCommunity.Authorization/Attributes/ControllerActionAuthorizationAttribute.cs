namespace XperienceCommunity.Authorization
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ControllerActionAuthorizationAttribute : Attribute
    {
        /// <summary>
        /// Creates Authorization, this method will only check if a user is Authenticated
        /// </summary>
        public ControllerActionAuthorizationAttribute()
        {
            AuthorizationConfiguration = new AuthorizationConfiguration();
        }

        /// <summary>
        /// Create Authorization based on a given type.
        /// </summary>
        /// <param name="values">the values (can provide none if ByPageACL or ByAuthenticated)</param>
        /// <returns></returns>
        public ControllerActionAuthorizationAttribute(AuthorizationType type, string[]? values = null)
        {
            values ??= [];
            AuthorizationConfiguration = CreateAuthorizationCofiguration(type, values);
        }

        /// <summary>
        /// Use a custom IAuthorization class.  Must register this in the services container BEFORE the services.UseAuthorization().
        /// </summary>
        /// <param name="customAuthorization">Your class that implements IAuthorization, must also register in the services container BEFORE the services.UseAuthorization().</param>
        /// <returns></returns>
        public ControllerActionAuthorizationAttribute(Type customAuthorization)
        {
            AuthorizationConfiguration = new AuthorizationConfiguration()
            {
                CustomAuthorization = customAuthorization
            };
        }

        /// <summary>
        /// This has all the options available for you to configure.
        /// </summary>
        /// <param name="userAuthenticationRequired">If the user is required to be authenticated, true by default.</param>
        /// <param name="checkPageACL">If the Page's ACL security settings should be checked/enforced. false by default.</param>
        /// <param name="customUnauthorizedRedirect">If instead of throwing the general Login or Unauthorized result, you wish to redirect to another location.</param>
        /// <param name="roles">Roles the user must be part of.</param>
        /// <param name="users">Usernames of the authorized users.</param>
        /// <param name="customAuthorization">Type of the IAuthorization inherited class that you wish to use to authorize this request.  Must register this class in the services container BEFORE the services.UseAuthorization().</param>
        /// <param name="cacheAuthenticationResults">If the authenticated results should be cached to decrease time for re-validating on the same resources, default is true.</param>
        public ControllerActionAuthorizationAttribute(
            bool userAuthenticationRequired = true,
            bool checkPageACL = false,
            string? customUnauthorizedRedirect = null,
            string[]? roles = null,
            string[]? users = null,
            Type? customAuthorization = null,
            bool cacheAuthenticationResults = true
            )
        {
            AuthorizationConfiguration = new AuthorizationConfiguration
            {
                UserAuthenticationRequired = userAuthenticationRequired,
                CheckPageACL = checkPageACL,
                Roles = (roles ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
                Users = (users ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
                CacheAuthenticationResults = cacheAuthenticationResults,
                CustomUnauthorizedRedirect = !string.IsNullOrWhiteSpace(customUnauthorizedRedirect) ? customUnauthorizedRedirect : null,
                CustomAuthorization = customAuthorization
            };
        }

        private static AuthorizationConfiguration CreateAuthorizationCofiguration(AuthorizationType type, string[] values)
        {
            var authorizationConfiguration = new AuthorizationConfiguration();
            switch (type)
            {
                case AuthorizationType.ByAuthenticated:
                    authorizationConfiguration.UserAuthenticationRequired = true;
                    break;
                case AuthorizationType.ByPageACL:
                    authorizationConfiguration.CheckPageACL = true;
                    break;
                case AuthorizationType.ByRole:
                    authorizationConfiguration.Roles = values;
                    break;
                case AuthorizationType.ByUser:
                    authorizationConfiguration.Users = values;
                    break;
            }
            return authorizationConfiguration;
        }

        /// <summary>
        /// The Authorication Configuration
        /// </summary>
        public AuthorizationConfiguration AuthorizationConfiguration;
    }
}
