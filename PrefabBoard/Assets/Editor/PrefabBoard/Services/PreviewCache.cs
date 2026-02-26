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
            PreviewDebugCapture.Begin(prefabGuid, AssetDatabase.GetAssetPath(prefabAsset), renderMode, canvasSize);

            var cacheKey = BuildCacheKey(prefabGuid, hasUiContent, renderMode, canvasSize);
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, cached, "Final: cache hit");
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
                    PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, customPreview, "Final: custom UI preview");
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
                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, preview, "Final: AssetPreview");
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
                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, miniThumbnail, "Final: mini thumbnail");
                return miniThumbnail;
            }

            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, null, "Final: prefab icon fallback");
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
                return hinted;
            }

            if (TryGetPrimaryRectTransform(prefabAsset, out var rectTransform))
            {
                var rectSize = rectTransform.rect.size;
                if (rectSize.x > 1f && rectSize.y > 1f)
                {
                    var fromRect = SanitizeCanvasSize(rectSize, new Vector2Int(0, 0));
                    if (fromRect.x > 0 && fromRect.y > 0)
                    {
                        return fromRect;
                    }
                }
            }

            var fallback = new Vector2Int(220, 120);
            return fallback;
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
            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.ScreenSpace, null, "ScreenSpace: pending");
            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.WorldSpace, null, "WorldSpace: pending");

            var screenSpace = TryRenderUiPrefabPreviewScreenSpace(prefabAsset, canvasSize);
            if (screenSpace != null)
            {
                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, screenSpace, "Final: selected ScreenSpace");
                return screenSpace;
            }

            var worldSpace = TryRenderUiPrefabPreviewWorldSpace(prefabAsset, canvasSize);
            if (worldSpace != null)
            {
                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, worldSpace, "Final: selected WorldSpace");
                return worldSpace;
            }

            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, null, "Final: both custom pipelines returned null");
            Debug.LogWarning("PrefabBoard: UI preview render produced an empty frame for " + AssetDatabase.GetAssetPath(prefabAsset));
            return null;
        }

        private static Texture2D TryRenderUiPrefabPreviewScreenSpace(GameObject prefabAsset, Vector2Int canvasSize)
        {
            Scene previewScene = default;
            var sceneCreated = false;

            GameObject instance = null;
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            GameObject contentObject = null;
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

                var textureSize = ComputeTextureSize(canvasSize);
                var previewCamera = CreatePreviewCamera(previewScene, canvasSize, out cameraObject);
                var previewCanvas = CreatePreviewCanvas(previewScene, previewCamera, canvasSize, out canvasObject);
                var previewContent = CreatePreviewContent(previewScene, previewCanvas, out contentObject);

                AttachInstanceToPreviewContent(instance, previewContent, canvasSize);
                PrepareUiForPreviewScreenSpace(instance, previewCamera, canvasSize);
                Canvas.ForceUpdateCanvases();
                Canvas.ForceUpdateCanvases();

                renderTexture = RenderTexture.GetTemporary(textureSize.x, textureSize.y, 24, RenderTextureFormat.ARGB32);
                previewCamera.targetTexture = renderTexture;
                previewCamera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.ReadPixels(new Rect(0f, 0f, textureSize.x, textureSize.y), 0, 0);
                texture.Apply(false, false);
                PreviewDebugCapture.SetStageTexture(
                    PreviewDebugStage.ScreenSpace,
                    texture,
                    $"ScreenSpace: rendered {textureSize.x}x{textureSize.y}, canvas {canvasSize.x}x{canvasSize.y}, plane {previewCamera.planeDistance:0.###}");

                return texture;
            }
            catch (System.Exception ex)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                PreviewDebugCapture.SetStageError(PreviewDebugStage.ScreenSpace, ex.Message);
                Debug.LogWarning("PrefabBoard: ScreenSpace preview failed for " + AssetDatabase.GetAssetPath(prefabAsset) + "\n" + ex.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (contentObject != null)
                {
                    Object.DestroyImmediate(contentObject);
                }

                if (canvasObject != null)
                {
                    Object.DestroyImmediate(canvasObject);
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

        private static Texture2D TryRenderUiPrefabPreviewWorldSpace(GameObject prefabAsset, Vector2Int canvasSize)
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

                PrepareUiForPreviewWorldSpace(instance, canvasSize);
                Canvas.ForceUpdateCanvases();
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
                PreviewDebugCapture.SetStageTexture(
                    PreviewDebugStage.WorldSpace,
                    texture,
                    $"WorldSpace: rendered {textureSize.x}x{textureSize.y}, bounds center {bounds.center}, size {bounds.size}");

                return texture;
            }
            catch (System.Exception ex)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                PreviewDebugCapture.SetStageError(PreviewDebugStage.WorldSpace, ex.Message);
                Debug.LogWarning("PrefabBoard: WorldSpace preview failed for " + AssetDatabase.GetAssetPath(prefabAsset) + "\n" + ex.Message);
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

        private static Camera CreatePreviewCamera(Scene previewScene, Vector2Int canvasSize, out GameObject cameraObject)
        {
            cameraObject = new GameObject("PrefabBoardPreviewCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
            camera.cullingMask = ~0;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 200f;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(1f, canvasSize.y * 0.5f);
            camera.aspect = Mathf.Max(0.01f, canvasSize.x / (float)canvasSize.y);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            return camera;
        }

        private static RectTransform CreatePreviewCanvas(Scene previewScene, Camera previewCamera, Vector2Int canvasSize, out GameObject canvasObject)
        {
            canvasObject = new GameObject("PrefabBoardPreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(canvasObject, previewScene);

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(canvasSize.x, canvasSize.y);
            canvasRect.anchoredPosition = Vector2.zero;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = previewCamera;
            canvas.planeDistance = 1f;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(canvasSize.x, canvasSize.y);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return canvasRect;
        }

        private static RectTransform CreatePreviewContent(Scene previewScene, RectTransform canvasRect, out GameObject contentObject)
        {
            contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(contentObject, previewScene);

            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(canvasRect, false);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            return contentRect;
        }

        private static void AttachInstanceToPreviewContent(GameObject instance, RectTransform contentRect, Vector2Int canvasSize)
        {
            if (instance == null || contentRect == null)
            {
                return;
            }

            if (instance.transform is RectTransform rootRect)
            {
                rootRect.SetParent(contentRect, false);
                rootRect.localScale = Vector3.one;

                if (IsStretchRect(rootRect))
                {
                    rootRect.offsetMin = Vector2.zero;
                    rootRect.offsetMax = Vector2.zero;
                }
                else
                {
                    rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                    rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                    rootRect.anchoredPosition = Vector2.zero;
                    if (rootRect.rect.width < 1f || rootRect.rect.height < 1f)
                    {
                        rootRect.sizeDelta = new Vector2(Mathf.Min(canvasSize.x, 512), Mathf.Min(canvasSize.y, 512));
                    }
                }

                return;
            }

            instance.transform.SetParent(contentRect, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private static void PrepareUiForPreviewScreenSpace(GameObject root, Camera previewCamera, Vector2Int canvasSize)
        {
            if (root == null)
            {
                return;
            }

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
            {
                var createdCanvas = root.AddComponent<Canvas>();
                createdCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = previewCamera;
                canvas.planeDistance = 1f;
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

        private static void PrepareUiForPreviewWorldSpace(GameObject root, Vector2Int canvasSize)
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
