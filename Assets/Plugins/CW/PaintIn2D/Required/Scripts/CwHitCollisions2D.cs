using UnityEngine;
using CW.Common;
using PaintCore;

namespace PaintIn2D
{
	/// <summary>This component can be added to any Rigidbody2D, and it will fire hit events when it hits something.</summary>
	[HelpURL(CwCommon.HelpUrlPrefix + "CwHitCollisions2D")]
	[AddComponentMenu(PaintCore.CwCommon.ComponentHitMenuPrefix + "Hit Collisions 2D")]
	public class CwHitCollisions2D : MonoBehaviour
	{
		public enum OrientationType
		{
			WorldUp,
			CameraUp
		}

		public enum PressureType
		{
			Constant,
			ImpactSpeed
		}

		/// <summary>This allows you to filter collisions to specific layers.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = -1;

		/// <summary>If there are multiple contact points, skip them?</summary>
		public bool OnlyUseFirstContact { set { onlyUseFirstContact = value; } get { return onlyUseFirstContact; } } [SerializeField] private bool onlyUseFirstContact = true;

		/// <summary>If this component is generating too many hits, then you can use this setting to ignore hits for the specified amount of seconds.
		/// 0 = Unlimited.</summary>
		public float Delay { set { delay = value; } get { return delay; } } [SerializeField] private float delay;

		/// <summary>How should the hit point be oriented?
		/// WorldUp = It will be rotated to the normal, where the up vector is world up.
		/// CameraUp = It will be rotated to the normal, where the up vector is world up.</summary>
		public OrientationType Orientation { set { orientation = value; } get { return orientation; } } [SerializeField] private OrientationType orientation;

		/// <summary>Orient to a specific camera?
		/// None = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		/// <summary>Should the applied paint be applied as a preview?</summary>
		public bool Preview { set { preview = value; } get { return preview; } } [SerializeField] private bool preview;

		/// <summary>If the collision impact speed is below this value, then the collision will be ignored.</summary>
		public float Threshold { set { threshold = value; } get { return threshold; } } [SerializeField] private float threshold = 50.0f;

		/// <summary>This allows you to set how the pressure value will be calculated.
		/// Constant = The <b>PressureConstant</b> value will be directly used.
		/// ImpactSpeed = The pressure will be 0 when the collision impact speed is <b>PressureMin</b>, and 1 when the impact speed is or exceeds <b>PressureMax</b>.</summary>
		public PressureType PressureMode { set { pressureMode = value; } get { return pressureMode; } } [SerializeField] private PressureType pressureMode = PressureType.ImpactSpeed;

		/// <summary>The impact strength required for a hit to occur with a pressure of 0.</summary>
		public float PressureMin { set { pressureMin = value; } get { return pressureMin; } } [SerializeField] private float pressureMin = 50.0f;

		/// <summary>The impact strength required for a hit to occur with a pressure of 1.</summary>
		public float PressureMax { set { pressureMax = value; } get { return pressureMax; } } [SerializeField] private float pressureMax = 100.0f;

		/// <summary>The pressure value used when <b>PressureMode</b> is set to <b>Constant</b>.</summary>
		public float PressureConstant { set { pressureConstant = value; } get { return pressureConstant; } } [SerializeField] [Range(0.0f, 1.0f)] private float pressureConstant = 1.0f;

		/// <summary>The calculated pressure value will be multiplied by this.</summary>
		public float PressureMultiplier { set { pressureMultiplier = value; } get { return pressureMultiplier; } } [SerializeField] private float pressureMultiplier = 1.0f;

		/// <summary>If you want the raycast hit point to be offset from the surface a bit, this allows you to set by how much in world space.</summary>
		public float Offset { set { offset = value; } get { return offset; } } [SerializeField] private float offset;

		/// <summary>This allows you to override the order this paint gets applied to the object during the current frame.</summary>
		public int Priority { set { priority = value; } get { return priority; } } [SerializeField] private int priority;

		/// <summary>Hit events are normally sent to all components attached to the current GameObject, but this setting allows you to override that. This is useful if you want to use multiple <b>CwHitCollisions2D</b> components with different settings and results.</summary>
		public GameObject Root { set { ClearHitCache(); root = value; } get { return root; } } [SerializeField] private GameObject root;

		[SerializeField]
		private float cooldown;

		[System.NonSerialized]
		private CwHitCache hitCache = new CwHitCache();

		public CwHitCache HitCache
		{
			get
			{
				return hitCache;
			}
		}

		/// <summary>This component sends hit events to a cached list of components that can receive them. If this list changes then you must manually call this method.</summary>
		[ContextMenu("Clear Hit Cache")]
		public void ClearHitCache()
		{
			hitCache.Clear();
		}

		protected virtual void OnCollisionEnter2D(Collision2D collision)
		{
			CheckCollision(collision);
		}

