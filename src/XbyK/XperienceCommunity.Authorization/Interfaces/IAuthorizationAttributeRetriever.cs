using System;
using System.Collections.Generic;
using System.Text;

namespace XperienceCommunity.Authorization
{
    internal interface IAuthorizationAttributeRetriever
    {
        IEnumerable<RegisterPageBuilderAuthorizationAttribute> GetPageBuilderAuthorizationAttributes();
    }
}
