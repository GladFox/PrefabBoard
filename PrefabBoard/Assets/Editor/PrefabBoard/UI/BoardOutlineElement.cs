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

        public event Action<string> ItemFocusRequested;
        public event Action<string> GroupFocusRequested;

        public BoardOutlineElement()
        {
            AddToClassList("pb-outline");

            var title = new Label("Board Items");
            title.AddToClassList("pb-outline-title");
            Add(title);

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

            var validGroupIds = new HashSet<string>();
            var itemsByGroup = new Dictionary<string, List<BoardItemData>>();
            var ungrouped = new List<BoardItemData>();

            foreach (var group in _board.groups)
            {
                if (group == null || string.IsNullOrEmpty(group.id))
                {
                    continue;
                }

                validGroupIds.Add(group.id);
                itemsByGroup[group.id] = new List<BoardItemData>();
            }

            foreach (var item in _board.items)
            {
                if (item == null || string.IsNullOrEmpty(item.id))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.groupId) &&
                    validGroupIds.Contains(item.groupId) &&
                    itemsByGroup.TryGetValue(item.groupId, out var groupItems))
                {
                    groupItems.Add(item);
                }
                else
                {
                    ungrouped.Add(item);
                }
            }

            foreach (var group in _board.groups)
            {
                if (group == null || string.IsNullOrEmpty(group.id))
                {
                    continue;
                }

                itemsByGroup.TryGetValue(group.id, out var groupItems);
                groupItems ??= new List<BoardItemData>();

                var groupName = string.IsNullOrWhiteSpace(group.name) ? "Group" : group.name;
                var groupButton = new Button(() => GroupFocusRequested?.Invoke(group.id))
                {
                    text = $"{groupName} ({groupItems.Count})"
                };
                groupButton.AddToClassList("pb-outline-group");
                _scroll.Add(groupButton);

                if (groupItems.Count == 0)
                {
                    AddInfoLabel("Empty group", "pb-outline-empty");
                    continue;
                }

                foreach (var item in groupItems)
                {
                    _scroll.Add(CreateItemButton(item, true));
                }
            }

            if (ungrouped.Count > 0)
            {
                var ungroupedLabel = new Label($"Ungrouped ({ungrouped.Count})");
                ungroupedLabel.AddToClassList("pb-outline-ungrouped");
                _scroll.Add(ungroupedLabel);

                foreach (var item in ungrouped)
                {
                    _scroll.Add(CreateItemButton(item, false));
                }
            }
            else if (_board.items.Count == 0)
            {
                AddInfoLabel("No items on board.");
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
