using PrefabBoard.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class PreviewTestSceneMenu
    {
        private const int MinCanvasSize = 64;
        private const int MaxCanvasSize = 4096;
        private const string DefaultScenePath = "Assets/Scenes/Test.unity";

        [MenuItem("Tools/PrefabBoard/Create Test Scene/From Last Preview Capture")]
        private static void CreateFromLastCapture()
        {
            if (PreviewCache.TryCreateTestSceneFromLastCapture(out var scenePath, out var error))
            {
                Debug.Log("PrefabBoard: Test scene created at " + scenePath + " from last preview capture.");
                AssetDatabase.Refresh();
                return;
            }

            var message = string.IsNullOrEmpty(error) ? "Unknown error." : error;
            Debug.LogWarning("PrefabBoard: Failed to create test scene from capture. " + message);
            EditorUtility.DisplayDialog("PrefabBoard", "Test scene was not created.\n" + message, "OK");
        }

        [MenuItem("Tools/PrefabBoard/Create Test Scene/From Selected Prefab")]
        private static void CreateFromSelectedPrefab()
        {
            if (!TryGetSelectedPrefabGuid(out var prefabGuid, out var prefabPath))
            {
                EditorUtility.DisplayDialog(
                    "PrefabBoard",
                    "Select a prefab asset in Project and run this command again.",
                    "OK");
                return;
            }

            var canvasSize = ResolveCanvasSize();
            var renderMode = ResolveRenderMode(prefabGuid);
            if (PreviewCache.TryCreateTestScene(prefabGuid, canvasSize, renderMode, DefaultScenePath, out var error))
            {
                Debug.Log("PrefabBoard: Test scene created at " + DefaultScenePath + " for " + prefabPath + ".");
                AssetDatabase.Refresh();
                return;
            }

            var message = string.IsNullOrEmpty(error) ? "Unknown error." : error;
            Debug.LogWarning("PrefabBoard: Failed to create test scene for selected prefab. " + message);
            EditorUtility.DisplayDialog("PrefabBoard", "Test scene was not created.\n" + message, "OK");
        }

        [MenuItem("Tools/PrefabBoard/Create Test Scene/From Selected Prefab", true)]
        private static bool ValidateCreateFromSelectedPrefab()
        {
            return TryGetSelectedPrefabGuid(out _, out _);
        }

        private static bool TryGetSelectedPrefabGuid(out string prefabGuid, out string prefabPath)
        {
            prefabGuid = string.Empty;
            prefabPath = string.Empty;

            var selected = Selection.activeObject as GameObject;
            if (selected == null)
            {
                return false;
            }

            prefabPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return false;
            }

            if (PrefabUtility.GetPrefabAssetType(selected) == PrefabAssetType.NotAPrefab)
            {
                return false;
            }

            prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            return !string.IsNullOrEmpty(prefabGuid);
        }

        private static Vector2Int ResolveCanvasSize()
        {
            var captureSize = PreviewDebugCapture.CanvasSize;
            if (captureSize.x >= MinCanvasSize && captureSize.y >= MinCanvasSize)
            {
                return captureSize;
            }

            if (GameViewResolutionUtils.TryGetResolution(out var resolution))
            {
                return ClampCanvasSize(new Vector2Int(
                    Mathf.RoundToInt(resolution.x),
                    Mathf.RoundToInt(resolution.y)));
            }

            return new Vector2Int(1920, 1080);
        }

        private static Vector2Int ClampCanvasSize(Vector2Int raw)
        {
            var width = Mathf.Clamp(raw.x, MinCanvasSize, MaxCanvasSize);
            var height = Mathf.Clamp(raw.y, MinCanvasSize, MaxCanvasSize);
            return new Vector2Int(width, height);
        }

        private static BoardItemPreviewRenderMode ResolveRenderMode(string prefabGuid)
        {
            if (!string.IsNullOrEmpty(prefabGuid) &&
                prefabGuid == PreviewDebugCapture.PrefabGuid)
            {
                return PreviewDebugCapture.RenderMode;
            }

            return BoardItemPreviewRenderMode.Auto;
        }
    }
}
