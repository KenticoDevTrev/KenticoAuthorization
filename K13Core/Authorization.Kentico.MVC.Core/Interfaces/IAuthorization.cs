using CMS.DocumentEngine;
using System.Threading.Tasks;

namespace Authorization.Kentico.Interfaces
{
    public interface IAuthorization
    {
        public Task<bool> IsAuthorizedAsync(UserContext user, AuthorizationConfiguration authConfig, TreeNode currentPage = null, string pageTemplateIdentifier = default);
    }
}
