using UnityEngine;
using CW.Common;
using PaintCore;

namespace PaintIn2D
{
	/// <summary>This class contains common code for screen based mouse/finger hit components.</summary>
	public abstract class CwHitScreenBase2D : CwHitPointers
	{
		public enum RotationType
		{
			CameraUp,
			DrawAngle,
			ThisRotation,
			CustomRotation
		}

		public enum EmitType
		{
			PointsIn3D    = 0,
			PointsOnUV    = 20,
			TrianglesIn3D = 30
		}

		public enum InterceptType
		{
			None,
			//InterceptX = 1,
			//InterceptY = 2,
			InterceptZ = 3
		}

		/// <summary>Orient to a specific camera?
		/// None = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		/// <summary>The layers you want the raycast to hit.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

		/// <summary>This allows you to control the hit data this component sends out.
		/// PointsIn3D = Point drawing in 3D.
		/// PointsOnUV = Point drawing on UV (requires non-convex <b>MeshCollider</b>).
		/// TrianglesIn3D = Triangle drawing in 3D (requires non-convex <b>MeshCollider</b>).</summary>
		public EmitType Emit { set { emit = value; } get { return emit; } } [UnityEngine.Serialization.FormerlySerializedAs("draw")] [SerializeField] private EmitType emit;

		/// <summary>This allows you to control how the paint is rotated.
		/// CameraUp = The rotation will be aligned to the camera.
		/// DrawAngle = The paint will be rotated to match the drawing angle.
		/// ThisRotation = The current <b>Transform.rotation</b> will be used.
		/// CustomRotation = The specified <b>CustomTransform.rotation</b> will be used.</summary>
		public RotationType RotateTo { set { rotateTo = value; } get { return rotateTo; } } [SerializeField] private RotationType rotateTo;

		/// <summary>This allows you to specify the <b>Transform</b> when using <b>RotateTo = CustomRotation/CustomLocalRotation</b>.</summary>
		public Transform CustomTransform { set { customTransform = value; } get { return customTransform; } } [SerializeField] private Transform customTransform;

		/// <summary>Should painting triggered from this component be eligible for being undone?</summary>
		public bool StoreStates { set { storeStates = value; } get { return storeStates; } } [SerializeField] protected bool storeStates = true;

		/// <summary>This allows you to override the order this paint gets applied to the object during the current frame.</summary>
		public int Priority { set { priority = value; } get { return priority; } } [SerializeField] private int priority;

		/// <summary>If the mouse/finger doesn't hit a collider, should a hit point be generated anyway?
		/// InterceptZ = A hit point will be created on a plane that lies flat on the Z axis.</summary>
		public InterceptType Intercept { set { intercept = value; } get { return intercept; } } [SerializeField] private InterceptType intercept = InterceptType.InterceptZ;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			if (GetComponent<CwPointerMouse>() == null)
			{
				gameObject.AddComponent<CwPointerMouse>();
			}

			if (GetComponent<CwPointerTouch>() == null)
			{
				gameObject.AddComponent<CwPointerTouch>();
			}

			if (GetComponent<CwPointerPen>() == null)
			{
				gameObject.AddComponent<CwPointerPen>();
			}
		}
#endif

		protected virtual void DoQuery(Vector2 screenPosition, ref Camera camera, ref Ray ray, ref CwHit hit3D, ref RaycastHit2D hit2D)
		{
			var hit = default(RaycastHit);

			camera = CwHelper.GetCamera(_camera);
			ray    = camera.ScreenPointToRay(screenPosition);
			hit2D  = Physics2D.GetRayIntersection(ray, float.PositiveInfinity, layers);

			Physics.Raycast(ray, out hit, float.PositiveInfinity, layers);

			hit3D = new CwHit(hit);
		}

