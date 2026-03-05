using PrefabBoard.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.UI
{
    [CustomEditor(typeof(PrefabBoardAsset))]
    public sealed class PrefabBoardAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open", GUILayout.Width(120f), GUILayout.Height(24f)))
                {
                    PrefabBoardWindow.OpenBoard((PrefabBoardAsset)target);
                }
            }
        }
    }
}
