using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Localization;
using CMS.Membership;
using CMS.Modules;
using CMS.SiteProvider;
using Kentico.Content.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using XperienceCommunity.Authorization.Events;

namespace XperienceCommunity.Authorization.Implementations
{
    public class AuthorizationContext : IAuthorizationContext
    {
        private readonly IProgressiveCache _progressiveCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserInfoProvider _userInfoProvider;
        private readonly IUserRoleInfoProvider _userRoleInfoProvider;
        private readonly IUserSiteInfoProvider _userSiteInfoProvider;
        private readonly IEventLogService _eventLogService;
        private readonly IPageRetriever _pageRetriever;
        private readonly IPageDataContextRetriever _pageDataContextRetriever;
        private readonly IAuthorizationContextCustomizer _authorizationContextCustomizer;
        private readonly ISiteService _siteService;

        public HttpContext _httpContext { get; }

        public AuthorizationContext(IProgressiveCache progressiveCache,
            IHttpContextAccessor httpContextAccessor,
            IUserInfoProvider userInfoProvider,
            IUserRoleInfoProvider userRoleInfoProvider,
            IUserSiteInfoProvider userSiteInfoProvider,
            IEventLogService eventLogService,
            IPageRetriever pageRetriever,
            IPageDataContextRetriever pageDataContextRetriever,
            IAuthorizationContextCustomizer authorizationContextCustomizer,
            ISiteService siteService)
        {
            _progressiveCache = progressiveCache;
            _httpContextAccessor = httpContextAccessor;
            _userInfoProvider = userInfoProvider;
            _userRoleInfoProvider = userRoleInfoProvider;
            _userSiteInfoProvider = userSiteInfoProvider;
            _eventLogService = eventLogService;
            _pageRetriever = pageRetriever;
            _pageDataContextRetriever = pageDataContextRetriever;
            _authorizationContextCustomizer = authorizationContextCustomizer;
            _siteService = siteService;
            _httpContext = _httpContextAccessor.HttpContext;
        }

        public async Task<TreeNode> GetCurrentPageAsync()
        {
            TreeNode foundNode = null;
            string SiteName = SiteContextSafe().SiteName;
            string DefaultCulture = SiteContextSafe().DefaultVisitorCulture;

            // Create GetPage Event Arguments
            GetPageEventArgs pageArgs = new GetPageEventArgs()
            {
                RelativeUrl = GetUrl(UriHelper.GetDisplayUrl(_httpContext.Request), (_httpContext.Request.PathBase.HasValue ? _httpContext.Request.PathBase.Value : ""), SiteName),
                HttpContext = _httpContext,
                SiteName = SiteName,
                Culture = await GetCultureAsync(),
                DefaultCulture = DefaultCulture
            };

            var customTreeNode = await _authorizationContextCustomizer.GetCustomPageAsync(pageArgs, AuthorizationEventType.Before);
            if (customTreeNode != null)
            {
                if (customTreeNode.NodeACLID <= 0)
                {
                    throw new NullReferenceException("The TreeNode does not contain the NodeACLID property, which is required for Permission lookup.");
                }
                foundNode = customTreeNode;
            }

            if (foundNode == null)
            {
                // Try to find the page from node alias path, default lookup type

                try
                {

                    if (_pageDataContextRetriever.TryRetrieve<TreeNode>(out var pageContext))
                    {
                        foundNode = pageContext.Page;
                    }
                }
                catch (InvalidOperationException)
                {
                    // this may be thrown for invalid pages or internal requests
                }
                if (foundNode == null)
                {
                    foundNode = await _progressiveCache.LoadAsync(async cs =>
                    {
                        var pages = await DocumentHelper.GetDocuments()
                        .Path(pageArgs.RelativeUrl, PathTypeEnum.Single)
                        .Culture(!string.IsNullOrWhiteSpace(pageArgs.Culture) ? pageArgs.Culture : pageArgs.DefaultCulture)
                        .CombineWithAnyCulture()
                        .CombineWithDefaultCulture()
                        .OnSite(pageArgs.SiteName)
                        .Columns("NodeACLID", "NodeID", "DocumentID", "DocumentCulture") // The Fields required for authorization
                        .GetEnumerableTypedResultAsync();

                        var pageList = pages.ToList();
                        var page = pageList.FirstOrDefault();
                        if (cs.Cached && pageList.Any())
                        {
                            cs.CacheDependency = CacheHelper.GetCacheDependency(new string[]
                            {
                                    $"nodeid|{page.NodeID}",
                                    $"documentid|{page.DocumentID}"
                            });
                        }
                        return page;
                    }, new CacheSettings(1440, "KenticoAuthorizeGetTreeNode", pageArgs.RelativeUrl, pageArgs.SiteName));

                }

                pageArgs.FoundPage = foundNode;
                var customTreeNodeAfter = await _authorizationContextCustomizer.GetCustomPageAsync(pageArgs, AuthorizationEventType.After);
                if (customTreeNodeAfter != null)
                {
                    if (customTreeNode.NodeACLID <= 0)
                    {
                        new NullReferenceException("The TreeNode does not contain the NodeACLID property, which is required for Permission lookup.");
                    }
                    foundNode = pageArgs.FoundPage;
                }
            }

            return foundNode;
        }

