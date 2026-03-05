using System;
using System.Collections.Generic;
using PrefabBoard.Editor.Data;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class GroupFrameElement : VisualElement
    {
        public enum ResizeHandle
        {
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private readonly VisualElement _titleBar;
        private readonly Label _titleLabel;
        private readonly Dictionary<ResizeHandle, VisualElement> _resizeHandles = new Dictionary<ResizeHandle, VisualElement>();

        public string GroupId { get; }

        public event Action<GroupFrameElement, PointerDownEvent> PrimaryPointerDown;
        public event Action<GroupFrameElement, ResizeHandle, PointerDownEvent> ResizePointerDown;

        public GroupFrameElement(string groupId)
        {
            GroupId = groupId;
            AddToClassList("pb-group");
            style.position = Position.Absolute;

            _titleBar = new VisualElement();
            _titleBar.AddToClassList("pb-group-title-bar");
            _titleBar.RegisterCallback<PointerDownEvent>(OnTitlePointerDown);

            _titleLabel = new Label();
            _titleLabel.AddToClassList("pb-group-title");

            _titleBar.Add(_titleLabel);

            CreateResizeHandle(ResizeHandle.Left, "pb-group-resize-handle--left");
            CreateResizeHandle(ResizeHandle.Right, "pb-group-resize-handle--right");
            CreateResizeHandle(ResizeHandle.Top, "pb-group-resize-handle--top");
            CreateResizeHandle(ResizeHandle.Bottom, "pb-group-resize-handle--bottom");
            CreateResizeHandle(ResizeHandle.TopLeft, "pb-group-resize-handle--top-left");
            CreateResizeHandle(ResizeHandle.TopRight, "pb-group-resize-handle--top-right");
            CreateResizeHandle(ResizeHandle.BottomLeft, "pb-group-resize-handle--bottom-left");
            CreateResizeHandle(ResizeHandle.BottomRight, "pb-group-resize-handle--bottom-right");
            Add(_titleBar);
        }

        public void Bind(BoardGroupData data, bool selected)
        {
            _titleLabel.text = string.IsNullOrWhiteSpace(data.name) ? "Group" : data.name;
            EnableInClassList("pb-group--selected", selected);
        }

        private void CreateResizeHandle(ResizeHandle handle, string className)
        {
            var handleElement = new VisualElement();
            handleElement.AddToClassList("pb-group-resize-handle");
            handleElement.AddToClassList(className);
            handleElement.RegisterCallback<PointerDownEvent>(evt => OnResizePointerDown(handle, evt));
            _resizeHandles[handle] = handleElement;
            Add(handleElement);
        }

        private void OnTitlePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            PrimaryPointerDown?.Invoke(this, evt);
            evt.StopPropagation();
        }

        private void OnResizePointerDown(ResizeHandle handle, PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            ResizePointerDown?.Invoke(this, handle, evt);
            evt.StopPropagation();
        }

    }
}
