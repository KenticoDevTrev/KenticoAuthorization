using System;

namespace Authorization.Kentico.MVC.Events
{
    [Obsolete("These events are no longer used, please instead implement your own IAuthorizationContextCustomizer and register it in the service collection.")]
    public static class AuthorizeEvents
    {
        public static GetPageEventHandler GetPage;

        public static GetUserEventHandler GetUser;

        public static GetCultureEventHandler GetCulture;

        public static AuthorizingEventHandler Authorizing;

        static AuthorizeEvents()
        {
            GetPage = new GetPageEventHandler()
            {
                Name = "AuthorizeEvents.GetPage"
            };

            GetUser = new GetUserEventHandler()
            {
                Name = "AuthorizeEvents.GetUser"
            };

            GetCulture = new GetCultureEventHandler()
            {
                Name = "AuthorizeEvents.GetCulture"
            };
            Authorizing = new AuthorizingEventHandler()
            {
                Name = "AuthorizeEvents.Authorizing"
            };
        }
    }
}
