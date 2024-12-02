using CMS.ContentEngine;
using CMS.Websites;
using System.Threading.Tasks;

namespace XperienceCommunity.Authorization
{
    public interface IAuthorization
    {
        /// <summary>
        /// Returns whether or not the request is authorized based on the given information.
        /// </summary>
        /// <param name="user">The User requesting access</param>
        /// <param name="authConfig">The Authorization configuration information</param>
        /// <param name="currentPage">The current page being authorized, if any</param>
        /// <param name="pageTemplateIdentifier">The page builder template identifier, if any</param>
        /// <returns>If they are authorized or not.</returns>
        public Task<bool> IsAuthorizedAsync(UserContext user, AuthorizationConfiguration authConfig, IWebPageFieldsSource? currentPage = null, string? pageTemplateIdentifier = default);
    }
}
