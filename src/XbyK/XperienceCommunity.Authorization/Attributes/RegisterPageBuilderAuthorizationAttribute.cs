using XperienceCommunity.Authorization.Internal;

namespace XperienceCommunity.Authorization
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]

    public class RegisterPageBuilderAuthorizationAttribute : Attribute
    {
        /// <summary>
        /// Create Authorization (on Page Template or Class Name) based on a given type (users, roles, pageACL, or just Authenticated)
        /// </summary>
        /// <param name="pageBuilderType">What Type to check (Page Template or Class Name)</param>
        /// <param name="pageBuilderValue">The Page Builder Value (either the Page Template Identifier or the Class Name)</param>
        /// <param name="type">What type of Authentication (Usernames, Roles, PageACL permissions, or just IsAuthenticated)</param>
        /// <param name="values">Username or Roles to check (can put none or null if ByPageACL Member Permissions or ByAuthentication)</param>
        /// <param name="templateIdentifiersArePrefix">If true, then the pageBuilderValues can be the beginning of the actual value of the checked Page Template Identifier or Class Name (ex: "Generic.HomePage" would match "Generic.HomePage_Default" and "Generic.HomePage_ReturnVisitor") </param>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string pageBuilderValue, AuthorizationType type, string[]? values = null, bool templateIdentifiersArePrefix = false)
        {
            PageBuilderConfiguration = CreatePageBuilderConfiguration(pageBuilderType, templateIdentifiersArePrefix, [pageBuilderValue]);

            AuthorizationConfiguration = CreateAuthorizationCofiguration(type, values ?? []);
        }


        /// <summary>
        /// Create Authorization (on Page Template or Class Name) based on a given type (users, roles, pageACL, or just Authenticated)
        /// </summary>
        /// <param name="values">; Array of values (usernames or roles), can put none or null if ByPageACL or ByAuthenticated</param>
        /// <param name="pageBuilderType">What Type to check (Page Template or Class Name)</param>
        /// <param name="pageBuilderValues">The Page Builder Value (either the Page Template Identifier or the Class Name)</param>
        /// <param name="type">What type of Authentication (Usernames, Roles, PageACL Member Permissions, or just IsAuthenticated)</param>
        /// <param name="values">Username or Roles to check (can put none or null if ByPageACL or ByAuthentication).</param>
        /// <param name="templateIdentifiersArePrefix">If true, then the pageBuilderValues can be the beginning of the actual value of the checked Page Template Identifier or Class Name (ex: "Generic.HomePage" would match "Generic.HomePage_Default" and "Generic.HomePage_ReturnVisitor") </param>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string[] pageBuilderValues, AuthorizationType type, string[]? values = null, bool templateIdentifiersArePrefix = false)
        {
            pageBuilderValues ??= [];
            PageBuilderConfiguration = CreatePageBuilderConfiguration(pageBuilderType, templateIdentifiersArePrefix, pageBuilderValues);

            values ??= [];
            AuthorizationConfiguration = CreateAuthorizationCofiguration(type, values);
        }


        /// <summary>
        /// Create Authorization (on Page Template or Class Name) based on a given custom IAuthorization Class Logic.
        /// Must register your implementation of the IAuthorization interface in your Service Collection BEFORE the services.UseAuthorization().
        /// </summary>
        /// <param name="pageBuilderType">What Type to check (Page Template or Class Name)</param>
        /// <param name="pageBuilderValue">The Page Builder Value (either the Page Template Identifier or the Class Name)</param>
        /// <param name="customAuthorization">Your class that implements IAuthorization, must also register in the services container BEFORE the services.UseAuthorization().</param>
        /// <param name="templateIdentifiersArePrefix">If true, then the pageBuilderValues can be the beginning of the actual value of the checked Page Template Identifier or Class Name (ex: "Generic.HomePage" would match "Generic.HomePage_Default" and "Generic.HomePage_ReturnVisitor") </param>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string pageBuilderValue, Type customAuthorization, bool templateIdentifiersArePrefix = false)
        {
            PageBuilderConfiguration = CreatePageBuilderConfiguration(pageBuilderType, templateIdentifiersArePrefix, [pageBuilderValue]);

            AuthorizationConfiguration = new AuthorizationConfiguration() {
                CustomAuthorization = customAuthorization
            };
        }


        /// <summary>
        /// Create Authorization (on Page Template or Class Name) based on a given custom IAuthorization Class Logic.
        /// Must register your implementation of the IAuthorization interface in your Service Collection BEFORE the services.UseAuthorization().
        /// </summary>
        /// <param name="pageBuilderType">What Type to check (Page Template or Class Name)</param>
        /// <param name="pageBuilderValues">The Page Builder Value (either the Page Template Identifier or the Class Name).</param>
        /// <param name="customAuthorization">Your class that implements IAuthorization, must also register in the services container BEFORE the services.UseAuthorization().</param>
        /// <param name="templateIdentifiersArePrefix">If true, then the pageBuilderValues can be the beginning of the actual value of the checked Page Template Identifier or Class Name (ex: "Generic.HomePage" would match "Generic.HomePage_Default" and "Generic.HomePage_ReturnVisitor") </param>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string[] pageBuilderValues, Type customAuthorization, bool templateIdentifiersArePrefix = false)
        {
            PageBuilderConfiguration = CreatePageBuilderConfiguration(pageBuilderType, templateIdentifiersArePrefix, pageBuilderValues);

            AuthorizationConfiguration = new AuthorizationConfiguration() {
                CustomAuthorization = customAuthorization
            };
        }

        /// <summary>
        /// Full configuration
        /// </summary>
        /// <param name="pageTemplateIdentifiers">Page template Identifier that this rule applies to.</param>
        /// <param name="templateIdentifiersArePrefix">If true, then the given page template identifiers are treated as prefixes and any page template that starts with any of those values will have this rule apply.</param>
        /// <param name="pageClassNames">Page type class names that this rule applies to.</param>
        /// <param name="userAuthenticationRequired">If the user is required to be authenticated, true by default.</param>
        /// <param name="checkPageACL">If the Page's ACL security settings (Member Permissions) should be checked/enforced. false by default.</param>
        /// <param name="customUnauthorizedRedirect">If instead of throwing the general Login or Unauthorized result, you wish to redirect to another location.</param>
        /// <param name="roles">Roles the user must be part of, comma or semi-colon separated list</param>
        /// <param name="users">Usernames of the authorized users, comma or semi-colon separated list</param>
        /// <param name="customAuthorization">Type of the IAuthorization inherited class that you wish to use to authorize this request.  Must register this class in the services container BEFORE the services.UseAuthorization().</param>
        /// <param name="cacheAuthenticationResults">If the authenticated results should be cached to decrease time for re-validating on the same resources, default is true.</param>
        public RegisterPageBuilderAuthorizationAttribute(
            string[]? pageTemplateIdentifiers = null,
            bool templateIdentifiersArePrefix = false,
            string[]? pageClassNames = null,
            bool userAuthenticationRequired = true,
            bool checkPageACL = false,
            string? customUnauthorizedRedirect = null,
            string[]? roles = null,
            string[]? users = null,
            Type? customAuthorization = null,
            bool cacheAuthenticationResults = true
            )
        {
            PageBuilderConfiguration = new PageBuilderAuthorizationConfiguration {
                TemplateIdentifiersArePrefix = templateIdentifiersArePrefix,
                PageTemplateIdentifiers = (pageTemplateIdentifiers ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
                PageClassNames = (pageClassNames ?? []).Where(x => !string.IsNullOrWhiteSpace(x))
            };

            AuthorizationConfiguration = new AuthorizationConfiguration {
                UserAuthenticationRequired = userAuthenticationRequired,
                CheckPageACL = checkPageACL,
                CacheAuthenticationResults = cacheAuthenticationResults,
                Roles = (roles ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
                Users = (users ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
                CustomUnauthorizedRedirect = (!string.IsNullOrWhiteSpace(customUnauthorizedRedirect) ? customUnauthorizedRedirect : null),
                CustomAuthorization = customAuthorization
            };
        }

        private static PageBuilderAuthorizationConfiguration CreatePageBuilderConfiguration(PageBuilderAuthorizationTypes type, bool templateValuesArePrefix, string[] values)
        {
            var pageBuilderAuthorizationConfiguration = new PageBuilderAuthorizationConfiguration();
            switch (type) {
                case PageBuilderAuthorizationTypes.ByPageTemplate:
                    pageBuilderAuthorizationConfiguration.PageTemplateIdentifiers = values;
                    pageBuilderAuthorizationConfiguration.TemplateIdentifiersArePrefix = templateValuesArePrefix;
                    break;
                case PageBuilderAuthorizationTypes.ByPageType:
                    pageBuilderAuthorizationConfiguration.PageClassNames = values;
                    break;
            }
            return pageBuilderAuthorizationConfiguration;
        }

        private static AuthorizationConfiguration CreateAuthorizationCofiguration(AuthorizationType type, string[] values)
        {
            var authorizationConfiguration = new AuthorizationConfiguration();
            switch (type) {
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

        public PageBuilderAuthorizationConfiguration PageBuilderConfiguration { get; set; }
        public AuthorizationConfiguration AuthorizationConfiguration { get; set; }
    }
}
