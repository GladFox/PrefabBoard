using System;
using System.Collections.Generic;
using System.Linq;
using PrefabBoard.Editor.Data;
using PrefabBoard.Editor.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrefabBoard.Editor.UI
{
    public sealed class BoardCanvasElement : VisualElement
    {
        private enum Mode { None, Panning, DragItems, DragGroup, BoxSelect }

        private readonly VisualElement _groupsLayer;
        private readonly VisualElement _itemsLayer;
        private readonly VisualElement _overlayLayer;
        private readonly SelectionOverlayElement _selectionOverlay;
        private readonly VisualElement _dragGhost;

        private readonly Dictionary<string, PrefabCardElement> _cards = new Dictionary<string, PrefabCardElement>();
        private readonly Dictionary<string, GroupFrameElement> _groups = new Dictionary<string, GroupFrameElement>();
        private readonly HashSet<string> _selectedItems = new HashSet<string>();
        private readonly Dictionary<string, Vector2> _dragStartItemPos = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, Vector2> _dragStartGroupItemPos = new Dictionary<string, Vector2>();

        private PrefabBoardAsset _board;
        private string _selectedGroupId;
        private string _search = string.Empty;

        private Mode _mode;
        private int _pointerId = -1;
        private Vector2 _mouseStart;
        private Vector2 _panStart;
        private Vector2 _dragWorldStart;
        private Rect _dragGroupRectStart;
        private string _draggingGroupId;
        private bool _spacePressed;
        private bool _pendingPreview;

        public BoardCanvasElement()
        {
            AddToClassList("pb-canvas");
            focusable = true;

            _groupsLayer = CreateLayer("pb-layer-groups", true);
            _itemsLayer = CreateLayer("pb-layer-items", true);
            _overlayLayer = CreateLayer("pb-layer-overlay", false);
            _selectionOverlay = new SelectionOverlayElement();
            _dragGhost = new VisualElement();
            _dragGhost.AddToClassList("pb-drag-ghost");
            _dragGhost.style.display = DisplayStyle.None;
            _dragGhost.pickingMode = PickingMode.Ignore;
            _overlayLayer.Add(_selectionOverlay);
            _overlayLayer.Add(_dragGhost);

            Add(_groupsLayer);
            Add(_itemsLayer);
            Add(_overlayLayer);

            RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);

            generateVisualContent += OnGenerateVisualContent;
            schedule.Execute(() => { if (_pendingPreview) RefreshVisualState(); }).Every(200);
        }

        public bool IsGridEnabled => _board != null && _board.viewSettings != null && _board.viewSettings.gridEnabled;
        public bool IsSnapEnabled => _board != null && _board.viewSettings != null && _board.viewSettings.snapEnabled;

        public void SetBoard(PrefabBoardAsset board)
        {
            _board = board;
            ClearDragState();
            RebuildFromData();
        }

        public void SetSearchQuery(string query)
        {
            _search = query ?? string.Empty;
            RefreshVisualState();
        }

        public void SetGridEnabled(bool enabled)
        {
            if (_board == null) return;
            BoardUndo.Record(_board, "Toggle Grid");
            _board.viewSettings.gridEnabled = enabled;
            BoardUndo.MarkDirty(_board);
            MarkDirtyRepaint();
        }

        public void SetSnapEnabled(bool enabled)
        {
            if (_board == null) return;
            BoardUndo.Record(_board, "Toggle Snap");
            _board.viewSettings.snapEnabled = enabled;
            BoardUndo.MarkDirty(_board);
        }

        public void ResetView()
        {
            if (_board == null) return;
            BoardUndo.Record(_board, "Reset View");
            _board.pan = Vector2.zero;
            _board.zoom = 1f;
            BoardUndo.MarkDirty(_board);
            RefreshVisualState();
        }

        public void FrameSelection()
        {
            if (_board == null) return;

            Rect? bounds = null;
            foreach (var id in _selectedItems)
            {
                var item = FindItem(id);
                if (item == null) continue;
                bounds = bounds == null ? new Rect(item.position, item.size) : Expand(bounds.Value, new Rect(item.position, item.size));
            }

            if (!string.IsNullOrEmpty(_selectedGroupId))
            {
                var group = FindGroup(_selectedGroupId);
                if (group != null) bounds = bounds == null ? group.rect : Expand(bounds.Value, group.rect);
            }

            if (bounds == null)
            {
                ResetView();
                return;
            }

            var rect = bounds.Value;
            var padding = 96f;
            var fitW = Mathf.Max(1f, contentRect.width - padding);
            var fitH = Mathf.Max(1f, contentRect.height - padding);
            var zoom = Mathf.Min(fitW / Mathf.Max(1f, rect.width), fitH / Mathf.Max(1f, rect.height));
            zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);

            var center = rect.center;
            var screenCenter = new Vector2(contentRect.width * 0.5f, contentRect.height * 0.5f);

            BoardUndo.Record(_board, "Frame Selection");
            _board.zoom = zoom;
            _board.pan = screenCenter - center * zoom;
            BoardUndo.MarkDirty(_board);
            RefreshVisualState();
        }

        public void CreateGroupFromSelection()
        {
            if (_board == null) return;

            var rect = BuildGroupRect();
            var group = BoardGroupData.Create($"Group {_board.groups.Count + 1}", rect);

            BoardUndo.Record(_board, "Create Group");
            _board.groups.Add(group);
            foreach (var id in _selectedItems)
            {
                var item = FindItem(id);
                if (item != null) item.groupId = group.id;
            }
            _selectedGroupId = group.id;
            BoardUndo.MarkDirty(_board);
            RebuildFromData();
        }

        public Vector2 ScreenToWorld(Vector2 screen) => _board == null ? screen : (screen - _board.pan) / Mathf.Max(0.0001f, _board.zoom);
        public Vector2 WorldToScreen(Vector2 world) => _board == null ? world : world * _board.zoom + _board.pan;

        public void RebuildFromData()
        {
            _groupsLayer.Clear();
            _itemsLayer.Clear();
            _overlayLayer.Clear();
            _overlayLayer.Add(_selectionOverlay);
            _overlayLayer.Add(_dragGhost);
            _selectionOverlay.SetVisible(false);
            HideDragGhost();
            _cards.Clear();
            _groups.Clear();

            if (_board == null) return;

            foreach (var group in _board.groups.Where(x => x != null && !string.IsNullOrEmpty(x.id)))
            {
                var ve = new GroupFrameElement(group.id);
                ve.PrimaryPointerDown += OnGroupPointerDown;
                ve.ContextMenuPopulateRequested += (el, evt) => evt.menu.AppendAction("Delete Group", _ => DeleteGroup(el.GroupId), DropdownMenuAction.AlwaysEnabled);
                _groups[group.id] = ve;
                _groupsLayer.Add(ve);
            }

            foreach (var item in _board.items.Where(x => x != null && !string.IsNullOrEmpty(x.id)))
            {
                var ve = new PrefabCardElement(item.id);
                ve.PrimaryPointerDown += OnCardPointerDown;
                ve.DoubleClicked += OnCardDoubleClicked;
                ve.ExternalDragRequested += OnCardExternalDrag;
                ve.ContextMenuPopulateRequested += OnCardContextMenu;
                _cards[item.id] = ve;
                _itemsLayer.Add(ve);
            }

            RefreshVisualState();
        }

        private float MinZoom => _board?.viewSettings != null ? Mathf.Max(0.05f, _board.viewSettings.minZoom) : 0.2f;
        private float MaxZoom => _board?.viewSettings != null ? Mathf.Max(MinZoom, _board.viewSettings.maxZoom) : 2f;

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_board == null || _board.viewSettings == null || !_board.viewSettings.gridEnabled) return;
            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 0f || height <= 0f) return;

            var step = Mathf.Max(4f, _board.viewSettings.gridStep);
            var stepPx = step * _board.zoom;
            while (stepPx < 10f) { step *= 2f; stepPx = step * _board.zoom; }

            var painter = mgc.painter2D;
            painter.strokeColor = new Color(1f, 1f, 1f, 0.07f);
            painter.lineWidth = 1f;

            var startX = Mathf.Repeat(_board.pan.x, stepPx);
            var startY = Mathf.Repeat(_board.pan.y, stepPx);

            for (var x = startX; x <= width; x += stepPx)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }

            for (var y = startY; y <= height; y += stepPx)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(width, y));
                painter.Stroke();
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            if (_board == null) return;
            var mouse = evt.localMousePosition;
            var before = ScreenToWorld(mouse);
            var zoom = Mathf.Clamp(_board.zoom * Mathf.Exp(-evt.delta.y * 0.05f), MinZoom, MaxZoom);
            if (Mathf.Approximately(zoom, _board.zoom)) return;

            BoardUndo.Record(_board, "Zoom Board");
            _board.zoom = zoom;
            _board.pan = mouse - before * zoom;
            BoardUndo.MarkDirty(_board);
            RefreshVisualState();
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_board == null) return;
            HideDragGhost();
            Focus();
            var mouse = evt.localPosition;

            if (evt.button == 2 || (_spacePressed && evt.button == 0))
            {
                _mode = Mode.Panning;
                _pointerId = evt.pointerId;
                _mouseStart = mouse;
                _panStart = _board.pan;
                CaptureMouse();
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0) return;

            if (!(evt.shiftKey || evt.ctrlKey || evt.commandKey))
            {
                _selectedItems.Clear();
                _selectedGroupId = null;
            }

            _mode = Mode.BoxSelect;
            _pointerId = evt.pointerId;
            _mouseStart = mouse;
            _selectionOverlay.SetRect(new Rect(mouse, Vector2.zero));
            _selectionOverlay.SetVisible(true);
            CaptureMouse();
            RefreshVisualState();
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_board == null || evt.pointerId != _pointerId) return;
            var mouse = evt.localPosition;
            var world = ScreenToWorld(mouse);

            if (_mode == Mode.Panning)
            {
                _board.pan = _panStart + (mouse - _mouseStart);
                BoardUndo.MarkDirty(_board);
                RefreshVisualState();
                evt.StopPropagation();
                return;
            }

            if (_mode == Mode.DragItems)
            {
                var delta = world - _dragWorldStart;
                foreach (var pair in _dragStartItemPos)
                {
                    var item = FindItem(pair.Key);
                    if (item != null) item.position = Snap(pair.Value + delta);
                }
                BoardUndo.MarkDirty(_board);
                RefreshVisualState();
                evt.StopPropagation();
                return;
            }

            if (_mode == Mode.DragGroup)
            {
                var delta = world - _dragWorldStart;
                var group = FindGroup(_draggingGroupId);
                if (group != null)
                {
                    group.rect.position = _dragGroupRectStart.position + delta;
                    foreach (var pair in _dragStartGroupItemPos)
                    {
                        var item = FindItem(pair.Key);
                        if (item != null) item.position = Snap(pair.Value + delta);
                    }
                    BoardUndo.MarkDirty(_board);
                    RefreshVisualState();
                }
                evt.StopPropagation();
                return;
            }

            if (_mode == Mode.BoxSelect)
            {
                _selectionOverlay.SetRect(RectFromPoints(_mouseStart, mouse));
                _selectionOverlay.SetVisible(true);
                evt.StopPropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_board == null || evt.pointerId != _pointerId) return;

            if (_mode == Mode.BoxSelect)
            {
                var rect = RectFromPoints(_mouseStart, evt.localPosition);
                _selectionOverlay.SetVisible(false);
                if (rect.width >= 6f || rect.height >= 6f)
                {
                    if (!(evt.shiftKey || evt.ctrlKey || evt.commandKey)) _selectedItems.Clear();
                    foreach (var id in SelectByBox(rect)) _selectedItems.Add(id);
                    _selectedGroupId = null;
                }
            }

            ClearDragState();
            RefreshVisualState();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_board == null) return;
            if (evt.keyCode == KeyCode.Space) _spacePressed = true;

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection();
                evt.StopPropagation();
            }
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.D)
            {
                DuplicateSelection();
                evt.StopPropagation();
            }
            if (evt.keyCode == KeyCode.F)
            {
                FrameSelection();
                evt.StopPropagation();
            }
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Space) _spacePressed = false;
        }
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (_board == null) return;
            var hasPrefabs = HasDraggedPrefabs();
            DragAndDrop.visualMode = hasPrefabs ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (hasPrefabs)
            {
                var localMouse = evt.localMousePosition;
                UpdateDragGhost(localMouse);
            }
            else
            {
                HideDragGhost();
            }
            evt.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (_board == null || !HasDraggedPrefabs()) return;

            HideDragGhost();
            var world = ScreenToWorld(evt.localMousePosition);
            var added = new List<string>();
            var offset = 0;

            BoardUndo.Record(_board, "Add Prefabs");
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (!AssetGuidUtils.IsPrefabAsset(obj)) continue;
                var guid = AssetGuidUtils.GuidFromObject(obj);
                if (string.IsNullOrEmpty(guid)) continue;
                var item = BoardItemData.Create(guid, world + new Vector2(offset * 24f, offset * 24f));
                _board.items.Add(item);
                added.Add(item.id);
                offset++;
            }
            DragAndDrop.AcceptDrag();
            BoardUndo.MarkDirty(_board);

            _selectedItems.Clear();
            foreach (var id in added) _selectedItems.Add(id);
            _selectedGroupId = null;
            RebuildFromData();
            evt.StopPropagation();
        }

        private void OnCardPointerDown(PrefabCardElement card, PointerDownEvent evt)
        {
            if (_board == null) return;
            Focus();
            SelectItem(card.ItemId, evt.shiftKey || evt.ctrlKey || evt.commandKey);

            _dragStartItemPos.Clear();
            var ids = _selectedItems.Contains(card.ItemId) ? _selectedItems : new HashSet<string> { card.ItemId };
            foreach (var id in ids)
            {
                var item = FindItem(id);
                if (item != null) _dragStartItemPos[id] = item.position;
            }

            _mode = Mode.DragItems;
            _pointerId = evt.pointerId;
            _dragWorldStart = ScreenToWorld(card.ChangeCoordinatesTo(this, evt.localPosition));
            BoardUndo.Record(_board, "Move Cards");
            CaptureMouse();
            RefreshVisualState();
        }

        private void OnCardDoubleClicked(PrefabCardElement card)
        {
            var item = FindItem(card.ItemId);
            if (item == null) return;
            if (!AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) || asset == null) return;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }

        private void OnCardExternalDrag(PrefabCardElement card)
        {
            var item = FindItem(card.ItemId);
            if (item == null) return;
            if (!AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) || asset == null) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { asset };
            DragAndDrop.StartDrag(asset.name);
        }

        private void OnCardContextMenu(PrefabCardElement card, ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Ping Asset", _ => PingCard(card.ItemId), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Open Prefab", _ => OpenCard(card.ItemId), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Duplicate Card", _ => DuplicateCards(new[] { card.ItemId }), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", _ => DeleteCards(new[] { card.ItemId }), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Create Group From Selection", _ => CreateGroupFromSelection(), DropdownMenuAction.AlwaysEnabled);
            var contextTargetIds = GetContextTargetItemIds(card.ItemId);
            foreach (var g in _board.groups)
            {
                var gid = g.id;
                var name = string.IsNullOrWhiteSpace(g.name) ? "Group" : g.name;
                evt.menu.AppendAction($"Add To Group/{name}", _ => AddItemsToGroup(contextTargetIds, gid), DropdownMenuAction.AlwaysEnabled);
            }
            evt.menu.AppendAction("Add To Group/None", _ => AddItemsToGroup(contextTargetIds, string.Empty), DropdownMenuAction.AlwaysEnabled);
        }

        private void OnGroupPointerDown(GroupFrameElement groupElement, PointerDownEvent evt)
        {
            if (_board == null) return;
            Focus();
            _selectedItems.Clear();
            _selectedGroupId = groupElement.GroupId;
            _draggingGroupId = groupElement.GroupId;
            _dragStartGroupItemPos.Clear();

            var group = FindGroup(_draggingGroupId);
            if (group == null) return;

            foreach (var item in _board.items)
            {
                if (item != null && item.groupId == _draggingGroupId) _dragStartGroupItemPos[item.id] = item.position;
            }

            _dragGroupRectStart = group.rect;
            _dragWorldStart = ScreenToWorld(groupElement.ChangeCoordinatesTo(this, evt.localPosition));
            _mode = Mode.DragGroup;
            _pointerId = evt.pointerId;
            BoardUndo.Record(_board, "Move Group");
            CaptureMouse();
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (_board == null) return;
            if (_cards.Count != _board.items.Count || _groups.Count != _board.groups.Count)
            {
                RebuildFromData();
                return;
            }

            _pendingPreview = false;
            foreach (var group in _board.groups)
            {
                if (group == null || !_groups.TryGetValue(group.id, out var ve)) continue;
                var pos = WorldToScreen(group.rect.position);
                var size = group.rect.size * _board.zoom;
                ve.style.left = pos.x;
                ve.style.top = pos.y;
                ve.style.width = Mathf.Max(20f, size.x);
                ve.style.height = Mathf.Max(20f, size.y);
                ve.Bind(group, _selectedGroupId == group.id);
            }

            var query = _search.Trim();
            foreach (var item in _board.items)
            {
                if (item == null || !_cards.TryGetValue(item.id, out var card)) continue;

                var pos = WorldToScreen(item.position);
                var size = item.size * _board.zoom;
                card.style.left = pos.x;
                card.style.top = pos.y;
                card.style.width = Mathf.Max(80f, size.x);
                card.style.height = Mathf.Max(44f, size.y);

                var title = ResolveTitle(item);
                var note = string.IsNullOrWhiteSpace(item.note) ? string.Empty : TrimNote(item.note);
                var missing = !AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) || asset == null;
                var preview = PreviewCache.GetPreview(item.prefabGuid, out var loading);
                if (loading) _pendingPreview = true;

                var highlighted = IsMatch(item, title, query);
                card.Bind(item, title, note, preview, missing, _selectedItems.Contains(item.id), highlighted);
            }

            MarkDirtyRepaint();
        }

        private void SelectItem(string id, bool additive)
        {
            if (!additive) _selectedItems.Clear();
            if (additive && _selectedItems.Contains(id)) _selectedItems.Remove(id);
            else _selectedItems.Add(id);
            _selectedGroupId = null;
        }

        private List<string> SelectByBox(Rect screenRect)
        {
            var min = ScreenToWorld(new Vector2(screenRect.xMin, screenRect.yMin));
            var max = ScreenToWorld(new Vector2(screenRect.xMax, screenRect.yMax));
            var worldRect = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));

            var result = new List<string>();
            foreach (var item in _board.items)
            {
                if (item == null) continue;
                if (new Rect(item.position, item.size).Overlaps(worldRect, true)) result.Add(item.id);
            }
            return result;
        }

        private void DeleteSelection()
        {
            if (_board == null) return;
            if (_selectedItems.Count == 0 && string.IsNullOrEmpty(_selectedGroupId)) return;

            BoardUndo.Record(_board, "Delete Selection");
            if (_selectedItems.Count > 0)
            {
                _board.items.RemoveAll(i => i != null && _selectedItems.Contains(i.id));
                _selectedItems.Clear();
            }
            if (!string.IsNullOrEmpty(_selectedGroupId))
            {
                var gid = _selectedGroupId;
                _board.groups.RemoveAll(g => g != null && g.id == gid);
                foreach (var item in _board.items) if (item != null && item.groupId == gid) item.groupId = string.Empty;
                _selectedGroupId = null;
            }
            BoardUndo.MarkDirty(_board);
            RebuildFromData();
        }

        private void DuplicateSelection()
        {
            if (_board == null || _selectedItems.Count == 0) return;
            DuplicateCards(_selectedItems.ToArray());
        }

        private void DuplicateCards(IEnumerable<string> ids)
        {
            BoardUndo.Record(_board, "Duplicate Cards");
            var newIds = new List<string>();
            foreach (var id in ids)
            {
                var src = FindItem(id);
                if (src == null) continue;
                var dupe = new BoardItemData
                {
                    id = Guid.NewGuid().ToString("N"),
                    prefabGuid = src.prefabGuid,
                    position = src.position + new Vector2(24f, 24f),
                    size = src.size,
                    titleOverride = src.titleOverride,
                    note = src.note,
                    tagColor = src.tagColor,
                    tags = src.tags != null ? src.tags.ToArray() : null,
                    groupId = src.groupId
                };
                _board.items.Add(dupe);
                newIds.Add(dupe.id);
            }
            _selectedItems.Clear();
            foreach (var id in newIds) _selectedItems.Add(id);
            BoardUndo.MarkDirty(_board);
            RebuildFromData();
        }
        private void DeleteCards(IEnumerable<string> ids)
        {
            var set = new HashSet<string>(ids ?? Array.Empty<string>());
            if (set.Count == 0) return;
            BoardUndo.Record(_board, "Delete Cards");
            _board.items.RemoveAll(i => i != null && set.Contains(i.id));
            foreach (var id in set) _selectedItems.Remove(id);
            BoardUndo.MarkDirty(_board);
            RebuildFromData();
        }

        private void DeleteGroup(string groupId)
        {
            if (_board == null || string.IsNullOrEmpty(groupId)) return;
            BoardUndo.Record(_board, "Delete Group");
            _board.groups.RemoveAll(g => g != null && g.id == groupId);
            foreach (var item in _board.items) if (item != null && item.groupId == groupId) item.groupId = string.Empty;
            if (_selectedGroupId == groupId) _selectedGroupId = null;
            BoardUndo.MarkDirty(_board);
            RebuildFromData();
        }

        private void AddSelectedToGroup(string groupId)
        {
            AddItemsToGroup(_selectedItems, groupId);
        }

        private void AddItemsToGroup(IEnumerable<string> itemIds, string groupId)
        {
            if (_board == null) return;
            var ids = new HashSet<string>(itemIds ?? Array.Empty<string>());
            if (ids.Count == 0) return;

            BoardUndo.Record(_board, "Assign Group");
            foreach (var id in ids)
            {
                var item = FindItem(id);
                if (item != null) item.groupId = groupId;
            }
            BoardUndo.MarkDirty(_board);
            RefreshVisualState();
        }

        private IEnumerable<string> GetContextTargetItemIds(string contextItemId)
        {
            if (_selectedItems.Contains(contextItemId))
            {
                return _selectedItems.ToArray();
            }

            return new[] { contextItemId };
        }

        private void PingCard(string id)
        {
            var item = FindItem(id);
            if (item == null) return;
            if (AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) && asset != null)
                EditorGUIUtility.PingObject(asset);
        }

        private void OpenCard(string id)
        {
            var item = FindItem(id);
            if (item == null) return;
            if (AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) && asset != null)
                AssetDatabase.OpenAsset(asset);
        }

        private string ResolveTitle(BoardItemData item)
        {
            if (!string.IsNullOrWhiteSpace(item.titleOverride)) return item.titleOverride;
            if (AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) && asset != null) return asset.name;
            return "Missing Prefab";
        }

        private static string TrimNote(string text)
        {
            var parts = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            if (parts.Length <= 2) return text;
            return string.Join("\n", parts.Take(2)) + "...";
        }

        private bool IsMatch(BoardItemData item, string title, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            var q = query.ToLowerInvariant();
            if (!string.IsNullOrEmpty(title) && title.ToLowerInvariant().Contains(q)) return true;
            if (!string.IsNullOrEmpty(item.note) && item.note.ToLowerInvariant().Contains(q)) return true;
            if (item.tags != null && item.tags.Any(t => !string.IsNullOrEmpty(t) && t.ToLowerInvariant().Contains(q))) return true;
            if (AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) && asset != null)
                return asset.name.ToLowerInvariant().Contains(q);
            return false;
        }

        private Rect BuildGroupRect()
        {
            if (_selectedItems.Count == 0)
            {
                var center = ScreenToWorld(new Vector2(contentRect.width * 0.5f, contentRect.height * 0.5f));
                return new Rect(center - new Vector2(260f, 180f), new Vector2(520f, 360f));
            }

            Rect? bounds = null;
            foreach (var id in _selectedItems)
            {
                var item = FindItem(id);
                if (item == null) continue;
                var rect = new Rect(item.position, item.size);
                bounds = bounds == null ? rect : Expand(bounds.Value, rect);
            }

            if (bounds == null) return new Rect(Vector2.zero, new Vector2(520f, 360f));
            var result = bounds.Value;
            result.xMin -= 48f;
            result.yMin -= 64f;
            result.xMax += 48f;
            result.yMax += 48f;
            return result;
        }

        private Vector2 Snap(Vector2 value)
        {
            if (_board?.viewSettings == null || !_board.viewSettings.snapEnabled) return value;
            var step = Mathf.Max(1f, _board.viewSettings.gridStep);
            return new Vector2(Mathf.Round(value.x / step) * step, Mathf.Round(value.y / step) * step);
        }

        private bool HasDraggedPrefabs() => DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Any(AssetGuidUtils.IsPrefabAsset);

        private BoardItemData FindItem(string id) => _board?.items.Find(x => x != null && x.id == id);
        private BoardGroupData FindGroup(string id) => _board?.groups.Find(x => x != null && x.id == id);

        private void ClearDragState()
        {
            if (_pointerId != -1 && HasMouseCapture()) ReleaseMouse();
            _mode = Mode.None;
            _pointerId = -1;
            _draggingGroupId = null;
            _dragStartItemPos.Clear();
            _dragStartGroupItemPos.Clear();
            _selectionOverlay.SetVisible(false);
            HideDragGhost();
        }

        private void UpdateDragGhost(Vector2 mouseScreen)
        {
            if (_board == null) return;

            var previewSizeWorld = new Vector2(220f, 120f);
            var previewSizeScreen = previewSizeWorld * Mathf.Max(0.2f, _board.zoom);

            _dragGhost.style.left = mouseScreen.x + 12f;
            _dragGhost.style.top = mouseScreen.y + 12f;
            _dragGhost.style.width = Mathf.Max(64f, previewSizeScreen.x * 0.5f);
            _dragGhost.style.height = Mathf.Max(36f, previewSizeScreen.y * 0.5f);
            _dragGhost.style.display = DisplayStyle.Flex;
        }

        private void HideDragGhost()
        {
            _dragGhost.style.display = DisplayStyle.None;
        }

        private static Rect RectFromPoints(Vector2 a, Vector2 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static Rect Expand(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        private static VisualElement CreateLayer(string className, bool picking)
        {
            var ve = new VisualElement();
            ve.AddToClassList(className);
            ve.style.position = Position.Absolute;
            ve.style.left = 0f;
            ve.style.top = 0f;
            ve.style.right = 0f;
            ve.style.bottom = 0f;
            ve.pickingMode = picking ? PickingMode.Position : PickingMode.Ignore;
            return ve;
        }
    }
}

