namespace VRTK
{
    using UnityEngine;

    /// <summary>
    /// Event Payload
    /// </summary>
    /// <param name="sender">The object emitting the event.</param>
    public delegate void InputActionEventHandler(object sender);

    public abstract class VRTK_BaseInputAction : MonoBehaviour
    {
        /// <summary>
        /// Emitted when the input action begins.
        /// </summary>
        public event InputActionEventHandler ActionStarted;
        /// <summary>
        /// Emitted when the input action ends.
        /// </summary>
        public event InputActionEventHandler ActionEnded;

        public virtual void OnActionStarted()
        {
            if (ActionStarted != null)
            {
                ActionStarted(this);
            }
        }

        public virtual void OnActionEnded()
        {
            if (ActionEnded != null)
            {
                ActionEnded(this);
            }
        }
    }
}