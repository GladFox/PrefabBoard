using System;
using PrefabBoard.Editor.Data;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public enum PreviewDebugStage
    {
        ScreenSpace = 0,
        WorldSpace = 1,
        Final = 2
    }

    public static class PreviewDebugCapture
    {
        private static Texture2D _screenTexture;
        private static Texture2D _worldTexture;
        private static Texture2D _finalTexture;

        private static string _screenNote = string.Empty;
        private static string _worldNote = string.Empty;
        private static string _finalNote = string.Empty;

        public static bool CaptureEnabled = true;

        public static string PrefabGuid { get; private set; } = string.Empty;
        public static string PrefabPath { get; private set; } = string.Empty;
        public static BoardItemPreviewRenderMode RenderMode { get; private set; }
        public static Vector2Int CanvasSize { get; private set; } = Vector2Int.zero;
        public static DateTime LastUpdateUtc { get; private set; } = DateTime.MinValue;

        public static Texture2D ScreenTexture => _screenTexture;
        public static Texture2D WorldTexture => _worldTexture;
        public static Texture2D FinalTexture => _finalTexture;

        public static string ScreenNote => _screenNote;
        public static string WorldNote => _worldNote;
        public static string FinalNote => _finalNote;

        public static void Begin(string prefabGuid, string prefabPath, BoardItemPreviewRenderMode renderMode, Vector2Int canvasSize)
        {
            if (!CaptureEnabled)
            {
                return;
            }

            PrefabGuid = prefabGuid ?? string.Empty;
            PrefabPath = prefabPath ?? string.Empty;
            RenderMode = renderMode;
            CanvasSize = canvasSize;
            LastUpdateUtc = DateTime.UtcNow;
        }

        public static void SetStageTexture(PreviewDebugStage stage, Texture2D source, string note)
        {
            if (!CaptureEnabled)
            {
                return;
            }

            switch (stage)
            {
                case PreviewDebugStage.ScreenSpace:
                    SetTexture(ref _screenTexture, source, "PrefabBoard_Debug_Screen");
                    _screenNote = note ?? string.Empty;
                    break;
                case PreviewDebugStage.WorldSpace:
                    SetTexture(ref _worldTexture, source, "PrefabBoard_Debug_World");
                    _worldNote = note ?? string.Empty;
                    break;
                default:
                    SetTexture(ref _finalTexture, source, "PrefabBoard_Debug_Final");
                    _finalNote = note ?? string.Empty;
                    break;
            }

            LastUpdateUtc = DateTime.UtcNow;
        }

        public static void SetStageError(PreviewDebugStage stage, string error)
        {
            var message = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim();
            SetStageTexture(stage, null, "Error: " + message);
        }

        public static void Clear()
        {
            Destroy(ref _screenTexture);
            Destroy(ref _worldTexture);
            Destroy(ref _finalTexture);

            _screenNote = string.Empty;
            _worldNote = string.Empty;
            _finalNote = string.Empty;
            PrefabGuid = string.Empty;
            PrefabPath = string.Empty;
            CanvasSize = Vector2Int.zero;
            LastUpdateUtc = DateTime.MinValue;
        }

        private static void SetTexture(ref Texture2D target, Texture2D source, string name)
        {
            Destroy(ref target);
            if (source == null)
            {
                return;
            }

            target = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = name
            };

            target.SetPixels32(source.GetPixels32());
            target.Apply(false, false);
        }

        private static void Destroy(ref Texture2D texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                texture = null;
            }
        }
    }
}
