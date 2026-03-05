using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabBoard.Editor.Data
{
    public sealed class PrefabBoardAsset : ScriptableObject
    {
        public string boardId = Guid.NewGuid().ToString("N");
        public string boardName = "Board";
        public Vector2 pan;
        public float zoom = 1f;
        public List<BoardItemData> items = new List<BoardItemData>();
        public List<BoardGroupData> groups = new List<BoardGroupData>();
        public BoardViewSettings viewSettings = new BoardViewSettings();
    }
}