		protected void PaintAt(CwPointConnector connector, CwHitCache hitCache, Vector2 screenPosition, Vector2 screenPositionOld, bool preview, float pressure, object owner)
		{
			var camera        = default(Camera);
			var ray           = default(Ray);
			var hit2D         = default(RaycastHit2D);
			var hit3D         = default(CwHit);
			var finalPosition = default(Vector3);
			var finalRotation = default(Quaternion);

			DoQuery(screenPosition, ref camera, ref ray, ref hit3D, ref hit2D);

			var valid2D = hit2D.distance > 0.0f;
			var valid3D = hit3D.Distance > 0.0f;

			// Hit 3D?
			if (valid3D == true && (valid2D == false || hit3D.Distance < hit2D.distance))
			{
				CalcHitData(hit3D.Position, hit3D.Normal, ray, camera, screenPositionOld, ref finalPosition, ref finalRotation);

				if (emit == EmitType.PointsIn3D)
				{
					if (connector != null)
					{
						connector.SubmitPoint(gameObject, preview, priority, pressure, finalPosition, finalRotation, owner);
					}
					else
					{
						hitCache.InvokePoint(gameObject, preview, priority, pressure, finalPosition, finalRotation);
					}

					return;
				}
				else if (emit == EmitType.PointsOnUV)
				{
					hitCache.InvokeCoord(gameObject, preview, priority, pressure, hit3D, finalRotation);

					return;
				}
				else if (emit == EmitType.TrianglesIn3D)
				{
					hitCache.InvokeTriangle(gameObject, preview, priority, pressure, hit3D, finalRotation);

					return;
				}
			}
			// Hit 2D?
			else if (valid2D == true)
			{
				CalcHitData(hit2D.point, new Vector3(0.0f, 0.0f, -1.0f), ray, camera, screenPositionOld, ref finalPosition, ref finalRotation);

				if (emit == EmitType.PointsIn3D)
				{
					if (connector != null)
					{
						connector.SubmitPoint(gameObject, preview, priority, pressure, finalPosition, finalRotation, owner);
					}
					else
					{
						hitCache.InvokePoint(gameObject, preview, priority, pressure, finalPosition, finalRotation);
					}

					return;
				}
			}
			// Intercept 2D?
			else if (intercept != InterceptType.None)
			{
				if (intercept == InterceptType.InterceptZ && ray.direction.z != 0.0f)
				{
					var point = ray.GetPoint(ray.origin.z / -ray.direction.z);

					CalcHitData(point, new Vector3(0.0f, 0.0f, -1.0f), ray, camera, screenPositionOld, ref finalPosition, ref finalRotation);

					if (emit == EmitType.PointsIn3D)
					{
						if (connector != null)
						{
							connector.SubmitPoint(gameObject, preview, priority, pressure, finalPosition, finalRotation, owner);
						}
						else
						{
							hitCache.InvokePoint(gameObject, preview, priority, pressure, finalPosition, finalRotation);
						}

						return;
					}
				}
			}

			if (connector != null)
			{
				connector.BreakHits(owner);
			}
		}

		private void CalcHitData(Vector3 hitPoint, Vector3 hitNormal, Ray ray, Camera camera, Vector2 screenPositionOld, ref Vector3 finalPosition, ref Quaternion finalRotation)
		{
			finalPosition = hitPoint;
			finalRotation = Quaternion.identity;

			switch (rotateTo)
			{
				case RotationType.CameraUp: finalRotation = camera.transform.rotation; break;
				case RotationType.DrawAngle:
				{
					var rayOld  = camera.ScreenPointToRay(screenPositionOld);
					var finalUp = default(Vector3);

					if (camera.orthographic == true)
					{
						finalUp = Vector3.Cross(rayOld.GetPoint(1.0f) - ray.origin, ray.GetPoint(1.0f) - ray.origin);
					}
					else
					{
						finalUp = Vector3.Cross(rayOld.direction, ray.direction);
					}

					finalRotation = Quaternion.LookRotation(Vector3.back, finalUp);
				}
				break;
				case RotationType.ThisRotation: finalRotation = transform.rotation; break;
				case RotationType.CustomRotation: if (customTransform != null) finalRotation = customTransform.rotation; break;
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using UnityEditor;
	using TARGET = CwHitScreenBase2D;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwHitScreenBase2D_Editor : CwEditor
	{
		protected virtual void DrawBasic()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("emit", "This allows you to control the hit data this component sends out.\n\nPointsIn3D = Point drawing in 3D.\n\nPointsOnUV = Point drawing on UV (requires non-convex MeshCollider).\n\nTrianglesIn3D = Triangle drawing in 3D (requires non-convex MeshCollider).");
			BeginError(Any(tgts, t => t.Layers == 0));
				Draw("layers", "The layers you want the raycast to hit.");
			EndError();
			Draw("guiLayers", "Fingers that began touching the screen on top of these UI layers will be ignored.");
			BeginError(Any(tgts, t => CwHelper.GetCamera(t.Camera) == null));
				Draw("_camera", "Orient to a specific camera?\n\nNone = MainCamera.");
			EndError();

			Separator();

			Draw("rotateTo", "This allows you to control how the paint is rotated.\n\nCameraUp = The rotation will be aligned to the camera.\n\nDrawAngle = The paint will be rotated to match the drawing angle.\n\nThisRotation = The current <b>Transform.rotation</b> will be used.\n\nCustomRotation = The specified <b>CustomTransform.rotation</b> will be used.");
			if (Any(tgts, t => t.RotateTo == CwHitScreenBase2D.RotationType.CustomRotation))
			{
				BeginIndent();
					Draw("customTransform", "This allows you to specify the Transform when using RotateTo = CustomRotation.");
				EndIndent();
			}
		}

		protected void DrawAdvancedFoldout()
		{
			if (DrawFoldout("Advanced", "Show advanced settings?") == true)
			{
				BeginIndent();
					DrawAdvanced();
				EndIndent();
			}
		}

		protected virtual void DrawAdvanced()
		{
			Draw("storeStates", "Should painting triggered from this component be eligible for being undone?");
			Draw("priority", "This allows you to override the order this paint gets applied to the object during the current frame.");
			Draw("intercept", "If the mouse/finger doesn't hit a collider, should a hit point be generated anyway?\n\t\t/// InterceptZ = A hit point will be created on a plane that lies flat on the Z axis.");
		}
	}
}
#endif