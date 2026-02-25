using System;
using UnityEngine;

namespace PrefabBoard.Editor.Data
{
    [Serializable]
    public sealed class BoardGroupData
    {
        public string id;
        public string name;
        public Rect rect;
        public Color color = new Color(0.2f, 0.5f, 0.9f, 0.18f);
        public int zOrder;

        public static BoardGroupData Create(string name, Rect rect)
        {
            return new BoardGroupData
            {
                id = Guid.NewGuid().ToString("N"),
                name = name,
                rect = rect
            };
        }
    }
}
