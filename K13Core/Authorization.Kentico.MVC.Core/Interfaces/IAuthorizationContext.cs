using CMS.DocumentEngine;
using System.Threading.Tasks;

namespace Authorization.Kentico.Interfaces
{
    public interface IAuthorizationContext
    {
        /// <summary>
        /// Gets the current User Context
        /// </summary>
        /// <returns></returns>
        Task<UserContext> GetCurrentUserAsync();

        /// <summary>
        /// Gets the current TreeNode Page.
        /// </summary>
        /// <returns></returns>
        Task<TreeNode> GetCurrentPageAsync();

        /// <summary>
        /// Gets the Page Template Identifier (if there is one) on the page.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        Task<string> GetCurrentPageTemplateIdentifierAsync(TreeNode page);
    }
}
