using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorization.Kentico.MVC.Events
{
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
