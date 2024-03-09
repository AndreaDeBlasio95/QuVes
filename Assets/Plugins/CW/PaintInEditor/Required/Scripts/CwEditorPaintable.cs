using CW.Common;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PaintInEditor
{
	/// <summary>This class handles the current state of each texture you're currently painting in the editor.</summary>
	public class CwEditorPaintable
	{
		public class OriginalState
		{
			public List<Material> Materials = new List<Material>();

			public List<Material> Clones = new List<Material>();

			public MaterialPropertyBlock MainProperties;

			public List<MaterialPropertyBlock> Properties = new List<MaterialPropertyBlock>();
		}

		public bool NeedsExporting = true;

		public Renderer Target;

		public Object TargetSource;

		public int MaterialIndex;

		public string SlotName;

		public string SlotKeywords;

		public int SlotChannel;

		public CwEditorTextureProfile TextureProfile;

		public RenderTexture Current;

		public Texture Original;

		public Texture2D Asset;

		public Mesh paintMesh;

		private static MaterialPropertyBlock tempProperties = new MaterialPropertyBlock();

		private static List<Material> tempMaterials = new List<Material>();

		public List<RenderTexture> UndoStates = new List<RenderTexture>();

		public List<RenderTexture> RedoStates = new List<RenderTexture>();

		private static Stack<Material> materialPool = new Stack<Material>();

		private static Stack<OriginalState> statesPool = new Stack<OriginalState>();

		private static Stack<MaterialPropertyBlock> propertyPool = new Stack<MaterialPropertyBlock>();

		private static Dictionary<Renderer, OriginalState> originalStates = new Dictionary<Renderer, OriginalState>();

		private static List<Material[]> cache = new List<Material[]>();

		public bool CanResize(Vector2Int newSize)
		{
			return Current != null && newSize.x > 0 && newSize.y > 0;
		}

		public void FullDilate()
		{
			Dilate(Mathf.Max(Current.width, Current.height) / 4);
		}

		public void Dilate(int steps)
		{
			CwEditorDilate.Dilate(Current, paintMesh, SlotChannel, steps);
		}

		public void Resize(Vector2Int newSize)
		{
			if (CanResize(newSize) == true)
			{
				var copy      = CwCommon.GetRenderTexture(Current);
				var oldActive = RenderTexture.active;

				Graphics.Blit(Current, copy);

				Current.Release();
				Current.width  = newSize.x;
				Current.height = newSize.y;
				Current.Create();

				Graphics.Blit(copy, Current);

				CwCommon.ReleaseRenderTexture(copy);

				RenderTexture.active = oldActive;
			}
		}

		public static Material[] ToArray(List<Material> list)
		{
			while (cache.Count <= list.Count)
			{
				cache.Add(new Material[cache.Count]);
			}

			var array = cache[list.Count];

			list.CopyTo(array, 0);

			return array;
		}

		private static Material GetClone(Material m)
		{
			if (m != null)
			{
				while (materialPool.Count > 0)
				{
					var clone = materialPool.Pop();

					if (clone != null)
					{
						clone.shader = m.shader;
						clone.CopyPropertiesFromMaterial(m);

						return clone;
					}
				}

				return new Material(m);
			}

			return null;
		}

		public void Apply()
		{
			if (Target != null)
			{
				var originalState = default(OriginalState);

				if (originalStates.TryGetValue(Target, out originalState) == false)
				{
					originalState = statesPool.Count > 0 ? statesPool.Pop() : new OriginalState();

					Target.GetSharedMaterials(originalState.Materials);

					Target.GetPropertyBlock(tempProperties);

					if (tempProperties.isEmpty == false)
					{
						var mainProperties = propertyPool.Count > 0 ? propertyPool.Pop() : new MaterialPropertyBlock();

						Target.GetPropertyBlock(mainProperties);

						originalState.MainProperties = mainProperties;
					}

					for (var i = 0; i < originalState.Materials.Count; i++)
					{
						var clone = GetClone(originalState.Materials[i]);

						originalState.Clones.Add(clone);

						Target.GetPropertyBlock(tempProperties, i);

						if (tempProperties.isEmpty == false)
						{
							var properties = propertyPool.Count > 0 ? propertyPool.Pop() : new MaterialPropertyBlock();

							Target.GetPropertyBlock(properties, i);

							originalState.Properties.Add(properties);
						}
						else
						{
							originalState.Properties.Add(null);
						}
					}

					Target.sharedMaterials = ToArray(originalState.Clones);

					originalStates.Add(Target, originalState);
				}

				if (MaterialIndex >= 0 && MaterialIndex < originalState.Materials.Count)
				{
					originalState.Clones[MaterialIndex].EnableKeyword(SlotKeywords);

					Target.GetPropertyBlock(tempProperties, MaterialIndex);

					tempProperties.SetTexture(SlotName, Current);

					Target.SetPropertyBlock(tempProperties, MaterialIndex);
				}
			}
		}

		public static void RemoveAll()
		{
			foreach (var pair in originalStates)
			{
				var renderer      = pair.Key;
				var originalState = pair.Value;

				if (renderer != null)
				{
					renderer.sharedMaterials = ToArray(originalState.Materials);

					renderer.SetPropertyBlock(originalState.MainProperties);

					for (var i = 0; i < originalState.Properties.Count; i++)
					{
						renderer.SetPropertyBlock(originalState.Properties[i], i);
					}
				}

				originalState.Materials.Clear();

				foreach (var c in originalState.Clones)
				{
					if (c != null)
					{
						materialPool.Push(c);
					}
				}

				if (originalState.MainProperties != null)
				{
					originalState.MainProperties.Clear();

					originalState.MainProperties = null;
				}

				originalState.Clones.Clear();

				foreach (var p in originalState.Properties)
				{
					if (p != null)
					{
						p.Clear();

						propertyPool.Push(p);
					}
				}

				originalState.Properties.Clear();

				statesPool.Push(originalState);
			}

			originalStates.Clear();
		}

		public void AddUndoState()
		{
			// Clear redo state
			foreach (var t in RedoStates)
			{
				t.Release();

				Object.DestroyImmediate(t);
			}

			RedoStates.Clear();

			// Store new state
			var state = new RenderTexture(Current);

			Graphics.Blit(Current, state);

			UndoStates.Insert(0, state);

			// Make sure there aren't too many statess
			while (UndoStates.Count > CwPaintInEditor.Settings.MaxUndoRedo)
			{
				var t = UndoStates[UndoStates.Count - 1];

				t.Release();

				Object.DestroyImmediate(t);

				UndoStates.RemoveAt(UndoStates.Count - 1);
			}
		}

		public void Undo()
		{
			if (UndoStates.Count > 0)
			{
				RedoStates.Insert(0, Current);

				Current = UndoStates[0];

				UndoStates.RemoveAt(0);

				Apply();
			}
		}

		public void Redo()
		{
			if (RedoStates.Count > 0)
			{
				UndoStates.Insert(0, Current);

				Current = RedoStates[0];

				RedoStates.RemoveAt(0);

				Apply();
			}
		}

		private Vector2Int CalculateSize(Texture t)
		{
			var size = new Vector2Int(CwPaintInEditor.Settings.MinResolution, CwPaintInEditor.Settings.MinResolution);

			if (t != null && CwPaintInEditor.Settings.ForceResolution == false)
			{
				size.x = Mathf.Max(size.x, t.width );
				size.y = Mathf.Max(size.y, t.height);
			}

			return size;
		}

		private void GetExistingOrDefault(ref Vector2Int size, ref Texture texture)
		{
			if (Target.HasPropertyBlock() == true)
			{
				Target.GetPropertyBlock(tempProperties, MaterialIndex);

				var tex = tempProperties.GetTexture(SlotName);

				if (tex != null)
				{
					size    = CalculateSize(tex);
					texture = tex;

					return;
				}
			}

			var sr = Target as SpriteRenderer;

			if (sr != null && sr.sprite != null)
			{
				var tex = sr.sprite.texture;

				if (tex != null)
				{
					size    = CalculateSize(tex);
					texture = tex;

					return;
				}
			}

			Target.GetSharedMaterials(tempMaterials);

			if (tempMaterials.Count > MaterialIndex)
			{
				var material = tempMaterials[MaterialIndex];

				if (material != null)
				{
					var tex = material.GetTexture(SlotName);

					if (tex != null)
					{
						size    = CalculateSize(tex);
						texture = tex;

						return;
					}
				}
			}

			
			size    = CalculateSize(TextureProfile.DefaultTexture);
			texture = TextureProfile.DefaultTexture;
		}

		public void Initialize()
		{
			var size    = default(Vector2Int);
			var texture = default(Texture);

			GetExistingOrDefault(ref size, ref texture);

			var desc = new RenderTextureDescriptor(size.x, size.y, RenderTextureFormat.ARGB64, 0);

			Current  = new RenderTexture(desc);
			Original = texture;

			var path = AssetDatabase.GetAssetPath(Original);

			if (string.IsNullOrEmpty(path) == false)
			{
				if (path.EndsWith(".png", System.StringComparison.InvariantCultureIgnoreCase) ||
					path.EndsWith(".tga", System.StringComparison.InvariantCultureIgnoreCase))
				{
					Asset = Original as Texture2D;
				}
			}

			Current.wrapMode = TextureWrapMode.Repeat;

			Current.Create();

			if (TextureProfile.IsNormalMap == true)
			{
				CwCommon.BlitNormal(Current, texture);
			}
			else
			{
				Graphics.Blit(texture, Current);
			}

			if (Current.useMipMap == true && Current.autoGenerateMips == false)
			{
				Current.GenerateMips();
			}
		}

		public void Erase()
		{
			if (Current != null)
			{
				if (TextureProfile.IsNormalMap == true)
				{
					CwCommon.BlitNormal(Current, Original);
				}
				else
				{
					Graphics.Blit(Original, Current);
				}
			}
		}

		public void Clear()
		{
			if (Current != null)
			{
				if (Current != null) Current.Release();

				Object.DestroyImmediate(Current);

				Current = null;
			}

			foreach (var t in UndoStates)
			{
				if (t != null) t.Release();

				Object.DestroyImmediate(t);
			}

			UndoStates.Clear();

			foreach (var t in RedoStates)
			{
				if (t != null) t.Release();

				Object.DestroyImmediate(t);
			}

			RedoStates.Clear();

			if (paintMesh != null)
			{
				Object.DestroyImmediate(paintMesh);

				paintMesh = null;
			}
		}
	}
}