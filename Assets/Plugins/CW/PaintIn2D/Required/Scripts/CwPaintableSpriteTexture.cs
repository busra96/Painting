using UnityEngine;
using PaintCore;

namespace PaintIn2D
{
	/// <summary>This component allows you to make one texture on the attached Renderer paintable.
	/// NOTE: If the texture or texture slot you want to paint is part of a shared material (e.g. prefab material), then I recommend you add the CwMaterialCloner component to make it unique.</summary>
	[HelpURL(CwCommon.HelpUrlPrefix + "CwPaintableSpriteTexture")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Paintable Sprite Texture")]
	[DisallowMultipleComponent]
	public class CwPaintableSpriteTexture : CwPaintableTexture
	{
		[System.NonSerialized]
		private CwPaintableSprite parent;

		protected override void ApplyTexture(Texture texture)
		{
			if (parent == null)
			{
				parent = GetComponentInParent<CwPaintableSprite>();
			}

			if (parent != null)
			{
				parent.ApplyTexture(Slot.Name, texture);
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwPaintableSpriteTexture;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwPaintableSpriteTexture_Editor : CwPaintableTexture_Editor
	{
	}
}
#endif