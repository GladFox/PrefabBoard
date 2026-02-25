using System;
using UnityEngine;

namespace PrefabBoard.Editor.Data
{
    [Serializable]
    public sealed class BoardViewSettings
    {
        public bool gridEnabled = true;
        public bool snapEnabled;
        public float gridStep = 64f;
        public float minZoom = 0.2f;
        public float maxZoom = 2f;
    }
}
