using System.IO;
using PrefabBoard.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class PreviewRigSettingsProvider
    {
        public const string SettingsAssetPath = "Assets/Editor/PrefabBoard/Settings/PreviewRigSettings.asset";

        public static PreviewRigSettingsAsset TryGetSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PreviewRigSettingsAsset>(SettingsAssetPath);
            if (settings != null)
            {
                return settings;
            }

            var guids = AssetDatabase.FindAssets("t:PreviewRigSettingsAsset");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PreviewRigSettingsAsset>(path);
        }

        public static PreviewRigSettingsAsset GetOrCreateSettings()
        {
            var settings = TryGetSettings();
            if (settings != null)
            {
                return settings;
            }

            EnsureFolderExists(SettingsAssetPath);
            settings = ScriptableObject.CreateInstance<PreviewRigSettingsAsset>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }

        [MenuItem("Tools/PrefabBoard/Preview Rig Settings")]
        private static void OpenSettings()
        {
            var settings = GetOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        private static void EnsureFolderExists(string assetPath)
        {
            var folderPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
