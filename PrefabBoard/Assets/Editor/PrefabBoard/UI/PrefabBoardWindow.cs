using System.Collections.Generic;
using PrefabBoard.Editor.Data;
using PrefabBoard.Editor.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class PrefabBoardWindow : EditorWindow
    {
        private const string StylePath = "Assets/Editor/PrefabBoard/Styles/PrefabBoard.uss";

        private BoardLibraryAsset _library;
        private PrefabBoardAsset _currentBoard;

        private BoardToolbarElement _toolbar;
        private BoardCanvasElement _canvas;
        private BoardOutlineElement _outline;

        [MenuItem("Window/Prefab Board")]
        public static void Open()
        {
            var window = GetWindow<PrefabBoardWindow>();
            window.titleContent = new GUIContent("Prefab Board");
            window.minSize = new Vector2(900f, 540f);
            window.Show();
        }

        public void CreateGUI()
        {
            BuildUi();
            LoadData();
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null)
            {
                rootVisualElement.styleSheets.Add(style);
            }

            _toolbar = new BoardToolbarElement();
            _canvas = new BoardCanvasElement();
            _outline = new BoardOutlineElement();

            var contentRow = new VisualElement();
            contentRow.AddToClassList("pb-content-row");
            contentRow.style.flexGrow = 1f;
            _canvas.style.flexGrow = 1f;
            _outline.style.flexShrink = 0f;

            rootVisualElement.Add(_toolbar);
            rootVisualElement.Add(contentRow);
            contentRow.Add(_canvas);
            contentRow.Add(_outline);

            _toolbar.BoardSelectionChanged += OnBoardSelectionChanged;
            _toolbar.NewBoardRequested += OnNewBoard;
            _toolbar.DuplicateBoardRequested += OnDuplicateBoard;
            _toolbar.RenameBoardRequested += OnRenameBoard;
            _toolbar.DeleteBoardRequested += OnDeleteBoard;
            _toolbar.CreateGroupRequested += () => _canvas.CreateGroupFromSelection();
            _toolbar.HomeRequested += () => _canvas.ResetView();
            _toolbar.SearchChanged += value => _canvas.SetSearchQuery(value);
            _toolbar.GridToggled += value => _canvas.SetGridEnabled(value);
            _toolbar.SnapToggled += value => _canvas.SetSnapEnabled(value);

            _outline.ItemFocusRequested += id => _canvas.FocusItem(id);
            _outline.GroupFocusRequested += id => _canvas.FocusGroup(id);
            _canvas.BoardDataChanged += () => _outline.Rebuild();
        }

        private void LoadData()
        {
            _library = BoardRepository.LoadOrCreateLibrary();
            _currentBoard = BoardRepository.GetLastOrFirstBoard(_library);
            RefreshBindings();
        }

        private void RefreshBindings()
        {
            if (_library == null)
            {
                return;
            }

            CleanupNullBoards();

            var boards = _library.boards;
            if (_currentBoard == null && boards.Count > 0)
            {
                _currentBoard = boards[0];
            }

            var selectedIndex = _currentBoard != null ? boards.IndexOf(_currentBoard) : 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, boards.Count - 1));

            _toolbar.SetBoards(boards, selectedIndex);
            _toolbar.SetBoardName(_currentBoard != null ? _currentBoard.boardName : string.Empty);

            if (_currentBoard != null)
            {
                if (_currentBoard.viewSettings == null)
                {
                    _currentBoard.viewSettings = new BoardViewSettings();
                }

                _toolbar.SetGridSnap(_currentBoard.viewSettings.gridEnabled, _currentBoard.viewSettings.snapEnabled);
            }

            _canvas.SetBoard(_currentBoard);
            _outline.SetBoard(_currentBoard);
            Repaint();
        }

        private void OnBoardSelectionChanged(int index)
        {
            if (_library == null || index < 0 || index >= _library.boards.Count)
            {
                return;
            }

            _currentBoard = _library.boards[index];
            BoardRepository.SetLastOpenedBoard(_library, _currentBoard);
            RefreshBindings();
        }

        private void OnNewBoard()
        {
            if (_library == null)
            {
                return;
            }

            var desiredName = string.IsNullOrWhiteSpace(_toolbar.BoardNameInput)
                ? $"Board {_library.boards.Count + 1}"
                : _toolbar.BoardNameInput.Trim();

            _currentBoard = BoardRepository.CreateBoard(_library, desiredName);
            RefreshBindings();
        }

        private void OnDuplicateBoard()
        {
            if (_library == null || _currentBoard == null)
            {
                return;
            }

            _currentBoard = BoardRepository.DuplicateBoard(_library, _currentBoard);
            RefreshBindings();
        }

        private void OnRenameBoard()
        {
            if (_currentBoard == null)
            {
                return;
            }

            var desiredName = _toolbar.BoardNameInput;
            if (string.IsNullOrWhiteSpace(desiredName))
            {
                return;
            }

            BoardUndo.Record(_currentBoard, "Rename Board");
            BoardRepository.RenameBoard(_currentBoard, desiredName);
            AssetDatabase.SaveAssets();
            RefreshBindings();
        }

        private void OnDeleteBoard()
        {
            if (_library == null || _currentBoard == null)
            {
                return;
            }

            var confirm = EditorUtility.DisplayDialog(
                "Delete board",
                $"Delete board '{_currentBoard.boardName}'?",
                "Delete",
                "Cancel");

            if (!confirm)
            {
                return;
            }

            BoardRepository.DeleteBoard(_library, _currentBoard);
            _currentBoard = BoardRepository.GetLastOrFirstBoard(_library);
            RefreshBindings();
        }

        private void CleanupNullBoards()
        {
            if (_library == null)
            {
                return;
            }

            _library.boards.RemoveAll(board => board == null);
            if (_library.boards.Count == 0)
            {
                _currentBoard = BoardRepository.CreateBoard(_library, "Main Board");
            }

            EditorUtility.SetDirty(_library);
            AssetDatabase.SaveAssets();
        }
    }
}
