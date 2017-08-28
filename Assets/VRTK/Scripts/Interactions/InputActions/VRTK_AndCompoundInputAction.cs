namespace VRTK
{
    using UnityEngine;
    using System.Collections.Generic;

    public class VRTK_AndCompoundInputAction : VRTK_BaseInputAction
    {
        [SerializeField]
        protected List<VRTK_BaseInputAction> inputActions = new List<VRTK_BaseInputAction>();

        protected bool[] activeAction = new bool[0];

        protected virtual void OnEnable()
        {
            activeAction = new bool[inputActions.Count];
            for (int i = 0; i < inputActions.Count; i++)
            {
                inputActions[i].ActionStarted += AllActionStarted;
                inputActions[i].ActionEnded += AllActionEnded;
            }
        }

        protected virtual void OnDisable()
        {
            for (int i = 0; i < inputActions.Count; i++)
            {
                inputActions[i].ActionStarted -= AllActionStarted;
                inputActions[i].ActionEnded -= AllActionEnded;
            }
        }

        protected virtual void AllActionStarted(object sender)
        {
            activeAction[inputActions.IndexOf((VRTK_BaseInputAction)sender)] = true;
            if (AllActionsOn())
            {
                OnActionStarted();
            }
        }

        protected virtual void AllActionEnded(object sender)
        {
            activeAction[inputActions.IndexOf((VRTK_BaseInputAction)sender)] = false;
            OnActionEnded();
        }

        protected virtual bool AllActionsOn()
        {
            for (int i = 0; i < activeAction.Length; i++)
            {
                if (!activeAction[i])
                {
                    return false;
                }
            }
            return (activeAction.Length > 0);
        }
    }
}