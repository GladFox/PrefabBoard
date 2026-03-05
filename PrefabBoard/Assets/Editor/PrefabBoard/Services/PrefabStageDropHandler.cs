using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrefabBoard.Editor.Services
{
    [InitializeOnLoad]
    public static class PrefabStageDropHandler
    {
        private const string PayloadKey = "PrefabBoard.ExternalDragPrefabs";

        // Top and bottom fraction of a hierarchy row that triggers sibling-insert mode.
        private const float InsertZoneRatio = 0.25f;

        static PrefabStageDropHandler()
        {
            SceneView.duringSceneGui += OnSceneViewGui;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGui;
        }

        public static void SetPayload(GameObject[] prefabs)
        {
            DragAndDrop.SetGenericData(PayloadKey, prefabs);
        }

        public static bool HasPayload()
        {
            if (!(DragAndDrop.GetGenericData(PayloadKey) is GameObject[] prefabs) || prefabs.Length == 0)
            {
                return false;
            }

            return IsCurrentDragPayload(prefabs);
        }

        public static void ClearPayload()
        {
            DragAndDrop.SetGenericData(PayloadKey, null);
        }

        private static void OnSceneViewGui(SceneView _)
        {
            HandleDrag(null, -1);
        }

        private static void OnHierarchyGui(int instanceId, Rect selectionRect)
        {
            var evt = Event.current;
            if (evt == null || !selectionRect.Contains(evt.mousePosition))
            {
                return;
            }

            Transform parent = null;
            var siblingIndex = -1;

#pragma warning disable CS0618
            var hoveredObject = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618

            if (hoveredObject != null)
            {
                var relY = (evt.mousePosition.y - selectionRect.y) / selectionRect.height;

                if (relY < InsertZoneRatio)
                {
                    // Insert before hoveredObject — same parent, before its current index.
                    parent = hoveredObject.transform.parent;
                    siblingIndex = hoveredObject.transform.GetSiblingIndex();
                }
                else if (relY > 1f - InsertZoneRatio)
                {
                    // Insert after hoveredObject — same parent, after its current index.
                    parent = hoveredObject.transform.parent;
                    siblingIndex = hoveredObject.transform.GetSiblingIndex() + 1;
                }
                else
                {
                    // Make child of hoveredObject.
                    parent = hoveredObject.transform;
                    siblingIndex = -1;
                }
            }

            HandleDrag(parent, siblingIndex);
        }

        private static void HandleDrag(Transform requestedParent, int requestedSiblingIndex)
        {
            var evt = Event.current;
            if (evt == null || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform))
            {
                return;
            }

            if (!TryGetPayload(out var prefabs))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null && stage.prefabContentsRoot != null)
                {
                    var parent = ResolveParent(requestedParent, stage.prefabContentsRoot.transform);
                    InstantiateIntoStage(prefabs, stage, parent, requestedSiblingIndex);
                }
                else
                {
                    InstantiateIntoScene(prefabs, requestedParent, requestedSiblingIndex);
                }

                ClearPayload();
            }

            evt.Use();
        }

        private static bool TryGetPayload(out GameObject[] prefabs)
        {
            prefabs = DragAndDrop.GetGenericData(PayloadKey) as GameObject[];
            return prefabs != null && prefabs.Length > 0 && IsCurrentDragPayload(prefabs);
        }

        private static Transform ResolveParent(Transform requestedParent, Transform fallback)
        {
            if (fallback == null)
            {
                return null;
            }

            if (requestedParent == null)
            {
                return fallback;
            }

            if (requestedParent == fallback || requestedParent.IsChildOf(fallback))
            {
                return requestedParent;
            }

            return fallback;
        }

        private static void InstantiateIntoStage(GameObject[] prefabs, PrefabStage stage, Transform parent, int siblingIndex)
        {
            if (prefabs == null || stage == null)
            {
                return;
            }

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                try
                {
                    var instance = PrefabUtility.InstantiatePrefab(prefab, stage.scene) as GameObject;
                    if (instance == null)
                    {
                        continue;
                    }

                    Undo.RegisterCreatedObjectUndo(instance, "Add Prefab From PrefabBoard");
                    if (parent != null)
                    {
                        Undo.SetTransformParent(instance.transform, parent, "Parent Prefab In Prefab Stage");
                        if (siblingIndex >= 0)
                        {
                            instance.transform.SetSiblingIndex(siblingIndex);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("PrefabBoard: failed to drop prefab into Prefab Mode. " + ex.Message);
                }
            }
        }

        private static void InstantiateIntoScene(GameObject[] prefabs, Transform parent, int siblingIndex)
        {
            if (prefabs == null)
            {
                return;
            }

            // Use the parent's scene, or fall back to the active scene.
            var targetScene = parent != null ? parent.gameObject.scene : SceneManager.GetActiveScene();

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                try
                {
                    var instance = PrefabUtility.InstantiatePrefab(prefab, targetScene) as GameObject;
                    if (instance == null)
                    {
                        continue;
                    }

                    Undo.RegisterCreatedObjectUndo(instance, "Add Prefab From PrefabBoard");
                    if (parent != null)
                    {
                        Undo.SetTransformParent(instance.transform, parent, "Parent Prefab In Scene");
                    }

                    if (siblingIndex >= 0)
                    {
                        instance.transform.SetSiblingIndex(siblingIndex);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("PrefabBoard: failed to drop prefab into scene. " + ex.Message);
                }
            }
        }

        private static bool IsCurrentDragPayload(GameObject[] prefabs)
        {
            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length == 0 || refs.Length != prefabs.Length)
            {
                return false;
            }

            for (var i = 0; i < prefabs.Length; i++)
            {
                if (!(refs[i] is GameObject go) || go != prefabs[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
