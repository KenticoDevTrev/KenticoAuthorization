using CMS.Base;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Headless.Internal;
using CMS.Helpers;
using CMS.Membership;
using CMS.Modules;
using CMS.Websites;
using CMS.Websites.Internal;
using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Web;
using XperienceCommunity.Authorization.Events;
using XperienceCommunity.MemberRoles;

namespace XperienceCommunity.Authorization.Implementations
{
    public class AuthorizationContext : IAuthorizationContext
    {
        private readonly IProgressiveCache _progressiveCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEventLogService _eventLogService;
        private readonly IAuthorizationContextCustomizer _authorizationContextCustomizer;
        private readonly IWebsiteChannelContext _websiteChannelContext;
        private readonly IWebPageDataContextRetriever _webPageDataContextRetriever;
        private readonly IContentQueryExecutor _contentQueryExecutor;
        private readonly IInfoProvider<WebPageUrlPathInfo> _webPageUrlPathInfoProvider;
        private readonly IInfoProvider<ContentLanguageInfo> _contentLanguageInfoProvider;
        private readonly IInfoProvider<SettingsKeyInfo> _settingsKeyInfoProvider;
        private readonly IInfoProvider<MemberInfo> _memberInfoProvider;
        private readonly IInfoProvider<MemberRoleTagInfo> _memberRoleTagInfoProvider;
        private readonly IInfoProvider<TagInfo> _tagInfoProvider;
        private readonly IContentLanguageRetriever _contentLanguageRetriever;

        public HttpContext? HttpContext { get; }

        public AuthorizationContext(IProgressiveCache progressiveCache,
            IHttpContextAccessor httpContextAccessor,
            IEventLogService eventLogService,
            IAuthorizationContextCustomizer authorizationContextCustomizer,
            IWebsiteChannelContext websiteChannelContext,
            IWebPageDataContextRetriever webPageDataContextRetriever,
            IContentQueryExecutor contentQueryExecutor,
            IInfoProvider<WebPageUrlPathInfo> webPageUrlPathInfoProvider,
            IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
            IInfoProvider<SettingsKeyInfo> settingsKeyInfoProvider,
            IInfoProvider<MemberInfo> memberInfoProvider,
            IInfoProvider<MemberRoleTagInfo> memberRoleTagInfoProvider,
            IInfoProvider<TagInfo> tagInfoProvider,
            IContentLanguageRetriever contentLanguageRetriever)
        {
            _progressiveCache = progressiveCache;
            _httpContextAccessor = httpContextAccessor;
            _eventLogService = eventLogService;
            _authorizationContextCustomizer = authorizationContextCustomizer;
            _websiteChannelContext = websiteChannelContext;
            _webPageDataContextRetriever = webPageDataContextRetriever;
            _contentQueryExecutor = contentQueryExecutor;
            _webPageUrlPathInfoProvider = webPageUrlPathInfoProvider;
            _contentLanguageInfoProvider = contentLanguageInfoProvider;
            _settingsKeyInfoProvider = settingsKeyInfoProvider;
            _memberInfoProvider = memberInfoProvider;
            _memberRoleTagInfoProvider = memberRoleTagInfoProvider;
            _tagInfoProvider = tagInfoProvider;
            _contentLanguageRetriever = contentLanguageRetriever;
            HttpContext = _httpContextAccessor.HttpContext;
        }

        public async Task<IWebPageFieldsSource?> GetCurrentPageAsync()
        {
            IWebPageFieldsSource? foundPage = null;
            var currentSite = await SiteContextSafe();

            if (HttpContext == null) {
                return null;
            }

            // First check Custom Page Finder Logic
            var pageArgs = new GetPageEventArgs(
                relativeUrl: await GetUrl(UriHelper.GetDisplayUrl(HttpContext.Request), (HttpContext.Request.PathBase.HasValue ? HttpContext.Request.PathBase.Value : "")),
                siteName: _websiteChannelContext.WebsiteChannelName,
                httpContext: HttpContext,
                culture: await GetCultureAsync(),
                defaultCulture: currentSite.DefaultLanguage
            );

            var customTreeNode = await _authorizationContextCustomizer.GetCustomPageAsync(pageArgs, AuthorizationEventType.Before);
            if (customTreeNode != null) {
                foundPage = customTreeNode;
            }

            // Use Kentico Page Builder Context
            if (foundPage == null && _webPageDataContextRetriever.TryRetrieve(out var webPageDataContext)) {
                foundPage = await GetWebPageFieldSource(webPageDataContext.WebPage.WebPageItemID, webPageDataContext.WebPage.ContentTypeName, webPageDataContext.WebPage.LanguageName, webPageDataContext.WebPage.WebsiteChannelID, GetPreviewEnabled(HttpContext));
            }

            // Try to find the page from web page item tree path, default lookup type
            foundPage ??= await GetPageFromUrlPathAndChannel(pageArgs.RelativeUrl, _websiteChannelContext.WebsiteChannelID);

            return foundPage;
        }

