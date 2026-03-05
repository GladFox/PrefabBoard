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
        private readonly ToolbarSearchField _searchField;
        private readonly Toggle _gridToggle;
        private readonly Toggle _snapToggle;

        private List<string> _boardNames = new List<string>();

        public event Action<int> BoardSelectionChanged;
        public event Action NewBoardRequested;
        public event Action<string> SearchChanged;
        public event Action<bool> GridToggled;
        public event Action<bool> SnapToggled;

        public BoardToolbarElement()
        {
            AddToClassList("pb-toolbar");

            _boardPopup = new PopupField<string>("Board", new List<string> { "No Boards" }, 0);
            _boardPopup.style.minWidth = 200f;
            ConfigureCompactLabel(_boardPopup);
            _boardPopup.RegisterValueChangedCallback(OnBoardPopupChanged);

            var newBoardButton = new Button(() => NewBoardRequested?.Invoke()) { text = "New" };

            _searchField = new ToolbarSearchField();
            _searchField.style.minWidth = 220f;
            _searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));

            _gridToggle = new Toggle("Grid") { value = true };
            ConfigureCompactLabel(_gridToggle);
            _gridToggle.RegisterValueChangedCallback(evt => GridToggled?.Invoke(evt.newValue));

            _snapToggle = new Toggle("Snap") { value = false };
            ConfigureCompactLabel(_snapToggle);
            _snapToggle.RegisterValueChangedCallback(evt => SnapToggled?.Invoke(evt.newValue));

            AddToolbarElement(_boardPopup);
            AddToolbarElement(newBoardButton);
            AddToolbarElement(_searchField);
            AddToolbarElement(_gridToggle);
            AddToolbarElement(_snapToggle, false);
        }

        public void SetBoards(IReadOnlyList<PrefabBoardAsset> boards, int selectedIndex)
        {
            _boardNames = new List<string>();
            if (boards != null)
            {
                for (var i = 0; i < boards.Count; i++)
                {
                    var board = boards[i];
                    _boardNames.Add(board == null ? "Missing" : board.name);
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

        public void SetGridSnap(bool gridEnabled, bool snapEnabled)
        {
            _gridToggle.SetValueWithoutNotify(gridEnabled);
            _snapToggle.SetValueWithoutNotify(snapEnabled);
        }

        private void OnBoardPopupChanged(ChangeEvent<string> evt)
        {
            var index = _boardPopup.index;
            if (index >= 0)
            {
                BoardSelectionChanged?.Invoke(index);
            }
        }

        private static void ConfigureCompactLabel<T>(BaseField<T> field)
        {
            if (field == null || field.labelElement == null)
            {
                return;
            }

            field.labelElement.style.minWidth = 0f;
            field.labelElement.style.width = StyleKeyword.Auto;
            field.labelElement.style.marginRight = 4f;
        }

        private void AddToolbarElement(VisualElement element, bool withTrailingSpacing = true)
        {
            if (element == null)
            {
                return;
            }

            element.style.marginRight = withTrailingSpacing ? 6f : 0f;
            Add(element);
        }
    }
}
