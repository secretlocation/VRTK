// Artificial Door|ArtificialControllables|120020
namespace VRTK.Controllables.ArtificialBased
{
    using UnityEngine;
    using VRTK.GrabAttachMechanics;
    using VRTK.SecondaryControllerGrabActions;

    /// <summary>
    /// A artificially simulated openable door.
    /// </summary>
    /// <remarks>
    /// **Required Components:**
    ///  * `Collider` - A Unity Collider to determine when an interaction has occured. Can be a compound collider set in child GameObjects. Will be automatically added at runtime.
    /// 
    /// **Script Usage:**
    ///  * Place the `VRTK_ArtificialDoor` script onto the GameObject that is to become the door.
    ///  * Create a nested GameObject under the door GameObject and position it where the hinge should operate.
    ///  * Apply the nested hinge GameObject to the `Hinge Point` parameter on the Artificial Door script.
    ///
    ///   > At runtime, the Artificial Door script GameObject will become the child of a runtime created GameObject that determines the rotational offset for the door.
    /// </remarks>
    [AddComponentMenu("VRTK/Scripts/Interactables/Controllables/Artificial/VRTK_ArtificialDoor")]
    public class VRTK_ArtificialDoor : VRTK_BaseControllable
    {

        [Header("Hinge Settings")]

        [Tooltip("A Transform that denotes the position where the door will rotate around.")]
        public Transform hingePoint;
        [Tooltip("The minimum angle the door can swing to, will be translated into a negative angle.")]
        [Range(-180f, 180f)]
        public float minimumAngle = -180f;
        [Tooltip("The maximum angle the door can swing to, will be considered a positive angle.")]
        [Range(-180f, 180f)]
        public float maximumAngle = 180f;
        [Tooltip("The angle at which the door rotation can be within the minimum or maximum angle before the minimum or maximum angles are considered reached.")]
        public float minMaxThresholdAngle = 1f;
        [Tooltip("The angle at which will be considered as the resting position of the door.")]
        [SerializeField]
        protected float restingAngle = 0f;
        [Tooltip("The threshold angle from the `Resting Angle` that the current angle of the door needs to be within to snap the door back to the `Resting Angle`")]
        public float forceShutThresholdAngle = 1f;
        [Tooltip("If this is checked then the door will not be able to be moved.")]
        public bool isLocked = false;

        [Header("Interaction Settings")]

        [Tooltip("The simulated friction when the door is grabbed.")]
        public float grabbedFriction = 1f;
        [Tooltip("The simulated friction when the door is released.")]
        public float releasedFriction = 1f;
        [Tooltip("A collection of GameObjects that will be used as the valid collisions to determine if the door can be interacted with.")]
        public GameObject[] onlyInteractWith = new GameObject[0];

        protected VRTK_InteractableObject controlInteractableObject;
        protected VRTK_RotateTransformGrabAttach grabMechanic;
        protected VRTK_SwapControllerGrabAction secondaryAction;
        protected bool createInteractableObject;
        protected GameObject parentOffset;
        protected bool createParentOffset;
        protected GameObject doorContainer;
        protected bool rotationReset;

        /// <summary>
        /// The GetValue method returns the current rotation value of the door.
        /// </summary>
        /// <returns>The actual rotation of the door.</returns>
        public override float GetValue()
        {
            float currentValue = doorContainer.transform.localEulerAngles[(int)operateAxis];
            return (currentValue > 180f ? currentValue - 360f : currentValue);
        }

        /// <summary>
        /// The GetNormalizedValue method returns the current rotation value of the door normalized between `0f` and `1f`.
        /// </summary>
        /// <returns>The normalized rotation of the door.</returns>
        public override float GetNormalizedValue()
        {
            return VRTK_SharedMethods.NormalizeValue(GetValue(), minimumAngle, maximumAngle);
        }

        /// <summary>
        /// The GetContainer method returns the GameObject that is generated to hold the door control.
        /// </summary>
        /// <returns>The GameObject container of the door control.</returns>
        public virtual GameObject GetContainer()
        {
            return doorContainer;
        }

