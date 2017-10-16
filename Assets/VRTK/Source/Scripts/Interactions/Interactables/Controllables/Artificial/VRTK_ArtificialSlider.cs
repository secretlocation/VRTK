// Artificial Slider|ArtificialControllables|120030
namespace VRTK.Controllables.ArtificialBased
{
    using UnityEngine;
    using System.Collections;
    using VRTK.GrabAttachMechanics;
    using VRTK.SecondaryControllerGrabActions;

    [AddComponentMenu("VRTK/Scripts/Interactables/Controllables/Artificial/VRTK_ArtificialSlider")]
    public class VRTK_ArtificialSlider : VRTK_BaseControllable
    {
        [Header("Slider Settings")]

        [Tooltip("The maximum length that the slider can be moved from the origin position across the `Operate Axis`. A negative value will allow it to move the opposite way.")]
        public float maximumLength = 0.1f;
        [Tooltip("The position the slider when it is at the default resting point given in a normalized value of `0f` (start point) to `1f` end point.")]
        [Range(0f, 1f)]
        [SerializeField]
        protected float restingPosition = 0f;
        [Tooltip("The threshold the slider has to be within the `Resting Position` before the slider is forced back to the `Resting Position` if it is not grabbed.")]
        public float forceRestingPositionThreshold = 0f;

        [Header("Value Step Settings")]

        [Tooltip("The minimum `(x)` and the maximum `(y)` step values for the slider to register along the `Operate Axis`.")]
        public Vector2 stepValueRange = new Vector3(0f, 1f);
        [Tooltip("The increments the slider value will change in between the `Step Value Range`.")]
        public float stepSize = 0.1f;
        [Tooltip("If this is checked then the value for the slider will be the step value and not the absolute position of the slider Transform.")]
        public bool useStepAsValue = true;

        [Header("Snap Settings")]

        [Tooltip("If this is checked then the slider will snap to the position of the nearest step along the value range.")]
        public bool snapToStep = false;
        [Tooltip("The speed in which the slider will snap to the relevant point along the `Operate Axis`")]
        public float snapForce = 10f;

        [Header("Interaction Settings")]

        [Tooltip("The maximum distance the grabbing object is away from the slider before it is automatically released.")]
        public float detachDistance = 1f;
        [Tooltip("The amount of friction to the slider Rigidbody when it is released.")]
        public float releaseFriction = 10f;
        [Tooltip("The speed in which to track the grabbed slider to the interacting object.")]
        public float trackingSpeed = 25f;
        [Tooltip("A collection of GameObjects that will be used as the valid collisions to determine if the door can be interacted with.")]
        public GameObject[] onlyInteractWith = new GameObject[0];

        protected VRTK_InteractableObject controlInteractableObject;
        protected VRTK_MoveTransformGrabAttach grabMechanic;
        protected VRTK_SwapControllerGrabAction secondaryAction;
        protected bool createInteractableObject;
        protected Vector2 axisLimits;
        protected Vector3 previousLocalPosition;
        protected float previousStepValue;
        protected Coroutine setRestingPositionAtEndOfFrameRoutine;

        /// <summary>
        /// The GetValue method returns the current position value of the slider.
        /// </summary>
        /// <returns>The actual position of the button.</returns>
        public override float GetValue()
        {
            return transform.localPosition[(int)operateAxis];
        }

        /// <summary>
        /// The GetNormalizedValue method returns the current position value of the slider normalized between `0f` and `1f`.
        /// </summary>
        /// <returns>The normalized position of the button.</returns>
        public override float GetNormalizedValue()
        {
            return VRTK_SharedMethods.NormalizeValue(GetValue(), originalLocalPosition[(int)operateAxis], MaximumLength()[(int)operateAxis]);
        }

        /// <summary>
        /// The GetStepValue method returns the current position of the slider based on the step value range.
        /// </summary>
        /// <param name="currentValue">The current position value of the slider to get the Step Value for.</param>
        /// <returns>The current Step Value based on the slider position.</returns>
        public virtual float GetStepValue(float currentValue)
        {
            return Mathf.Round((stepValueRange.x + Mathf.Clamp01(currentValue / maximumLength) * (stepValueRange.y - stepValueRange.x)) / stepSize) * stepSize;
        }

