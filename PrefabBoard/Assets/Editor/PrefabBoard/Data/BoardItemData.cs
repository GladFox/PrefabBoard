using System;
using UnityEngine;

namespace PrefabBoard.Editor.Data
{
    public enum BoardItemPreviewRenderMode
    {
        Auto = 0,
        Resolution = 1,
        ControlSize = 2
    }

    [Serializable]
    public sealed class BoardItemData
    {
        public string id;
        public string prefabGuid;
        public Vector2 position;
        public Vector2 size = new Vector2(220f, 120f);
        public string titleOverride;
        [TextArea(2, 6)] public string note;
        public Color tagColor = Color.clear;
        public string[] tags;
        public string groupId;
        public BoardItemPreviewRenderMode previewRenderMode = BoardItemPreviewRenderMode.Auto;

        public static BoardItemData Create(string prefabGuid, Vector2 position)
        {
            return new BoardItemData
            {
                id = Guid.NewGuid().ToString("N"),
                prefabGuid = prefabGuid,
                position = position,
                size = new Vector2(220f, 120f),
                previewRenderMode = BoardItemPreviewRenderMode.Auto
            };
        }
    }
}
