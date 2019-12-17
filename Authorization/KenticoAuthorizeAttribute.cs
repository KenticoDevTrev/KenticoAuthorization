using Authorization.Kentico.MVC.Events;
using CMS.Base;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Localization;
using CMS.Membership;
using CMS.SiteProvider;
using Kentico.Content.Web.Mvc;
using Kentico.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Authorization.Kentico.MVC
{
    public class KenticoAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// True by default, if no other attributes specified and this is true, then will authorize any logged in user.  Set to false to  
        /// </summary>
        public bool UserAuthenticationRequired { get; set; } = true;

        /// <summary>
        /// Comma, semi-color or pipe delimited list of ResourceName+PermissionName, such as CMS.Blog.Modify|My_Module.MyCustomPermission
        /// </summary>
        public string ResourceAndPermissionNames { get; set; }

        /// <summary>
        /// Set to true to leverage Kentico's Page Security, must be able to find the node for this check to run
        /// </summary>
        public bool CheckPageACL { get; set; } = false;

        /// <summary>
        /// The Node Permission this will check when it does an ACL check.  Default is Read
        /// </summary>
        public NodePermissionsEnum NodePermissionToCheck { get; set; } = NodePermissionsEnum.Read;

        /// <summary>
        /// Custom redirect path, useful if you want to direct users to a specific unauthorized page or perhaps a JsonResult action for AJAX calls.
        /// </summary>
        public string CustomUnauthorizedRedirect { get; set; }

        /// <summary>
        /// True by default, this will cache authentication requests using Kentico's CacheHelper.
        /// </summary>
        public bool CacheAuthenticationResults { get; set; } = true;

        /// <summary>
        /// Checks Roles, Users, Resource Names, and Page ACL depending on configuration
        /// </summary>
        /// <param name="httpContext">The Route Context</param>
        /// <returns>If the request is authorized.</returns>
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            AuthorizingEventArgs AuthorizingArgs = new AuthorizingEventArgs()
            {
                CurrentUser = GetCurrentUser(httpContext),
                FoundPage = GetTreeNode(httpContext),
                Authorized = false
            };

            bool IsAuthorized = false;

            // Start event, allow user to overwrite FoundPage
            using (var KenticoAuthorizeAuthorizingTaskHandler = AuthorizeEvents.Authorizing.StartEvent(AuthorizingArgs))
            {
                if (!AuthorizingArgs.SkipDefaultValidation)
                {
                    AuthorizingArgs.Authorized = CacheHelper.Cache(cs =>
                    {
                        bool Authorized = false;
                        List<string> CacheDependencies = new List<string>();

                        // Will remain true only if no other higher priority authorization items were specified
                        bool OnlyAuthenticatedCheck = true;

                        // Roles
                        if (!Authorized && !string.IsNullOrWhiteSpace(Roles))
                        {
                            OnlyAuthenticatedCheck = false;
                            CacheDependencies.Add("cms.role|all");
                            CacheDependencies.Add("cms.userrole|all");
                            CacheDependencies.Add("cms.membershiprole|all");
                            CacheDependencies.Add("cms.membershipuser|all");

                            foreach (string Role in Roles.Split(";,|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (AuthorizingArgs.CurrentUser.IsInRole(Role, SiteContext.CurrentSiteName, true, true))
                                {
                                    Authorized = true;
                                    break;
                                }
                            }
                        }

                        // Users
                        if (!Authorized && !string.IsNullOrWhiteSpace(Users))
                        {
                            OnlyAuthenticatedCheck = false;
                            foreach (string User in Users.Split(";,|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (User.ToLower().Trim() == AuthorizingArgs.CurrentUser.UserName.ToLower().Trim())
                                {
                                    Authorized = true;
                                    break;
                                }
                            }
                        }

                        // Explicit Permissions
                        if (!Authorized && !string.IsNullOrWhiteSpace(ResourceAndPermissionNames))
                        {
                            OnlyAuthenticatedCheck = false;
                            CacheDependencies.Add("cms.role|all");
                            CacheDependencies.Add("cms.userrole|all");
                            CacheDependencies.Add("cms.membershiprole|all");
                            CacheDependencies.Add("cms.membershipuser|all");
                            CacheDependencies.Add("cms.permission|all");
                            CacheDependencies.Add("cms.rolepermission|all");

                            foreach (string ResourcePermissionName in ResourceAndPermissionNames.Split(";,|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                            {
                                string[] StringParts = ResourcePermissionName.Split('.');
                                string PermissionName = StringParts.Last();
                                string ResourceName = string.Join(".", StringParts.Take(StringParts.Length - 1));
                                if (UserSecurityHelper.IsAuthorizedPerResource(ResourceName, PermissionName, SiteContext.CurrentSiteName, AuthorizingArgs.CurrentUser))
                                {
                                    Authorized = true;
                                    break;
                                }
                            }
                        }

                        // Check page level security
                        if (!Authorized && CheckPageACL)
                        {
                            if (AuthorizingArgs.FoundPage != null)
                            {
                                OnlyAuthenticatedCheck = false;
                                CacheDependencies.Add("cms.role|all");
                                CacheDependencies.Add("cms.userrole|all");
                                CacheDependencies.Add("cms.membershiprole|all");
                                CacheDependencies.Add("cms.membershipuser|all");
                                CacheDependencies.Add("nodeid|" + AuthorizingArgs.FoundPage.NodeID);
                                CacheDependencies.Add("cms.acl|all");
                                CacheDependencies.Add("cms.aclitem|all");

                                if (TreeSecurityProvider.IsAuthorizedPerNode(AuthorizingArgs.FoundPage, NodePermissionToCheck, AuthorizingArgs.CurrentUser) != AuthorizationResultEnum.Denied)
                                {
                                    Authorized = true;
                                }
                            }
                        }

                        // If there were no other authentication properties, check if this is purely an "just requires authentication" area
                        if (OnlyAuthenticatedCheck && (!UserAuthenticationRequired || !AuthorizingArgs.CurrentUser.IsPublic()))
                        {
                            Authorized = true;
                        }

                        if (cs.Cached)
                        {
                            cs.CacheDependency = CacheHelper.GetCacheDependency(CacheDependencies.Distinct().ToArray());
                        }

                        return Authorized;
                    }, new CacheSettings(CacheAuthenticationResults ? CacheHelper.CacheMinutes(SiteContext.CurrentSiteName) : 0, "AuthorizeCore", AuthorizingArgs.CurrentUser.UserID, (AuthorizingArgs.FoundPage != null ? AuthorizingArgs.FoundPage.DocumentID : -1), SiteContext.CurrentSiteName, Users, Roles, ResourceAndPermissionNames, CheckPageACL, NodePermissionToCheck, CustomUnauthorizedRedirect, UserAuthenticationRequired));
                }
                IsAuthorized = AuthorizingArgs.Authorized;
            }

            return IsAuthorized;
        }

        /// <summary>
        /// Main Authorization method, if not authorized adjusted the filterContext's results to redirect to an Unauthorized result or custom redirect.
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // If they are authorized, handle accordingly
            if (!AuthorizeCore(filterContext.HttpContext))
            {
                // Custom provided redirect
                if (!string.IsNullOrWhiteSpace(CustomUnauthorizedRedirect))
                {
                    filterContext.Result = new RedirectResult(CustomUnauthorizedRedirect);
                }
                else
                {
                    // Just throw an unauthorzied request
                    filterContext.Result = new HttpUnauthorizedResult();
                }
            }
        }

        #region "TreeNode Retreival"

        /// <summary>
        /// Can override this if you need to implement custom logic, such as a custom route.  httpContext.Request.RequestContext.RouteData.Values is often used to grab route data.
        /// </summary>
        /// <param name="httpContext">The HttpContext of the request</param>
        /// <returns>The Tree Node for this request, null acceptable.</returns>
        private TreeNode GetTreeNode(HttpContextBase httpContext)
        {
            TreeNode FoundNode = null;
            string SiteName = SiteContextSafe().SiteName;
            string DefaultCulture = SiteContextSafe().DefaultVisitorCulture;
            // Create GetPage Event Arguments
            GetPageEventArgs PageArgs = new GetPageEventArgs()
            {
                RelativeUrl = GetUrl(httpContext.Request.Url.AbsolutePath, httpContext.Request.ApplicationPath, SiteName),
                HttpContext = httpContext,
                SiteName = SiteName,
                Culture = GetCulture(),
                DefaultCulture = DefaultCulture
            };

            // Start event, allow user to overwrite FoundPage
            using (var KenticoAuthorizeGetPageTaskHandler = AuthorizeEvents.GetPage.StartEvent(PageArgs))
            {
                if (PageArgs.FoundPage == null)
                {
                    try
                    {
                        // Try to find the page from node alias path, default lookup type
                        PageArgs.FoundPage = CacheHelper.Cache(cs =>
                        {
                            TreeNode Page = DocumentHelper.GetDocuments()
                            .Path(PageArgs.RelativeUrl, PathTypeEnum.Single)
                            .Culture(!string.IsNullOrWhiteSpace(PageArgs.Culture) ? PageArgs.Culture : PageArgs.DefaultCulture)
                            .CombineWithAnyCulture()
                            .CombineWithDefaultCulture()
                            .OnSite(PageArgs.SiteName)
                            .Columns("NodeACLID", "NodeID", "DocumentID", "DocumentCulture") // The Fields required for authorization
                            .FirstOrDefault();

                            if (cs.Cached && Page != null)
                            {
                                cs.CacheDependency = CacheHelper.GetCacheDependency(new string[]
                                {
                                $"nodeid|{Page.NodeID}",
                                $"documentid|{Page.DocumentID}"
                                });
                            }
                            return Page;
                        }, new CacheSettings(1440, "KenticoAuthorizeGetTreeNode", PageArgs.RelativeUrl, PageArgs.SiteName));
                    }
                    catch (Exception ex)
                    {
                        PageArgs.ExceptionOnLookup = ex;
                    }
                }
                else if (PageArgs.FoundPage.NodeACLID <= 0)
                {
                    PageArgs.ExceptionOnLookup = new NullReferenceException("The TreeNode does not contain the NodeACLID property, which is required for Permission lookup.");
                }

                // Finish the event
                KenticoAuthorizeGetPageTaskHandler.FinishEvent();

                // Pass the Found Node back from the args
                FoundNode = PageArgs.FoundPage;
            }

            return PageArgs.FoundPage;
        }

        /// <summary>
        /// Gets the Relative Url without the Application Path, and with Url cleaned.
        /// </summary>
        /// <param name="RelativeUrl"></param>
        /// <param name="ApplicationPath"></param>
        /// <param name="SiteName"></param>
        /// <returns></returns>
        private string GetUrl(string RelativeUrl, string ApplicationPath, string SiteName)
        {
            // Remove Application Path from Relative Url if it exists at the beginning
            if (!string.IsNullOrWhiteSpace(ApplicationPath) && ApplicationPath != "/" && RelativeUrl.ToLower().IndexOf(ApplicationPath.ToLower()) == 0)
            {
                RelativeUrl = RelativeUrl.Substring(ApplicationPath.Length);
            }

            return GetCleanUrl(RelativeUrl, SiteName);
        }

        /// <summary>
        /// Gets the Url cleaned up with special characters removed
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="SiteName"></param>
        /// <returns></returns>
        private string GetCleanUrl(string Url, string SiteName)
        {
            // Remove trailing or double //'s and any url parameters / anchors
            Url = "/" + Url.Trim("/ ".ToCharArray()).Split('?')[0].Split('#')[0];
            Url = HttpUtility.UrlDecode(Url);

            // Replace forbidden characters
            // Remove / from the forbidden characters because that is part of the Url, of course.

            if (!string.IsNullOrWhiteSpace(SiteName))
            {
                string ForbiddenCharacters = URLHelper.ForbiddenURLCharacters(SiteName).Replace("/", "");
                string Replacement = URLHelper.ForbiddenCharactersReplacement(SiteName).ToString();
                Url = ReplaceAnyCharInString(Url, ForbiddenCharacters.ToCharArray(), Replacement);
            }

            // Escape special url characters
            Url = URLHelper.EscapeSpecialCharacters(Url);

            return Url;
        }

        /// <summary>
        /// Replaces any char in the char array with the replace value for the string
        /// </summary>
        /// <param name="value">The string to replace values in</param>
        /// <param name="CharsToReplace">The character array of characters to replace</param>
        /// <param name="ReplaceValue">The value to replace them with</param>
        /// <returns>The cleaned string</returns>
        private string ReplaceAnyCharInString(string value, char[] CharsToReplace, string ReplaceValue)
        {
            string[] temp = value.Split(CharsToReplace, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(ReplaceValue, temp);
        }

        #endregion

        #region "Culture Retrieval"

        /// <summary>
        /// Gets the Current Culture, needed for User Culture Permissions
        /// </summary>
        /// <returns></returns>
        private string GetCulture()
        {
            string SiteName = SiteContextSafe().SiteName;
            string DefaultCulture = SiteContextSafe().DefaultVisitorCulture;
            string Culture = "";

            // Handle Preview, during Route Config the Preview isn't available and isn't really needed, so ignore the thrown exception
            bool PreviewEnabled = false;
            try
            {
                PreviewEnabled = HttpContext.Current.Kentico().Preview().Enabled;
            }
            catch (InvalidOperationException) { }

            GetCultureEventArgs CultureArgs = new GetCultureEventArgs()
            {
                DefaultCulture = DefaultCulture,
                SiteName = SiteName,
                Request = HttpContext.Current.Request,
                PreviewEnabled = PreviewEnabled
            };

            using (var AuthorizeGetCultureTaskHandler = AuthorizeEvents.GetCulture.StartEvent(CultureArgs))
            {

                // If Preview is enabled, use the Kentico Preview CultureName
                if (PreviewEnabled)
                {
                    try
                    {
                        CultureArgs.Culture = HttpContext.Current.Kentico().Preview().CultureName;
                    }
                    catch (Exception) { }
                }

                // If culture still not set, use the LocalizationContext.CurrentCulture
                if (string.IsNullOrWhiteSpace(CultureArgs.Culture))
                {
                    try
                    {
                        CultureArgs.Culture = LocalizationContext.CurrentCulture.CultureName;
                    }
                    catch (Exception) { }
                }

                // If that fails then use the System.Globalization.CultureInfo
                if (string.IsNullOrWhiteSpace(CultureArgs.Culture))
                {
                    try
                    {
                        CultureArgs.Culture = System.Globalization.CultureInfo.CurrentCulture.Name;
                    }
                    catch (Exception) { }
                }

                AuthorizeGetCultureTaskHandler.FinishEvent();
                // set the culture
                Culture = CultureArgs.Culture;
            }
            return Culture;
        }

        #endregion

        #region "User Retrieval"

        /// <summary>
        /// Returns the user to check.  Default is to use the HttpContext's User Identity as the username
        /// </summary>
        /// <param name="httpContext">The HttpContext of the request</param>
        /// <returns>The UserInfo, should return the Public user if they are not logged in.</returns>
        private UserInfo GetCurrentUser(HttpContextBase httpContext)
        {
            UserInfo FoundUser = null;

            // Create GetUser Event Arguments
            GetUserEventArgs UserArgs = new GetUserEventArgs()
            {
                HttpContext = httpContext,
            };

            using (var KenticoAuthorizeGetUserTaskHandler = AuthorizeEvents.GetUser.StartEvent(UserArgs))
            {
                if (UserArgs.FoundUser == null)
                {
                    try
                    {
                        // Grab Username and find the user
                        string Username = !string.IsNullOrWhiteSpace(UserArgs.FoundUserName) ? UserArgs.FoundUserName : DataHelper.GetNotEmpty((httpContext.User != null && httpContext.User.Identity != null ? httpContext.User.Identity.Name : "public"), "public");
                        UserArgs.FoundUser = CacheHelper.Cache(cs =>
                        {
                            UserInfo UserObj = UserInfoProvider.GetUsers()
                            .WhereEquals("Username", Username)
                            .FirstOrDefault();
                            if (UserObj == null || !UserObj.Enabled)
                            {
                                UserObj = UserInfoProvider.GetUsers()
                                .WhereEquals("Username", "public")
                                .FirstOrDefault();
                            }
                            if (cs.Cached)
                            {
                                cs.CacheDependency = CacheHelper.GetCacheDependency("cms.user|byid|" + UserObj.UserID);
                            }
                            return UserObj;
                        }, new CacheSettings(60, "KenticoAuthorizeGetCurrentUser", Username));
                    }
                    catch (Exception ex)
                    {
                        UserArgs.ExceptionOnLookup = ex;
                    }
                }

                // Finish event
                KenticoAuthorizeGetUserTaskHandler.FinishEvent();

                // Pass found user along
                FoundUser = UserArgs.FoundUser;
            }

            return FoundUser;
        }

        #endregion

        /// <summary>
        /// Returns the SiteInfo, if not findable does the first site in Kentico
        /// </summary>
        /// <returns></returns>
        private SiteInfo SiteContextSafe()
        {
            return SiteContext.CurrentSite ?? CacheHelper.Cache(cs =>
            {
                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency("cms.site|all");
                }
                return SiteInfoProvider.GetSites().TopN(1).FirstOrDefault();
            }, new CacheSettings(1440, "KenticoAuthorizationGetSiteContextSafe"));
        }

    }
}