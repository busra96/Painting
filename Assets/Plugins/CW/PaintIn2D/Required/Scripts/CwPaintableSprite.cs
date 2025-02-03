using System.Collections.Generic;
using UnityEngine;
using PaintCore;
using System;
using UnityEngine.Events;
using CW.Common;

namespace PaintIn2D
{
	/// <summary>This component can be used to make a <b>SpriteRenderer</b> paintable.
	/// NOTE: This paintable must have a <b>CwPaintableTextureSprite</b> that targets Material 0's <b>_MainTex</b> slot.</summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SpriteRenderer))]
	[HelpURL(CwCommon.HelpUrlPrefix + "CwPaintableSprite")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Paintable Sprite")]
	public class CwPaintableSprite : CwModel
	{
		public enum ActivationType
		{
			Awake,
			OnEnable,
			Start,
			OnFirstUse
		}

		/// <summary>This allows you to control when this component actually activates and becomes ready for painting. You probably don't need to change this.</summary>
		public ActivationType Activation { set { activation = value; } get { return activation; } } [SerializeField] private ActivationType activation = ActivationType.Start;

		/// <summary>Automatically change the <b>CwPaintableSprite</b> component's <b>Width</b> and <b>Height</b> to match the sprite?</summary>
		public bool AutoSize { set { autoSize = value; } get { return autoSize; } } [SerializeField] private bool autoSize = true;

		/// <summary>Automatically change the <b>CwPaintableSprite</b> component's <b>Texture</b> to match the sprite?</summary>
		public bool AutoTexture { set { autoTexture = value; } get { return autoTexture; } } [SerializeField] private bool autoTexture = true;

		/// <summary>Automatically change the <b>CwPaintableSprite</b> component's <b>Advanced/LocalMask</b> to match the sprite?</summary>
		public bool AutoMask { set { autoMask = value; } get { return autoMask; } } [SerializeField] private bool autoMask = true;

		/// <summary>Automatically generate a mesh collider based on this sprite?</summary>
		public bool GenerateMeshCollider { set { generateMeshCollider = value; } get { return generateMeshCollider; } } [SerializeField] private bool generateMeshCollider;

		/// <summary>This event will be invoked before this component is activated.</summary>
		public UnityEvent OnActivating { get { if (onActivating == null) onActivating = new UnityEvent(); return onActivating; } } [SerializeField] private UnityEvent onActivating;

		/// <summary>This event will be invoked after this component is activated.</summary>
		public UnityEvent OnActivated { get { if (onActivated == null) onActivated = new UnityEvent(); return onActivated; } } [SerializeField] private UnityEvent onActivated;

		/// <summary>This event will be invoked before this component is deactivated.</summary>
		public UnityEvent OnDeactivating { get { if (onDeactivating == null) onDeactivating = new UnityEvent(); return onDeactivating; } } [SerializeField] private UnityEvent onDeactivating;

		/// <summary>This event will be invoked after this component is deactivated.</summary>
		public UnityEvent OnDeactivated { get { if (onDeactivated == null) onDeactivated = new UnityEvent(); return onDeactivated; } } [SerializeField] private UnityEvent onDeactivated;

		/// <summary>This lets you know if this paintable has been activated.
		/// Being activated means each associated CwMaterialCloner and CwPaintableTexture has been Activated.
		/// NOTE: If you manually add CwMaterialCloner or CwPaintableTexture components after activation, then you must manually Activate().</summary>
		public override bool IsActivated
		{
			get
			{
				return activated;
			}
		}

		[SerializeField]
		private bool activated;

		[System.NonSerialized]
		private HashSet<CwPaintableSpriteTexture> paintableSpriteTextures = new HashSet<CwPaintableSpriteTexture>();

		[System.NonSerialized]
		private bool cachedSpriteSet;

		[System.NonSerialized]
		private SpriteRenderer cachedSprite;

		[System.NonSerialized]
		private SpriteRenderer cachedSpriteRenderer;

		[System.NonSerialized]
		protected Mesh preparedMesh;

		[System.NonSerialized]
		protected Matrix4x4 preparedMatrix;

		[System.NonSerialized]
		protected Mesh bakedMesh;

		[System.NonSerialized]
		protected bool bakedMeshSet;

		[System.NonSerialized]
		protected static List<Vector3> tempVertices = new List<Vector3>();

		[System.NonSerialized]
		private MaterialPropertyBlock properties;

		[System.NonSerialized]
		private static List<CwPaintableTexture> tempPaintableTextures = new List<CwPaintableTexture>();

		[System.NonSerialized]
		private static List<CwPaintableSpriteTexture> tempPaintableSpriteTextures = new List<CwPaintableSpriteTexture>();

		private List<CwPaintableSpriteTexture> tempTextures = new List<CwPaintableSpriteTexture>();

		public List<CwPaintableSpriteTexture> GetPaintableSpriteTextures()
		{
			tempTextures.Clear();

			GetComponentsInChildren(false, tempTextures);

			for (var i = tempTextures.Count - 1; i >= 0; i--)
			{
				if (tempTextures[i].GetComponentInParent<CwPaintableSprite>() != this)
				{
					tempTextures.RemoveAt(i);
				}
			}

			return tempTextures;
		}

		/// <summary>This method will remove all <b>CwPaintableMesh</b> and <b>CwPaintableTexture</b> components from this GameObject.</summary>
		public override void RemoveComponents()
		{
			var paintableTextures = GetComponents<CwPaintableSpriteTexture>();

			for (var i = paintableTextures.Length - 1; i >= 0; i--)
			{
				var paintableTexture = paintableTextures[i];

				paintableTexture.Deactivate();

				CwHelper.Destroy(paintableTexture);
			}

			CwHelper.Destroy(this);
		}

		protected override void CacheRenderer()
		{
			base.CacheRenderer();

			if (TryCacheRenderer() == false)
			{
				Debug.LogError("This CwPaintableSprite (" + name + ") doesn't have a suitable SpriteRenderer, so it cannot be painted.", this);
			}
		}

		private bool TryCacheRenderer()
		{
			if (cachedRenderer is SpriteRenderer)
			{
				cachedSprite    = (SpriteRenderer)cachedRenderer;
				cachedSpriteSet = true;

				return true;
			}

			return false;
		}

		public override void GetPrepared(ref Mesh mesh, ref Matrix4x4 matrix, CwCoord coord)
		{
			if (prepared == false)
			{
				prepared = true;

				if (cachedRendererSet == false)
				{
					CacheRenderer();
				}

				TryGetPrepared(coord);
			}

			mesh   = preparedMesh;
			matrix = preparedMatrix;
		}

		private void TryGetPrepared(CwCoord coord)
		{
			if (cachedSpriteSet == true)
			{
				TryBakeMesh(cachedSprite.sprite);

				preparedMesh   = bakedMesh;
				preparedMatrix = cachedRenderer.localToWorldMatrix;
			}
		}

		private void ApplyTexture(Texture2D texture)
		{
			foreach (var pst in GetComponentsInChildren<CwPaintableSpriteTexture>())
			{
				if (pst.Texture == null)
				{
					pst.Texture  = texture;
					pst.Existing = CwPaintableTexture.ExistingType.Ignore;

					foreach (var cc in GetComponentsInChildren<CwChangeCounter>())
					{
						if (cc.GetComponentInParent<CwPaintableSpriteTexture>() == pst)
						{
							cc.Texture = texture;
						}
					}
				}
			}
		}

		private void ApplyMask(Texture2D texture)
		{
			foreach (var pst in GetComponentsInChildren<CwPaintableSpriteTexture>())
			{
				if (pst.LocalMaskTexture == null)
				{
					pst.LocalMaskTexture = texture;
					pst.LocalMaskChannel = CwChannel.Alpha;

					foreach (var ptmm in GetComponentsInChildren<CwPaintableTextureMonitorMask>())
					{
						if (ptmm.GetComponentInParent<CwPaintableSpriteTexture>() == pst)
						{
							ptmm.MaskTexture = texture;
							ptmm.MaskChannel = CwChannel.Alpha;
						}
					}
				}
				else
				{
					foreach (var ptmm in GetComponentsInChildren<CwPaintableTextureMonitorMask>())
					{
						if (ptmm.GetComponentInParent<CwPaintableSpriteTexture>() == pst)
						{
							ptmm.PaintableTexture = pst;
							ptmm.MaskTexture      = pst.LocalMaskTexture;
							ptmm.MaskChannel      = pst.LocalMaskChannel;
						}
					}
				}
			}
		}

		public override void Activate()
		{
			cachedSpriteRenderer = GetComponent<SpriteRenderer>();

			var sprite = cachedSpriteRenderer.sprite;

			if (sprite != null)
			{
				GetPaintableSpriteTextures();

				if (autoSize == true)
				{
					foreach (var pst in tempTextures)
					{
						pst.Width  = sprite.texture.width;
						pst.Height = sprite.texture.height;
					}
				}

				if (autoTexture == true)
				{
					ApplyTexture(sprite.texture);
				}

				if (autoMask == true)
				{
					ApplyMask(sprite.texture);
				}

				if (onActivating != null)
				{
					onActivating.Invoke();
				}

				// Activate textures
				AddPaintableTextures(transform);

				foreach (var paintableTexture in paintableSpriteTextures)
				{
					paintableTexture.Activate();
				}

				activated = true;

				if (onActivated != null)
				{
					onActivated.Invoke();
				}

				foreach (var pst in tempTextures)
				{
					if (pst.Slot == new CwSlot(0, "_MainTex"))
					{
						ApplyTexture("_MainTex", pst.Current);

						if (generateMeshCollider == true)
						{
							var meshCollider = GetComponent<MeshCollider>();

							if (meshCollider == null)
							{
								meshCollider = gameObject.AddComponent<MeshCollider>();
							}

							if (meshCollider != null)
							{
								TryBakeMesh(sprite);

								meshCollider.sharedMesh = bakedMesh;
							}
						}
					}
				}
			}
		}

		private void AddPaintableTextures(Transform root)
		{
			root.GetComponents(tempPaintableSpriteTextures);

			foreach (var paintableTexture in tempPaintableSpriteTextures)
			{
				paintableSpriteTextures.Add(paintableTexture);
			}

			tempPaintableSpriteTextures.Clear();

			for (var i = 0; i < root.childCount; i++)
			{
				var child = root.GetChild(i);

				if (child.GetComponent<CwPaintableSprite>() == null)
				{
					AddPaintableTextures(child);
				}
			}
		}

		public void ApplyTexture(string slot, Texture texture)
		{
			if (properties == null)
			{
				properties = new MaterialPropertyBlock();
			}

			cachedSpriteRenderer.GetPropertyBlock(properties);

			properties.SetTexture(slot, texture);

			cachedSpriteRenderer.SetPropertyBlock(properties);
		}

		public override List<CwPaintableTexture> FindPaintableTextures(CwGroup group)
		{
			tempPaintableTextures.Clear();

			foreach (var paintableTexture in paintableSpriteTextures)
			{
				if (paintableTexture.Group == group)
				{
					tempPaintableTextures.Add(paintableTexture);
				}
			}

			return tempPaintableTextures;
		}

		private void TryBakeMesh(Sprite sprite)
		{
			if (bakedMeshSet == false)
			{
				bakedMesh    = new Mesh();
				bakedMeshSet = true;
			}

			bakedMesh.Clear();

			tempVertices.Clear(); foreach (var v in sprite.vertices) tempVertices.Add(v);

			bakedMesh.SetVertices(tempVertices);
			bakedMesh.SetUVs(0, sprite.uv);
			bakedMesh.SetTriangles(sprite.triangles, 0);
			bakedMesh.RecalculateNormals();
			bakedMesh.RecalculateBounds();
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			gameObject.AddComponent<CwPaintableSpriteTexture>();
		}
#endif

		protected virtual void Awake()
		{
			if (activation == ActivationType.Awake && activated == false)
			{
				Activate();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (activation == ActivationType.OnEnable && activated == false)
			{
				Activate();
			}

			CwPaintableManager.GetOrCreateInstance();
		}

		protected virtual void Start()
		{
			if (activation == ActivationType.Start && activated == false)
			{
				Activate();
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwPaintableSprite;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwPaintableSprite_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			if (Any(tgts, t => t.IsActivated == true))
			{
				Info("This component has been activated.");
			}

			if (Any(tgts, t => t.IsActivated == true && Application.isPlaying == false))
			{
				Error("This component shouldn't be activated during edit mode. Deactivate it from the component context menu.");
			}

			Draw("activation", "This allows you to control when this component actually activates and becomes ready for painting. You probably don't need to change this.");
			Draw("autoSize", "Automatically change the <b>CwPaintableSprite</b> component's <b>Width</b> and <b>Height</b> to match the sprite?");
			Draw("autoTexture", "Automatically change the <b>CwPaintableSprite</b> component's <b>Texture</b> to match the sprite?");
			Draw("autoMask", "Automatically change the <b>CwPaintableSprite</b> component's <b>Advanced/LocalMask</b> to match the sprite?");

			Separator();

			if (DrawFoldout("Advanced", "Show advanced settings?") == true)
			{
				BeginIndent();
					Draw("generateMeshCollider", "Automatically generate a mesh collider based on this sprite?");
					Draw("hash", "The hash code for this model used for de/serialization of this instance.");
					Draw("onActivating");
					Draw("onActivated");
					Draw("onDeactivating");
					Draw("onDeactivated");
				EndIndent();
			}
		}
	}
}
#endif