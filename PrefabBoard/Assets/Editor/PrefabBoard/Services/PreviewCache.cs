using System.Collections.Generic;
using System.IO;
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
        private enum PreviewContentFitMode
        {
            Auto = 0,
            Fullscreen = 1,
            SingleControl = 2
        }

        private sealed class PreviewRigObjects
        {
            public GameObject rootObject;
            public GameObject cameraObject;
            public Camera camera;
            public GameObject canvasObject;
            public RectTransform canvasRect;
            public GameObject contentObject;
            public RectTransform contentRect;
        }

        private const int UiLayer = 5;
        private const int DefaultResolutionWidth = 1920;
        private const int DefaultResolutionHeight = 1080;
        private const int MinCanvasSize = 64;
        private const int MaxCanvasSize = 4096;
        private const int MaxPreviewTextureSize = 512;

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> CustomPreviewKeys = new HashSet<string>();
        private static readonly HashSet<string> FailedCustomPreviewKeys = new HashSet<string>();
        private const string DefaultDebugScenePath = "Assets/Scenes/Test.unity";

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
                var customPreview = TryRenderUiPrefabPreview(prefabAsset, canvasSize, renderMode);
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

        public static bool TryCreateTestSceneFromLastCapture(out string scenePath, out string error)
        {
            scenePath = DefaultDebugScenePath;
            error = string.Empty;

            var prefabGuid = PreviewDebugCapture.PrefabGuid;
            if (string.IsNullOrEmpty(prefabGuid))
            {
                error = "No preview capture found. Open a board card first to produce preview capture.";
                return false;
            }

            return TryCreateTestScene(prefabGuid, PreviewDebugCapture.CanvasSize, PreviewDebugCapture.RenderMode, scenePath, out error);
        }

        public static bool TryCreateTestScene(string prefabGuid, Vector2Int canvasSize, string scenePath, out string error)
        {
            return TryCreateTestScene(prefabGuid, canvasSize, BoardItemPreviewRenderMode.Auto, scenePath, out error);
        }

        public static bool TryCreateTestScene(
            string prefabGuid,
            Vector2Int canvasSize,
            BoardItemPreviewRenderMode renderMode,
            string scenePath,
            out string error)
        {
            error = string.Empty;

            if (string.IsNullOrEmpty(prefabGuid))
            {
                error = "Prefab GUID is empty.";
                return false;
            }

            if (!AssetGuidUtils.TryLoadAssetByGuid<GameObject>(prefabGuid, out var prefabAsset) || prefabAsset == null)
            {
                error = "Failed to resolve prefab by GUID: " + prefabGuid;
                return false;
            }

            return TryCreateTestScene(prefabAsset, canvasSize, renderMode, scenePath, out error);
        }

        private static Texture2D GetPrefabIcon()
        {
            return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
        }

        private static Texture2D GetMissingIcon()
        {
            return EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
        }

        private static bool TryCreateTestScene(
            GameObject prefabAsset,
            Vector2Int canvasSize,
            BoardItemPreviewRenderMode renderMode,
            string scenePath,
            out string error)
        {
            error = string.Empty;

            if (prefabAsset == null)
            {
                error = "Prefab asset is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                scenePath = DefaultDebugScenePath;
            }

            if (!scenePath.EndsWith(".unity"))
            {
                error = "Scene path must point to a .unity file: " + scenePath;
                return false;
            }

            if (!EnsureAssetFolderExists(scenePath, out error))
            {
                return false;
            }

            var safeCanvasSize = SanitizeCanvasSize(
                new Vector2(canvasSize.x, canvasSize.y),
                new Vector2Int(DefaultResolutionWidth, DefaultResolutionHeight));

            var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                var instance = Object.Instantiate(prefabAsset);
                if (instance == null)
                {
                    error = "Failed to instantiate prefab in scene: " + AssetDatabase.GetAssetPath(prefabAsset);
                    return false;
                }

                SceneManager.MoveGameObjectToScene(instance, testScene);
                instance.name = prefabAsset.name;
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                if (ShouldForceUiLayer())
                {
                    SetLayerRecursively(instance, UiLayer);
                }

                var rig = CreatePreviewRig(testScene, safeCanvasSize, false, out var rigError);
                if (rig == null || rig.camera == null || rig.contentRect == null)
                {
                    error = "Failed to build preview rig for test scene. " + rigError;
                    return false;
                }

                var fitMode = ResolveContentFitMode(renderMode);
                AttachInstanceToPreviewContent(instance, rig.contentRect, safeCanvasSize, fitMode);
                PrepareUiForPreviewScreenSpace(instance, rig.camera, safeCanvasSize);
                Canvas.ForceUpdateCanvases();
                Canvas.ForceUpdateCanvases();

                EditorSceneManager.MarkSceneDirty(testScene);
                if (!EditorSceneManager.SaveScene(testScene, scenePath))
                {
                    error = "Failed to save scene to path: " + scenePath;
                    return false;
                }

                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    Selection.activeObject = sceneAsset;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                error = "Exception while creating test scene: " + ex.Message;
                return false;
            }
        }

        private static bool EnsureAssetFolderExists(string assetPath, out string error)
        {
            error = string.Empty;

            var folderPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(folderPath))
            {
                return true;
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            var parts = folderPath.Split('/');
            if (parts.Length == 0)
            {
                return true;
            }

            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        error = "Failed to create folder: " + next;
                        return false;
                    }
                }

                current = next;
            }

            return true;
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

        private static PreviewContentFitMode ResolveContentFitMode(BoardItemPreviewRenderMode renderMode)
        {
            if (renderMode == BoardItemPreviewRenderMode.Resolution)
            {
                return PreviewContentFitMode.Fullscreen;
            }

            if (renderMode == BoardItemPreviewRenderMode.ControlSize)
            {
                return PreviewContentFitMode.SingleControl;
            }

            return PreviewContentFitMode.Auto;
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

        private static Texture2D TryRenderUiPrefabPreview(
            GameObject prefabAsset,
            Vector2Int canvasSize,
            BoardItemPreviewRenderMode renderMode)
        {
            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.ScreenSpace, null, "ScreenSpace: pending");
            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.WorldSpace, null, "WorldSpace: pending");

            var screenSpace = TryRenderUiPrefabPreviewScreenSpace(prefabAsset, canvasSize, renderMode);
            if (screenSpace != null)
            {
                if (!IsFlatTexture(screenSpace))
                {
                    PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, screenSpace, "Final: selected ScreenSpace");
                    return screenSpace;
                }

                var worldAfterFlatScreen = TryRenderUiPrefabPreviewWorldSpace(prefabAsset, canvasSize, renderMode);
                if (worldAfterFlatScreen != null && !IsFlatTexture(worldAfterFlatScreen))
                {
                    Object.DestroyImmediate(screenSpace);
                    PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, worldAfterFlatScreen, "Final: selected WorldSpace (screen looked flat)");
                    return worldAfterFlatScreen;
                }

                if (worldAfterFlatScreen != null)
                {
                    Object.DestroyImmediate(worldAfterFlatScreen);
                }

                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, screenSpace, "Final: screen was flat, world also flat/null");
                return screenSpace;
            }

            var worldSpace = TryRenderUiPrefabPreviewWorldSpace(prefabAsset, canvasSize, renderMode);
            if (worldSpace != null)
            {
                PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, worldSpace, "Final: selected WorldSpace");
                return worldSpace;
            }

            PreviewDebugCapture.SetStageTexture(PreviewDebugStage.Final, null, "Final: both custom pipelines returned null");
            Debug.LogWarning("PrefabBoard: UI preview render produced an empty frame for " + AssetDatabase.GetAssetPath(prefabAsset));
            return null;
        }

        private static Texture2D TryRenderUiPrefabPreviewScreenSpace(
            GameObject prefabAsset,
            Vector2Int canvasSize,
            BoardItemPreviewRenderMode renderMode)
        {
            Scene previewScene = default;
            var sceneCreated = false;
            var previousActiveScene = SceneManager.GetActiveScene();

            GameObject instance = null;
            PreviewRigObjects rig = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;

            try
            {
                previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                sceneCreated = true;
                SceneManager.SetActiveScene(previewScene);

                instance = Object.Instantiate(prefabAsset);
                if (instance == null)
                {
                    return null;
                }

                SceneManager.MoveGameObjectToScene(instance, previewScene);
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                if (ShouldForceUiLayer())
                {
                    SetLayerRecursively(instance, UiLayer);
                }

                var textureSize = ComputeTextureSize(canvasSize);
                rig = CreatePreviewRig(previewScene, canvasSize, true, out var rigError);
                if (rig == null || rig.camera == null || rig.contentRect == null)
                {
                    Debug.LogWarning("PrefabBoard: ScreenSpace preview rig setup failed for " + AssetDatabase.GetAssetPath(prefabAsset) + ". " + rigError);
                    return null;
                }

                var fitMode = ResolveContentFitMode(renderMode);
                AttachInstanceToPreviewContent(instance, rig.contentRect, canvasSize, fitMode);
                PrepareUiForPreviewScreenSpace(instance, rig.camera, canvasSize);

                renderTexture = RenderTexture.GetTemporary(textureSize.x, textureSize.y, 24, RenderTextureFormat.ARGB32);
                rig.camera.targetTexture = renderTexture;
                Canvas.ForceUpdateCanvases();
                Canvas.ForceUpdateCanvases();
                rig.camera.Render();

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
                    $"ScreenSpace: rendered {textureSize.x}x{textureSize.y}, canvas {canvasSize.x}x{canvasSize.y}");

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

                if (rig != null && rig.rootObject != null)
                {
                    Object.DestroyImmediate(rig.rootObject);
                }

                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }

                if (sceneCreated)
                {
                    if (previousActiveScene.IsValid())
                    {
                        SceneManager.SetActiveScene(previousActiveScene);
                    }

                    EditorSceneManager.CloseScene(previewScene, true);
                }
            }
        }

        private static Texture2D TryRenderUiPrefabPreviewWorldSpace(
            GameObject prefabAsset,
            Vector2Int canvasSize,
            BoardItemPreviewRenderMode renderMode)
        {
            Scene previewScene = default;
            var sceneCreated = false;
            var previousActiveScene = SceneManager.GetActiveScene();

            GameObject instance = null;
            GameObject cameraObject = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;

            try
            {
                previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                sceneCreated = true;
                SceneManager.SetActiveScene(previewScene);

                instance = Object.Instantiate(prefabAsset);
                if (instance == null)
                {
                    return null;
                }

                SceneManager.MoveGameObjectToScene(instance, previewScene);
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
                var settings = PreviewRigSettingsProvider.TryGetSettings();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = settings != null ? settings.cameraBackground : new Color(0.16f, 0.16f, 0.16f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = settings != null ? Mathf.Max(0.001f, settings.nearClipPlane) : 0.01f;
                camera.farClipPlane = settings != null ? Mathf.Max(camera.nearClipPlane + 0.1f, settings.farClipPlane) : 1000f;

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
                    if (previousActiveScene.IsValid())
                    {
                        SceneManager.SetActiveScene(previousActiveScene);
                    }

                    EditorSceneManager.CloseScene(previewScene, true);
                }
            }
        }

        private static PreviewRigObjects CreatePreviewRig(Scene previewScene, Vector2Int canvasSize, bool hidden, out string error)
        {
            error = string.Empty;
            var settings = PreviewRigSettingsProvider.TryGetSettings();

            if (settings != null &&
                settings.rigSource == PreviewRigSource.PrefabTemplate &&
                settings.rigPrefab != null)
            {
                var templateRig = CreatePreviewRigFromTemplate(previewScene, canvasSize, hidden, settings, out error);
                if (templateRig != null)
                {
                    return templateRig;
                }
            }

            return CreateBuiltInPreviewRig(previewScene, canvasSize, hidden, settings);
        }

        private static PreviewRigObjects CreateBuiltInPreviewRig(
            Scene previewScene,
            Vector2Int canvasSize,
            bool hidden,
            PreviewRigSettingsAsset settings)
        {
            var rig = new PreviewRigObjects
            {
                rootObject = new GameObject("PrefabBoardPreviewRig")
            };
            rig.rootObject.hideFlags = hidden ? HideFlags.HideAndDontSave : HideFlags.None;
            SceneManager.MoveGameObjectToScene(rig.rootObject, previewScene);
            if (settings == null || settings.forceUiLayer)
            {
                SetLayerRecursively(rig.rootObject, UiLayer);
            }

            rig.cameraObject = new GameObject("PrefabBoardPreviewCamera", typeof(Camera));
            rig.cameraObject.hideFlags = hidden ? HideFlags.HideAndDontSave : HideFlags.None;
            rig.cameraObject.transform.SetParent(rig.rootObject.transform, false);
            rig.camera = rig.cameraObject.GetComponent<Camera>();
            ConfigurePreviewCamera(rig.camera, canvasSize, settings, true);

            rig.canvasObject = new GameObject("PrefabBoardPreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rig.canvasObject.hideFlags = hidden ? HideFlags.HideAndDontSave : HideFlags.None;
            rig.canvasObject.transform.SetParent(rig.rootObject.transform, false);
            rig.canvasRect = rig.canvasObject.GetComponent<RectTransform>();
            var canvas = rig.canvasObject.GetComponent<Canvas>();
            var scaler = rig.canvasObject.GetComponent<CanvasScaler>();
            ConfigurePreviewCanvas(rig.canvasRect, canvas, scaler, rig.camera, canvasSize, settings);

            rig.contentObject = new GameObject("Content", typeof(RectTransform));
            rig.contentObject.hideFlags = hidden ? HideFlags.HideAndDontSave : HideFlags.None;
            rig.contentObject.transform.SetParent(rig.canvasRect, false);
            rig.contentRect = rig.contentObject.GetComponent<RectTransform>();
            ConfigurePreviewContent(rig.contentRect);

            if (settings == null || settings.forceUiLayer)
            {
                SetLayerRecursively(rig.rootObject, UiLayer);
            }

            return rig;
        }

        private static PreviewRigObjects CreatePreviewRigFromTemplate(
            Scene previewScene,
            Vector2Int canvasSize,
            bool hidden,
            PreviewRigSettingsAsset settings,
            out string error)
        {
            error = string.Empty;
            var rigRoot = Object.Instantiate(settings.rigPrefab);
            if (rigRoot == null)
            {
                error = "Failed to instantiate rig prefab.";
                return null;
            }

            rigRoot.name = "PrefabBoardPreviewRig";
            rigRoot.hideFlags = hidden ? HideFlags.HideAndDontSave : HideFlags.None;
            SceneManager.MoveGameObjectToScene(rigRoot, previewScene);
            ApplyHideFlagsRecursively(rigRoot, hidden ? HideFlags.HideAndDontSave : HideFlags.None);
            if (settings.forceUiLayer)
            {
                SetLayerRecursively(rigRoot, UiLayer);
            }

            var camera = ResolveComponent<Camera>(rigRoot.transform, settings.cameraPath);
            var canvas = ResolveComponent<Canvas>(rigRoot.transform, settings.canvasPath);
            if (camera == null || canvas == null)
            {
                Object.DestroyImmediate(rigRoot);
                error = "Rig prefab must contain Camera and Canvas.";
                return null;
            }

            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Object.DestroyImmediate(rigRoot);
                error = "Rig prefab Canvas object must have RectTransform.";
                return null;
            }

            var contentRect = ResolveContentRect(rigRoot.transform, canvasRect, settings.contentPath, hidden);

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            ConfigurePreviewCamera(camera, canvasSize, settings, false);
            ConfigurePreviewCanvas(canvasRect, canvas, scaler, camera, canvasSize, settings);
            ConfigurePreviewContent(contentRect);

            return new PreviewRigObjects
            {
                rootObject = rigRoot,
                cameraObject = camera.gameObject,
                camera = camera,
                canvasObject = canvas.gameObject,
                canvasRect = canvasRect,
                contentObject = contentRect.gameObject,
                contentRect = contentRect
            };
        }

        private static RectTransform ResolveContentRect(
            Transform rigRoot,
            RectTransform canvasRect,
            string contentPath,
            bool hidden)
        {
            var contentRect = ResolveComponent<RectTransform>(rigRoot, contentPath);
            if (contentRect == null)
            {
                var byName = canvasRect.Find("Content");
                contentRect = byName as RectTransform;
            }

            if (contentRect == null)
            {
                var contentObject = new GameObject("Content", typeof(RectTransform));
                contentObject.hideFlags = hidden ? HideFlags.HideAndDontSave : HideFlags.None;
                contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.SetParent(canvasRect, false);
            }
            else if (contentRect.parent != canvasRect)
            {
                contentRect.SetParent(canvasRect, false);
            }

            return contentRect;
        }

        private static T ResolveComponent<T>(Transform root, string relativePath) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                var direct = root.Find(relativePath);
                if (direct != null)
                {
                    var directComponent = direct.GetComponent<T>();
                    if (directComponent != null)
                    {
                        return directComponent;
                    }
                }
            }

            return root.GetComponentInChildren<T>(true);
        }

        private static void ConfigurePreviewCamera(
            Camera camera,
            Vector2Int canvasSize,
            PreviewRigSettingsAsset settings,
            bool _)
        {
            if (camera == null)
            {
                return;
            }

            var background = settings != null ? settings.cameraBackground : new Color(0.16f, 0.16f, 0.16f, 1f);
            var nearClip = settings != null ? Mathf.Max(0.001f, settings.nearClipPlane) : 0.01f;
            var farClip = settings != null ? Mathf.Max(nearClip + 0.1f, settings.farClipPlane) : 200f;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            if (settings == null || settings.forceUiLayer)
            {
                camera.cullingMask = 1 << UiLayer;
            }
            camera.nearClipPlane = nearClip;
            camera.farClipPlane = farClip;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(1f, canvasSize.y * 0.5f);
            camera.aspect = Mathf.Max(0.01f, canvasSize.x / (float)canvasSize.y);

            camera.transform.localPosition = new Vector3(0f, 0f, -10f);
            camera.transform.localRotation = Quaternion.identity;
            camera.transform.localScale = Vector3.one;
        }

        private static void ConfigurePreviewCanvas(
            RectTransform canvasRect,
            Canvas canvas,
            CanvasScaler scaler,
            Camera previewCamera,
            Vector2Int canvasSize,
            PreviewRigSettingsAsset settings)
        {
            if (canvasRect == null || canvas == null || scaler == null)
            {
                return;
            }

            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(canvasSize.x, canvasSize.y);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = previewCamera;
            canvas.planeDistance = settings != null ? Mathf.Max(0.01f, settings.planeDistance) : 1f;
            canvas.pixelPerfect = false;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(canvasSize.x, canvasSize.y);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        private static void ConfigurePreviewContent(RectTransform contentRect)
        {
            if (contentRect == null)
            {
                return;
            }

            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            contentRect.localPosition = Vector3.zero;
            contentRect.localRotation = Quaternion.identity;
            contentRect.localScale = Vector3.one;
        }

        private static void ApplyHideFlagsRecursively(GameObject root, HideFlags hideFlags)
        {
            if (root == null)
            {
                return;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                if (tr != null)
                {
                    tr.gameObject.hideFlags = hideFlags;
                }
            }
        }

        private static void AttachInstanceToPreviewContent(
            GameObject instance,
            RectTransform contentRect,
            Vector2Int canvasSize,
            PreviewContentFitMode fitMode)
        {
            if (instance == null || contentRect == null)
            {
                return;
            }

            if (instance.transform is RectTransform rootRect)
            {
                rootRect.SetParent(contentRect, false);
                rootRect.localScale = Vector3.one;
                ApplyContentFit(rootRect, canvasSize, fitMode);

                return;
            }

            instance.transform.SetParent(contentRect, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private static void ApplyContentFit(RectTransform rootRect, Vector2Int canvasSize, PreviewContentFitMode fitMode)
        {
            if (rootRect == null)
            {
                return;
            }

            if (fitMode == PreviewContentFitMode.Fullscreen)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;
                return;
            }

            if (fitMode == PreviewContentFitMode.SingleControl)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                var width = rootRect.rect.width;
                var height = rootRect.rect.height;
                if (width < 1f || height < 1f || IsStretchRect(rootRect))
                {
                    width = Mathf.Min(canvasSize.x, 512);
                    height = Mathf.Min(canvasSize.y, 512);
                }

                rootRect.sizeDelta = new Vector2(width, height);
                return;
            }

            if (IsStretchRect(rootRect))
            {
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                return;
            }

            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            if (rootRect.rect.width < 1f || rootRect.rect.height < 1f)
            {
                rootRect.sizeDelta = new Vector2(Mathf.Min(canvasSize.x, 512), Mathf.Min(canvasSize.y, 512));
            }
        }

        private static void PrepareUiForPreviewScreenSpace(GameObject root, Camera previewCamera, Vector2Int canvasSize)
        {
            if (root == null)
            {
                return;
            }

            var planeDistance = GetConfiguredPlaneDistance();

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
            {
                // Only inject a Canvas if the instance is not already nested inside one.
                // If it is already inside a preview canvas, the Image components will render
                // through that parent canvas without needing an extra nested canvas.
                var parentCanvas = root.transform.parent != null
                    ? root.transform.parent.GetComponentInParent<Canvas>()
                    : null;
                if (parentCanvas == null)
                {
                    var createdCanvas = root.AddComponent<Canvas>();
                    createdCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
            }

            canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = previewCamera;
                canvas.planeDistance = planeDistance;
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
                // A stretch canvas nested inside a parent canvas inherits its size from that parent.
                // Setting sizeDelta on a nested stretch rect would expand it beyond the parent bounds.
                // Only apply an explicit size when this is a root canvas (no parent canvas).
                var parent = canvasRect.transform.parent;
                return parent == null || parent.GetComponentInParent<Canvas>() == null;
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

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                if (tr != null)
                {
                    tr.gameObject.layer = layer;
                }
            }
        }

        private static float GetConfiguredPlaneDistance()
        {
            var settings = PreviewRigSettingsProvider.TryGetSettings();
            if (settings == null)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, settings.planeDistance);
        }

        private static bool ShouldForceUiLayer()
        {
            var settings = PreviewRigSettingsProvider.TryGetSettings();
            return settings == null || settings.forceUiLayer;
        }

        private static bool IsFlatTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return true;
            }

            var pixels = texture.GetPixels32();
            if (pixels == null || pixels.Length < 2)
            {
                return true;
            }

            var step = Mathf.Max(1, pixels.Length / 512);
            var first = pixels[0];
            var sampled = 0;
            var closeToFirst = 0;
            for (var i = 0; i < pixels.Length; i += step)
            {
                var p = pixels[i];
                sampled++;
                var delta = Mathf.Abs(p.r - first.r) + Mathf.Abs(p.g - first.g) + Mathf.Abs(p.b - first.b) + Mathf.Abs(p.a - first.a);
                if (delta <= 6)
                {
                    closeToFirst++;
                }
            }

            if (sampled == 0)
            {
                return true;
            }

            return closeToFirst / (float)sampled >= 0.995f;
        }

    }
}
