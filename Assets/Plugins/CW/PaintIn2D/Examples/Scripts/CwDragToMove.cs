using UnityEngine;
using UnityEngine.EventSystems;

namespace PaintIn2D
{
	/// <summary>This component turns the current sprite into one that can be dragged around using the mouse/touch.
	/// NOTE: This GameObject must have a 2D or 3D collider. If 2D, your main camera must have the <b>Physics2DCollider</b> component attached. If 3D, your main camera must have the <b>PhysicsCollider</b> component attached.</summary>
    [HelpURL(CwCommon.HelpUrlPrefix + "CwDragToMove")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Drag To Move")]
	public class CwDragToMove : MonoBehaviour, IDragHandler
    {
		public void OnDrag(PointerEventData eventData)
		{
			var camera = Camera.main;

			if (camera != null)
			{
				var currentPosition  = camera.ScreenToWorldPoint(eventData.position);
				var previousPosition = camera.ScreenToWorldPoint(eventData.position - eventData.delta);

				transform.position += currentPosition - previousPosition;
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwDragToMove;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwDragToMove_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Info("This component allows you to drag the current GameObject around using the mouse/touch.");
		}
	}
}
#endif