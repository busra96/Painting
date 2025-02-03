using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PaintIn2D
{
	/// <summary>This component turns the current sprite into one that can be read when you tap on it.
	/// NOTE: This GameObject must have a 2D or 3D collider. If 2D, your main camera must have the <b>Physics2DCollider</b> component attached. If 3D, your main camera must have the <b>PhysicsCollider</b> component attached.
	/// NOTE: To be read, this sprite must have its <b>Advanced / Read/Write</b> setting enabled.
	/// NOTE: To be read, this sprite must NOT be part of a texture atlas.</summary>
	[HelpURL(CwCommon.HelpUrlPrefix + "CwTapToReadColor")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Tap To Read Color")]
	[RequireComponent(typeof(SpriteRenderer))]
    public class CwTapToReadColor : MonoBehaviour, IPointerDownHandler
    {
		[System.Serializable] public class ColorEvent : UnityEvent<Color> {}

		[System.NonSerialized]
		private SpriteRenderer cachedSpriteRenderer;

		/// <summary>When a color is read, this event will be invoked.
		/// Color = The color that was read.</summary>
		public ColorEvent OnColor { get { if (onColor == null) onColor = new ColorEvent(); return onColor; } } [SerializeField] private ColorEvent onColor;

		public bool SpriteIsValid
		{
			get
			{
				var sr = GetComponent<SpriteRenderer>();

				if (sr != null)
				{
					var s = sr.sprite;

					if (s != null)
					{
						return s.packed == false && s.texture.isReadable == true;
					}
				}

				return false;
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			var sprite = cachedSpriteRenderer.sprite;

			if (sprite != null)
			{
				var t = sprite.texture;

				if (t != null)
				{
					if (t.isReadable == true)
					{
						var p = transform.InverseTransformPoint(eventData.pointerPressRaycast.worldPosition);
						var u = Mathf.InverseLerp(sprite.bounds.min.x, sprite.bounds.max.x, p.x);
						var v = Mathf.InverseLerp(sprite.bounds.min.y, sprite.bounds.max.y, p.y);
						var c = t.GetPixelBilinear(u, v);

						if (onColor != null)
						{
							onColor.Invoke(c);
						}
					}
					else
					{
						Debug.LogError("Can't read color because this sprite's texture isn't readable.");
					}
				}
			}
		}

		protected virtual void OnEnable()
		{
			cachedSpriteRenderer = GetComponent<SpriteRenderer>();
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwTapToReadColor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwTapToReadColor_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("onColor");

			if (Any(tgts, t => t.SpriteIsValid == false))
			{
				Error("The mask sprite is part of an atlas or is not readable.");
			}
		}
	}
}
#endif