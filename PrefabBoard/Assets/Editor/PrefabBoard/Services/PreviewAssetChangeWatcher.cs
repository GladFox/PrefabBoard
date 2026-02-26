using System;
using UnityEditor;

namespace PrefabBoard.Editor.Services
{
    internal sealed class PreviewAssetChangeWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            InvalidatePrefabPreviews(importedAssets);
            InvalidatePrefabPreviews(movedAssets);
        }

        private static void InvalidatePrefabPreviews(string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                return;
            }

            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                PreviewCache.InvalidateByAssetPath(path);
            }
        }
    }
}
