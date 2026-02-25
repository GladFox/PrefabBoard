using System;
using PrefabBoard.Editor.Data;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class GroupFrameElement : VisualElement
    {
        private readonly Label _titleLabel;

        public string GroupId { get; }

        public event Action<GroupFrameElement, PointerDownEvent> PrimaryPointerDown;
        public event Action<GroupFrameElement, ContextualMenuPopulateEvent> ContextMenuPopulateRequested;

        public GroupFrameElement(string groupId)
        {
            GroupId = groupId;
            AddToClassList("pb-group");
            style.position = Position.Absolute;

            _titleLabel = new Label();
            _titleLabel.AddToClassList("pb-group-title");
            Add(_titleLabel);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<ContextualMenuPopulateEvent>(OnPopulateContextMenu);
        }

        public void Bind(BoardGroupData data, bool selected)
        {
            _titleLabel.text = string.IsNullOrWhiteSpace(data.name) ? "Group" : data.name;
            EnableInClassList("pb-group--selected", selected);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
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
