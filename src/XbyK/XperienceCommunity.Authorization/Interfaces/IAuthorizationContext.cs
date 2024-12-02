using CMS.Websites;
using System.Threading.Tasks;

namespace XperienceCommunity.Authorization
{
    public interface IAuthorizationContext
    {
        /// <summary>
        /// Gets the current User Context
        /// </summary>
        /// <returns></returns>
        Task<UserContext> GetCurrentUserAsync();

        /// <summary>
        /// Gets the current Content Item/Page.
        /// </summary>
        /// <returns></returns>
        Task<IWebPageFieldsSource?> GetCurrentPageAsync();

        /// <summary>
        /// Gets the Page Template Identifier (if there is one) on the page.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        Task<string?> GetCurrentPageTemplateIdentifierAsync(IWebPageFieldsSource page);
    }
}
