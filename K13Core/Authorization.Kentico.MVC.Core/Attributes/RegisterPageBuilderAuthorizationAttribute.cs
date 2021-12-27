using CMS.DocumentEngine;
using System;
using System.Linq;

namespace Authorization.Kentico
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]

    public class RegisterPageBuilderAuthorizationAttribute : Attribute
    {

        /// <summary>
        /// Creates Authorization, this method will only check if a user is Authenticated
        /// </summary>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string pageBuilderValues, bool templateValuesArePrefix = false)
        {
            CreatePageBuilderConfiguration(pageBuilderType, templateValuesArePrefix, ValueToArray(pageBuilderValues));

            AuthorizationConfiguration = new AuthorizationConfiguration();
        }


        /// <summary>
        /// Create Authorization based on a given type.
        /// </summary>
        /// <param name="users">; , or | separated string of values, can put none or null if ByPageACL or ByAuthenticated</param>
        /// <returns></returns>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string pageBuilderValues, AuthorizationType type, string values = null, bool templateValuesArePrefix = false)
        {
            CreatePageBuilderConfiguration(pageBuilderType, templateValuesArePrefix, ValueToArray(pageBuilderValues));

            CreateAuthorizationCofiguration(type, ValueToArray(values));
        }

        /// <summary>
        /// Create Authorization based on a given type.
        /// </summary>
        /// <param name="values">the values (can provide none if ByPageACL or ByAuthenticated)</param>
        /// <returns></returns>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string[] pageBuilderValues,  AuthorizationType type, string[] values = null, bool templateValuesArePrefix = false)
        {
            pageBuilderValues ??= Array.Empty<string>();
            CreatePageBuilderConfiguration(pageBuilderType, templateValuesArePrefix,pageBuilderValues);

            values ??= Array.Empty<string>();
            CreateAuthorizationCofiguration(type, values);
        }

        /// <summary>
        /// Use a custom IAuthorization class.  Must register this in the services container BEFORE the services.UseAuthorization().
        /// </summary>
        /// <param name="customAuthorization">Your class that implements IAuthorization, must also register in the services container BEFORE the services.UseAuthorization().</param>
        /// <returns></returns>
        public RegisterPageBuilderAuthorizationAttribute(PageBuilderAuthorizationTypes pageBuilderType, string pageBuilderValues, Type customAuthorization, bool templateValuesArePrefix = false)
        {
            CreatePageBuilderConfiguration(pageBuilderType, templateValuesArePrefix, ValueToArray(pageBuilderValues));

            AuthorizationConfiguration = new AuthorizationConfiguration()
            {
                CustomAuthorization = customAuthorization
            };
        }

        /// <summary>
        /// Full configuration
        /// </summary>
        /// <param name="pageTemplateIdentifiers">Page template code names that this rule applies to, comma or semi-colon separated list</param>
        /// <param name="templateIdentifiersArePrefix">If true, then the given page template identifiers are treated as prefixes and any page template that starts with any of those values will have this rule apply.</param>
        /// <param name="pageClassNames">Page type class names that this rule applies to, , comma or semi-colon separated list</param>
        /// <param name="userAuthenticationRequired">If the user is required to be authenticated, true by default.</param>
        /// <param name="checkPageACL">If the Page's ACL security settings should be checked/enforced. false by default.</param>
        /// <param name="nodePermissionToCheck">What node permission to check if Page ACL is being used.  Default is the Read Permission</param>
        /// <param name="resourceAndPermissionNames">Resource+Permisssion names, comma or semi-colon separated list (ex: "mymodule.dosomething;mymodule.doanotherthing")</param>
        /// <param name="customUnauthorizedRedirect">If instead of throwing the general Login or Unauthorized result, you wish to redirect to another location.</param>
        /// <param name="roles">Roles the user must be part of, comma or semi-colon separated list</param>
        /// <param name="users">Usernames of the authorized users, comma or semi-colon separated list</param>
        /// <param name="customAuthorization">Type of the IAuthorization inherited class that you wish to use to authorize this request.  Must register this class in the services container BEFORE the services.UseAuthorization().</param>
        /// <param name="cacheAuthenticationResults">If the authenticated results should be cached to decrease time for re-validating on the same resources, default is true.</param>
        public RegisterPageBuilderAuthorizationAttribute(
            string pageTemplateIdentifiers = null,
            bool templateIdentifiersArePrefix = false,
            string pageClassNames = null,
            bool userAuthenticationRequired = true,
            bool checkPageACL = false,
            NodePermissionsEnum nodePermissionToCheck = NodePermissionsEnum.Read,
            string resourceAndPermissionNames = null,
            string customUnauthorizedRedirect = null,
            string roles = null,
            string users = null,
            Type customAuthorization = null,
            bool cacheAuthenticationResults = true
            )
        {
            PageBuilderConfiguration = new PageBuilderConfiguration()
            {
                TemplateIdentifiersArePrefix = templateIdentifiersArePrefix
            };
            if(!string.IsNullOrWhiteSpace(pageTemplateIdentifiers))
            {
                PageBuilderConfiguration.PageTemplateIdentifiers = ValueToArray(pageTemplateIdentifiers);
            }
            if (!string.IsNullOrWhiteSpace(pageClassNames))
            {
                PageBuilderConfiguration.PageClassNames = ValueToArray(pageClassNames);
            }

            AuthorizationConfiguration = new AuthorizationConfiguration
            {
                UserAuthenticationRequired = userAuthenticationRequired,
                CheckPageACL = checkPageACL,
                NodePermissionToCheck = nodePermissionToCheck,
                CacheAuthenticationResults = cacheAuthenticationResults
            };

            if (!string.IsNullOrWhiteSpace(resourceAndPermissionNames))
            {
                AuthorizationConfiguration.ResourceAndPermissionNames = ValueToArray(resourceAndPermissionNames);
            }
            if (!string.IsNullOrWhiteSpace(roles))
            {
                AuthorizationConfiguration.Roles = ValueToArray(roles);
            }
            if (!string.IsNullOrWhiteSpace(users))
            {
                AuthorizationConfiguration.Users = ValueToArray(users);
            }
            if (!string.IsNullOrWhiteSpace(customUnauthorizedRedirect))
            {
                AuthorizationConfiguration.CustomUnauthorizedRedirect = customUnauthorizedRedirect;
            }
            if (customAuthorization != null)
            {
                AuthorizationConfiguration.CustomAuthorization = customAuthorization;
            }
        }

        private void CreatePageBuilderConfiguration(PageBuilderAuthorizationTypes type, bool templateValuesArePrefix, string[] values)
        {
            PageBuilderConfiguration = new PageBuilderConfiguration();
            switch(type)
            {
                case PageBuilderAuthorizationTypes.ByPageTemplate:
                    PageBuilderConfiguration.PageTemplateIdentifiers = values;
                    PageBuilderConfiguration.TemplateIdentifiersArePrefix = templateValuesArePrefix;
                    break;
                case PageBuilderAuthorizationTypes.ByPageType:
                    PageBuilderConfiguration.PageClassNames = values;
                    break;
            }
        }

        private void CreateAuthorizationCofiguration(AuthorizationType type, string[] values)
        {
            AuthorizationConfiguration = new AuthorizationConfiguration();
            switch (type)
            {
                case AuthorizationType.ByAuthenticated:
                    AuthorizationConfiguration.UserAuthenticationRequired = true;
                    break;
                case AuthorizationType.ByPageACL:
                    AuthorizationConfiguration.CheckPageACL = true;
                    break;
                case AuthorizationType.ByPermission:
                    AuthorizationConfiguration.ResourceAndPermissionNames = values;
                    break;
                case AuthorizationType.ByRole:
                    AuthorizationConfiguration.Roles = values;
                    break;
                case AuthorizationType.ByUser:
                    AuthorizationConfiguration.Users = values;
                    break;
            }
        }

        private string[] ValueToArray(string values)
        {
            return !string.IsNullOrWhiteSpace(values) ? values.Split(";,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray() : Array.Empty<string>();
        }


        public PageBuilderConfiguration PageBuilderConfiguration { get; set; }
        public AuthorizationConfiguration AuthorizationConfiguration { get; set; }
    }
}
