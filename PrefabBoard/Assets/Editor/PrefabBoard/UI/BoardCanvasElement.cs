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
        private enum Mode { None, Panning, DragItems, DragGroup, ResizeGroup, BoxSelect }

        private readonly VisualElement _groupsLayer;
        private readonly VisualElement _itemsLayer;
        private readonly VisualElement _overlayLayer;
        private readonly SelectionOverlayElement _selectionOverlay;
        private readonly VisualElement _dragGhost;

        private readonly Dictionary<string, PrefabCardElement> _cards = new Dictionary<string, PrefabCardElement>();
        private readonly Dictionary<string, GroupFrameElement> _groups = new Dictionary<string, GroupFrameElement>();
        private readonly HashSet<string> _selectedItems = new HashSet<string>();
        private readonly HashSet<string> _dirtyPreviewGuids = new HashSet<string>();
        private readonly Dictionary<string, Vector2> _dragStartItemPos = new Dictionary<string, Vector2>();
        private const float MinGroupWidth = 120f;
        private const float MinGroupHeight = 90f;

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
        private GroupFrameElement.ResizeHandle _groupResizeHandle = GroupFrameElement.ResizeHandle.BottomRight;
        private string _dragPrimaryItemId;
        private bool _spacePressed;
        private bool _pendingPreview;
        private bool _previewInvalidationSubscribed;
        private bool _undoRedoSubscribed;
        private Vector2 _lastPreviewResolution;

        public event Action BoardDataChanged;

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
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            generateVisualContent += OnGenerateVisualContent;
            schedule.Execute(OnScheduledRefreshTick).Every(200);
        }

        public bool IsGridEnabled => _board != null && _board.viewSettings != null && _board.viewSettings.gridEnabled;
        public bool IsSnapEnabled => _board != null && _board.viewSettings != null && _board.viewSettings.snapEnabled;

        public void SetBoard(PrefabBoardAsset board)
        {
            _board = board;
            _dirtyPreviewGuids.Clear();
            _lastPreviewResolution = Vector2.zero;
            ClearDragState();
            RebuildFromData();
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            if (!_previewInvalidationSubscribed)
            {
                PreviewCache.PreviewInvalidated += OnPreviewInvalidated;
                _previewInvalidationSubscribed = true;
            }

            if (!_undoRedoSubscribed)
            {
                Undo.undoRedoPerformed += OnUndoRedoPerformed;
                _undoRedoSubscribed = true;
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            if (_previewInvalidationSubscribed)
            {
                PreviewCache.PreviewInvalidated -= OnPreviewInvalidated;
                _previewInvalidationSubscribed = false;
            }

            if (_undoRedoSubscribed)
            {
                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                _undoRedoSubscribed = false;
            }
        }

        private void OnUndoRedoPerformed()
        {
            if (_board == null)
            {
                return;
            }

            _selectedItems.RemoveWhere(id => FindItem(id) == null);
            if (!string.IsNullOrEmpty(_selectedGroupId) && FindGroup(_selectedGroupId) == null)
            {
                _selectedGroupId = null;
            }

            ClearDragState();
            RebuildFromData();
        }

        private void OnPreviewInvalidated(string prefabGuid)
        {
            if (_board == null || string.IsNullOrEmpty(prefabGuid))
            {
                return;
            }

            if (!_board.items.Any(x => x != null && x.prefabGuid == prefabGuid))
            {
                return;
            }

            _dirtyPreviewGuids.Add(prefabGuid);
        }

        private void OnScheduledRefreshTick()
        {
            TryStartExternalDragByWindowExit();

            if (!CanRefreshNow())
            {
                return;
            }

            var currentResolution = GetPreviewResolution();
            if (_lastPreviewResolution.x > 0f &&
                (_lastPreviewResolution - currentResolution).sqrMagnitude > 0.25f)
            {
                RefreshVisualState();
                return;
            }

            if (_dirtyPreviewGuids.Count > 0)
            {
                RefreshChangedPreviews();
                _dirtyPreviewGuids.Clear();
                return;
            }

            if (_pendingPreview)
            {
                RefreshVisualState();
            }
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

            FrameRect(bounds.Value);
        }

        public void FocusItem(string itemId)
        {
            if (_board == null || string.IsNullOrEmpty(itemId))
            {
                return;
            }

            var item = FindItem(itemId);
            if (item == null)
            {
                return;
            }

            _selectedItems.Clear();
            _selectedItems.Add(item.id);
            _selectedGroupId = null;
            FrameRect(new Rect(item.position, item.size));
        }

        public void FocusGroup(string groupId)
        {
            if (_board == null || string.IsNullOrEmpty(groupId))
            {
                return;
            }

            var group = FindGroup(groupId);
            if (group == null)
            {
                return;
            }

            _selectedItems.Clear();
            _selectedGroupId = group.id;
            FrameRect(group.rect);
        }

        public void CreateGroupFromSelection()
        {
            if (_board == null) return;

            var rect = BuildGroupRect();
            var group = BoardGroupData.Create($"Group {_board.groups.Count + 1}", rect);

            BoardUndo.Record(_board, "Create Group");
            _board.groups.Add(group);
            _selectedGroupId = group.id;
            BoardUndo.MarkDirty(_board);
            RebuildFromData();
        }

        public Vector2 ScreenToWorld(Vector2 screen) => _board == null ? screen : (screen - _board.pan) / Mathf.Max(0.0001f, _board.zoom);
        public Vector2 WorldToScreen(Vector2 world) => _board == null ? world : world * _board.zoom + _board.pan;

        private void FrameRect(Rect rect)
        {
            if (_board == null)
            {
                return;
            }

            var padding = 96f;
            var fitW = Mathf.Max(1f, contentRect.width - padding);
            var fitH = Mathf.Max(1f, contentRect.height - padding);
            var zoom = Mathf.Min(fitW / Mathf.Max(1f, rect.width), fitH / Mathf.Max(1f, rect.height));
            zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);

            var center = rect.center;
            var screenCenter = new Vector2(contentRect.width * 0.5f, contentRect.height * 0.5f);

            _board.zoom = zoom;
            _board.pan = screenCenter - center * zoom;
            BoardUndo.MarkDirty(_board);
            RefreshVisualState();
        }

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
                ve.ResizePointerDown += OnGroupResizePointerDown;
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
            NotifyBoardDataChanged();
        }

        private const float DefaultMinZoom = 0.02f;
        private const float AbsoluteMinZoom = 0.005f;

        private float MinZoom =>
            _board?.viewSettings != null
                ? Mathf.Max(AbsoluteMinZoom, Mathf.Min(_board.viewSettings.minZoom, DefaultMinZoom))
                : DefaultMinZoom;
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
            var mouse = new Vector2(evt.localMousePosition.x, evt.localMousePosition.y);
            var before = ScreenToWorld(mouse);
            var zoom = Mathf.Clamp(_board.zoom * Mathf.Exp(-evt.delta.y * 0.05f), MinZoom, MaxZoom);
            if (Mathf.Approximately(zoom, _board.zoom)) return;

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
            var mouse = new Vector2(evt.localPosition.x, evt.localPosition.y);

            if (evt.button == 2 || (_spacePressed && evt.button == 0))
            {
                _mode = Mode.Panning;
                _pointerId = evt.pointerId;
                _mouseStart = mouse;
                _panStart = _board.pan;
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0) return;

            if (TryStartGroupInteraction(mouse, evt.pointerId))
            {
                evt.StopPropagation();
                return;
            }

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
            RefreshVisualState();
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_board == null || evt.pointerId != _pointerId) return;
            var mouse = new Vector2(evt.localPosition.x, evt.localPosition.y);
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
                if (IsOutsideCanvas(mouse) && TryStartExternalDragFromCurrentDrag())
                {
                    ClearDragState();
                    RefreshVisualState();
                    evt.StopPropagation();
                    return;
                }

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
                    BoardUndo.MarkDirty(_board);
                    RefreshVisualState();
                }
                evt.StopPropagation();
                return;
            }

            if (_mode == Mode.ResizeGroup)
            {
                var delta = world - _dragWorldStart;
                var group = FindGroup(_draggingGroupId);
                if (group != null)
                {
                    group.rect = ResizeGroupRect(_dragGroupRectStart, delta, _groupResizeHandle);
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
                var rect = RectFromPoints(_mouseStart, new Vector2(evt.localPosition.x, evt.localPosition.y));
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

            var modifier = evt.ctrlKey || evt.commandKey;
            if (modifier && evt.keyCode == KeyCode.Z)
            {
                if (evt.shiftKey)
                {
                    Undo.PerformRedo();
                }
                else
                {
                    Undo.PerformUndo();
                }
                evt.StopPropagation();
                return;
            }

            if (modifier && evt.keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                evt.StopPropagation();
                return;
            }

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
                var localMouse = new Vector2(evt.localMousePosition.x, evt.localMousePosition.y);
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
            var world = ScreenToWorld(new Vector2(evt.localMousePosition.x, evt.localMousePosition.y));
            var added = new List<string>();
            var offset = 0;
            var previewResolution = GetPreviewResolution();

            BoardUndo.Record(_board, "Add Prefabs");
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (!AssetGuidUtils.IsPrefabAsset(obj)) continue;
                var guid = AssetGuidUtils.GuidFromObject(obj);
                if (string.IsNullOrEmpty(guid)) continue;
                var item = BoardItemData.Create(guid, world + new Vector2(offset * 24f, offset * 24f));
                item.size = PreviewCache.ResolvePreferredBoardItemSize(guid, previewResolution);
                item.previewRenderMode = BoardItemPreviewRenderMode.Auto;
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
            _dragPrimaryItemId = card.ItemId;
            var pointerOnCard = new Vector2(evt.localPosition.x, evt.localPosition.y);
            var canvasPointer = card.ChangeCoordinatesTo(this, pointerOnCard);
            _dragWorldStart = ScreenToWorld(new Vector2(canvasPointer.x, canvasPointer.y));
            BoardUndo.Record(_board, "Move Cards");
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
            StartExternalDrag(new[] { item });
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
        }

        private void OnGroupPointerDown(GroupFrameElement groupElement, PointerDownEvent evt)
        {
            if (_board == null)
            {
                return;
            }

            Focus();
            var pointerSource = evt.currentTarget as VisualElement ?? groupElement;
            var pointerOnSource = new Vector2(evt.localPosition.x, evt.localPosition.y);
            var canvasPointer = pointerSource.ChangeCoordinatesTo(this, pointerOnSource);
            TryStartGroupInteraction(new Vector2(canvasPointer.x, canvasPointer.y), evt.pointerId);
        }

        private void OnGroupResizePointerDown(GroupFrameElement groupElement, GroupFrameElement.ResizeHandle handle, PointerDownEvent evt)
        {
            if (_board == null)
            {
                return;
            }

            Focus();
            var group = FindGroup(groupElement.GroupId);
            if (group == null)
            {
                return;
            }

            var pointerSource = evt.currentTarget as VisualElement ?? groupElement;
            var pointerOnSource = new Vector2(evt.localPosition.x, evt.localPosition.y);
            var canvasPointer = pointerSource.ChangeCoordinatesTo(this, pointerOnSource);
            var mouse = new Vector2(canvasPointer.x, canvasPointer.y);
            _selectedItems.Clear();
            _selectedGroupId = group.id;
            _draggingGroupId = group.id;
            _dragGroupRectStart = group.rect;
            _dragWorldStart = ScreenToWorld(mouse);
            _groupResizeHandle = handle;
            _mode = Mode.ResizeGroup;
            _pointerId = evt.pointerId;
            BoardUndo.Record(_board, "Resize Group");
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
            var sizeAutoUpdated = false;
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
            var previewResolution = GetPreviewResolution();
            _lastPreviewResolution = previewResolution;
            foreach (var item in _board.items)
            {
                if (item == null || !_cards.TryGetValue(item.id, out var card)) continue;

                var resolved = PreviewCache.ResolvePreferredBoardItemSize(item.prefabGuid, previewResolution);
                if ((item.size - resolved).sqrMagnitude > 0.01f)
                {
                    item.size = resolved;
                    sizeAutoUpdated = true;
                }

                var pos = WorldToScreen(item.position);
                var size = item.size * _board.zoom;
                card.style.left = pos.x;
                card.style.top = pos.y;
                card.style.width = Mathf.Max(1f, size.x);
                card.style.height = Mathf.Max(1f, size.y);

                var title = ResolveTitle(item);
                var note = string.IsNullOrWhiteSpace(item.note) ? string.Empty : TrimNote(item.note);
                var missing = !AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) || asset == null;
                var preview = PreviewCache.GetPreview(item.prefabGuid, BoardItemPreviewRenderMode.Auto, item.size, previewResolution, out var loading);
                if (loading) _pendingPreview = true;

                var highlighted = IsMatch(item, title, query);
                card.Bind(item, title, note, preview, missing, _selectedItems.Contains(item.id), highlighted);
            }

            if (sizeAutoUpdated)
            {
                BoardUndo.MarkDirty(_board);
            }

            MarkDirtyRepaint();
        }

        private void RefreshChangedPreviews()
        {
            if (_board == null || _dirtyPreviewGuids.Count == 0)
            {
                return;
            }

            var query = _search.Trim();
            var previewResolution = GetPreviewResolution();
            _lastPreviewResolution = previewResolution;
            var loading = false;
            var anyUpdated = false;

            foreach (var item in _board.items)
            {
                if (item == null || !_dirtyPreviewGuids.Contains(item.prefabGuid))
                {
                    continue;
                }

                if (!_cards.TryGetValue(item.id, out var card))
                {
                    continue;
                }

                var resolved = PreviewCache.ResolvePreferredBoardItemSize(item.prefabGuid, previewResolution);
                if ((item.size - resolved).sqrMagnitude > 0.01f)
                {
                    item.size = resolved;
                    var size = item.size * _board.zoom;
                    card.style.width = Mathf.Max(1f, size.x);
                    card.style.height = Mathf.Max(1f, size.y);
                    BoardUndo.MarkDirty(_board);
                }

                var title = ResolveTitle(item);
                var note = string.IsNullOrWhiteSpace(item.note) ? string.Empty : TrimNote(item.note);
                var missing = !AssetGuidUtils.TryLoadAssetByGuid<UnityEngine.Object>(item.prefabGuid, out var asset) || asset == null;
                var preview = PreviewCache.GetPreview(item.prefabGuid, BoardItemPreviewRenderMode.Auto, item.size, previewResolution, out var cardLoading);
                loading |= cardLoading;

                var highlighted = IsMatch(item, title, query);
                card.Bind(item, title, note, preview, missing, _selectedItems.Contains(item.id), highlighted);
                anyUpdated = true;
            }

            _pendingPreview = loading;
            if (anyUpdated)
            {
                MarkDirtyRepaint();
            }
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
                    groupId = string.Empty,
                    previewRenderMode = BoardItemPreviewRenderMode.Auto
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

        private bool TryStartGroupInteraction(Vector2 mouseScreen, int pointerId)
        {
            if (_board == null)
            {
                return false;
            }

            if (!TryHitGroup(mouseScreen, out var group, out var handle))
            {
                return false;
            }

            _selectedItems.Clear();
            _selectedGroupId = group.id;
            _draggingGroupId = group.id;
            _dragGroupRectStart = group.rect;
            _dragWorldStart = ScreenToWorld(mouseScreen);
            _pointerId = pointerId;

            if (handle.HasValue)
            {
                _groupResizeHandle = handle.Value;
                _mode = Mode.ResizeGroup;
                BoardUndo.Record(_board, "Resize Group");
            }
            else
            {
                _groupResizeHandle = GroupFrameElement.ResizeHandle.BottomRight;
                _mode = Mode.DragGroup;
                BoardUndo.Record(_board, "Move Group");
            }

            RefreshVisualState();
            return true;
        }

        private bool TryHitGroup(Vector2 mouseScreen, out BoardGroupData group, out GroupFrameElement.ResizeHandle? handle)
        {
            group = null;
            handle = null;

            if (_board == null || _board.groups == null || _board.groups.Count == 0)
            {
                return false;
            }

            var world = ScreenToWorld(mouseScreen);
            var toleranceWorld = Mathf.Max(2f, 8f / Mathf.Max(0.0001f, _board.zoom));

            for (var i = _board.groups.Count - 1; i >= 0; i--)
            {
                var candidate = _board.groups[i];
                if (candidate == null)
                {
                    continue;
                }

                var rect = candidate.rect;
                var expanded = Rect.MinMaxRect(
                    rect.xMin - toleranceWorld,
                    rect.yMin - toleranceWorld,
                    rect.xMax + toleranceWorld,
                    rect.yMax + toleranceWorld);

                if (!expanded.Contains(world))
                {
                    continue;
                }

                var hitLeft = Mathf.Abs(world.x - rect.xMin) <= toleranceWorld;
                var hitRight = Mathf.Abs(world.x - rect.xMax) <= toleranceWorld;
                var hitTop = Mathf.Abs(world.y - rect.yMin) <= toleranceWorld;
                var hitBottom = Mathf.Abs(world.y - rect.yMax) <= toleranceWorld;

                if (hitLeft && hitTop) handle = GroupFrameElement.ResizeHandle.TopLeft;
                else if (hitRight && hitTop) handle = GroupFrameElement.ResizeHandle.TopRight;
                else if (hitLeft && hitBottom) handle = GroupFrameElement.ResizeHandle.BottomLeft;
                else if (hitRight && hitBottom) handle = GroupFrameElement.ResizeHandle.BottomRight;
                else if (hitLeft) handle = GroupFrameElement.ResizeHandle.Left;
                else if (hitRight) handle = GroupFrameElement.ResizeHandle.Right;
                else if (hitTop) handle = GroupFrameElement.ResizeHandle.Top;
                else if (hitBottom) handle = GroupFrameElement.ResizeHandle.Bottom;
                else if (!rect.Contains(world))
                {
                    continue;
                }

                group = candidate;
                return true;
            }

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

        private static Rect ResizeGroupRect(Rect start, Vector2 delta, GroupFrameElement.ResizeHandle handle)
        {
            var result = start;
            switch (handle)
            {
                case GroupFrameElement.ResizeHandle.Left:
                    result.xMin = start.xMin + delta.x;
                    break;
                case GroupFrameElement.ResizeHandle.Right:
                    result.xMax = start.xMax + delta.x;
                    break;
                case GroupFrameElement.ResizeHandle.Top:
                    result.yMin = start.yMin + delta.y;
                    break;
                case GroupFrameElement.ResizeHandle.Bottom:
                    result.yMax = start.yMax + delta.y;
                    break;
                case GroupFrameElement.ResizeHandle.TopLeft:
                    result.xMin = start.xMin + delta.x;
                    result.yMin = start.yMin + delta.y;
                    break;
                case GroupFrameElement.ResizeHandle.TopRight:
                    result.xMax = start.xMax + delta.x;
                    result.yMin = start.yMin + delta.y;
                    break;
                case GroupFrameElement.ResizeHandle.BottomLeft:
                    result.xMin = start.xMin + delta.x;
                    result.yMax = start.yMax + delta.y;
                    break;
                case GroupFrameElement.ResizeHandle.BottomRight:
                    result.xMax = start.xMax + delta.x;
                    result.yMax = start.yMax + delta.y;
                    break;
            }

            if (result.width < MinGroupWidth)
            {
                if (handle == GroupFrameElement.ResizeHandle.Left ||
                    handle == GroupFrameElement.ResizeHandle.TopLeft ||
                    handle == GroupFrameElement.ResizeHandle.BottomLeft)
                {
                    result.xMin = result.xMax - MinGroupWidth;
                }
                else
                {
                    result.xMax = result.xMin + MinGroupWidth;
                }
            }

            if (result.height < MinGroupHeight)
            {
                if (handle == GroupFrameElement.ResizeHandle.Top ||
                    handle == GroupFrameElement.ResizeHandle.TopLeft ||
                    handle == GroupFrameElement.ResizeHandle.TopRight)
                {
                    result.yMin = result.yMax - MinGroupHeight;
                }
                else
                {
                    result.yMax = result.yMin + MinGroupHeight;
                }
            }

            return result;
        }

        private Vector2 Snap(Vector2 value)
        {
            if (_board?.viewSettings == null || !_board.viewSettings.snapEnabled) return value;
            var step = Mathf.Max(1f, _board.viewSettings.gridStep);
            return new Vector2(Mathf.Round(value.x / step) * step, Mathf.Round(value.y / step) * step);
        }

        private Vector2 GetPreviewResolution()
        {
            var width = Mathf.Max(64f, contentRect.width);
            var height = Mathf.Max(64f, contentRect.height);

            if (GameViewResolutionUtils.TryGetResolution(out var gameViewResolution))
            {
                width = Mathf.Max(64f, gameViewResolution.x);
                height = Mathf.Max(64f, gameViewResolution.y);
            }

            return PreviewCache.ResolvePreferredResolution(new Vector2(width, height));
        }

        private bool CanRefreshNow()
        {
            if (_board == null || panel == null)
            {
                return false;
            }

            if (resolvedStyle.display == DisplayStyle.None ||
                resolvedStyle.visibility != Visibility.Visible)
            {
                return false;
            }

            return contentRect.width > 1f && contentRect.height > 1f;
        }

        private void NotifyBoardDataChanged()
        {
            BoardDataChanged?.Invoke();
        }

        private bool HasDraggedPrefabs() => DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Any(AssetGuidUtils.IsPrefabAsset);

        private BoardItemData FindItem(string id) => _board?.items.Find(x => x != null && x.id == id);
        private BoardGroupData FindGroup(string id) => _board?.groups.Find(x => x != null && x.id == id);

        private void ClearDragState()
        {
            _mode = Mode.None;
            _pointerId = -1;
            _draggingGroupId = null;
            _groupResizeHandle = GroupFrameElement.ResizeHandle.BottomRight;
            _dragPrimaryItemId = null;
            _dragStartItemPos.Clear();
            _selectionOverlay.SetVisible(false);
            HideDragGhost();
        }

        private bool TryStartExternalDragFromCurrentDrag()
        {
            if (_board == null)
            {
                return false;
            }

            var dragItems = new List<BoardItemData>();
            foreach (var kv in _dragStartItemPos)
            {
                var item = FindItem(kv.Key);
                if (item != null)
                {
                    dragItems.Add(item);
                }
            }

            // Fallback for edge cases when drag state has primary item but drag map is empty.
            if (dragItems.Count == 0 && !string.IsNullOrEmpty(_dragPrimaryItemId))
            {
                var primary = FindItem(_dragPrimaryItemId);
                if (primary != null)
                {
                    dragItems.Add(primary);
                }
            }

            return StartExternalDrag(dragItems);
        }

        private static bool IsPrimaryMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0);
#else
            return false;
#endif
        }

        private void TryStartExternalDragByWindowExit()
        {
            if (_mode != Mode.DragItems || _pointerId < 0 || _dragStartItemPos.Count == 0)
            {
                return;
            }

            if (!IsPrimaryMouseButtonPressed())
            {
                return;
            }

            var hoveredWindow = EditorWindow.mouseOverWindow;
            if (hoveredWindow != null && hoveredWindow.GetType() == typeof(PrefabBoardWindow))
            {
                return;
            }

            if (TryStartExternalDragFromCurrentDrag())
            {
                ClearDragState();
                RefreshVisualState();
            }
        }

        private static bool StartExternalDrag(IEnumerable<BoardItemData> items)
        {
            if (items == null)
            {
                return false;
            }

            var objects = new List<UnityEngine.Object>();
            var paths = new List<string>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                if (!AssetGuidUtils.TryLoadAssetByGuid<GameObject>(item.prefabGuid, out var prefabAsset) || prefabAsset == null)
                {
                    continue;
                }

                objects.Add(prefabAsset);
                var path = AssetDatabase.GetAssetPath(prefabAsset);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            if (objects.Count == 0)
            {
                return false;
            }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = objects.ToArray();
            if (paths.Count > 0)
            {
                DragAndDrop.paths = paths.ToArray();
            }

            var label = objects.Count == 1 ? objects[0].name : $"Prefabs ({objects.Count})";
            DragAndDrop.StartDrag(label);
            return true;
        }

        private bool IsOutsideCanvas(Vector2 localMouse)
        {
            return localMouse.x < 0f ||
                   localMouse.y < 0f ||
                   localMouse.x > contentRect.width ||
                   localMouse.y > contentRect.height;
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



