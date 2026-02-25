using System;
using PrefabBoard.Editor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class PrefabCardElement : VisualElement
    {
        private readonly Image _previewImage;
        private readonly Label _titleLabel;
        private readonly Label _noteLabel;
        private readonly VisualElement _missingIndicator;

        public string ItemId { get; }

        public event Action<PrefabCardElement, PointerDownEvent> PrimaryPointerDown;
        public event Action<PrefabCardElement> DoubleClicked;
        public event Action<PrefabCardElement> ExternalDragRequested;
        public event Action<PrefabCardElement, ContextualMenuPopulateEvent> ContextMenuPopulateRequested;

        public PrefabCardElement(string itemId)
        {
            ItemId = itemId;
            AddToClassList("pb-card");
            style.position = Position.Absolute;

            _previewImage = new Image { scaleMode = ScaleMode.ScaleToFit };
            _previewImage.AddToClassList("pb-card-preview");

            _titleLabel = new Label();
            _titleLabel.AddToClassList("pb-card-title");

            _noteLabel = new Label();
            _noteLabel.AddToClassList("pb-card-note");

            _missingIndicator = new VisualElement();
            _missingIndicator.AddToClassList("pb-card-missing");

            Add(_previewImage);
            Add(_titleLabel);
            Add(_noteLabel);
            Add(_missingIndicator);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            AddManipulator(new ContextualMenuManipulator(OnPopulateContextMenu));
        }

        public void Bind(BoardItemData item, string title, string note, Texture2D preview, bool missing, bool selected, bool highlighted)
        {
            _previewImage.image = preview;
            _titleLabel.text = title;
            _noteLabel.text = note;

            _missingIndicator.style.display = missing ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList("pb-card--selected", selected);
            EnableInClassList("pb-card--muted", !highlighted);
            EnableInClassList("pb-card--missing", missing);

            tooltip = missing ? "Prefab not found" : title;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (evt.clickCount == 2)
            {
                DoubleClicked?.Invoke(this);
                evt.StopPropagation();
                return;
            }

            if (evt.ctrlKey || evt.commandKey)
            {
                ExternalDragRequested?.Invoke(this);
                evt.StopPropagation();
                return;
            }

            PrimaryPointerDown?.Invoke(this, evt);
            evt.StopPropagation();
        }

        private void OnPopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            ContextMenuPopulateRequested?.Invoke(this, evt);
        }
    }
}
