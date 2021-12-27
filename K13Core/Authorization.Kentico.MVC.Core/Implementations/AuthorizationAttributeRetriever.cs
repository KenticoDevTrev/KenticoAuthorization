using Authorization.Kentico.Interfaces;
using CMS.Core;
using System.Collections.Generic;
using System.Reflection;

namespace Authorization.Kentico.Implementations
{
    public class AuthorizationAttributeRetriever : IAuthorizationAttributeRetriever
    {
        public AuthorizationAttributeRetriever()
        {
            List<RegisterPageBuilderAuthorizationAttribute> attributes = new List<RegisterPageBuilderAuthorizationAttribute>();
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
