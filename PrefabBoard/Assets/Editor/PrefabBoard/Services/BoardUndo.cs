using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class BoardUndo
    {
        public static void Record(Object target, string action)
        {
            if (target == null)
            {
                return;
            }

            Undo.RecordObject(target, action);
        }

        public static void RecordMany(Object[] targets, string action)
        {
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            Undo.RecordObjects(targets, action);
        }

        public static void MarkDirty(Object target)
        {
            if (target != null)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
