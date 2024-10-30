using CMS.Membership;
using Microsoft.AspNetCore.Http;

namespace XperienceCommunity.Authorization.Events
{
    public class GetUserEventArgs
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
    }
}
