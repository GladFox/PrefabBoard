using System.Collections.Generic;
using PrefabBoard.Editor.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PrefabBoard.Editor.Services
{
    public static class PreviewCache
    {
        private const int DefaultResolutionWidth = 1920;
        private const int DefaultResolutionHeight = 1080;
        private const int MinCanvasSize = 64;
        private const int MaxCanvasSize = 4096;
        private const int MaxPreviewTextureSize = 512;

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> CustomPreviewKeys = new HashSet<string>();
        private static readonly HashSet<string> FailedCustomPreviewKeys = new HashSet<string>();

        public static Texture2D GetPreview(
            string prefabGuid,
            BoardItemPreviewRenderMode renderMode,
            Vector2 controlSizeHint,
            Vector2 editorResolution,
            out bool loading)
        {
            loading = false;

            if (string.IsNullOrEmpty(prefabGuid))
            {
                return GetMissingIcon();
            }

            if (!AssetGuidUtils.TryLoadAssetByGuid<GameObject>(prefabGuid, out var prefabAsset) || prefabAsset == null)
            {
                return GetMissingIcon();
            }

            var hasUiContent = HasUiContent(prefabAsset);
            var canvasSize = hasUiContent
                ? ResolveCanvasSize(prefabAsset, renderMode, controlSizeHint, editorResolution)
                : Vector2Int.zero;

            var cacheKey = BuildCacheKey(prefabGuid, hasUiContent, renderMode, canvasSize);
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            if (hasUiContent && !FailedCustomPreviewKeys.Contains(cacheKey))
            {
                var customPreview = TryRenderUiPrefabPreview(prefabAsset, canvasSize);
                if (customPreview != null)
                {
                    Cache[cacheKey] = customPreview;
                    CustomPreviewKeys.Add(cacheKey);
                    FailedCustomPreviewKeys.Remove(cacheKey);
                    return customPreview;
                }

                FailedCustomPreviewKeys.Add(cacheKey);
            }

            var instanceId = prefabAsset.GetInstanceID();
            var preview = AssetPreview.GetAssetPreview(prefabAsset);
            loading = AssetPreview.IsLoadingAssetPreview(instanceId);

            if (preview != null)
            {
                Cache[cacheKey] = preview;
                return preview;
            }

            var miniThumbnail = AssetPreview.GetMiniThumbnail(prefabAsset) as Texture2D;
            if (loading)
            {
                return miniThumbnail != null ? miniThumbnail : GetPrefabIcon();
            }

            if (miniThumbnail != null)
            {
                Cache[cacheKey] = miniThumbnail;
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

            var prefix = prefabGuid + "|";
            var keysToRemove = new List<string>();
            foreach (var key in Cache.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    keysToRemove.Add(key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                DestroyCustomPreview(keysToRemove[i]);
                Cache.Remove(keysToRemove[i]);
                FailedCustomPreviewKeys.Remove(keysToRemove[i]);
            }
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

        private static void DestroyCustomPreview(string cacheKey)
        {
            if (!CustomPreviewKeys.Contains(cacheKey))
            {
                return;
            }

            if (Cache.TryGetValue(cacheKey, out var texture) && texture != null)
            {
                Object.DestroyImmediate(texture);
            }

            CustomPreviewKeys.Remove(cacheKey);
        }

        private static string BuildCacheKey(string prefabGuid, bool hasUiContent, BoardItemPreviewRenderMode renderMode, Vector2Int canvasSize)
        {
            if (!hasUiContent)
            {
                return prefabGuid + "|std";
            }

            return prefabGuid + "|ui|" + (int)renderMode + "|" + canvasSize.x + "x" + canvasSize.y;
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

        private static Vector2Int ResolveCanvasSize(
            GameObject prefabAsset,
            BoardItemPreviewRenderMode renderMode,
            Vector2 controlSizeHint,
            Vector2 editorResolution)
        {
            var resolutionSize = SanitizeCanvasSize(editorResolution, new Vector2Int(DefaultResolutionWidth, DefaultResolutionHeight));
            var controlSize = ResolveControlSize(prefabAsset, controlSizeHint, resolutionSize);

            if (renderMode == BoardItemPreviewRenderMode.Resolution)
            {
                return resolutionSize;
            }

            if (renderMode == BoardItemPreviewRenderMode.ControlSize)
            {
                return controlSize;
            }

            return IsStretchToScreenPrefab(prefabAsset) ? resolutionSize : controlSize;
        }

        private static Vector2Int ResolveControlSize(GameObject prefabAsset, Vector2 controlSizeHint, Vector2Int resolutionSize)
        {
            var hinted = SanitizeCanvasSize(controlSizeHint, new Vector2Int(0, 0));
            if (hinted.x > 0 && hinted.y > 0)
            {
                return ClampToViewportRange(hinted, resolutionSize);
            }

            if (TryGetPrimaryRectTransform(prefabAsset, out var rectTransform))
            {
                var rectSize = rectTransform.rect.size;
                if (rectSize.x > 1f && rectSize.y > 1f)
                {
                    var fromRect = SanitizeCanvasSize(rectSize, new Vector2Int(0, 0));
                    if (fromRect.x > 0 && fromRect.y > 0)
                    {
                        return ClampToViewportRange(fromRect, resolutionSize);
                    }
                }
            }

            var fallback = new Vector2Int(Mathf.RoundToInt(resolutionSize.x * 0.5f), Mathf.RoundToInt(resolutionSize.y * 0.5f));
            return ClampToViewportRange(fallback, resolutionSize);
        }

        private static bool IsStretchToScreenPrefab(GameObject prefabAsset)
        {
            if (!TryGetPrimaryRectTransform(prefabAsset, out var rectTransform))
            {
                return false;
            }

            return IsStretchRect(rectTransform);
        }

        private static bool TryGetPrimaryRectTransform(GameObject root, out RectTransform rectTransform)
        {
            rectTransform = null;
            if (root == null)
            {
                return false;
            }

            rectTransform = root.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                return true;
            }

            rectTransform = root.GetComponentInChildren<RectTransform>(true);
            return rectTransform != null;
        }

        private static bool IsStretchRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return false;
            }

            const float epsilon = 0.001f;
            var fullAnchors = Mathf.Abs(rectTransform.anchorMin.x) <= epsilon &&
                              Mathf.Abs(rectTransform.anchorMin.y) <= epsilon &&
                              Mathf.Abs(rectTransform.anchorMax.x - 1f) <= epsilon &&
                              Mathf.Abs(rectTransform.anchorMax.y - 1f) <= epsilon;

            if (!fullAnchors)
            {
                return false;
            }

            return Mathf.Abs(rectTransform.offsetMin.x) <= epsilon &&
                   Mathf.Abs(rectTransform.offsetMin.y) <= epsilon &&
                   Mathf.Abs(rectTransform.offsetMax.x) <= epsilon &&
                   Mathf.Abs(rectTransform.offsetMax.y) <= epsilon;
        }

        private static Vector2Int ClampToViewportRange(Vector2Int size, Vector2Int resolutionSize)
        {
            var maxWidth = Mathf.Clamp(Mathf.RoundToInt(resolutionSize.x * 1.5f), MinCanvasSize, MaxCanvasSize);
            var maxHeight = Mathf.Clamp(Mathf.RoundToInt(resolutionSize.y * 1.5f), MinCanvasSize, MaxCanvasSize);

            return new Vector2Int(
                Mathf.Clamp(size.x, MinCanvasSize, maxWidth),
                Mathf.Clamp(size.y, MinCanvasSize, maxHeight));
        }

        private static Vector2Int SanitizeCanvasSize(Vector2 rawSize, Vector2Int fallback)
        {
            var width = Mathf.RoundToInt(rawSize.x);
            var height = Mathf.RoundToInt(rawSize.y);

            if (width < MinCanvasSize || height < MinCanvasSize)
            {
                return fallback;
            }

            return new Vector2Int(
                Mathf.Clamp(width, MinCanvasSize, MaxCanvasSize),
                Mathf.Clamp(height, MinCanvasSize, MaxCanvasSize));
        }

        private static Texture2D TryRenderUiPrefabPreview(GameObject prefabAsset, Vector2Int canvasSize)
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

                PrepareUiForPreview(instance, canvasSize);
                Canvas.ForceUpdateCanvases();

                if (!TryCalculateWorldBounds(instance, out var bounds))
                {
                    return null;
                }

                var textureSize = ComputeTextureSize(canvasSize);
                var aspect = Mathf.Max(0.01f, textureSize.x / (float)textureSize.y);

                cameraObject = new GameObject("PrefabBoardPreviewCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;

                PositionPreviewCamera(camera, bounds, aspect);

                renderTexture = RenderTexture.GetTemporary(textureSize.x, textureSize.y, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.ReadPixels(new Rect(0f, 0f, textureSize.x, textureSize.y), 0, 0);
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

        private static void PrepareUiForPreview(GameObject root, Vector2Int canvasSize)
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
                canvas.pixelPerfect = false;

                var canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null && ShouldApplyCanvasSizeHint(canvasRect))
                {
                    canvasRect.sizeDelta = new Vector2(canvasSize.x, canvasSize.y);
                }
            }

            var scalers = root.GetComponentsInChildren<CanvasScaler>(true);
            foreach (var scaler in scalers)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            }
        }

        private static bool ShouldApplyCanvasSizeHint(RectTransform canvasRect)
        {
            if (canvasRect == null)
            {
                return false;
            }

            if (IsStretchRect(canvasRect))
            {
                return true;
            }

            var rectSize = canvasRect.rect.size;
            return rectSize.x < MinCanvasSize || rectSize.y < MinCanvasSize;
        }

        private static Vector2Int ComputeTextureSize(Vector2Int canvasSize)
        {
            var width = Mathf.Max(MinCanvasSize, canvasSize.x);
            var height = Mathf.Max(MinCanvasSize, canvasSize.y);

            var maxDimension = Mathf.Max(width, height);
            if (maxDimension <= MaxPreviewTextureSize)
            {
                return new Vector2Int(width, height);
            }

            var scale = MaxPreviewTextureSize / (float)maxDimension;
            var scaledWidth = Mathf.Clamp(Mathf.RoundToInt(width * scale), MinCanvasSize, MaxPreviewTextureSize);
            var scaledHeight = Mathf.Clamp(Mathf.RoundToInt(height * scale), MinCanvasSize, MaxPreviewTextureSize);
            return new Vector2Int(scaledWidth, scaledHeight);
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

        private static void PositionPreviewCamera(Camera camera, Bounds bounds, float aspect)
        {
            var center = bounds.center;
            var extents = bounds.extents;

            var verticalHalf = extents.y;
            var horizontalHalf = extents.x / Mathf.Max(0.01f, aspect);
            var size = Mathf.Max(verticalHalf, horizontalHalf) * 1.15f;
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
