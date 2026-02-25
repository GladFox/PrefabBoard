using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PrefabBoard.Editor.Services
{
    public static class PreviewCache
    {
        private const int CustomPreviewSize = 256;

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> CustomPreviewKeys = new HashSet<string>();
        private static readonly HashSet<string> FailedCustomPreviewKeys = new HashSet<string>();

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

            var hasUiContent = HasUiContent(prefabAsset);
            if (hasUiContent && !FailedCustomPreviewKeys.Contains(prefabGuid))
            {
                var customPreview = TryRenderUiPrefabPreview(prefabAsset);
                if (customPreview != null)
                {
                    Cache[prefabGuid] = customPreview;
                    CustomPreviewKeys.Add(prefabGuid);
                    FailedCustomPreviewKeys.Remove(prefabGuid);
                    return customPreview;
                }

                FailedCustomPreviewKeys.Add(prefabGuid);
            }

            var instanceId = prefabAsset.GetInstanceID();
            var preview = AssetPreview.GetAssetPreview(prefabAsset);
            loading = AssetPreview.IsLoadingAssetPreview(instanceId);

            if (preview != null)
            {
                Cache[prefabGuid] = preview;
                return preview;
            }

            var miniThumbnail = AssetPreview.GetMiniThumbnail(prefabAsset) as Texture2D;
            if (loading)
            {
                // Do not cache while AssetPreview is still generating.
                return miniThumbnail != null ? miniThumbnail : GetPrefabIcon();
            }

            if (miniThumbnail != null)
            {
                Cache[prefabGuid] = miniThumbnail;
                return miniThumbnail;
            }

            return GetPrefabIcon();
        }

        public static void Invalidate(string prefabGuid)
        {
            if (string.IsNullOrEmpty(prefabGuid))
            {
                return;
            }

            DestroyCustomPreview(prefabGuid);
            Cache.Remove(prefabGuid);
            FailedCustomPreviewKeys.Remove(prefabGuid);
        }

        public static void Clear()
        {
            foreach (var key in CustomPreviewKeys)
            {
                if (Cache.TryGetValue(key, out var texture) && texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }

            Cache.Clear();
            CustomPreviewKeys.Clear();
            FailedCustomPreviewKeys.Clear();
        }

        private static Texture2D GetPrefabIcon()
        {
            return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
        }

        private static Texture2D GetMissingIcon()
        {
            return EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
        }

        private static void DestroyCustomPreview(string prefabGuid)
        {
            if (!CustomPreviewKeys.Contains(prefabGuid))
            {
                return;
            }

            if (Cache.TryGetValue(prefabGuid, out var texture) && texture != null)
            {
                Object.DestroyImmediate(texture);
            }

            CustomPreviewKeys.Remove(prefabGuid);
        }

        private static bool HasUiContent(GameObject prefabAsset)
        {
            if (prefabAsset == null)
            {
                return false;
            }

            return prefabAsset.GetComponentInChildren<Canvas>(true) != null ||
                   prefabAsset.GetComponentInChildren<RectTransform>(true) != null;
        }

        private static Texture2D TryRenderUiPrefabPreview(GameObject prefabAsset)
        {
            Scene previewScene = default;
            var sceneCreated = false;

            GameObject instance = null;
            GameObject cameraObject = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;

            try
            {
                previewScene = EditorSceneManager.NewPreviewScene();
                sceneCreated = true;

                instance = PrefabUtility.InstantiatePrefab(prefabAsset, previewScene) as GameObject;
                if (instance == null)
                {
                    return null;
                }

                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                PrepareUiForPreview(instance);
                Canvas.ForceUpdateCanvases();

                if (!TryCalculateWorldBounds(instance, out var bounds))
                {
                    return null;
                }

                cameraObject = new GameObject("PrefabBoardPreviewCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;

                PositionPreviewCamera(camera, bounds);

                renderTexture = RenderTexture.GetTemporary(CustomPreviewSize, CustomPreviewSize, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(CustomPreviewSize, CustomPreviewSize, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.ReadPixels(new Rect(0f, 0f, CustomPreviewSize, CustomPreviewSize), 0, 0);
                texture.Apply(false, false);

                return texture;
            }
            catch
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }

                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }

                if (sceneCreated)
                {
                    EditorSceneManager.ClosePreviewScene(previewScene);
                }
            }
        }

        private static void PrepareUiForPreview(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
            {
                var createdCanvas = root.AddComponent<Canvas>();
                createdCanvas.renderMode = RenderMode.WorldSpace;
            }

            canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = null;
            }
        }

        private static bool TryCalculateWorldBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            var hasAny = false;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (!hasAny)
                {
                    bounds = renderer.bounds;
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            var rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
            foreach (var rectTransform in rectTransforms)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                for (var i = 0; i < corners.Length; i++)
                {
                    if (!hasAny)
                    {
                        bounds = new Bounds(corners[i], Vector3.zero);
                        hasAny = true;
                    }
                    else
                    {
                        bounds.Encapsulate(corners[i]);
                    }
                }
            }

            if (!hasAny)
            {
                return false;
            }

            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                bounds.Expand(1f);
            }

            return true;
        }

        private static void PositionPreviewCamera(Camera camera, Bounds bounds)
        {
            var center = bounds.center;
            var extents = bounds.extents;

            var size = Mathf.Max(extents.x, extents.y) * 1.25f;
            if (size < 0.1f)
            {
                size = 0.5f;
            }

            camera.orthographicSize = size;
            camera.transform.position = center + Vector3.back * 10f;
            camera.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }
    }
}