        /// <summary>
        /// The SetRestingPosition method allows the setting of the `Resting Position` parameter at runtime.
        /// </summary>
        /// <param name="newRestingPosition">The new resting position value.</param>
        /// <param name="speed">The speed to move to the new resting position.</param>
        /// <param name="forceSet">If `true` then the position will always be set even if the slider is currently outside of the resting threshold.</param>
        public virtual void SetRestingPosition(float newRestingPosition, float speed, bool forceSet)
        {
            restingPosition = newRestingPosition;
            if (forceSet || IsResting())
            {
                SnapToRestingPosition(speed);
            }
        }

        /// <summary>
        /// The SetRestingPositionWithStepValue sets the `Resting Position` parameter but uses a value within the `Step Value Range`.
        /// </summary>
        /// <param name="givenStepValue">The step value within the `Step Value Range` to set the `Resting Position` parameter to.</param>
        /// <param name="speed">The speed to move to the new resting position.</param>
        /// <param name="forceSet">If `true` then the position will always be set even if the slider is currently outside of the resting threshold.</param>
        public virtual void SetRestingPositionWithStepValue(float givenStepValue, float speed, bool forceSet)
        {
            restingPosition = VRTK_SharedMethods.NormalizeValue(givenStepValue, stepValueRange.x, stepValueRange.y);
            if (forceSet || IsResting())
            {
                SnapToRestingPosition(speed);
            }
        }

        /// <summary>
        /// The GetPositionFromStepValue returns the position the slider would be at based on the given step value.
        /// </summary>
        /// <param name="givenStepValue">The step value to check the position for.</param>
        /// <returns>The position the slider would be at based on the given step value.</returns>
        public virtual float GetPositionFromStepValue(float givenStepValue)
        {
            float normalizedStepValue = VRTK_SharedMethods.NormalizeValue(givenStepValue, stepValueRange.x, stepValueRange.y);
            return Mathf.Lerp(axisLimits.x, axisLimits.y, Mathf.Clamp01(normalizedStepValue));
        }

        /// <summary>
        /// The IsResting method returns whether the slider is at the resting position or within the resting position threshold.
        /// </summary>
        /// <returns>Returns `true` if the slider is at the resting position or within the resting position threshold.</returns>
        public virtual bool IsResting()
        {
            float currentValue = GetNormalizedValue();
            return ((currentValue <= restingPosition + forceRestingPositionThreshold && currentValue >= restingPosition - forceRestingPositionThreshold));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            previousLocalPosition = Vector3.one * float.MaxValue;
            previousStepValue = float.MaxValue;
            SetupInteractableObject();
            setRestingPositionAtEndOfFrameRoutine = StartCoroutine(SetRestingPositionAtEndOfFrameRoutine());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ManageInteractableListeners(false);
            if (createInteractableObject)
            {
                Destroy(controlInteractableObject);
            }
            if (setRestingPositionAtEndOfFrameRoutine != null)
            {
                StopCoroutine(setRestingPositionAtEndOfFrameRoutine);
            }
        }

        protected override ControllableEventArgs EventPayload()
        {
            ControllableEventArgs e = base.EventPayload();
            e.value = (useStepAsValue ? GetStepValue(GetValue()) : GetValue());
            return e;
        }

        protected virtual IEnumerator SetRestingPositionAtEndOfFrameRoutine()
        {
            yield return new WaitForEndOfFrame();
            SnapToRestingPosition(0f);
            if (snapToStep)
            {
                SetRestingPositionWithStepValue(GetStepValue(GetValue()), snapForce, true);
            }
            EmitEvents();
        }

        protected virtual void SetupInteractableObject()
        {
            controlInteractableObject = GetComponent<VRTK_InteractableObject>();
            if (controlInteractableObject == null)
            {
                controlInteractableObject = gameObject.AddComponent<VRTK_InteractableObject>();
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
                grabMechanic = controlInteractableObject.gameObject.AddComponent<VRTK_MoveTransformGrabAttach>();
                SetGrabMechanicParameters();
                controlInteractableObject.grabAttachMechanicScript = grabMechanic;
                ManageGrabbableListeners(true);
                grabMechanic.ResetState();
            }
        }

