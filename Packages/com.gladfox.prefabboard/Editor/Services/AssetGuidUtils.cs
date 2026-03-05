using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class AssetGuidUtils
    {
        public static string GuidFromObject(Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        public static bool TryLoadAssetByGuid<T>(string guid, out T asset) where T : Object
        {
            asset = null;
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null;
        }

        public static bool IsPrefabAsset(Object obj)
        {
            if (!(obj is GameObject gameObject))
            {
                return false;
            }

            return PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab;
        }
    }
}
