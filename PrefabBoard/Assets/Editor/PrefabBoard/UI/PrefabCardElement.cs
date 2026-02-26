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
        private readonly VisualElement _actionsContainer;
        private readonly Button _renderModeButton;

        public string ItemId { get; }

        public event Action<PrefabCardElement, PointerDownEvent> PrimaryPointerDown;
        public event Action<PrefabCardElement> DoubleClicked;
        public event Action<PrefabCardElement> ExternalDragRequested;
        public event Action<PrefabCardElement> RenderModeToggleRequested;
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

            _actionsContainer = new VisualElement();
            _actionsContainer.AddToClassList("pb-card-actions");

            _renderModeButton = new Button(OnRenderModeButtonClicked);
            _renderModeButton.AddToClassList("pb-card-render-mode");
            _renderModeButton.focusable = false;
            _renderModeButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            _renderModeButton.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
            _actionsContainer.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            _actionsContainer.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
            _actionsContainer.Add(_renderModeButton);

            Add(_previewImage);
            Add(_titleLabel);
            Add(_noteLabel);
            Add(_missingIndicator);
            Add(_actionsContainer);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<ContextualMenuPopulateEvent>(OnPopulateContextMenu);
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
            _renderModeButton.text = GetRenderModeShort(item.previewRenderMode);
            _renderModeButton.tooltip = GetRenderModeTooltip(item.previewRenderMode);

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

        private void OnRenderModeButtonClicked()
        {
            RenderModeToggleRequested?.Invoke(this);
        }

        private static string GetRenderModeShort(BoardItemPreviewRenderMode mode)
        {
            switch (mode)
            {
                case BoardItemPreviewRenderMode.Resolution:
                    return "R";
                case BoardItemPreviewRenderMode.ControlSize:
                    return "C";
                default:
                    return "A";
            }
        }

        private static string GetRenderModeTooltip(BoardItemPreviewRenderMode mode)
        {
            switch (mode)
            {
                case BoardItemPreviewRenderMode.Resolution:
                    return "Preview mode: Resolution";
                case BoardItemPreviewRenderMode.ControlSize:
                    return "Preview mode: Control Size";
                default:
                    return "Preview mode: Auto";
            }
        }
    }
}
