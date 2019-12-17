using CMS.Base;
using CMS.DocumentEngine;
using System;
using System.Web;
namespace Authorization.Kentico.MVC.Events
{
    public class GetPageEventArgs : CMSEventArgs
    {
        /// <summary>
        /// The Page that is found, this is what will be returned from the GetPage function, set this, must have the NodeACLID field set as that is what is used in ACL lookups
        /// </summary>
        public TreeNode FoundPage { get; set; }

        /// <summary>
        /// The Request's Relative Url (no query strings), cleaned to be proper lookup format
        /// </summary>
        public string RelativeUrl { get; set; }

        /// <summary>
        /// The current SiteName
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// The full HttpRequest object
        /// </summary>
        public HttpContextBase HttpContext { get; set; }

        /// <summary>
        /// If an exception occurred between the Before and After (while looking up), this is the exception. Can be used for custom logging.
        /// </summary>
        public Exception ExceptionOnLookup { get; set; }

        /// <summary>
        /// The Request's Culture
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The Site's default culture
        /// </summary>
        public string DefaultCulture { get; set; }
    }
}
