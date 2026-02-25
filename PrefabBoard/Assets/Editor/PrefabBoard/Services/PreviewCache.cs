using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class PreviewCache
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D GetPreview(string prefabGuid, out bool loading)
        {
            loading = false;

            if (string.IsNullOrEmpty(prefabGuid))
            {
                return GetMissingIcon();
            }

            if (Cache.TryGetValue(prefabGuid, out var cached) && cached != null)
            {
                return cached;
            }

            if (!AssetGuidUtils.TryLoadAssetByGuid<GameObject>(prefabGuid, out var prefabAsset) || prefabAsset == null)
            {
                return GetMissingIcon();
            }

            var instanceId = prefabAsset.GetInstanceID();
            var preview = AssetPreview.GetAssetPreview(prefabAsset);
            loading = AssetPreview.IsLoadingAssetPreview(instanceId);

            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(prefabAsset) as Texture2D;
            }

            if (preview != null)
            {
                Cache[prefabGuid] = preview;
                return preview;
            }

            return GetPrefabIcon();
        }

        public static void Invalidate(string prefabGuid)
        {
            if (!string.IsNullOrEmpty(prefabGuid))
            {
                Cache.Remove(prefabGuid);
            }
        }

        public static void Clear()
        {
            Cache.Clear();
        }

        private static Texture2D GetPrefabIcon()
        {
            return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
        }

        private static Texture2D GetMissingIcon()
        {
            return EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
        }
    }
}
