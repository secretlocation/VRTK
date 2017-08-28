namespace VRTK
{
    using UnityEngine;

    public class VRTK_OrCompoundInputAction : VRTK_BaseInputAction
    {
        [SerializeField]
        protected VRTK_BaseInputAction[] inputActions = new VRTK_BaseInputAction[0];

        protected virtual void OnEnable()
        {
            for (int i = 0; i < inputActions.Length; i++)
            {
                inputActions[i].ActionStarted += AnyActionStarted;
                inputActions[i].ActionEnded += AnyActionEnded;
            }
        }

        protected virtual void OnDisable()
        {
            for (int i = 0; i < inputActions.Length; i++)
            {
                inputActions[i].ActionStarted -= AnyActionStarted;
                inputActions[i].ActionEnded -= AnyActionEnded;
            }
        }

        protected virtual void AnyActionStarted(object sender)
        {
            OnActionStarted();
        }

        protected virtual void AnyActionEnded(object sender)
        {
            OnActionEnded();
        }
    }
}