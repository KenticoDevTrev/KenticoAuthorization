namespace XperienceCommunity.Authorization.Internal
{
    public class PageBuilderAuthorizationConfiguration
    {
        public IEnumerable<string> PageTemplateIdentifiers { get; set; } = [];
        public bool TemplateIdentifiersArePrefix { get; set; } = false;
        public IEnumerable<string> PageClassNames { get; set; } = [];

        public bool Applies(string pageTemplateIdentifier, string pageClassName)
        {
            return
                PageTemplateIdentifiers.Any(x => pageTemplateIdentifier.StartsWith(x, StringComparison.OrdinalIgnoreCase) && (TemplateIdentifiersArePrefix || x.Length == pageTemplateIdentifier.Length))
                ||
                PageClassNames.Contains(pageClassName, StringComparer.InvariantCultureIgnoreCase);
        }

        // Helper static methods
        public static PageBuilderAuthorizationConfiguration ByTemplate(string templateIdentifier, bool templateIdentifierIsPrefix = false)
        {
            return new PageBuilderAuthorizationConfiguration()
            {
                PageTemplateIdentifiers = [templateIdentifier],
                TemplateIdentifiersArePrefix = templateIdentifierIsPrefix
            };
        }
        public static PageBuilderAuthorizationConfiguration ByPageType(string pageType)
        {
            return new PageBuilderAuthorizationConfiguration()
            {
                PageClassNames = [pageType]
            };
        }
    }
}
