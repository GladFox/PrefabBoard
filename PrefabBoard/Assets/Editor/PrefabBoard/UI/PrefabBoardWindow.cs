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
        private static PrefabBoardAsset s_pendingBoardToOpen;

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

        public static void OpenBoard(PrefabBoardAsset board)
        {
            if (board == null)
            {
                Open();
                return;
            }

            s_pendingBoardToOpen = board;
            BoardRepository.SetLastOpenedBoard(board);

            var window = GetWindow<PrefabBoardWindow>();
            window.titleContent = new GUIContent("Prefab Board");
            window.minSize = new Vector2(900f, 540f);
            window.Show();
            window.Focus();
            window.TryApplyPendingBoardSelection();
        }

        public void CreateGUI()
        {
            BuildUi();
            LoadData();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnFocus()
        {
            if (_toolbar == null || _canvas == null || _outline == null)
            {
                return;
            }

            RefreshBindings();
        }

        private void OnProjectChanged()
        {
            if (_toolbar == null || _canvas == null || _outline == null)
            {
                return;
            }

            RefreshBindings();
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
            _toolbar.SearchChanged += value => _canvas.SetSearchQuery(value);
            _toolbar.GridToggled += value => _canvas.SetGridEnabled(value);
            _toolbar.SnapToggled += value => _canvas.SetSnapEnabled(value);

            _outline.HomeRequested += () => _canvas.ResetView();
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
            TryApplyPendingBoardSelection();
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

        private void TryApplyPendingBoardSelection()
        {
            if (s_pendingBoardToOpen == null)
            {
                return;
            }

            ReloadBoards();
            if (_boards.Contains(s_pendingBoardToOpen))
            {
                _currentBoard = s_pendingBoardToOpen;
                s_pendingBoardToOpen = null;
                RefreshBindings();
            }
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
            var desiredName = $"Board {_boards.Count + 1}";

            _currentBoard = BoardRepository.CreateBoard(desiredName);
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
