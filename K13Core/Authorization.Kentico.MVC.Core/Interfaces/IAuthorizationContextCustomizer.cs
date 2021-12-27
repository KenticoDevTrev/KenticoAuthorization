using Authorization.Kentico.MVC.Events;
using CMS.DocumentEngine;
using CMS.Membership;
using System.Threading.Tasks;

namespace Authorization.Kentico.Interfaces
{
    /// <summary>
    /// This allows you to modify logic before and after the default logic.  Rarely should you need to adjust this unless
    /// you need to register custom Page finding logic (such as MVC Routing), and culture logic / user logic that doesn't use the
    /// httpContext.
    /// 
    /// Most likely you should be using the IAuthorizationContext and overwriting if you are doing things like testing.
    /// </summary>
    public interface IAuthorizationContextCustomizer
    {
        /// <summary>
        /// Allows you to modify the culture logic both before the default logic and after.
        /// </summary>
        /// <param name="cultureArgs">Arguments used to help you determine the culture, and if the event is "after" then what the default logic found</param>
        /// <param name="eventType">If the Event is the Beginning (allows you to skip defalt logic by setting a value), or After (allows you to overwrite the value found by default logic)</param>
        /// <returns>The Culture you wish to have, if null it will ignore this logic</returns>
        public Task<string> GetCustomCultureAsync(GetCultureEventArgs cultureArgs, AuthorizationEventType eventType);

        /// <summary>
        /// Allows you to modify the Current Page Retrieval, in case you are using something other than pagebuilder.
        /// </summary>
        /// <param name="pageArgs">Arguments used to help you determine the current page, and if the event is "after" then what the default logic found</param>
        /// <param name="eventType">If the Event is the Beginning (allows you to skip defalt logic by setting a value), or After (allows you to overwrite the value found by default logic)</param>
        /// <returns>The Tree you wish to have, if null it will ignore this logic</returns>
        public Task<TreeNode> GetCustomPageAsync(GetPageEventArgs pageArgs, AuthorizationEventType eventType);

        /// <summary>
        /// Allows you to modify the current user used for authorization, in case you want to use something other than the default.
        /// </summary>
        /// <param name="userArgs">Arguments used to help you determine the current user, and if the event is "after" then what the default logic found</param>
        /// <param name="eventType">If the Event is the Beginning (allows you to skip defalt logic by setting a value), or After (allows you to overwrite the value found by default logic)</param>
        /// <returns>The user you wish to use, if null it will ignore this logic</returns>
        public Task<UserInfo> GetCustomUserAsync(GetUserEventArgs userArgs, AuthorizationEventType eventType);

        /// <summary>
        /// Allows you to modify the current user context used for authorization, in case you want to use something other than the default.
        /// </summary>
        /// <param name="userArgs">Arguments used to help you determine the current user, and if the event is "after" then what the default logic found</param>
        /// <param name="eventType">If the Event is the Beginning (allows you to skip defalt logic by setting a value), or After (allows you to overwrite the value found by default logic)</param>
        /// <returns>The user you wish to use, if null it will ignore this logic</returns>
        public Task<UserContext> GetCustomUserContextAsync(GetUserEventArgs userArgs, AuthorizationEventType eventType);
    }
}