        public Task<string> GetCurrentPageTemplateIdentifierAsync(TreeNode page)
        {
            if (page == null)
            {
                return Task.FromResult(string.Empty);
            }
            string templateIdentifier = "";
            try
            {
                string templateJson = (string)page.GetValue("DocumentPageTemplateConfiguration", "");
                if (!string.IsNullOrWhiteSpace(templateJson))
                {
                    var jConfiguration = JObject.Parse(templateJson);
                    var config = (dynamic)jConfiguration;
                    templateIdentifier = config.identifier;
                }
            }
            catch (Exception ex)
            {
                _eventLogService.LogException("Authorization.Kentico", "Invalid Template JSON", ex, additionalMessage: "Could not find template name for page " + page.NodeAliasPath);
            }
            return Task.FromResult(templateIdentifier);
        }

        public async Task<UserContext> GetCurrentUserAsync()
        {
            var site = SiteContextSafe();

            // Create GetUser Event Arguments
            GetUserEventArgs userArgs = new GetUserEventArgs()
            {
                HttpContext = _httpContext
            };

            // Allow them to customize the user context
            var customUserContext = await _authorizationContextCustomizer.GetCustomUserContextAsync(userArgs, AuthorizationEventType.Before);
            if (customUserContext != null)
            {
                return customUserContext;
            }

            // Get the current user
            var foundUser = await GetCurrentUserInfoAsync();

            // Allow them to customize the user context again now with the current UserInfo known
            userArgs.FoundUser = foundUser;
            customUserContext = await _authorizationContextCustomizer.GetCustomUserContextAsync(userArgs, AuthorizationEventType.After);
            if (customUserContext != null)
            {
                return customUserContext;
            }

            // Build user context
            return await _progressiveCache.LoadAsync(async cs =>
            {
                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency(new string[]
                    {
                        $"{UserInfo.OBJECT_TYPE}|byname|{foundUser?.UserName ?? "public"}",
                        $"{UserRoleInfo.OBJECT_TYPE}|all",
                        $"{RolePermissionInfo.OBJECT_TYPE}|all"
                    });
                }

                if (foundUser?.IsPublic() ?? true)
                {
                    var userContext = new UserContext()
                    {
                        UserName = "public",
                        IsAuthenticated = false,
                        Roles = Array.Empty<string>()
                    };
                    return userContext;
                }
                else
                {
                    // Get roles and permissions
                    var userContext = new UserContext()
                    {
                        UserName = foundUser.UserName,
                        IsAuthenticated = true,
                        IsGlobalAdmin = foundUser.SiteIndependentPrivilegeLevel == CMS.Base.UserPrivilegeLevelEnum.GlobalAdmin,
                    };

                    if (foundUser.SiteIndependentPrivilegeLevel == CMS.Base.UserPrivilegeLevelEnum.Admin || foundUser.SiteIndependentPrivilegeLevel == CMS.Base.UserPrivilegeLevelEnum.Editor)
                    {
                        // Only add editor/admin if they are on this site
                        var userSite = await new ObjectQuery<UserSiteInfo>()
                            .WhereEquals(nameof(UserSiteInfo.UserID), foundUser.UserID)
                            .WhereEquals(nameof(UserSiteInfo.SiteID), site.SiteID)
                            .GetEnumerableTypedResultAsync();
                        if (userSite.Any())
                        {
                            userContext.IsEditor = foundUser.SiteIndependentPrivilegeLevel == CMS.Base.UserPrivilegeLevelEnum.Editor;
                            userContext.IsAdministrator = foundUser.SiteIndependentPrivilegeLevel == CMS.Base.UserPrivilegeLevelEnum.Admin;
                        }
                    }

                    // Roles
                    var rolesResults = (await new ObjectQuery<RoleInfo>()
                        .Where($"RoleID in (Select UR.RoleID from CMS_UserRole UR where UserID = {foundUser.UserID})")
                        .WhereEqualsOrNull(nameof(RoleInfo.SiteID), site.SiteID)
                        .Columns(nameof(RoleInfo.RoleName))
                        .GetEnumerableTypedResultAsync());
                    var roles = rolesResults.ToList().Select(x => x.RoleName);

                    var membershipRolesResults = (await new ObjectQuery<RoleInfo>()
                        .Source(x => x.Join<MembershipRoleInfo>("CMS_Role.RoleID", "CMS_MembershipRole.RoleID"))
                        .Source(x => x.Join<MembershipInfo>("CMS_MembershipRole.MembershipID", "CMS_Membership.MembershipID"))
                        .Source(x => x.Join<MembershipUserInfo>("CMS_Membership.MembershipID", "CMS_MembershipUser.MembershipID"))
                        .WhereEquals("UserID", foundUser.UserID)
                        .Columns(nameof(RoleInfo.RoleName))
                        .GetEnumerableTypedResultAsync());
                    var membershipRoles = membershipRolesResults.ToList().Select(x => x.RoleName);

                    userContext.Roles = roles.Union(membershipRoles);

                    // Permission names
                    var permissionsResults = (await new ObjectQuery<PermissionNameInfo>()
                         .Source(x => x.Join<ResourceInfo>("CMS_Permission.ResourceID", "CMS_Resource.ResourceID"))
                         .Source(x => x.Join<RolePermissionInfo>("CMS_Permission.PermissionID", "CMS_RolePermission.PermissionID"))
                         .Source(x => x.Join<RoleInfo>("CMS_RolePermission.RoleID", "CMS_Role.RoleID"))
                         .WhereIn(nameof(RoleInfo.RoleName), userContext.Roles.ToArray())
                         .Columns($"{nameof(ResourceInfo.ResourceName)}+'.'+{nameof(PermissionNameInfo.PermissionName)} as PermissionName")
                         .GetEnumerableResultAsync());
                    userContext.Permissions = permissionsResults.ToList().Select(x => (string)x["PermissionName"]);
                    return userContext;
                }

            }, new CacheSettings(60, "UserContext", foundUser?.UserName ?? "public", site.SiteID));
        }

