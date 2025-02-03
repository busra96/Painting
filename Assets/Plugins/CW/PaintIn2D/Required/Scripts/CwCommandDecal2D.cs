using System.Collections.Generic;
using UnityEngine;
using CW.Common;
using PaintCore;

namespace PaintIn2D
{
	/// <summary>This class manages the decal 2D painting command.</summary>
	public class CwCommandDecal2D : CwCommand
	{
		public CwBlendMode     Blend;
		public Vector3         Position;
		public Vector3         EndPosition;
		public Vector3         Position2;
		public Vector3         EndPosition2;
		public int             Extrusions;
		public bool            Clip;
		public Matrix4x4       Matrix;
		public Vector3         Direction;
		public Color           Color;
		public float           Opacity;
		public CwHashedTexture Texture;
		public CwHashedTexture Shape;
		public Vector4         ShapeChannel;
		public CwHashedTexture TileTexture;
		public Matrix4x4       TileMatrix;
		public float           TileOpacity;
		public float           TileTransition;
		public Matrix4x4       MaskMatrix;
		public CwHashedTexture MaskShape;
		public Vector4         MaskChannel;
		public Vector3         MaskStretch;
		public Vector2         MaskInvert;

		public static CwCommandDecal2D Instance = new CwCommandDecal2D();

		private static Stack<CwCommandDecal2D> pool = new Stack<CwCommandDecal2D>();

		private static Material cachedSpotMaterial;
		private static Material cachedLineMaterial;
		private static Material cachedQuadMaterial;
		private static Material cachedLineClipMaterial;
		private static Material cachedQuadClipMaterial;

		private static int cachedSpotMaterialHash;
		private static int cachedLineMaterialHash;
		private static int cachedQuadMaterialHash;
		private static int cachedLineClipMaterialHash;
		private static int cachedQuadClipMaterialHash;

		public override bool RequireMesh { get { return true; } }

		private static int _Position       = Shader.PropertyToID("_Position");
		private static int _EndPosition    = Shader.PropertyToID("_EndPosition");
		private static int _Position2      = Shader.PropertyToID("_Position2");
		private static int _EndPosition2   = Shader.PropertyToID("_EndPosition2");
		private static int _Matrix         = Shader.PropertyToID("_Matrix");
		private static int _Direction      = Shader.PropertyToID("_Direction");
		private static int _Color          = Shader.PropertyToID("_Color");
		private static int _Opacity        = Shader.PropertyToID("_Opacity");
		private static int _Texture        = Shader.PropertyToID("_Texture");
		private static int _Shape          = Shader.PropertyToID("_Shape");
		private static int _ShapeChannel   = Shader.PropertyToID("_ShapeChannel");
		private static int _TileTexture    = Shader.PropertyToID("_TileTexture");
		private static int _TileMatrix     = Shader.PropertyToID("_TileMatrix");
		private static int _TileOpacity    = Shader.PropertyToID("_TileOpacity");
		private static int _TileTransition = Shader.PropertyToID("_TileTransition");
		private static int _MaskMatrix     = Shader.PropertyToID("_MaskMatrix");
		private static int _MaskTexture    = Shader.PropertyToID("_MaskTexture");
		private static int _MaskChannel    = Shader.PropertyToID("_MaskChannel");
		private static int _MaskStretch    = Shader.PropertyToID("_MaskStretch");
		private static int _MaskInvert     = Shader.PropertyToID("_MaskInvert");

		static CwCommandDecal2D()
		{
			BuildMaterial(ref cachedSpotMaterial, ref cachedSpotMaterialHash, "Hidden/PaintIn2D/CwDecal2D");
			BuildMaterial(ref cachedLineMaterial, ref cachedLineMaterialHash, "Hidden/PaintIn2D/CwDecal2D", "CW_LINE");
			BuildMaterial(ref cachedQuadMaterial, ref cachedQuadMaterialHash, "Hidden/PaintIn2D/CwDecal2D", "CW_QUAD");
			BuildMaterial(ref cachedLineClipMaterial, ref cachedLineClipMaterialHash, "Hidden/PaintIn2D/CwDecal2D", "CW_LINE_CLIP");
			BuildMaterial(ref cachedQuadClipMaterial, ref cachedQuadClipMaterialHash, "Hidden/PaintIn2D/CwDecal2D", "CW_QUAD_CLIP");
		}

