using CMS.Base;

namespace Authorization.Kentico.MVC.Events
{
    public class AuthorizingEventHandler : AdvancedHandler<AuthorizingEventHandler, AuthorizingEventArgs>
    {
        public AuthorizingEventHandler()
        {

        }

        public AuthorizingEventHandler StartEvent(AuthorizingEventArgs CultureArgs)
        {
            return base.StartEvent(CultureArgs);
        }

        public void FinishEvent()
        {
            base.Finish();
        }
    }
}
