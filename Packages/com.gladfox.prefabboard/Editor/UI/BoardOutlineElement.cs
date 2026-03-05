using System;
using System.Collections.Generic;
using PrefabBoard.Editor.Data;
using PrefabBoard.Editor.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class BoardOutlineElement : VisualElement
    {
        private readonly ScrollView _scroll;
        private PrefabBoardAsset _board;

        public event Action HomeRequested;
        public event Action<string> ItemFocusRequested;
        public event Action<string> GroupFocusRequested;

        public BoardOutlineElement()
        {
            AddToClassList("pb-outline");

            var title = new Label("Board Items");
            title.AddToClassList("pb-outline-title");
            Add(title);

            var homeButton = new Button(() => HomeRequested?.Invoke())
            {
                text = "Home"
            };
            homeButton.AddToClassList("pb-outline-home");
            Add(homeButton);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("pb-outline-scroll");
            Add(_scroll);
        }

        public void SetBoard(PrefabBoardAsset board)
        {
            _board = board;
            Rebuild();
        }

        public void Rebuild()
        {
            _scroll.Clear();

            if (_board == null)
            {
                AddInfoLabel("No board selected.");
                return;
            }

            var groups = new List<BoardGroupData>();
            foreach (var group in _board.groups)
            {
                if (group == null || string.IsNullOrEmpty(group.id))
                {
                    continue;
                }

                groups.Add(group);
            }

            var anchorsLabel = new Label($"Anchors ({groups.Count})");
            anchorsLabel.AddToClassList("pb-outline-ungrouped");
            _scroll.Add(anchorsLabel);

            if (groups.Count == 0)
            {
                AddInfoLabel("No anchors.");
            }
            else
            {
                foreach (var group in groups)
                {
                var groupName = string.IsNullOrWhiteSpace(group.name) ? "Group" : group.name;
                var groupButton = new Button(() => GroupFocusRequested?.Invoke(group.id))
                {
                    text = groupName
                };
                groupButton.AddToClassList("pb-outline-group");
                _scroll.Add(groupButton);
                }
            }

            var items = new List<BoardItemData>();
            foreach (var item in _board.items)
            {
                if (item != null && !string.IsNullOrEmpty(item.id))
                {
                    items.Add(item);
                }
            }

            var itemsLabel = new Label($"Elements ({items.Count})");
            itemsLabel.AddToClassList("pb-outline-ungrouped");
            _scroll.Add(itemsLabel);

            if (items.Count == 0)
            {
                AddInfoLabel("No elements.");
            }
            else
            {
                foreach (var item in items)
                {
                    _scroll.Add(CreateItemButton(item, false));
                }
            }
        }

        private Button CreateItemButton(BoardItemData item, bool grouped)
        {
            var itemButton = new Button(() => ItemFocusRequested?.Invoke(item.id))
            {
                text = ResolveItemLabel(item)
            };
            itemButton.AddToClassList("pb-outline-item");
            if (grouped)
            {
                itemButton.AddToClassList("pb-outline-item--grouped");
            }

            return itemButton;
        }

        private static string ResolveItemLabel(BoardItemData item)
        {
            if (!string.IsNullOrWhiteSpace(item.titleOverride))
            {
                return item.titleOverride;
            }

            if (AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) && asset != null)
            {
                return asset.name;
            }

            return "Missing Prefab";
        }

        private void AddInfoLabel(string text, string className = null)
        {
            var label = new Label(text);
            label.AddToClassList("pb-outline-info");
            if (!string.IsNullOrEmpty(className))
            {
                label.AddToClassList(className);
            }

            _scroll.Add(label);
        }
    }
}
