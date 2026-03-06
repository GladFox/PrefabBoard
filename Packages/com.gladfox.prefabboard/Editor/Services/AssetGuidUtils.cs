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

        public static bool TryResolvePrefabAsset(Object obj, out GameObject prefabAsset)
        {
            prefabAsset = null;
            if (obj == null)
            {
                return false;
            }

            var sourceGameObject = obj as GameObject;
            if (sourceGameObject == null && obj is Component component)
            {
                sourceGameObject = component.gameObject;
            }

            if (sourceGameObject == null)
            {
                return false;
            }

            if (IsPrefabAsset(sourceGameObject))
            {
                prefabAsset = sourceGameObject;
                return true;
            }

            var sourceFromObject = PrefabUtility.GetCorrespondingObjectFromSource(sourceGameObject) as GameObject;
            if (sourceFromObject != null && IsPrefabAsset(sourceFromObject))
            {
                prefabAsset = sourceFromObject;
                return true;
            }

            var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(sourceGameObject);
            if (nearestRoot != null)
            {
                var sourceFromRoot = PrefabUtility.GetCorrespondingObjectFromSource(nearestRoot) as GameObject;
                if (sourceFromRoot != null && IsPrefabAsset(sourceFromRoot))
                {
                    prefabAsset = sourceFromRoot;
                    return true;
                }

                var sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sourceGameObject);
                if (!string.IsNullOrEmpty(sourcePath))
                {
                    var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                    if (loaded != null && IsPrefabAsset(loaded))
                    {
                        prefabAsset = loaded;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryResolvePrefabGuid(Object obj, out string guid)
        {
            guid = string.Empty;
            if (!TryResolvePrefabAsset(obj, out var prefabAsset) || prefabAsset == null)
            {
                return false;
            }

            guid = GuidFromObject(prefabAsset);
            return !string.IsNullOrEmpty(guid);
        }
    }
}
