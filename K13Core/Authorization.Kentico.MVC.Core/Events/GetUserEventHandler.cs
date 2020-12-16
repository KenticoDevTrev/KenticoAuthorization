using CMS.Base;


namespace Authorization.Kentico.MVC.Events
{
    public class GetUserEventHandler : AdvancedHandler<GetUserEventHandler, GetUserEventArgs>
    {
        public GetUserEventHandler()
        {

        }

        public GetUserEventHandler StartEvent(GetUserEventArgs UserArgs)
        {
            return base.StartEvent(UserArgs);
        }

        public void FinishEvent()
        {
            base.Finish();
        }
    }
}
