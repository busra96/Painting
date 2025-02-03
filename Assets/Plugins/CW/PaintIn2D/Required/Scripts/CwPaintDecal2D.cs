using UnityEngine;
using CW.Common;
using PaintCore;

namespace PaintIn2D
{
	/// <summary>This allows you to paint a decal at a hit point. Hit points will automatically be sent by any <b>CwHit___</b> component on this GameObject, or its ancestors.</summary>
	[HelpURL(CwCommon.HelpUrlPrefix + "CwPaintDecal2D")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Paint Decal 2D")]
	public class CwPaintDecal2D : MonoBehaviour, IHitPoint, IHitLine, IHitTriangle, IHitQuad, IHitCoord
	{
		/// <summary>Only the CwModel/CwPaintableSprite GameObjects whose layers are within this mask will be eligible for painting.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = -1;

		/// <summary>If this is set, then only the specified CwModel/CwPaintableSprite will be painted, regardless of the layer setting.</summary>
		public CwModel TargetModel { set { targetModel = value; } get { return targetModel; } } [SerializeField] private CwModel targetModel;

		/// <summary>Only the <b>CwPaintableTexture</b> components with a matching group will be painted by this component.</summary>
		public CwGroup Group { set { group = value; } get { return group; } } [SerializeField] private CwGroup group;

		/// <summary>If this is set, then only the specified CwPaintableTexture will be painted, regardless of the layer or group setting.</summary>
		public CwPaintableTexture TargetTexture { set { targetTexture = value; } get { return targetTexture; } } [SerializeField] private CwPaintableTexture targetTexture;

		/// <summary>This allows you to choose how the paint from this component will combine with the existing pixels of the textures you paint.
		/// NOTE: See the <b>Blend Mode</b> section of the documentation for more information.</summary>
		public CwBlendMode BlendMode { set { blendMode = value; } get { return blendMode; } } [SerializeField] private CwBlendMode blendMode = CwBlendMode.AlphaBlend(new Vector4(1.0f, 1.0f, 1.0f, 0.0f));

		/// <summary>The decal texture that will be painted.</summary>
		public Texture Texture { set { texture = value; } get { return texture; } } [SerializeField] private Texture texture;

		/// <summary>This allows you to specify the shape of the decal. This is optional for most blending modes, because they usually derive their shape from the RGB or A values. However, if you're using the <b>Replace</b> blending mode, then you must manually specify the shape.</summary>
		public Texture Shape { set { shape = value; } get { return shape; } } [SerializeField] private Texture shape;

		/// <summary>This allows you specify the texture channel used when sampling <b>Shape</b>.</summary>
		public CwChannel ShapeChannel { set { shapeChannel = value; } get { return shapeChannel; } } [SerializeField] private CwChannel shapeChannel = CwChannel.Alpha;

		/// <summary>The color of the paint.</summary>
		public Color Color { set { color = value; } get { return color; } } [SerializeField] private Color color = Color.white;

		/// <summary>The opacity of the brush.</summary>
		public float Opacity { set { opacity = value; } get { return opacity; } } [Range(0.0f, 1.0f)] [SerializeField] private float opacity = 1.0f;

		/// <summary>The angle of the texture in degrees.</summary>
		public float Angle { set { angle = value; } get { return angle; } } [Range(-180.0f, 180.0f)] [SerializeField] private float angle;

		/// <summary>This allows you to control the mirroring and aspect ratio of the texture.
		/// 1, 1 = No scaling.
		/// -1, 1 = Horizontal Flip.</summary>
		public Vector3 Scale { set { scale = value; } get { return scale; } } [SerializeField] private Vector3 scale = Vector3.one;

		/// <summary>The radius of the paint brush.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [SerializeField] private float radius = 0.1f;

		/// <summary>This allows you to apply a tiled detail texture to your texture. This tiling will be applied in world space using triplanar mapping.</summary>
		public Texture TileTexture { set { tileTexture = value; } get { return tileTexture; } } [SerializeField] private Texture tileTexture;

		/// <summary>This allows you to adjust the tiling position + rotation + scale using a <b>Transform</b>.</summary>
		public Transform TileTransform { set { tileTransform = value; } get { return tileTransform; } } [SerializeField] private Transform tileTransform;

		/// <summary>This allows you to control the triplanar influence.
		/// 0 = No influence.
		/// 1 = Full influence.</summary>
		public float TileOpacity { set { tileOpacity = value; } get { return tileOpacity; } } [UnityEngine.Serialization.FormerlySerializedAs("tileBlend")] [Range(0.0f, 1.0f)] [SerializeField] private float tileOpacity = 1.0f;

		/// <summary>This allows you to control how quickly the triplanar mapping transitions between the X/Y/Z planes.</summary>
		public float TileTransition { set { tileTransition = value; } get { return tileTransition; } } [Range(1.0f, 200.0f)] [SerializeField] private float tileTransition = 4.0f;

		/// <summary>If your scene contains a <b>CwMask</b>, should this paint component use it?</summary>
		public bool FindMask { set { findMask = value; } get { return findMask; } } [SerializeField] private bool findMask = true;

		/// <summary>This stores a list of all modifiers used to change the way this component applies paint (e.g. <b>CwModifyColorRandom</b>).</summary>
		public CwModifierList Modifiers { get { if (modifiers == null) modifiers = new CwModifierList(); return modifiers; } } [SerializeField] private CwModifierList modifiers;

		/// <summary>This method will invert the scale.x value.</summary>
		[ContextMenu("Flip Horizontal")]
		public void FlipHorizontal()
		{
			scale.x = -scale.x;
		}

		/// <summary>This method will invert the scale.y value.</summary>
		[ContextMenu("Flip Vertical")]
		public void FlipVertical()
		{
			scale.y = -scale.y;
		}

		/// <summary>This method increments the angle by the specified amount of degrees, and wraps it to the -180..180 range.</summary>
		public void IncrementAngle(float degrees)
		{
			angle = Mathf.Repeat(angle + 180.0f + degrees, 360.0f) - 180.0f;
		}

		/// <summary>This method multiplies the <b>Opacity</b> by the specified value.</summary>
		public void MultiplyOpacity(float multiplier)
		{
			opacity = Mathf.Clamp01(opacity * multiplier);
		}

		/// <summary>This method increments the <b>Opacity</b> by the specified value.</summary>
		public void IncrementOpacity(float delta)
		{
			opacity = Mathf.Clamp01(opacity + delta);
		}

		/// <summary>This method multiplies the <b>Radius</b> by the specified value.</summary>
		public void MultiplyRadius(float multiplier)
		{
			radius *= multiplier;
		}

		/// <summary>This method increases the <b>Radius</b> by the specified value.</summary>
		public void IncrementRadius(float delta)
		{
			radius += delta;
		}

		/// <summary>This method multiplies the <b>Scale</b> by the specified value.</summary>
		public void MultiplyScale(float multiplier)
		{
			scale *= multiplier;
		}

		/// <summary>This method increases the <b>Scale</b> by the specified value.</summary>
		public void IncrementScale(float multiplier)
		{
			scale += Vector3.one * multiplier;
		}

		/// <summary>This method paints all pixels at the specified point using the shape of a texture.</summary>
		public void HandleHitPoint(bool preview, int priority, float pressure, int seed, Vector3 position, Quaternion rotation)
		{
			if (modifiers != null && modifiers.Count > 0)
			{
				CwHelper.BeginSeed(seed);
					modifiers.ModifyPosition(ref position, preview, pressure);
				CwHelper.EndSeed();
			}

			CwCommandDecal2D.Instance.SetState(preview, priority);
			CwCommandDecal2D.Instance.SetLocation(position);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = PaintCore.CwCommon.GetRadius(worldSize);
			var worldPosition = position;

			HandleMaskCommon(worldPosition);

			CwPaintableManager.SubmitAll(CwCommandDecal2D.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints all pixels between the two specified points using the shape of a texture.</summary>
		public void HandleHitLine(bool preview, int priority, float pressure, int seed, Vector3 position, Vector3 endPosition, Quaternion rotation, bool clip)
		{
			CwCommandDecal2D.Instance.SetState(preview, priority);
			CwCommandDecal2D.Instance.SetLocation(position, endPosition, clip: clip);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = PaintCore.CwCommon.GetRadius(worldSize, position, endPosition);
			var worldPosition = PaintCore.CwCommon.GetPosition(position, endPosition);

			HandleMaskCommon(worldPosition);

			CwPaintableManager.SubmitAll(CwCommandDecal2D.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints all pixels between three points using the shape of a texture.</summary>
		public void HandleHitTriangle(bool preview, int priority, float pressure, int seed, Vector3 positionA, Vector3 positionB, Vector3 positionC, Quaternion rotation)
		{
			CwCommandDecal2D.Instance.SetState(preview, priority);
			CwCommandDecal2D.Instance.SetLocation(positionA, positionB, positionC);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = PaintCore.CwCommon.GetRadius(worldSize, positionA, positionB, positionC);
			var worldPosition = PaintCore.CwCommon.GetPosition(positionA, positionB, positionC);

			HandleMaskCommon(worldPosition);

			CwPaintableManager.SubmitAll(CwCommandDecal2D.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints all pixels between two pairs of points using the shape of a texture.</summary>
		public void HandleHitQuad(bool preview, int priority, float pressure, int seed, Vector3 position, Vector3 endPosition, Vector3 position2, Vector3 endPosition2, Quaternion rotation, bool clip)
		{
			CwCommandDecal2D.Instance.SetState(preview, priority);
			CwCommandDecal2D.Instance.SetLocation(position, endPosition, position2, endPosition2, clip: clip);

			var worldSize     = HandleHitCommon(preview, pressure, seed, rotation);
			var worldRadius   = PaintCore.CwCommon.GetRadius(worldSize, position, endPosition, position2, endPosition2);
			var worldPosition = PaintCore.CwCommon.GetPosition(position, endPosition, position2, endPosition2);

			HandleMaskCommon(worldPosition);

			CwPaintableManager.SubmitAll(CwCommandDecal2D.Instance, worldPosition, worldRadius, layers, group, targetModel, targetTexture);
		}

		/// <summary>This method paints the scene using the current component settings at the specified <b>CwHit</b>.</summary>
		public void HandleHitCoord(bool preview, int priority, float pressure, int seed, CwHit hit, Quaternion rotation)
		{
			var model = hit.Transform.GetComponent<CwModel>();

			if (model != null)
			{
				var paintableTextures = model.FindPaintableTextures(group);

				for (var i = paintableTextures.Count - 1; i >= 0; i--)
				{
					var paintableTexture = paintableTextures[i];
					var coord            = paintableTexture.GetCoord(ref hit);

					if (modifiers != null && modifiers.Count > 0)
					{
						var position = (Vector3)coord;

						CwHelper.BeginSeed(seed);
							modifiers.ModifyPosition(ref position, preview, pressure);
						CwHelper.EndSeed();

						coord = position;
					}

					CwCommandDecal2D.Instance.SetState(preview, priority);
					CwCommandDecal2D.Instance.SetLocation(coord);

					HandleHitCommon(preview, pressure, seed, rotation);

					CwCommandDecal2D.Instance.ClearMask();

					CwCommandDecal2D.Instance.ApplyAspect(paintableTexture.Current);

					CwPaintableManager.Submit(CwCommandDecal2D.Instance, model, paintableTexture);
				}
			}
		}

		private Vector3 HandleHitCommon(bool preview, float pressure, int seed, Quaternion rotation)
		{
			var finalOpacity  = opacity;
			var finalRadius   = radius;
			var finalScale    = scale;
			var finalColor    = color;
			var finalAngle    = angle;
			var finalTexture  = texture;
			var finalMatrix   = tileTransform != null ? tileTransform.localToWorldMatrix : Matrix4x4.identity;

			if (modifiers != null && modifiers.Count > 0)
			{
				CwHelper.BeginSeed(seed);
					modifiers.ModifyColor(ref finalColor, preview, pressure);
					modifiers.ModifyAngle(ref finalAngle, preview, pressure);
					modifiers.ModifyOpacity(ref finalOpacity, preview, pressure);
					modifiers.ModifyRadius(ref finalRadius, preview, pressure);
					modifiers.ModifyScale(ref finalScale, preview, pressure);
					modifiers.ModifyTexture(ref finalTexture, preview, pressure);
				CwHelper.EndSeed();
			}

			var finalAspect = PaintCore.CwCommon.GetAspect(shape, finalTexture);
			var finalSize   = PaintCore.CwCommon.ScaleAspect(finalScale * finalRadius, finalAspect);

			CwCommandDecal2D.Instance.SetShape(rotation, finalSize, finalAngle);

			CwCommandDecal2D.Instance.SetMaterial(blendMode, finalTexture, shape, shapeChannel, finalColor, finalOpacity, tileTexture, finalMatrix, tileOpacity, tileTransition);

			return finalSize;
		}

		private void HandleMaskCommon(Vector3 worldPosition)
		{
			if (findMask == true)
			{
				var mask = CwMask.Find(worldPosition, layers);

				if (mask != null)
				{
					CwCommandDecal2D.Instance.SetMask(mask.Matrix, mask.Texture, mask.Channel, mask.Invert, mask.Stretch);
				}
				else
				{
					CwCommandDecal2D.Instance.ClearMask();
				}
			}
			else
			{
				CwCommandDecal2D.Instance.ClearMask();
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			var m = Matrix4x4.TRS(transform.position, transform.rotation, scale * radius * 2.0f);

			Gizmos.matrix = m;

			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

			if (shape != null)
			{
				for (var i = 0; i < 16; i++)
				{
					var subMatrix = m * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, Mathf.Lerp(-0.5f, 0.5f, i / 15.0f)));

					PaintCore.CwCommon.DrawShapeOutline(shape, shapeChannel, subMatrix);
				}
			}

			if (texture != null)
			{
				if (blendMode.Index == CwBlendMode.ALPHA_BLEND || blendMode.Index == CwBlendMode.ALPHA_BLEND_INVERSE || blendMode.Index == CwBlendMode.PREMULTIPLIED)
				{
					for (var i = 0; i < 16; i++)
					{
						var subMatrix = m * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, Mathf.Lerp(-0.5f, 0.5f, i / 15.0f)));

						PaintCore.CwCommon.DrawShapeOutline(texture, CwChannel.Alpha, subMatrix);
					}
				}
			}
		}
#endif
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using UnityEditor;
	using TARGET = CwPaintDecal2D;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwPaintDecal2D_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Layers == 0 && t.TargetModel == null));
				Draw("layers", "Only the CwModel/CwPaintableSprite GameObjects whose layers are within this mask will be eligible for painting.");
			EndError();
			Draw("group", "Only the CwPaintableTexture components with a matching group will be painted by this component.");

			Separator();

			Draw("blendMode", "This allows you to choose how the paint from this component will combine with the existing pixels of the textures you paint.\n\nNOTE: See the Blend Mode section of the documentation for more information.");
			BeginError(Any(tgts, t => t.Texture == null && t.Shape == null));
				Draw("texture", "The texture that will be painted.");
			EndError();
			EditorGUILayout.BeginHorizontal();
				BeginError(Any(tgts, t => t.BlendMode.Index == CwBlendMode.REPLACE && t.Shape == null));
					Draw("shape", "This allows you to specify the shape of the texture. This is optional for most blending modes, because they usually derive their shape from the RGB or A values. However, if you're using the Replace blending mode, then you must manually specify the shape.");
				EndError();
				EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeChannel"), GUIContent.none, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();
			Draw("color", "The color of the paint.");
			Draw("opacity", "The opacity of the brush.");

			Separator();

			Draw("angle", "The angle of the texture in degrees.");
			Draw("scale", "This allows you to control the mirroring and aspect ratio of the texture.\n\n1, 1 = No scaling.\n-1, 1 = Horizontal Flip.");
			BeginError(Any(tgts, t => t.Radius <= 0.0f));
				Draw("radius", "The radius of the paint brush.");
			EndError();

			Separator();

			if (DrawFoldout("Advanced", "Show advanced settings?") == true)
			{
				BeginIndent();
					Draw("targetModel", "If this is set, then only the specified CwModel/CwPaintableSprite will be painted, regardless of the layer setting.");
					Draw("targetTexture", "If this is set, then only the specified CwPaintableTexture will be painted, regardless of the layer or group setting.");
					Draw("findMask", "If your scene contains a <b>CwMask</b>, should this paint component use it?");

					Separator();

					Draw("tileTexture", "This allows you to apply a tiled detail texture to your texture. This tiling will be applied in world space using triplanar mapping.");
					Draw("tileTransform", "This allows you to adjust the tiling position + rotation + scale using a Transform.");
					Draw("tileOpacity", "This allows you to control the triplanar influence.\n\n0 = No influence.\n\n1 = Full influence.");
					Draw("tileTransition", "This allows you to control how quickly the triplanar mapping transitions between the X/Y/Z planes.");
				EndIndent();
			}

			Separator();

			tgt.Modifiers.DrawEditorLayout(serializedObject, target, "Color", "Angle", "Opacity", "Radius", "Scale", "Texture", "Position");
		}
	}
}
#endif