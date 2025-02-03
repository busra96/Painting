#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PaintCore;

namespace PaintIn2D
{
	public class CwContext
	{
		[MenuItem("CONTEXT/SpriteRenderer/Make Paintable (Paint in 2D)", true)]
		private static bool MeshRendererMakePaintableValidate(MenuCommand menuCommand)
		{
			var gameObject = GetGameObject(menuCommand); return gameObject.GetComponent<CwModel>() == null;
		}

		[MenuItem("CONTEXT/SpriteRenderer/Make Paintable (Paint in 2D)", false)]
		private static void MeshRendererMakePaintable(MenuCommand menuCommand)
		{
			var gameObject = GetGameObject(menuCommand); AddSingleComponent<CwPaintableSprite>(gameObject); AddSingleComponent<CwPaintableSpriteTexture>(gameObject);
		}

		private static void AddSingleComponent<T>(GameObject gameObject, System.Action<T> action = null)
			where T : Component
		{
			if (gameObject != null)
			{
				if (gameObject.GetComponent<T>() == null)
				{
					var component = Undo.AddComponent<T>(gameObject);

					if (action != null)
					{
						action(component);
					}
				}
			}
		}

		private static GameObject GetGameObject(MenuCommand menuCommand)
		{
			if (menuCommand != null)
			{
				var component = menuCommand.context as Component;

				if (component != null)
				{
					return component.gameObject;
				}
			}

			return null;
		}
	}
}
#endif