using CMS.Core;
using System.Collections.Generic;
using System.Reflection;

namespace XperienceCommunity.Authorization.Implementations
{
    public class AuthorizationAttributeRetriever : IAuthorizationAttributeRetriever
    {
        public AuthorizationAttributeRetriever()
        {
            var attributes = new List<RegisterPageBuilderAuthorizationAttribute>();
            // Find filters that apply
            foreach (var assembly in AssemblyDiscoveryHelper.GetAssemblies(true))
            {
                attributes.AddRange(assembly.GetCustomAttributes<RegisterPageBuilderAuthorizationAttribute>());
            }
            Attributes = attributes;
        }

        public List<RegisterPageBuilderAuthorizationAttribute> Attributes { get; private set; }

        public IEnumerable<RegisterPageBuilderAuthorizationAttribute> GetPageBuilderAuthorizationAttributes()
        {
            return Attributes;
            
        }
    }
}
