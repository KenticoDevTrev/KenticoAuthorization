using CMS.Base;
using CMS.DocumentEngine;
using CMS.Membership;
using System.Web;

namespace Authorization.Kentico.MVC.Events
{
    public class AuthorizingEventArgs : CMSEventArgs
    {
        /// <summary>
        /// The Current User's context
        /// </summary>
        public UserContext CurrentUser { get; set; }

        /// <summary>
        /// The current Authorization rule being checked
        /// </summary>
        public AuthorizationConfiguration AuthConfiguration { get; set; }

        /// <summary>
        /// The Current Page
        /// </summary>
        public TreeNode FoundPage { get; set; }
        
        /// <summary>
        /// The page template if arrived through a page template
        /// </summary>
        public string PageTemplateName { get; set; }

        /// <summary>
        /// If true, then the default Validation Logic will not run.
        /// </summary>
        public bool SkipDefaultValidation { get; set; } = false;

        /// <summary>
        /// If the request is Authorized or not.
        /// </summary>
        public bool Authorized { get; set; }

    }
}
