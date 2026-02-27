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

        private List<PrefabBoardAsset> _boards = new List<PrefabBoardAsset>();
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
            BoardRepository.EnsureStorage();
            ReloadBoards();
            if (_boards.Count == 0)
            {
                var created = BoardRepository.CreateBoard("Main Board");
                if (created != null)
                {
                    _boards.Add(created);
                }
            }

            _currentBoard = BoardRepository.GetLastOrFirstBoard(_boards);
            RefreshBindings();
        }

        private void RefreshBindings()
        {
            ReloadBoards();
            if (_boards.Count == 0)
            {
                _currentBoard = BoardRepository.CreateBoard("Main Board");
                ReloadBoards();
            }

            if (_currentBoard == null && _boards.Count > 0)
            {
                _currentBoard = _boards[0];
            }

            var selectedIndex = _currentBoard != null ? _boards.IndexOf(_currentBoard) : 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, _boards.Count - 1));

            _toolbar.SetBoards(_boards, selectedIndex);
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
            if (index < 0 || index >= _boards.Count)
            {
                return;
            }

            _currentBoard = _boards[index];
            BoardRepository.SetLastOpenedBoard(_currentBoard);
            RefreshBindings();
        }

        private void OnNewBoard()
        {
            var desiredName = string.IsNullOrWhiteSpace(_toolbar.BoardNameInput)
                ? $"Board {_boards.Count + 1}"
                : _toolbar.BoardNameInput.Trim();

            _currentBoard = BoardRepository.CreateBoard(desiredName);
            RefreshBindings();
        }

        private void OnDuplicateBoard()
        {
            if (_currentBoard == null)
            {
                return;
            }

            _currentBoard = BoardRepository.DuplicateBoard(_currentBoard);
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
            if (_currentBoard == null)
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

            BoardRepository.DeleteBoard(_currentBoard);
            ReloadBoards();
            _currentBoard = BoardRepository.GetLastOrFirstBoard(_boards);
            RefreshBindings();
        }

        private void ReloadBoards()
        {
            _boards = BoardRepository.GetAllBoards();
            if (_currentBoard != null && !_boards.Contains(_currentBoard))
            {
                _currentBoard = null;
            }
        }
    }
}