        private SiteInfo SiteContextSafe()
        {
            try
            {
                return (SiteInfo)_siteService.CurrentSite;
            } catch(Exception)
            {
                // if site context isn't available yet return first site...
                return _progressiveCache.Load(cs =>
                {
                    if (cs.Cached)
                    {
                        cs.CacheDependency = CacheHelper.GetCacheDependency("cms.site|all");
                    }
                    return SiteInfo.Provider.Get().TopN(1).FirstOrDefault();
                }, new CacheSettings(1440, "KenticoAuthorizationGetSiteContextSafe"));
            }
        }

        /// <summary>
        /// Gets the Current Culture, needed for User Culture Permissions
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetCultureAsync()
        {
            string siteName = SiteContextSafe().SiteName;
            string defaultCulture = SiteContextSafe().DefaultVisitorCulture;
            string culture = "";

            // Handle Preview, during Route Config the Preview isn't available and isn't really needed, so ignore the thrown exception
            bool previewEnabled = false;
            try
            {
                previewEnabled = _httpContextAccessor.HttpContext.Kentico().Preview().Enabled;
            }
            catch (InvalidOperationException) { }

            GetCultureEventArgs cultureArgs = new GetCultureEventArgs()
            {
                DefaultCulture = defaultCulture,
                SiteName = siteName,
                Request = _httpContextAccessor.HttpContext.Request,
                PreviewEnabled = previewEnabled
            };

            string customCulture = await _authorizationContextCustomizer.GetCustomCultureAsync(cultureArgs, AuthorizationEventType.Before);
            if (!string.IsNullOrWhiteSpace(customCulture))
            {
                return customCulture;
            }

            // If Preview is enabled, use the Kentico Preview CultureName
            if (previewEnabled)
            {
                try
                {
                    culture = _httpContextAccessor.HttpContext.Kentico().Preview().CultureName;
                }
                catch (Exception) { }
            }

            // If culture still not set, use the LocalizationContext.CurrentCulture
            if (string.IsNullOrWhiteSpace(culture))
            {
                try
                {
                    culture = LocalizationContext.CurrentCulture.CultureName;
                }
                catch (Exception) { }
            }

            // If that fails then use the System.Globalization.CultureInfo
            if (string.IsNullOrWhiteSpace(culture))
            {
                try
                {
                    culture = System.Globalization.CultureInfo.CurrentCulture.Name;
                }
                catch (Exception) { }
            }

            cultureArgs.Culture = culture;

            string customCultureAfter = await _authorizationContextCustomizer.GetCustomCultureAsync(cultureArgs, AuthorizationEventType.After);
            if (!string.IsNullOrWhiteSpace(customCultureAfter))
            {
                culture = customCultureAfter;
            }

            return culture;
        }

