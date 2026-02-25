using System;
using System.Collections.Generic;
using System.IO;
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

        public static BoardLibraryAsset LoadOrCreateLibrary()
        {
            EnsureFolder("Assets", "Editor");
            EnsureFolder("Assets/Editor", "PrefabBoards");
            EnsureFolder(RootFolder, "Boards");

            var library = AssetDatabase.LoadAssetAtPath<BoardLibraryAsset>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<BoardLibraryAsset>();
                AssetDatabase.CreateAsset(library, LibraryPath);
                AssetDatabase.SaveAssets();
            }

            CleanupNullBoards(library);
            if (library.boards.Count == 0)
            {
                var board = CreateBoard(library, "Main Board");
                library.lastOpenedBoardId = board != null ? board.boardId : string.Empty;
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }

            return library;
        }

        public static PrefabBoardAsset GetLastOrFirstBoard(BoardLibraryAsset library)
        {
            if (library == null)
            {
                return null;
            }

            CleanupNullBoards(library);
            if (!string.IsNullOrEmpty(library.lastOpenedBoardId))
            {
                var last = library.boards.Find(board => board != null && board.boardId == library.lastOpenedBoardId);
                if (last != null)
                {
                    return last;
                }
            }

            return library.boards.Count > 0 ? library.boards[0] : null;
        }

        public static PrefabBoardAsset CreateBoard(BoardLibraryAsset library, string boardName)
        {
            if (library == null)
            {
                return null;
            }

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

            library.boards.Add(board);
            library.lastOpenedBoardId = board.boardId;

            EditorUtility.SetDirty(library);
            EditorUtility.SetDirty(board);
            AssetDatabase.SaveAssets();
            return board;
        }

        public static PrefabBoardAsset DuplicateBoard(BoardLibraryAsset library, PrefabBoardAsset source)
        {
            if (library == null || source == null)
            {
                return null;
            }

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

            library.boards.Add(duplicate);
            library.lastOpenedBoardId = duplicate.boardId;

            EditorUtility.SetDirty(library);
            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
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

        public static void DeleteBoard(BoardLibraryAsset library, PrefabBoardAsset board)
        {
            if (library == null || board == null)
            {
                return;
            }

            library.boards.Remove(board);
            var boardPath = AssetDatabase.GetAssetPath(board);
            if (!string.IsNullOrEmpty(boardPath))
            {
                AssetDatabase.DeleteAsset(boardPath);
            }

            if (library.boards.Count == 0)
            {
                var fallbackBoard = CreateBoard(library, "Main Board");
                library.lastOpenedBoardId = fallbackBoard != null ? fallbackBoard.boardId : string.Empty;
            }
            else
            {
                library.lastOpenedBoardId = library.boards[0] != null ? library.boards[0].boardId : string.Empty;
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        public static void SetLastOpenedBoard(BoardLibraryAsset library, PrefabBoardAsset board)
        {
            if (library == null)
            {
                return;
            }

            library.lastOpenedBoardId = board != null ? board.boardId : string.Empty;
            EditorUtility.SetDirty(library);
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            var path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void CleanupNullBoards(BoardLibraryAsset library)
        {
            if (library == null)
            {
                return;
            }

            library.boards.RemoveAll(board => board == null);
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
