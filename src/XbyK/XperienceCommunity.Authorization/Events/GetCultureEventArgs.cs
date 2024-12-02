using Microsoft.AspNetCore.Http;

namespace XperienceCommunity.Authorization.Events
{
    public class GetCultureEventArgs(string defaultCulture, string siteName, HttpRequest request, bool previewEnabled)
    {
        public string? Culture { get; set; }

        /// <summary>
        /// The Site's Default culture, based on the Current Site
        /// </summary>
        public string DefaultCulture { get; set; } = defaultCulture;

        /// <summary>
        /// The Site Code Name of the current site
        /// </summary>
        public string SiteName { get; set; } = siteName;

        /// <summary>
        /// The HttpRequest
        /// </summary>
        public HttpRequest Request { get; set; } = request;

        /// <summary>
        /// True if Kentico's Preview is enable, if true the culture will be set by the PreviewEnabled after the "Before" event.
        /// </summary>
        public bool PreviewEnabled { get; set; } = previewEnabled;
    }
}
