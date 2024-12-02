using CMS.Membership;
using Microsoft.AspNetCore.Http;

namespace XperienceCommunity.Authorization.Events
{
    public class GetUserEventArgs(HttpContext httpContext)
    {

        /// <summary>
        /// The User that is found, this is what will be returned from the GetCurrentUser function, set this.
        /// </summary>
        public MemberInfo? FoundUser { get; set; } = null;

        /// <summary>
        /// The Username for the user, you can optionally set this instead of the Found User and the Authorization will automatically look up the user and handle if they are enabled or not.
        /// </summary>
        public string? FoundUserName { get; set; } = null;

        /// <summary>
        /// The HttpContext of the request
        /// </summary>
        public HttpContext HttpContext { get; set; } = httpContext;
    }
}
