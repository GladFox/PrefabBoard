using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class GameViewResolutionUtils
    {
        public static bool TryGetResolution(out Vector2 resolution)
        {
            resolution = Vector2.zero;

            try
            {
                var editorAssembly = typeof(UnityEditor.Editor).Assembly;
                var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                if (gameViewType == null)
                {
                    return false;
                }

                var getMainGameView = gameViewType.GetMethod("GetMainGameView", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (getMainGameView == null)
                {
                    return false;
                }

                var gameView = getMainGameView.Invoke(null, null) as EditorWindow;
                if (gameView == null)
                {
                    return false;
                }

                var size = TryGetCurrentSize(gameViewType, gameView);
                if (size.x <= 0f || size.y <= 0f)
                {
                    size = gameView.position.size;
                }

                if (size.x <= 0f || size.y <= 0f)
                {
                    return false;
                }

                resolution = size;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Vector2 TryGetCurrentSize(Type gameViewType, EditorWindow gameView)
        {
            var currentGameViewSize = gameViewType.GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (currentGameViewSize == null)
            {
                return Vector2.zero;
            }

            var gameViewSize = currentGameViewSize.GetValue(gameView, null);
            if (gameViewSize == null)
            {
                return Vector2.zero;
            }

            var sizeType = gameViewSize.GetType();
            var widthProp = sizeType.GetProperty("width", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var heightProp = sizeType.GetProperty("height", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (widthProp == null || heightProp == null)
            {
                return Vector2.zero;
            }

            var width = Convert.ToSingle(widthProp.GetValue(gameViewSize, null));
            var height = Convert.ToSingle(heightProp.GetValue(gameViewSize, null));
            return new Vector2(width, height);
        }
    }
}
