using UnityEngine;

namespace PaintInEditor
{
    /// <summary>This allows you to mark a string as a reference to a <b>CwEditorTextureProfile</b>.</summary>
    public class CwEditorTextureProfileAttribute : PropertyAttribute
    {
    }
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using UnityEditor;
	using CW.Common;

	[CustomPropertyDrawer(typeof(CwEditorTextureProfileAttribute))]
	public class CwEditorTextureProfileDrawer : PropertyDrawer
	{
		public static void Draw(Rect position, SerializedProperty property)
		{
			var groupData = CwEditorTextureProfile_Editor.CachedInstances.Find(p => p != null && p.name == property.stringValue);

			CwEditor.BeginError(groupData == null);
				if (GUI.Button(position, groupData != null ? groupData.name : "MISSING: " + property.stringValue, EditorStyles.popup) == true)
				{
					var menu = new GenericMenu();

					foreach (var p in CwEditorTextureProfile_Editor.CachedInstances)
					{
						if (p != null)
						{
							menu.AddItem(new GUIContent(p.name), p.name == property.stringValue, () => { property.stringValue = p.name; property.serializedObject.ApplyModifiedProperties(); });
						}
					}

					menu.DropDown(position);
				}
			CwEditor.EndError();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var right = position; right.xMin += EditorGUIUtility.labelWidth;

			EditorGUI.LabelField(position, label);

			Draw(right, property);
		}
	}
}
#endif