        protected virtual void SetGrabMechanicParameters()
        {
            if (grabMechanic != null)
            {
                grabMechanic.releaseDecelerationDamper = releaseFriction;
                axisLimits = new Vector2(originalLocalPosition[(int)operateAxis], MaximumLength()[(int)operateAxis]);
                switch (operateAxis)
                {
                    case OperatingAxis.xAxis:
                        grabMechanic.xAxisLimits = axisLimits;
                        break;
                    case OperatingAxis.yAxis:
                        grabMechanic.yAxisLimits = axisLimits;
                        break;
                    case OperatingAxis.zAxis:
                        grabMechanic.zAxisLimits = axisLimits;
                        break;
                }
                grabMechanic.trackingSpeed = trackingSpeed;
                grabMechanic.detachDistance = detachDistance;
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

        protected virtual Vector3 MaximumLength()
        {
            return originalLocalPosition + (AxisDirection() * maximumLength);
        }

        protected virtual void SnapToRestingPosition(float speed)
        {
            float positionOnAxis = Mathf.Lerp(axisLimits.x, axisLimits.y, Mathf.Clamp01(restingPosition));
            SnapToPosition(positionOnAxis, speed);
        }

        protected virtual void SnapToPosition(float positionOnAxis, float speed)
        {
            if (grabMechanic != null)
            {
                grabMechanic.SetCurrentPosition((AxisDirection() * Mathf.Sign(maximumLength)) * positionOnAxis, speed);
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
            SetGrabMechanicParameters();
        }

        protected virtual void InteractableObjectUngrabbed(object sender, InteractableObjectEventArgs e)
        {
            SetGrabMechanicParameters();
            if (snapToStep)
            {
                SetRestingPositionWithStepValue(GetStepValue(GetValue()), snapForce, true);
            }

            if (ForceRestingPosition())
            {
                SnapToRestingPosition(snapForce);
            }
        }

        protected virtual bool ForceRestingPosition()
        {
            return (forceRestingPositionThreshold > 0f && !IsGrabbed() && (Mathf.Abs(restingPosition - GetNormalizedValue()) <= forceRestingPositionThreshold));
        }

        protected virtual bool IsGrabbed()
        {
            return (controlInteractableObject != null && controlInteractableObject.IsGrabbed());
        }

        protected virtual void ManageGrabbableListeners(bool state)
        {
            if (grabMechanic != null)
            {
                if (state)
                {
                    grabMechanic.TransformPositionChanged += GrabMechanicTransformPositionChanged;
                }
                else
                {
                    grabMechanic.TransformPositionChanged -= GrabMechanicTransformPositionChanged;
                }
            }
        }

        protected virtual void GrabMechanicTransformPositionChanged(object sender, MoveTransformGrabAttachEventArgs e)
        {
            EmitEvents();
        }

        protected virtual void EmitEvents()
        {
            float currentPosition = GetNormalizedValue();
            ControllableEventArgs payload = EventPayload();

            if (useStepAsValue)
            {
                if (GetStepValue(GetValue()) != previousStepValue)
                {
                    OnValueChanged(payload);
                }
            }
            else
            {
                if (!VRTK_SharedMethods.Vector3ShallowCompare(transform.localPosition, previousLocalPosition, equalityFidelity))
                {
                    OnValueChanged(payload);
                }
            }
            previousStepValue = GetStepValue(GetValue());
            previousLocalPosition = transform.localPosition;

            if (currentPosition >= (1f - maximumLength) && !AtMaxLimit())
            {
                atMaxLimit = true;
                OnMaxLimitReached(payload);
            }
            else if (currentPosition <= (0f + maximumLength) && !AtMinLimit())
            {
                atMinLimit = true;
                OnMinLimitReached(payload);
            }
            else if (currentPosition > maximumLength && currentPosition < (1f - maximumLength))
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