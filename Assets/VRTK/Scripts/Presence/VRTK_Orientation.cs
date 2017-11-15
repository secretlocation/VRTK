using UnityEngine;

namespace VRTK
{
    [RequireComponent(typeof(VRTK_BasicTeleport))]
    public class VRTK_Orientation : MonoBehaviour
    {
		private static Vector3 _up = Vector3.up;
		public static Vector3 Up
		{
			get { return _up; }
			private set { _up = value; }
		}

		private static Vector3 _down = Vector3.down;
		public static Vector3 Down
		{
			get { return _down; }
			private set { _down = value; }
		}

		private static Vector3 _forward = Vector3.forward;
		public static Vector3 Forward
		{
			get { return _forward; }
			private set { _forward = value; }
		}

		private static Vector3 _back = Vector3.back;
		public static Vector3 Back
		{
			get { return _back; }
			private set { _back = value; }
		}

		private static Vector3 _left = Vector3.left;
		public static Vector3 Left
		{
			get { return _left; }
			private set { _left = value; }
		}

		private static Vector3 _right = Vector3.right;
		public static Vector3 Right
		{
			get { return _right; }
			private set { _right = value; }
		}

		private static Quaternion _rotation = Quaternion.identity;
		public static Quaternion Rotation
		{
			get { return _rotation; }
			private set { _rotation = value; }
		}

		protected Transform rig;
        protected Transform playArea;
        protected VRTK_BasicTeleport teleporter;

		#region MonoBehaviour
		protected virtual void Awake()
		{
			VRTK_SDKManager.instance.AddBehaviourToToggleOnLoadedSetupChange(this);
			teleporter = GetComponent<VRTK_BasicTeleport>();
		}

		protected virtual void OnEnable()
		{
			SetupRig();
			SetupPlayArea();
		}

		protected virtual void OnDestroy()
		{
			VRTK_SDKManager.instance.RemoveBehaviourToToggleOnLoadedSetupChange(this);
		}
		#endregion

		/// <summary>
		/// The SetOrigin method moves the play area and orients the world relative to a target.
		/// </summary>
		/// <param name="target">The target to be set as the origin</param>
		public virtual void SetOrigin(Transform target)
        {
			// set worlds orientation
			SetOrientation(target);
			if (rig != null && playArea != null)
            {
				// zero position to mitigate potential play area offset
				rig.position = Vector3.zero;

				if (teleporter != null)
				{
					// force teleport player to position
					teleporter.ForceTeleport(target.position);
				}
				else
				{
					// if no teleporter force play area position
					playArea.position = target.position;
				}

                // remove artificial play area rotation
                playArea.localRotation = Quaternion.identity;
            }
		}

        /// <summary>
        /// The SetOrientation method orients the world relative to a target.
        /// </summary>
        /// <param name="forward">The forward direction to orient around</param>
        /// <param name="up">The up direction to orient around</param>
        public virtual void SetOrientation(Vector3 forward, Vector3 up)
        {
			Up = up;
			Down = -up;
			Forward = forward;
			Back = -forward;
			Right = Vector3.Cross(forward, up);
			Left = Vector3.Cross(forward, -up);
			Rotation = Quaternion.LookRotation(forward, up);

			Physics.gravity = Down * Physics.gravity.magnitude;
			if (rig != null) { rig.rotation = Rotation; }
        }

        /// <summary>
        /// The SetOrientation method orients the world relative to a target.
        /// </summary>
        /// <param name="target">The target to orient around</param>
        public virtual void SetOrientation(Transform target)
        {
            SetOrientation(target.forward, target.up);
        }

        protected virtual void SetupRig()
        {
            rig = VRTK_SDKManager.instance.transform;
        }

        protected virtual void SetupPlayArea()
        {
            playArea = VRTK_DeviceFinder.PlayAreaTransform();
        }
    }
}