		public override void Apply(Material material)
		{
			base.Apply(material);

			Blend.Apply(material);

			var inv = Matrix.inverse;

			material.SetVector(_Position, inv.MultiplyPoint(Position));
			material.SetVector(_EndPosition, inv.MultiplyPoint(EndPosition));
			material.SetVector(_Position2, inv.MultiplyPoint(Position2));
			material.SetVector(_EndPosition2, inv.MultiplyPoint(EndPosition2));
			material.SetMatrix(_Matrix, inv);
			material.SetVector(_Direction, Direction);
			material.SetColor(_Color, CwHelper.ToLinear(Color));
			material.SetFloat(_Opacity, Opacity);
			material.SetTexture(_Texture, Texture);
			material.SetTexture(_Shape, Shape);
			material.SetVector(_ShapeChannel, ShapeChannel);
			material.SetTexture(_TileTexture, TileTexture);
			material.SetMatrix(_TileMatrix, TileMatrix);
			material.SetFloat(_TileOpacity, TileOpacity);
			material.SetFloat(_TileTransition, TileTransition);
			material.SetMatrix(_MaskMatrix, MaskMatrix);
			material.SetTexture(_MaskTexture, MaskShape);
			material.SetVector(_MaskChannel, MaskChannel);
			material.SetVector(_MaskStretch, MaskStretch);
			material.SetVector(_MaskInvert, MaskInvert);
		}

		public override void Pool()
		{
			pool.Push(this);
		}

		public override void Transform(Matrix4x4 posMatrix, Matrix4x4 rotMatrix, Matrix4x4 rotMatrix2)
		{
			Position     = posMatrix.MultiplyPoint(Position);
			EndPosition  = posMatrix.MultiplyPoint(EndPosition);
			Position2    = posMatrix.MultiplyPoint(Position2);
			EndPosition2 = posMatrix.MultiplyPoint(EndPosition2);
			Matrix       = rotMatrix * Matrix * rotMatrix2;
			Direction    = Matrix.MultiplyVector(Vector3.forward).normalized;
		}

		public override CwCommand SpawnCopy()
		{
			var command = SpawnCopy(pool);

			command.Blend          = Blend;
			command.Position       = Position;
			command.EndPosition    = EndPosition;
			command.Position2      = Position2;
			command.EndPosition2   = EndPosition2;
			command.Extrusions     = Extrusions;
			command.Clip           = Clip;
			command.Matrix         = Matrix;
			command.Direction      = Direction;
			command.Color          = Color;
			command.Opacity        = Opacity;
			command.Texture        = Texture;
			command.Shape          = Shape;
			command.ShapeChannel   = ShapeChannel;
			command.TileTexture    = TileTexture;
			command.TileMatrix     = TileMatrix;
			command.TileOpacity    = TileOpacity;
			command.TileTransition = TileTransition;
			command.MaskMatrix     = MaskMatrix;
			command.MaskShape      = MaskShape;
			command.MaskChannel    = MaskChannel;
			command.MaskStretch    = MaskStretch;
			command.MaskInvert     = MaskInvert;

			return command;
		}

		public override void Apply(CwPaintableTexture paintableTexture)
		{
			base.Apply(paintableTexture);

			if (Blend.Index == CwBlendMode.REPLACE_ORIGINAL || Blend.Index == CwBlendMode.NORMAL_REPLACE_ORIGINAL)
			{
				Blend.Color   = paintableTexture.Color;
				Blend.Texture = paintableTexture.Texture;
			}
		}

