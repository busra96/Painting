using UnityEngine;
using CW.Common;
using PaintCore;

namespace PaintIn2D
{
	/// <summary>This component turns the current <b>SpriteRenderer</b> component into a paint mask.</summary>
	[RequireComponent(typeof(SpriteRenderer))]
	[HelpURL(CwCommon.HelpUrlPrefix + "CwMaskSprite")]
	[AddComponentMenu(CwCommon.ComponentMenuPrefix + "Mask Sprite")]
	public class CwMaskSprite : CwMask
	{
		[System.NonSerialized]
		private RenderTexture spriteTexture;

		[System.NonSerialized]
		private Matrix4x4 spriteMatrix = Matrix4x4.identity;

		/*
		[System.NonSerialized]
		private static Mesh tempMesh;

		[System.NonSerialized]
		private static List<Vector3> tempVertices = new List<Vector3>();
		*/

		public override Matrix4x4 Matrix
		{
			get
			{
				return spriteMatrix * transform.worldToLocalMatrix;
			}
		}

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
						return s.packed == false;
					}
				}

				return false;
			}
		}

		/// <summary>This method will update the sprite mask texture. You must manually call this if you modify the mask's <b>Sprite</b>.</summary>
		[ContextMenu("Update Texture")]
		public void UpdateTexture()
		{
			var sr = GetComponent<SpriteRenderer>();

			if (sr != null)
			{
				var s = sr.sprite;

				if (s != null)
				{
					/*
					if (spriteTexture == null)
					{
						spriteTexture = new RenderTexture(Mathf.RoundToInt(s.rect.width), Mathf.RoundToInt(s.rect.height), 0);
					}

					Texture = spriteTexture;

					if (tempMesh == null)
					{
						tempMesh = new Mesh();
					}
					else
					{
						tempMesh.Clear();
					}

					tempVertices.Clear();

					foreach (var v in s.uv)
					{
						tempVertices.Add(v);
					}

					tempMesh.SetVertices(tempVertices);
					tempMesh.SetUVs(0, tempVertices);
					tempMesh.SetTriangles(s.triangles, 0);

					CwBlit.Blit(spriteTexture, tempMesh, 0, s.texture, CwCoord.First);

					spriteMatrix = Matrix4x4.TRS(s.bounds.center, Quaternion.identity, new Vector3(CwHelper.Reciprocal(s.bounds.size.x), CwHelper.Reciprocal(s.bounds.size.y), 1.0f));
					*/

					spriteMatrix = Matrix4x4.TRS(s.bounds.center, Quaternion.identity, new Vector3(CwHelper.Reciprocal(s.bounds.size.x), CwHelper.Reciprocal(s.bounds.size.y), 1.0f));

					Texture = s.texture;
				}
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			UpdateTexture();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			spriteTexture = CwHelper.Destroy(spriteTexture);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn2D
{
	using UnityEditor;
	using TARGET = CwMaskSprite;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwMaskSprite_Editor : CwMask_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();

			if (Any(tgts, t => t.SpriteIsValid == false))
			{
				Error("The mask sprite is part of an atlas. This is not supported.");
			}
		}
	}
}
#endif