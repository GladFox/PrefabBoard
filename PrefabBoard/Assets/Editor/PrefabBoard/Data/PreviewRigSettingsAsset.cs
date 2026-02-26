using UnityEngine;

namespace PrefabBoard.Editor.Data
{
    public enum PreviewRigSource
    {
        BuiltIn = 0,
        PrefabTemplate = 1
    }

    [CreateAssetMenu(fileName = "PreviewRigSettings", menuName = "PrefabBoard/Preview Rig Settings")]
    public sealed class PreviewRigSettingsAsset : ScriptableObject
    {
        public PreviewRigSource rigSource = PreviewRigSource.BuiltIn;
        public GameObject rigPrefab;

        [Tooltip("Relative path under rig prefab root. Leave empty to use first Camera in hierarchy.")]
        public string cameraPath = string.Empty;

        [Tooltip("Relative path under rig prefab root. Leave empty to use first Canvas in hierarchy.")]
        public string canvasPath = string.Empty;

        [Tooltip("Relative path under rig prefab root. Leave empty to use child named Content or canvas root.")]
        public string contentPath = "Content";

        public Color cameraBackground = new Color(0.16f, 0.16f, 0.16f, 1f);
        public float nearClipPlane = 0.01f;
        public float farClipPlane = 200f;
        public float planeDistance = 1f;
        public bool forceUiLayer = true;
    }
}
