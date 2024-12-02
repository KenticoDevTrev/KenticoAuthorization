using CMS.Websites;
using Microsoft.AspNetCore.Http;

namespace XperienceCommunity.Authorization.Events
{
    public class GetPageEventArgs(string relativeUrl, string siteName, HttpContext httpContext, string culture, string defaultCulture)
    {

        /// <summary>
        /// The Page that is found, this is what will be returned from the GetPage function, set this
        /// </summary>
        public IWebPageFieldsSource? FoundPage { get; set; } = null;

        /// <summary>
        /// The Request's Relative Url (no query strings), cleaned to be proper lookup format
        /// </summary>
        public string RelativeUrl { get; set; } = relativeUrl;

        /// <summary>
        /// The current SiteName
        /// </summary>
        public string SiteName { get; set; } = siteName;

        /// <summary>
        /// The full HttpRequest object
        /// </summary>
        public HttpContext HttpContext { get; set; } = httpContext;

        /// <summary>
        /// If an exception occurred between the Before and After (while looking up), this is the exception. Can be used for custom logging.
        /// </summary>
        public Exception? ExceptionOnLookup { get; set; } = null;

        /// <summary>
        /// The Request's Culture
        /// </summary>
        public string Culture { get; set; } = culture;

        /// <summary>
        /// The Site's default culture
        /// </summary>
        public string DefaultCulture { get; set; } = defaultCulture;
    }
}