        private static bool GetPreviewEnabled(HttpContext httpContext)
        {
            try {
                return httpContext.Kentico().Preview().Enabled;
            } catch(Exception) {
                return false;
            }
        }

        public async Task<string?> GetCurrentPageTemplateIdentifierAsync(IWebPageFieldsSource page)
        {
            if (page == null) {
                return null;
            }

            var previewEnabled = HttpContext != null && GetPreviewEnabled(HttpContext);

            return await _progressiveCache.LoadAsync(async cs => {

                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"webpageitem|byid|{page.SystemFields.WebPageItemID}");
                }

                var query =
@$"Select {nameof(ContentItemCommonDataInfo.ContentItemCommonDataVisualBuilderTemplateConfiguration)} 
    from CMS_ContentItemCommonData 
        where {nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentItemID)} = {page.SystemFields.ContentItemID}
        and {nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentLanguageID)} = {page.SystemFields.ContentItemCommonDataContentLanguageID}
        order by {nameof(ContentItemCommonDataInfo.ContentItemCommonDataIsLatest)} {(previewEnabled ? "asc" : "desc")}";

                var result = await ConnectionHelper.ExecuteReaderAsync(query, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, CancellationToken.None);
                var ds = DatasetFromReader(result);

                if (ds.Tables[0].Rows.Count > 0) {
                    try {
                        var templateJson = (string)ds.Tables[0].Rows[0][nameof(ContentItemCommonDataInfo.ContentItemCommonDataVisualBuilderTemplateConfiguration)];
                        if (!string.IsNullOrWhiteSpace(templateJson)) {
                            var jConfiguration = JObject.Parse(templateJson);
                            var config = (dynamic)jConfiguration;
                            return config.identifier;
                        }
                    } catch (Exception ex) {
                        _eventLogService.LogException("Authorization.Kentico", "Invalid Template JSON", ex, additionalMessage: "Could not find template name for page with web page item id " + page.SystemFields.WebPageItemID);
                    }
                }
                return null;
            }, new CacheSettings(30, "Authorization_GetPageTemplateIdentifier", page.SystemFields.ContentItemID, page.SystemFields.ContentItemCommonDataContentLanguageID, previewEnabled));
        }

        private static UserContext GetPublicUserContext()
        {
            return new UserContext() {
                UserName = "public",
                IsAuthenticated = false,
                Roles = []
            };
        }

        public async Task<UserContext> GetCurrentUserAsync()
        {
            if (HttpContext == null) {
                return GetPublicUserContext();
            }

            var site = SiteContextSafe();

            // Create GetUser Event Arguments
            var userArgs = new GetUserEventArgs(HttpContext);

            // Allow them to customize the user context
            var customUserContext = await _authorizationContextCustomizer.GetCustomUserContextAsync(userArgs, AuthorizationEventType.Before);
            if (customUserContext != null) {
                return customUserContext;
            }

            // Get the current user
            var foundUser = await GetCurrentUserInfoAsync(HttpContext);

            // Allow them to customize the user context again now with the current UserInfo known
            userArgs.FoundUser = foundUser;
            customUserContext = await _authorizationContextCustomizer.GetCustomUserContextAsync(userArgs, AuthorizationEventType.After);
            if (customUserContext != null) {
                return customUserContext;
            }

            // Build user context
            if (foundUser == null || !foundUser.MemberEnabled || foundUser.MemberName.Equals("public", StringComparison.OrdinalIgnoreCase)) {
                return GetPublicUserContext();
            }

            return await _progressiveCache.LoadAsync(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency(
                    [
                        $"{MemberRoleTagInfo.OBJECT_TYPE}|all"
                    ]);
                }

                // get roles
                var roles = (await _tagInfoProvider.Get()
                .Source(x => x.InnerJoin<MemberRoleTagInfo>(nameof(TagInfo.TagID), nameof(MemberRoleTagInfo.MemberRoleTagTagID)))
                .WhereEquals(nameof(MemberRoleTagInfo.MemberRoleTagMemberID), foundUser.MemberID)
                .Columns(nameof(TagInfo.TagName))
                .GetEnumerableTypedResultAsync())
                .Select(x => x.TagName);

                return new UserContext() {
                    IsAuthenticated = true,
                    UserName = foundUser.MemberName,
                    Roles = roles
                };
            }, new CacheSettings(60, "UserContext", foundUser?.MemberName ?? "public"));
        }

        private async Task<WebsiteChannelContext> SiteContextSafe()
        {
            int webChannelId = 0;
            try {
                webChannelId = _websiteChannelContext.WebsiteChannelID;
            } catch (Exception) {

            }

            // if site context isn't available yet return first website...
            return await _progressiveCache.Load(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{WebsiteChannelInfo.OBJECT_TYPE}|all");
                }

                var query = @$"select WebsiteChannelID, ChannelID, ChannelName, ContentLanguageName from CMS_WebsiteChannel
inner join CMS_Channel on ChannelID = WebsiteChannelChannelID
inner join CMS_ContentLanguage on ContentLanguageID = WebsiteChannelPrimaryContentLanguageID
{(webChannelId > 0 ? $"where WebsiteChannelID = {webChannelId}" : "")} order by WebsiteChannelID";

                var reader = await ConnectionHelper.ExecuteReaderAsync(query, [], QueryTypeEnum.SQLQuery, CommandBehavior.Default, CancellationToken.None);
                var ds = DatasetFromReader(reader);
                if (ds.Tables[0].Rows.Count > 0) {
                    var row = ds.Tables[0].Rows[0];
                    return new WebsiteChannelContext((int)row["WebsiteChannelID"], (int)row["ChannelID"], (string)row["ChannelName"], (string)row["ContentLanguageName"]);
                }

                // no web channels
                return new WebsiteChannelContext(0, 0, "", "");
            }, new CacheSettings(1440, "Authorize_KenticoAuthorizationGetSiteContextSafe", webChannelId));
        }

        record WebsiteChannelContext(int WebsiteChannelID, int ChannelID, string ChannelName, string DefaultLanguage);

        /// <summary>
        /// Gets the Current Culture, needed for User Culture Permissions
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetCultureAsync()
        {
            // Can't operate without a context
            if (HttpContext == null) {
                return "en";
            }

            var currentSite = await SiteContextSafe();
            string? culture = null;

            // Handle Preview, during Route Config the Preview isn't available and isn't really needed, so ignore the thrown exception
            bool previewEnabled = GetPreviewEnabled(HttpContext);

            var cultureArgs = new GetCultureEventArgs(
                defaultCulture: currentSite.DefaultLanguage,
                siteName: currentSite.ChannelName,
                request: HttpContext.Request,
                previewEnabled: previewEnabled
            );

            var customCulture = await _authorizationContextCustomizer.GetCustomCultureAsync(cultureArgs, AuthorizationEventType.Before);
            if (!string.IsNullOrWhiteSpace(customCulture)) {
                return customCulture;
            }

            // If Preview is enabled, use the Kentico Preview CultureName
            if (previewEnabled) {
                try {
                    culture = HttpContext.Kentico().Preview().LanguageName;
                } catch (Exception) { }
            }

            // If that fails then use the System.Globalization.CultureInfo
            if (string.IsNullOrWhiteSpace(culture)) {
                try {
                    culture = (await _contentLanguageRetriever.GetContentLanguageOrThrow(System.Globalization.CultureInfo.CurrentCulture.Name)).ContentLanguageName;
                    
                } catch (InvalidOperationException) { 
                
                    try {
                        culture = (await _contentLanguageRetriever.GetContentLanguageOrThrow(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName)).ContentLanguageName;
                    } catch(InvalidOperationException) {

                    }
                }
            }

            cultureArgs.Culture = culture;

            var customCultureAfter = await _authorizationContextCustomizer.GetCustomCultureAsync(cultureArgs, AuthorizationEventType.After);
            if (!string.IsNullOrWhiteSpace(customCultureAfter)) {
                culture = customCultureAfter;
            }

            return culture ?? "en";
        }

        /// <summary>
        /// Gets the Relative Url without the Application Path, and with Url cleaned.
        /// </summary>
        /// <param name="relativeUrl"></param>
        /// <param name="applicationPath"></param>
        /// <returns></returns>
        private async Task<string> GetUrl(string relativeUrl, string applicationPath)
        {
            // Remove Application Path from Relative Url if it exists at the beginning
            if (!string.IsNullOrWhiteSpace(applicationPath) && applicationPath != "/" && relativeUrl.StartsWith(applicationPath, StringComparison.InvariantCultureIgnoreCase)) {
                relativeUrl = relativeUrl[applicationPath.Length..];
            }

            return await GetCleanUrl(relativeUrl);
        }

        /// <summary>
        /// Gets the Url cleaned up with special characters removed
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<string> GetCleanUrl(string url)
        {
            // Remove trailing or double //'s and any url parameters / anchors
            url = "/" + url.Trim("/ ".ToCharArray()).Split('?')[0].Split('#')[0];
            url = HttpUtility.UrlDecode(url);

            // Replace forbidden characters
            // Remove / from the forbidden characters because that is part of the Url, of course.
            var forbiddenChars = await GetUrlForbiddenCharacterInfo();
            url = ReplaceAnyCharInString(url, forbiddenChars.ForbiddenCharacters, forbiddenChars.ForbiddenCharacterReplacement);

            // Escape special url characters
            url = URLHelper.EscapeSpecialCharacters(url);

            return url;
        }

        private async Task<ForbiddenCharactersResults> GetUrlForbiddenCharacterInfo()
        {
            return await _progressiveCache.LoadAsync(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency([$"{SettingsKeyInfo.OBJECT_TYPE}|byname|CMSForbiddenURLCharacters", $"{SettingsKeyInfo.OBJECT_TYPE}|byname|CMSForbiddenCharactersReplacement"]);
                }
                var items = (await _settingsKeyInfoProvider.Get()
                .Columns(nameof(SettingsKeyInfo.KeyName), nameof(SettingsKeyInfo.KeyValue))
                .WhereIn(nameof(SettingsKeyInfo.KeyName), ["CMSForbiddenURLCharacters", "CMSForbiddenCharactersReplacement"])
                .GetEnumerableTypedResultAsync())
                .ToDictionary(key => key.KeyName.ToLowerInvariant(), value => value.KeyValue);

                return new ForbiddenCharactersResults((items.TryGetValue("cmsforbiddenurlcharacters", out var chars) ? chars.ToCharArray() : []), (items.TryGetValue("cmsforbiddencharactersreplacement", out var replacement) ? replacement : ""));

            }, new CacheSettings(1440, "Authorization_GetForbiddenCharacters"));
        }

        record ForbiddenCharactersResults(char[] ForbiddenCharacters, string ForbiddenCharacterReplacement);

        /// <summary>
        /// Replaces any char in the char array with the replace value for the string
        /// </summary>
        /// <param name="value">The string to replace values in</param>
        /// <param name="charsToReplace">The character array of characters to replace</param>
        /// <param name="replaceValue">The value to replace them with</param>
        /// <returns>The cleaned string</returns>
        private static string ReplaceAnyCharInString(string value, char[] charsToReplace, string replaceValue)
        {
            string[] temp = value.Split(charsToReplace, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(replaceValue, temp);
        }

        /// <summary>
        /// Returns the user to check.  Default is to use the HttpContext's User Identity as the username
        /// </summary>
        /// <param name="httpContext">The HttpContext of the request</param>
        /// <returns>The UserInfo, should return the Public user if they are not logged in.</returns>
        public async Task<MemberInfo?> GetCurrentUserInfoAsync(HttpContext httpContext)
        {
            MemberInfo? foundUser = null;
            var site = SiteContextSafe();
            // Create GetUser Event Arguments
            var userArgs = new GetUserEventArgs(httpContext);

            var customUser = await _authorizationContextCustomizer.GetCustomUserAsync(userArgs, AuthorizationEventType.Before);
            if (customUser != null) {
                return customUser;
            }

            // Grab Username and find the user
            string username = !string.IsNullOrWhiteSpace(userArgs.FoundUserName) ? userArgs.FoundUserName : httpContext?.User?.Identity?.Name ?? "public";

            foundUser = await _progressiveCache.LoadAsync(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency("cms.member|byname|" + userArgs.FoundUserName);
                }

                var memberObj = await _memberInfoProvider.Get().WhereEquals(nameof(MemberInfo.MemberName), username).GetEnumerableTypedResultAsync();
                if (!memberObj.Any()) {
                    return null;
                }
                var foundMember = memberObj.First();
                return foundMember.MemberEnabled ? foundMember : null;
            }, new CacheSettings(60, "Authorization_KenticoAuthorizeGetCurrentUser", username));

            userArgs.FoundUser = foundUser;
            customUser = await _authorizationContextCustomizer.GetCustomUserAsync(userArgs, AuthorizationEventType.Before);
            if (customUser != null) {
                foundUser = customUser;
            }

            return foundUser;
        }


        private async Task<IWebPageFieldsSource?> GetPageFromUrlPathAndChannel(string relativeUrl, int websiteChannelID)
        {
            var langIdToName = await GetContentLanguageIDToName();

            var previewEnabled = HttpContext != null && GetPreviewEnabled(HttpContext);
            var reader = await _progressiveCache.LoadAsync(async cs => {

                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{WebPageUrlPathInfo.OBJECT_TYPE}|all");
                }

                return await _webPageUrlPathInfoProvider.Get()
                .Source(x => x.InnerJoin<WebPageItemInfo>(nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID), nameof(WebPageItemInfo.WebPageItemID)))
                .Source(x => x.InnerJoin<ContentItemInfo>(nameof(WebPageItemInfo.WebPageItemContentItemID), nameof(ContentItemInfo.ContentItemID)))
                .Source(x => x.InnerJoin<DataClassInfo>(nameof(ContentItemInfo.ContentItemContentTypeID), nameof(DataClassInfo.ClassID)))
                .WhereEquals(nameof(WebPageUrlPathInfo.WebPageUrlPath), relativeUrl.TrimStart('~').TrimStart('/'))
                .WhereEquals(nameof(WebPageUrlPathInfo.WebPageUrlPathWebsiteChannelID), websiteChannelID)
                .Columns(nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID), nameof(WebPageUrlPathInfo.WebPageUrlPathWebsiteChannelID), nameof(DataClassInfo.ClassName), nameof(WebPageUrlPathInfo.WebPageUrlPathContentLanguageID))
                .ExecuteReaderAsync();
            }, new CacheSettings(30, "Authorization_GetPageFromUrlPathAndChannel", relativeUrl, websiteChannelID));

            // Convert to Dataset / tables
            var ds = DatasetFromReader(reader);

            if (ds.Tables[0].Rows.Count > 0) {
                var row = ds.Tables[0].Rows[0];
                if (langIdToName.TryGetValue((int)row[nameof(WebPageUrlPathInfo.WebPageUrlPathContentLanguageID)], out var langName)) {
                    return await GetWebPageFieldSource((int)row[nameof(WebPageUrlPathInfo.WebPageUrlPathWebPageItemID)], (string)row[nameof(DataClassInfo.ClassName)], langName, (int)row[nameof(WebPageUrlPathInfo.WebPageUrlPathWebsiteChannelID)], previewEnabled);
                }
            }

            return null;
        }

        private static DataSet DatasetFromReader(IDataReader? reader)
        {
            var ds = new DataSet();
            if (reader == null) {
                ds.Tables.Add(new DataTable());
            } else {
                // read each data result into a datatable
                do {
                    var table = new DataTable();
                    table.Load(reader);
                    ds.Tables.Add(table);
                } while (!reader.IsClosed);
            }
            return ds;
        }

        private async Task<Dictionary<int, string>> GetContentLanguageIDToName()
        {
            return await _progressiveCache.LoadAsync(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");
                }
                return (await _contentLanguageInfoProvider.Get()
                .Columns(nameof(ContentLanguageInfo.ContentLanguageID), nameof(ContentLanguageInfo.ContentLanguageName))
                .GetEnumerableTypedResultAsync())
                .ToDictionary(key => key.ContentLanguageID, value => value.ContentLanguageName);

            }, new CacheSettings(1440, "Authorization_ContentLanguageIDToName"));
        }

        private async Task<IWebPageFieldsSource?> GetWebPageFieldSource(int webPageItemID, string className, string languageName, int websiteChannelId, bool previewEnabled)
        {
            return await _progressiveCache.LoadAsync(async cs => {
                if (cs.Cached) {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"webpageitem|byid|{webPageItemID}");
                }

                // convert web page data context to the web page info
                var queryBuilder = new ContentItemQueryBuilder();
                queryBuilder.ForContentType(className, query => query
                    .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemID), webPageItemID))
                    .ForWebsiteChannels([websiteChannelId])
                    ).InLanguage(languageName, false);

                return (await _contentQueryExecutor.GetMappedWebPageResult<IWebPageFieldsSource>(queryBuilder, options: new ContentQueryExecutionOptions() {
                    ForPreview = previewEnabled,
                    IncludeSecuredItems = true
                })).FirstOrDefault();
            }, new CacheSettings(30, "Authorization_GetCurrentPageAsync", webPageItemID, className, languageName, websiteChannelId, previewEnabled));
        }

    }
}