        /// <summary>
        /// Gets the Relative Url without the Application Path, and with Url cleaned.
        /// </summary>
        /// <param name="relativeUrl"></param>
        /// <param name="applicationPath"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        private string GetUrl(string relativeUrl, string applicationPath, string siteName)
        {
            // Remove Application Path from Relative Url if it exists at the beginning
            if (!string.IsNullOrWhiteSpace(applicationPath) && applicationPath != "/" && relativeUrl.ToLower().IndexOf(applicationPath.ToLower()) == 0)
            {
                relativeUrl = relativeUrl.Substring(applicationPath.Length);
            }

            return GetCleanUrl(relativeUrl, siteName);
        }

        /// <summary>
        /// Gets the Url cleaned up with special characters removed
        /// </summary>
        /// <param name="url"></param>
        /// <param name="siteName"></param>
        /// <returns></returns>
        private string GetCleanUrl(string url, string siteName)
        {
            // Remove trailing or double //'s and any url parameters / anchors
            url = "/" + url.Trim("/ ".ToCharArray()).Split('?')[0].Split('#')[0];
            url = HttpUtility.UrlDecode(url);

            // Replace forbidden characters
            // Remove / from the forbidden characters because that is part of the Url, of course.

            if (!string.IsNullOrWhiteSpace(siteName))
            {
                string ForbiddenCharacters = URLHelper.ForbiddenURLCharacters(siteName).Replace("/", "");
                string Replacement = URLHelper.ForbiddenCharactersReplacement(siteName).ToString();
                url = ReplaceAnyCharInString(url, ForbiddenCharacters.ToCharArray(), Replacement);
            }

            // Escape special url characters
            url = URLHelper.EscapeSpecialCharacters(url);

            return url;
        }

        /// <summary>
        /// Replaces any char in the char array with the replace value for the string
        /// </summary>
        /// <param name="value">The string to replace values in</param>
        /// <param name="charsToReplace">The character array of characters to replace</param>
        /// <param name="replaceValue">The value to replace them with</param>
        /// <returns>The cleaned string</returns>
        private string ReplaceAnyCharInString(string value, char[] charsToReplace, string replaceValue)
        {
            string[] temp = value.Split(charsToReplace, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(replaceValue, temp);
        }

        /// <summary>
        /// Returns the user to check.  Default is to use the HttpContext's User Identity as the username
        /// </summary>
        /// <param name="httpContext">The HttpContext of the request</param>
        /// <returns>The UserInfo, should return the Public user if they are not logged in.</returns>
        public async Task<UserInfo> GetCurrentUserInfoAsync()
        {
            UserInfo foundUser = null;
            var site = SiteContextSafe();
            // Create GetUser Event Arguments
            GetUserEventArgs userArgs = new GetUserEventArgs()
            {
                HttpContext = _httpContext
            };

            var customUser = await _authorizationContextCustomizer.GetCustomUserAsync(userArgs, AuthorizationEventType.Before);
            if (customUser != null)
            {
                return customUser;
            }


            // Grab Username and find the user
            string username = !string.IsNullOrWhiteSpace(userArgs.FoundUserName) ? userArgs.FoundUserName : DataHelper.GetNotEmpty((_httpContext.User != null && _httpContext.User.Identity != null ? _httpContext.User.Identity.Name : "public"), "public");

            foundUser = await _progressiveCache.LoadAsync(async cs =>
            {
                var userObj = await _userInfoProvider.GetAsync(username);

                if (userObj == null || !userObj.Enabled)
                {
                    userObj = await _userInfoProvider.GetAsync("public");
                }
                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency("cms.user|byid|" + userObj.UserID);
                }
                return userObj;
            }, new CacheSettings(60, "KenticoAuthorizeGetCurrentUser", username));

            userArgs.FoundUser = foundUser;
            customUser = await _authorizationContextCustomizer.GetCustomUserAsync(userArgs, AuthorizationEventType.Before);
            if (customUser != null)
            {
                foundUser = customUser;
            }

            return foundUser;
        }


    }
}
