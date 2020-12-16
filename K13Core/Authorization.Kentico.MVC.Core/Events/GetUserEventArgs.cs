using CMS.Base;
using CMS.DocumentEngine;
using CMS.Membership;
using Microsoft.AspNetCore.Http;
using System;
using System.Web;
namespace Authorization.Kentico.MVC.Events
{
    public class GetUserEventArgs : CMSEventArgs
    {
        /// <summary>
        /// The User that is found, this is what will be returned from the GetCurrentUser function, set this.
        /// </summary>
        public UserInfo FoundUser { get; set; }

        /// <summary>
        /// The Username for te user, you can optionally set this instead of the Found User and the Authorization will automatically look up the user and handle if they are enabled or not.
        /// </summary>
        public string FoundUserName { get; set; }

        /// <summary>
        /// The HttpContext of the request
        /// </summary>
        public HttpContext HttpContext { get; set; }

        /// <summary>
        /// If an exception occurred between the Before and After (while looking up), this is the exception. Can be used for custom logging.
        /// </summary>
        public Exception ExceptionOnLookup { get; set; }
    }
}
