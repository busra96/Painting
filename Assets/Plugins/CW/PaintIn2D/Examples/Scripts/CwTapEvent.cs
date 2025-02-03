using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PaintIn2D
{
	/// <summary>This component turns the current sprite into one that can be tapped, which will then invoke an event.
	/// NOTE: This GameObject must have a 2D or 3D collider. If 2D, your main camera must have the <b>Physics2DCollider</b> component attached. If 3D, your main camera must have the <b>PhysicsCollider</b> component attached.</summary>
	[HelpURL(CwCommon.HelpUrlPrefix + "CwTapEvent")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Tap Event")]
	[RequireComponent(typeof(SpriteRenderer))]
	public class CwTapEvent : MonoBehaviour, IPointerClickHandler
	{
		/// <summary>If the mouse/finger moves more than this pixel distance during the click/tap, it will be ignored. This is used to prevent conflicts with dragging.</summary>
		//public float MaxDelta { set { maxDelta = value; } get { return maxDelta; } } [SerializeField] private float maxDelta = 10.0f;

		/// <summary>When this sprite is tapped, this event will be invoked.</summary>
		public UnityEvent OnTap { get { if (onTap == null) onTap = new UnityEvent(); return onTap; } } [SerializeField] private UnityEvent onTap;

		public void OnPointerClick(PointerEventData eventData)
		{
			//if (Vector2.Distance(eventData.pressPosition, eventData.position) <= MaxDelta)
			if (eventData.dragging == false)
			{
				if (onTap != null)
				{
					onTap.Invoke();
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwTapEvent;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwTapEvent_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			//Draw("maxDelta", "If the mouse/finger moves more than this pixel distance during the click/tap, it will be ignored. This is used to prevent conflicts with dragging.");

			//Separator();

			Draw("onTap");
		}
	}
}
#endif