using System.IO;
using PrefabBoard.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.UI
{
    public sealed class PreviewDebugWindow : EditorWindow
    {
        private const float PreviewHeight = 200f;
        private Vector2 _scroll;

        [MenuItem("Tools/PrefabBoard/Preview Debug")]
        public static void Open()
        {
            var window = GetWindow<PreviewDebugWindow>("Prefab Preview Debug");
            window.minSize = new Vector2(560f, 380f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            var enabled = EditorGUILayout.ToggleLeft("Capture Enabled", PreviewDebugCapture.CaptureEnabled, GUILayout.Width(160f));
            if (enabled != PreviewDebugCapture.CaptureEnabled)
            {
                PreviewDebugCapture.CaptureEnabled = enabled;
            }

            if (GUILayout.Button("Clear", GUILayout.Width(80f)))
            {
                PreviewDebugCapture.Clear();
            }

            if (GUILayout.Button("Save PNGs", GUILayout.Width(100f)))
            {
                SavePngs();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Prefab", string.IsNullOrEmpty(PreviewDebugCapture.PrefabPath) ? "-" : PreviewDebugCapture.PrefabPath);
            EditorGUILayout.LabelField("GUID", string.IsNullOrEmpty(PreviewDebugCapture.PrefabGuid) ? "-" : PreviewDebugCapture.PrefabGuid);
            EditorGUILayout.LabelField("Render Mode", PreviewDebugCapture.RenderMode.ToString());
            EditorGUILayout.LabelField("Canvas Size", $"{PreviewDebugCapture.CanvasSize.x} x {PreviewDebugCapture.CanvasSize.y}");
            EditorGUILayout.LabelField("Last Update (UTC)", PreviewDebugCapture.LastUpdateUtc == System.DateTime.MinValue ? "-" : PreviewDebugCapture.LastUpdateUtc.ToString("u"));

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawTextureBlock("ScreenSpace", PreviewDebugCapture.ScreenTexture, PreviewDebugCapture.ScreenNote);
            DrawTextureBlock("WorldSpace", PreviewDebugCapture.WorldTexture, PreviewDebugCapture.WorldNote);
            DrawTextureBlock("Final", PreviewDebugCapture.FinalTexture, PreviewDebugCapture.FinalNote);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawTextureBlock(string title, Texture2D texture, string note)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(note))
            {
                EditorGUILayout.HelpBox(note, MessageType.None);
            }

            var rect = GUILayoutUtility.GetRect(64f, PreviewHeight, GUILayout.ExpandWidth(true));
            if (texture != null)
            {
                EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f, 1f));
            }

            EditorGUILayout.Space(8f);
        }

        private static void SavePngs()
        {
            var dir = Path.Combine("Temp", "PrefabBoardPreviewDebug");
            Directory.CreateDirectory(dir);

            var saved = 0;
            saved += SaveTexture(PreviewDebugCapture.ScreenTexture, Path.Combine(dir, "screen.png")) ? 1 : 0;
            saved += SaveTexture(PreviewDebugCapture.WorldTexture, Path.Combine(dir, "world.png")) ? 1 : 0;
            saved += SaveTexture(PreviewDebugCapture.FinalTexture, Path.Combine(dir, "final.png")) ? 1 : 0;

            Debug.Log(saved > 0
                ? "PrefabBoard: debug previews saved to " + dir
                : "PrefabBoard: no debug previews to save.");
        }

        private static bool SaveTexture(Texture2D texture, string path)
        {
            if (texture == null)
            {
                return false;
            }

            var bytes = texture.EncodeToPNG();
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            File.WriteAllBytes(path, bytes);
            return true;
        }
    }
}
