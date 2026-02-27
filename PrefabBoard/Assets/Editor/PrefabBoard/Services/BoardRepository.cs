using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrefabBoard.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.Services
{
    public static class BoardRepository
    {
        public const string RootFolder = "Assets/Editor/PrefabBoards";
        public const string BoardsFolder = RootFolder + "/Boards";
        public const string LibraryPath = RootFolder + "/BoardLibrary.asset";

        private const string LastOpenedBoardIdEditorPrefKey = "PrefabBoard.LastOpenedBoardId";

        public static void EnsureStorage()
        {
            EnsureFolder("Assets", "Editor");
            EnsureFolder("Assets/Editor", "PrefabBoards");
            EnsureFolder(RootFolder, "Boards");
        }

        public static List<PrefabBoardAsset> GetAllBoards()
        {
            EnsureStorage();

            var result = new List<PrefabBoardAsset>();
            var guids = AssetDatabase.FindAssets("t:PrefabBoardAsset", new[] { BoardsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var board = AssetDatabase.LoadAssetAtPath<PrefabBoardAsset>(path);
                if (board != null)
                {
                    result.Add(board);
                }
            }

            result = result
                .OrderBy(board => board != null ? board.boardName : string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(board => board != null ? board.boardId : string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return result;
        }

        public static PrefabBoardAsset GetLastOrFirstBoard(IReadOnlyList<PrefabBoardAsset> boards)
        {
            if (boards == null || boards.Count == 0)
            {
                return null;
            }

            var lastId = EditorPrefs.GetString(LastOpenedBoardIdEditorPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(lastId))
            {
                for (var i = 0; i < boards.Count; i++)
                {
                    var board = boards[i];
                    if (board != null && board.boardId == lastId)
                    {
                        return board;
                    }
                }
            }

            return boards[0];
        }

        public static PrefabBoardAsset CreateBoard(string boardName)
        {
            EnsureStorage();

            var board = ScriptableObject.CreateInstance<PrefabBoardAsset>();
            board.boardId = Guid.NewGuid().ToString("N");
            board.boardName = string.IsNullOrWhiteSpace(boardName) ? "Board" : boardName.Trim();
            board.pan = Vector2.zero;
            board.zoom = 1f;
            board.viewSettings = new BoardViewSettings();
            board.items = new List<BoardItemData>();
            board.groups = new List<BoardGroupData>();

            var safeName = SanitizeFileName(board.boardName);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{BoardsFolder}/{safeName}.asset");
            AssetDatabase.CreateAsset(board, path);

            EditorUtility.SetDirty(board);
            AssetDatabase.SaveAssets();
            SetLastOpenedBoard(board);
            return board;
        }

        public static PrefabBoardAsset DuplicateBoard(PrefabBoardAsset source)
        {
            if (source == null)
            {
                return null;
            }

            EnsureStorage();

            var duplicate = ScriptableObject.CreateInstance<PrefabBoardAsset>();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), duplicate);

            duplicate.boardId = Guid.NewGuid().ToString("N");
            duplicate.boardName = source.boardName + " Copy";

            var groupIdMap = new Dictionary<string, string>();
            foreach (var item in duplicate.items)
            {
                item.id = Guid.NewGuid().ToString("N");
            }

            foreach (var group in duplicate.groups)
            {
                var oldId = group.id;
                var newId = Guid.NewGuid().ToString("N");
                group.id = newId;
                if (!string.IsNullOrEmpty(oldId))
                {
                    groupIdMap[oldId] = newId;
                }
            }

            foreach (var item in duplicate.items)
            {
                if (string.IsNullOrEmpty(item.groupId))
                {
                    continue;
                }

                if (groupIdMap.TryGetValue(item.groupId, out var mappedGroupId))
                {
                    item.groupId = mappedGroupId;
                }
                else
                {
                    item.groupId = string.Empty;
                }
            }

            var safeName = SanitizeFileName(duplicate.boardName);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{BoardsFolder}/{safeName}.asset");
            AssetDatabase.CreateAsset(duplicate, path);

            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
            SetLastOpenedBoard(duplicate);
            return duplicate;
        }

        public static void RenameBoard(PrefabBoardAsset board, string newName)
        {
            if (board == null)
            {
                return;
            }

            board.boardName = string.IsNullOrWhiteSpace(newName) ? board.boardName : newName.Trim();
            EditorUtility.SetDirty(board);
        }

        public static void DeleteBoard(PrefabBoardAsset board)
        {
            if (board == null)
            {
                return;
            }

            var boardPath = AssetDatabase.GetAssetPath(board);
            if (!string.IsNullOrEmpty(boardPath))
            {
                AssetDatabase.DeleteAsset(boardPath);
            }

            AssetDatabase.SaveAssets();

            var boards = GetAllBoards();
            if (boards.Count == 0)
            {
                var fallbackBoard = CreateBoard("Main Board");
                SetLastOpenedBoard(fallbackBoard);
            }
            else
            {
                SetLastOpenedBoard(boards[0]);
            }
        }

        public static void SetLastOpenedBoard(PrefabBoardAsset board)
        {
            if (board == null || string.IsNullOrEmpty(board.boardId))
            {
                EditorPrefs.DeleteKey(LastOpenedBoardIdEditorPrefKey);
                return;
            }

            EditorPrefs.SetString(LastOpenedBoardIdEditorPrefKey, board.boardId);
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            var path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var result = value;
            foreach (var invalidChar in invalidChars)
            {
                result = result.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(result) ? "Board" : result;
        }
    }
}
