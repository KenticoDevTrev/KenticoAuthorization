using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Authorization.Kentico
{
    public class PageBuilderConfiguration
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
        public static PageBuilderConfiguration ByTemplate(string templateIdentifier, bool templateIdentifierIsPrefix = false)
        {
            return new PageBuilderConfiguration()
            {
                PageTemplateIdentifiers = new string[] { templateIdentifier },
                TemplateIdentifiersArePrefix = templateIdentifierIsPrefix
            };
        }
        public static PageBuilderConfiguration ByPageType(string pageType)
        {
            return new PageBuilderConfiguration()
            {
                PageClassNames = new string[] { pageType }
            };
        }
    }
}
