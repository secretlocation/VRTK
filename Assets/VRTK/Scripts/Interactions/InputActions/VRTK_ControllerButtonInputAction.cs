namespace VRTK
{
    public class VRTK_ControllerButtonInputAction : VRTK_BaseInputAction
    {
        public SDK_BaseController.ControllerType controllerType = SDK_BaseController.ControllerType.Undefined;
        public VRTK_ControllerEvents.ButtonAlias button = VRTK_ControllerEvents.ButtonAlias.Undefined;
        public VRTK_ControllerEvents controllerEvents;

        protected virtual void OnEnable()
        {
            controllerEvents = (controllerEvents != null ? controllerEvents : GetComponentInParent<VRTK_ControllerEvents>());
            if (controllerEvents != null)
            {
                controllerEvents.SubscribeToButtonAliasEvent(button, true, ButtonActionStart);
                controllerEvents.SubscribeToButtonAliasEvent(button, false, ButtonActionEnd);
            }
        }

        protected virtual void OnDisable()
        {
            if (controllerEvents != null)
            {
                controllerEvents.UnsubscribeToButtonAliasEvent(button, true, ButtonActionStart);
                controllerEvents.UnsubscribeToButtonAliasEvent(button, false, ButtonActionEnd);
            }
        }

        protected virtual void ButtonActionStart(object sender, ControllerInteractionEventArgs e)
        {
            if (controllerType == SDK_BaseController.ControllerType.Undefined || VRTK_DeviceFinder.GetCurrentControllerType(e.controllerReference) == controllerType)
            {
                OnActionStarted();
            }
        }

        protected virtual void ButtonActionEnd(object sender, ControllerInteractionEventArgs e)
        {
            if (controllerType == SDK_BaseController.ControllerType.Undefined || VRTK_DeviceFinder.GetCurrentControllerType(e.controllerReference) == controllerType)
            {
                OnActionEnded();
            }
        }
    }
}