		protected virtual void OnCollisionStay2D(Collision2D collision)
		{
			CheckCollision(collision);
		}

		protected virtual void Update()
		{
			cooldown -= Time.deltaTime;
		}

		private void CheckCollision(Collision2D collision)
		{
			if (cooldown > 0.0f)
			{
				return;
			}

			//var impulse = collision.relativeVelocity.magnitude / Time.fixedDeltaTime;
			var impulse = 0.0f;

			foreach (var i in collision.contacts)
			{
				impulse += i.normalImpulse / Time.fixedDeltaTime;
			}

			// Only handle the collision if the impact was strong enough
			if (impulse >= threshold)
			{
				cooldown = delay;

				// Calculate up vector ahead of time
				var finalUp       = orientation == OrientationType.CameraUp ? PaintCore.CwCommon.GetCameraUp(_camera) : Vector3.up;
				var contacts      = collision.contacts;
				var finalPressure = pressureMultiplier;
				var finalRoot     = root != null ? root : gameObject;

				switch (pressureMode)
				{
					case PressureType.Constant:
					{
						finalPressure *= pressureConstant;
					}
					break;

					case PressureType.ImpactSpeed:
					{
						finalPressure *= Mathf.InverseLerp(pressureMin, pressureMax, impulse);
					}
					break;
				}

				for (var i = contacts.Length - 1; i >= 0; i--)
				{
					var contact = contacts[i];

					if (CwHelper.IndexInMask(contact.otherCollider.gameObject.layer, layers) == true)
					{
						var finalPosition = contact.point + contact.normal * offset;
						var finalRotation = Quaternion.LookRotation(Vector3.forward, -contact.normal);

						hitCache.InvokePoint(finalRoot, preview, priority, finalPressure, finalPosition, finalRotation);

						if (onlyUseFirstContact == true)
						{
							break;
						}
					}
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using UnityEditor;
	using TARGET = CwHitCollisions2D;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwHitCollisions2D_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("layers", "This allows you to filter collisions to specific layers.");

			Separator();

			Draw("onlyUseFirstContact", "If there are multiple contact points, skip them?");
			BeginError(Any(tgts, t => t.Delay < 0.0f));
				Draw("delay", "If this component is generating too many hits, then you can use this setting to ignore hits for the specified amount of seconds.\n\n0 = Unlimited.");
			EndError();

			Draw("orientation", "How should the hit point be oriented?\nNone = It will be treated as a point with no rotation.\n\nWorldUp = It will be rotated to the normal, where the up vector is world up.\n\nCameraUp = It will be rotated to the normal, where the up vector is world up.");
			BeginIndent();
				if (Any(tgts, t => t.Orientation == CwHitCollisions2D.OrientationType.CameraUp))
				{
					Draw("_camera", "Orient to a specific camera?\nNone = MainCamera.");
				}
			EndIndent();

			Separator();

			Draw("preview", "Should the applied paint be applied as a preview?");
			Draw("threshold", "If the collision impact speed is below this value, then the collision will be ignored.");
			Draw("pressureMode", "This allows you to set how the pressure value will be calculated.\n\nConstant = The <b>PressureConstant</b> value will be directly used.\n\nImpactSpeed = The pressure will be 0 when the collision impact speed is <b>PressureMin</b>, and 1 when the impact speed is or exceeds <b>PressureMax</b>.");
			BeginIndent();
				if (Any(tgts, t => t.PressureMode == CwHitCollisions2D.PressureType.Constant))
				{
					Draw("pressureConstant", "The pressure value used when PressureMode is set to Constant.", "Constant");
				}
				if (Any(tgts, t => t.PressureMode == CwHitCollisions2D.PressureType.ImpactSpeed))
				{
					Draw("pressureMin", "The impact strength required for a hit to occur with a pressure of 0.", "Min");
					Draw("pressureMax", "The impact strength required for a hit to occur with a pressure of 1.", "Max");
				}
				Draw("pressureMultiplier", "The calculated pressure value will be multiplied by this.", "Multiplier");
			EndIndent();

			Separator();

			if (DrawFoldout("Advanced", "Show advanced settings?") == true)
			{
				BeginIndent();
					Draw("offset", "If you want the raycast hit point to be offset from the surface a bit, this allows you to set by how much in world space.");
					Draw("priority", "This allows you to override the order this paint gets applied to the object during the current frame.");
					Draw("root", "Hit events are normally sent to all components attached to the current GameObject, but this setting allows you to override that. This is useful if you want to use multiple CwHitCollisions2D components with different settings and results.");
				EndIndent();
			}

			Separator();

			tgt.HitCache.Inspector(tgt.Root != null ? tgt.Root : tgt.gameObject, point: true);
		}
	}
}
#endif