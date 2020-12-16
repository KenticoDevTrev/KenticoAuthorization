using CMS.Base;


namespace Authorization.Kentico.MVC.Events
{
    public class GetPageEventHandler : AdvancedHandler<GetPageEventHandler, GetPageEventArgs>
    {
        public GetPageEventHandler()
        {

        }

        public GetPageEventHandler StartEvent(GetPageEventArgs PageArgs)
        {
            return base.StartEvent(PageArgs);
        }

        public void FinishEvent()
        {
            base.Finish();
        }
    }
}