        /// <summary>
        /// The SetRestingAngle method sets the angle that is considered the door resting state.
        /// </summary>
        /// <param name="newAngle">The angle in which to set as the resting state.</param>
        /// <param name="forceSet">If `true` then the angle will always be set even if the door is currently outside of the resting threshold.</param>
        public virtual void SetRestingAngle(float newAngle, bool forceSet)
        {
            if (grabMechanic != null)
            {
                newAngle = Mathf.Clamp(newAngle, minimumAngle, maximumAngle);
                float currentValue = GetValue();
                if (forceSet || IsResting())
                {
                    doorContainer.transform.localEulerAngles = AxisDirection() * newAngle;
                }
                restingAngle = newAngle;
                grabMechanic.SetRotation(restingAngle);
            }
        }

        /// <summary>
        /// The IsResting method returns whether the door is at the resting angle or within the resting angle threshold.
        /// </summary>
        /// <returns>Returns `true` if the door is at the resting angle or within the resting angle threshold.</returns>
        public virtual bool IsResting()
        {
            float currentValue = GetValue();
            return ((currentValue <= restingAngle + minMaxThresholdAngle && currentValue >= restingAngle - minMaxThresholdAngle));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            doorContainer = gameObject;
            rotationReset = false;
            SetupParentOffset();
            SetupInteractableObject();
            SetRestingAngle(restingAngle, true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ManageInteractableListeners(false);
            ManageGrabbableListeners(false);
            if (createInteractableObject)
            {
                Destroy(controlInteractableObject);
            }
            RemoveParentOffset();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (hingePoint != null)
            {
                Bounds doorBounds = VRTK_SharedMethods.GetBounds(transform, transform);
                Vector3 limits = transform.rotation * ((AxisDirection() * doorBounds.size[(int)operateAxis]) * 0.53f);
                Vector3 hingeStart = hingePoint.transform.position - limits;
                Vector3 hingeEnd = hingePoint.transform.position + limits;
                Gizmos.DrawLine(hingeStart, hingeEnd);
                Gizmos.DrawSphere(hingeStart, 0.01f);
                Gizmos.DrawSphere(hingeEnd, 0.01f);
            }
        }

        protected virtual void SetupParentOffset()
        {
            createParentOffset = false;
            if (hingePoint != null)
            {
                hingePoint.transform.SetParent(transform.parent);
                Vector3 storedScale = transform.localScale;
                parentOffset = new GameObject(VRTK_SharedMethods.GenerateVRTKObjectName(true, name, "Controllable", "ArtificialBased", "DoorContainer"));
                parentOffset.transform.SetParent(transform.parent);
                parentOffset.transform.localPosition = transform.localPosition;
                parentOffset.transform.localRotation = transform.localRotation;
                parentOffset.transform.localScale = Vector3.one;
                transform.SetParent(parentOffset.transform);
                parentOffset.transform.localPosition = hingePoint.localPosition;
                transform.localPosition = -hingePoint.localPosition;
                transform.localScale = storedScale;
                hingePoint.transform.SetParent(transform);
                doorContainer = parentOffset;
                createParentOffset = true;
            }
        }

        protected virtual void RemoveParentOffset()
        {
            if (createParentOffset && parentOffset != null && gameObject.activeInHierarchy)
            {
                transform.SetParent(parentOffset.transform.parent);
                Destroy(parentOffset);
            }
        }

        protected virtual void SetupInteractableObject()
        {
            controlInteractableObject = GetComponent<VRTK_InteractableObject>();
            if (controlInteractableObject == null)
            {
                controlInteractableObject = doorContainer.AddComponent<VRTK_InteractableObject>();
                controlInteractableObject.isGrabbable = true;
                controlInteractableObject.ignoredColliders = (onlyInteractWith.Length > 0 ? VRTK_SharedMethods.ColliderExclude(GetComponentsInChildren<Collider>(true), VRTK_SharedMethods.GetCollidersInGameObjects(onlyInteractWith, true, true)) : new Collider[0]);
                SetupGrabMechanic();
                SetupSecondaryAction();
            }
            ManageInteractableListeners(true);
        }

        protected virtual void SetupGrabMechanic()
        {
            if (controlInteractableObject != null)
            {
                grabMechanic = controlInteractableObject.gameObject.AddComponent<VRTK_RotateTransformGrabAttach>();
                grabMechanic.rotateAround = (VRTK_RotateTransformGrabAttach.RotationAxis)operateAxis;
                grabMechanic.rotationFriction = grabbedFriction;
                grabMechanic.releaseDecelerationDamper = releasedFriction;
                grabMechanic.angleLimits = new Vector2(minimumAngle, maximumAngle);
                controlInteractableObject.grabAttachMechanicScript = grabMechanic;
                ManageGrabbableListeners(true);
            }
        }

        protected virtual void SetupSecondaryAction()
        {
            if (controlInteractableObject != null)
            {
                secondaryAction = controlInteractableObject.gameObject.AddComponent<VRTK_SwapControllerGrabAction>();
                controlInteractableObject.secondaryGrabActionScript = secondaryAction;
            }
        }

        protected virtual void ManageInteractableListeners(bool state)
        {
            if (controlInteractableObject != null)
            {
                if (state)
                {
                    controlInteractableObject.InteractableObjectGrabbed += InteractableObjectGrabbed;
                    controlInteractableObject.InteractableObjectUngrabbed += InteractableObjectUngrabbed;
                }
                else
                {
                    controlInteractableObject.InteractableObjectGrabbed -= InteractableObjectGrabbed;
                    controlInteractableObject.InteractableObjectUngrabbed -= InteractableObjectUngrabbed;
                }
            }
        }

        protected virtual void InteractableObjectGrabbed(object sender, InteractableObjectEventArgs e)
        {
            if (grabMechanic != null)
            {
                if (IsResting() && isLocked)
                {
                    grabMechanic.angleLimits = Vector2.zero;
                }
                if (!isLocked)
                {
                    grabMechanic.angleLimits = new Vector2(minimumAngle, maximumAngle);
                }
            }
        }

        protected virtual void InteractableObjectUngrabbed(object sender, InteractableObjectEventArgs e)
        {
            rotationReset = false;
            ResetRotation();
        }

        protected virtual void ManageGrabbableListeners(bool state)
        {
            if (grabMechanic != null)
            {
                if (state)
                {
                    grabMechanic.AngleChanged += GrabMechanicAngleChanged;
                }
                else
                {
                    grabMechanic.AngleChanged -= GrabMechanicAngleChanged;
                }
            }
        }

        protected virtual void GrabMechanicAngleChanged(object sender, RotateTransformGrabAttachEventArgs e)
        {
            EmitEvents();
            if (controlInteractableObject != null && !controlInteractableObject.IsGrabbed())
            {
                ResetRotation();
            }
        }

        protected virtual void ResetRotation()
        {
            if (!rotationReset && grabMechanic != null)
            {
                float currentValue = GetValue();
                if (currentValue <= restingAngle + forceShutThresholdAngle && currentValue >= restingAngle - forceShutThresholdAngle)
                {
                    rotationReset = true;
                    grabMechanic.SetRotation(restingAngle - currentValue, releasedFriction * 0.1f);
                }
            }
        }

        protected virtual void EmitEvents()
        {
            ControllableEventArgs payload = EventPayload();
            float currentAngle = GetValue();
            OnValueChanged(payload);

            bool atMaxAngle = (!IsResting() && (currentAngle >= (maximumAngle - minMaxThresholdAngle)) || (currentAngle <= (minimumAngle + minMaxThresholdAngle)));

            if (atMaxAngle && !AtMaxLimit())
            {
                atMaxLimit = true;
                OnMaxLimitReached(payload);
            }
            else if (IsResting() && !AtMinLimit())
            {
                atMinLimit = true;
                OnMinLimitReached(payload);
            }
            else if (!atMaxAngle && !IsResting())
            {
                if (AtMinLimit())
                {
                    OnMinLimitExited(payload);
                }
                if (AtMaxLimit())
                {
                    OnMaxLimitExited(payload);
                }
                atMinLimit = false;
                atMaxLimit = false;
            }
        }
    }
}