using System.Collections.Generic;
using UnityEngine;

namespace PrefabBoard.Editor.Data
{
    public sealed class BoardLibraryAsset : ScriptableObject
    {
        public List<PrefabBoardAsset> boards = new List<PrefabBoardAsset>();
        public string lastOpenedBoardId;
    }
}
