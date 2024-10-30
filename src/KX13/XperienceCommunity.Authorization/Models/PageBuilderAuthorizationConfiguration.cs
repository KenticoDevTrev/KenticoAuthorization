using System;
using System.Collections.Generic;
using System.Linq;

namespace XperienceCommunity.Authorization.Internal
{
    public class PageBuilderAuthorizationConfiguration
    {
        public IEnumerable<string> PageTemplateIdentifiers { get; set; } = Array.Empty<string>();
        public bool TemplateIdentifiersArePrefix { get; set; } = false;
        public IEnumerable<string> PageClassNames { get; set; } = Array.Empty<string>();

        public bool Applies(string pageTemplateIdentifier, string pageClassName)
        {
            return
                PageTemplateIdentifiers.Any(x => x.StartsWith(pageTemplateIdentifier, StringComparison.OrdinalIgnoreCase) && (TemplateIdentifiersArePrefix || x.Length == pageTemplateIdentifier.Length))
                ||
                PageClassNames.Contains(pageClassName, StringComparer.InvariantCultureIgnoreCase);
        }

        // Helper static methods
        public static PageBuilderAuthorizationConfiguration ByTemplate(string templateIdentifier, bool templateIdentifierIsPrefix = false)
        {
            return new PageBuilderAuthorizationConfiguration()
            {
                PageTemplateIdentifiers = new string[] { templateIdentifier },
                TemplateIdentifiersArePrefix = templateIdentifierIsPrefix
            };
        }
        public static PageBuilderAuthorizationConfiguration ByPageType(string pageType)
        {
            return new PageBuilderAuthorizationConfiguration()
            {
                PageClassNames = new string[] { pageType }
            };
        }
    }
}
