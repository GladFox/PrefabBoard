using System;
using System.Collections.Generic;
using PrefabBoard.Editor.Data;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class BoardToolbarElement : VisualElement
    {
        private readonly PopupField<string> _boardPopup;
        private readonly TextField _boardNameField;
        private readonly ToolbarSearchField _searchField;
        private readonly Toggle _gridToggle;
        private readonly Toggle _snapToggle;

        private List<string> _boardNames = new List<string>();

        public event Action<int> BoardSelectionChanged;
        public event Action NewBoardRequested;
        public event Action<string> SearchChanged;
        public event Action<bool> GridToggled;
        public event Action<bool> SnapToggled;

        public string BoardNameInput => _boardNameField.value;

        public BoardToolbarElement()
        {
            AddToClassList("pb-toolbar");

            _boardPopup = new PopupField<string>("Board", new List<string> { "No Boards" }, 0);
            _boardPopup.style.minWidth = 200f;
            _boardPopup.RegisterValueChangedCallback(OnBoardPopupChanged);

            _boardNameField = new TextField("Name")
            {
                value = string.Empty
            };
            _boardNameField.style.width = 180f;

            var newBoardButton = new Button(() => NewBoardRequested?.Invoke()) { text = "New" };

            _searchField = new ToolbarSearchField();
            _searchField.style.minWidth = 220f;
            _searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));

            _gridToggle = new Toggle("Grid") { value = true };
            _gridToggle.RegisterValueChangedCallback(evt => GridToggled?.Invoke(evt.newValue));

            _snapToggle = new Toggle("Snap") { value = false };
            _snapToggle.RegisterValueChangedCallback(evt => SnapToggled?.Invoke(evt.newValue));

            Add(_boardPopup);
            Add(newBoardButton);
            Add(_boardNameField);
            Add(_searchField);
            Add(_gridToggle);
            Add(_snapToggle);
        }

        public void SetBoards(IReadOnlyList<PrefabBoardAsset> boards, int selectedIndex)
        {
            _boardNames = new List<string>();
            if (boards != null)
            {
                for (var i = 0; i < boards.Count; i++)
                {
                    var board = boards[i];
                    _boardNames.Add(board == null ? "Missing" : board.boardName);
                }
            }

            if (_boardNames.Count == 0)
            {
                _boardNames.Add("No Boards");
                selectedIndex = 0;
            }

            _boardPopup.choices = _boardNames;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, _boardNames.Count - 1);
            _boardPopup.SetValueWithoutNotify(_boardNames[selectedIndex]);
        }

        public void SetBoardName(string boardName)
        {
            _boardNameField.SetValueWithoutNotify(boardName ?? string.Empty);
        }

        public void SetGridSnap(bool gridEnabled, bool snapEnabled)
        {
            _gridToggle.SetValueWithoutNotify(gridEnabled);
            _snapToggle.SetValueWithoutNotify(snapEnabled);
        }

        private void OnBoardPopupChanged(ChangeEvent<string> evt)
        {
            var index = _boardNames.IndexOf(evt.newValue);
            if (index >= 0)
            {
                BoardSelectionChanged?.Invoke(index);
            }
        }
    }
}