		/// <summary>This method allows you to set the shape and rotation of the texture.
		/// NOTE: The <b>rotation</b> argument is in world space, where <b>Quaternion.identity</b> means the paint faces forward on the +Z axis, and up is +Y.</summary>
		public void SetShape(Quaternion rotation, Vector3 size, float angle)
		{
			Matrix    = Matrix4x4.TRS(Vector3.zero, rotation * Quaternion.Euler(0.0f, 0.0f, angle), size);
			Direction = rotation * Vector3.forward;
		}

		public void SetLocation(Vector3 position)
		{
			Extrusions = 0;
			Clip       = false;
			Position   = position;
		}

		public void SetLocation(Vector3 position, Vector3 endPosition, bool clip = false)
		{
			Extrusions  = 1;
			Clip        = clip;
			Position    = position;
			EndPosition = endPosition;
		}

		public void SetLocation(Vector3 positionA, Vector3 positionB, Vector3 positionC)
		{
			Extrusions   = 2;
			Clip         = false;
			Position     = positionA;
			EndPosition  = positionB;
			Position2    = positionC;
			EndPosition2 = positionA;
		}

		public void SetLocation(Vector3 position, Vector3 endPosition, Vector3 position2, Vector3 endPosition2, bool clip = false)
		{
			Extrusions   = 2;
			Clip         = clip;
			Position     = position;
			EndPosition  = endPosition;
			Position2    = position2;
			EndPosition2 = endPosition2;
		}

		public void ClearMask()
		{
			MaskShape   = null;
			MaskChannel = Vector3.one;
			MaskInvert  = new Vector2(0.0f, 1.0f);
		}

		public void SetMask(Matrix4x4 matrix, Texture shape, CwChannel channel, bool invert, Vector3 stretch)
		{
			MaskMatrix  = matrix;
			MaskShape   = shape;
			MaskChannel = PaintCore.CwCommon.IndexToVector((int)channel);
			MaskStretch = new Vector3(stretch.x * 2.0f, stretch.y * 2.0f, 2.0f);
			MaskInvert  = invert ? new Vector2(1.0f, -1.0f) : new Vector2(0.0f, 1.0f);
		}

		public void ApplyAspect(Texture texture)
		{
			if (texture != null)
			{
				var width  = texture.width;
				var height = texture.height;

				if (width > height)
				{
					Matrix.m00 *= height / (float)width;
				}
				else
				{
					Matrix.m00 *= width / (float)height;
				}
			}
		}

		public void SetMaterial(CwBlendMode blendMode, Texture texture, Texture shape, CwChannel shapeChannel, Color color, float opacity, Texture tileTexture, Matrix4x4 tileMatrix, float tileOpacity, float tileTransition)
		{
			switch (Extrusions)
			{
				case 0:
				{
					Material = new CwHashedMaterial(cachedSpotMaterial, cachedSpotMaterialHash);
				}
				break;

				case 1:
				{
					if (Clip == true)
					{
						Material = new CwHashedMaterial(cachedLineClipMaterial, cachedLineClipMaterialHash);
					}
					else
					{
						Material = new CwHashedMaterial(cachedLineMaterial, cachedLineMaterialHash);
					}
				}
				break;

				case 2:
				{
					if (Clip == true)
					{
						Material = new CwHashedMaterial(cachedQuadClipMaterial, cachedQuadClipMaterialHash);
					}
					else
					{
						Material = new CwHashedMaterial(cachedQuadMaterial, cachedQuadMaterialHash);
					}
				}
				break;
			}

			Blend          = blendMode;
			Pass           = blendMode;
			Color          = color;
			Opacity        = opacity;
			Texture        = texture;
			Shape          = shape;
			ShapeChannel   = PaintCore.CwCommon.IndexToVector((int)shapeChannel);
			TileTexture    = tileTexture;
			TileMatrix     = tileMatrix;
			TileOpacity    = tileOpacity;
			TileTransition = tileTransition;
		}
	}
}