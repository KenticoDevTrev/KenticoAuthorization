using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authorization.Kentico.MVC.Events
{
    public static class AuthorizeEvents
    {
        public static GetUserEventHandler GetUser;

        public static AuthorizingEventHandler Authorizing;

        static AuthorizeEvents()
        {
            GetUser = new GetUserEventHandler()
            {
                Name = "AuthorizeEvents.GetUser"
            };
            Authorizing = new AuthorizingEventHandler()
            {
                Name = "AuthorizeEvents.Authorizing"
            };
        }
    }
}
