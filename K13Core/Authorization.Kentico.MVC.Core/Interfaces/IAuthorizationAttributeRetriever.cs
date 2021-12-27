using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Kentico.Interfaces
{
    internal interface IAuthorizationAttributeRetriever
    {
        IEnumerable<RegisterPageBuilderAuthorizationAttribute> GetPageBuilderAuthorizationAttributes();
    }
}
