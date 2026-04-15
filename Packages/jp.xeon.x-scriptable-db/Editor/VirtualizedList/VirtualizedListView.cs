using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// List view with virtual scroll support.
    /// Only renders items within the visible area to efficiently display large numbers of items.
    /// </summary>
    public class VirtualizedListView<T>
    {
        /// <summary>Height of a single row</summary>
        public float ItemHeight { get; set; } = 24f;

        /// <summary>Additional height when expanded</summary>
        public float ExpandedExtraHeight { get; set; } = 100f;

        /// <summary>Number of buffer rows (rows pre-fetched outside the visible area)</summary>
        public int BufferCount { get; set; } = 5;

        /// <summary>Currently selected index</summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>Currently expanded index</summary>
        public int ExpandedIndex { get; set; } = -1;

        /// <summary>Total number of items</summary>
        public int TotalCount => items?.Count ?? 0;

        /// <summary>First visible index</summary>
        public int FirstVisibleIndex { get; private set; }

        /// <summary>Last visible index</summary>
        public int LastVisibleIndex { get; private set; }

        private IList<T> items;
        private Vector2 scrollPosition;
        private float viewHeight;
        private Dictionary<int, float> expandedHeights = new();

        /// <summary>Item drawing callback</summary>
        public Action<int, T, Rect, bool, bool> OnDrawItem;

        /// <summary>Callback to draw additional content for an expanded item</summary>
        public Action<int, T, Rect> OnDrawExpandedContent;

        /// <summary>Selection change callback</summary>
        public Action<int> OnSelectionChanged;

        /// <summary>
        /// Sets the data source.
        /// </summary>
        /// <param name="newItems">List of items to display</param>
        public void SetItems(IList<T> newItems)
        {
            items = newItems;
            expandedHeights.Clear();
        }

        /// <summary>
        /// Sets the expanded height.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="height">Height</param>
        public void SetExpandedHeight(int index, float height)
        {
            expandedHeights[index] = height;
        }

        /// <summary>
        /// Scrolls to the specified index.
        /// </summary>
        /// <param name="index">Target index to scroll to</param>
        public void ScrollToIndex(int index)
        {
            if (items == null || index < 0 || index >= items.Count)
                return;

            var targetY = CalculateOffsetForIndex(index);
            scrollPosition.y = targetY;
        }

        /// <summary>
        /// Draws the list.
        /// </summary>
        /// <param name="viewRect">View area</param>
        public void Draw(Rect viewRect)
        {
            if (items == null || items.Count == 0)
            {
                EditorGUI.LabelField(viewRect, "No items to display");
                return;
            }

            viewHeight = viewRect.height;

            // Calculate total content height
            var totalHeight = CalculateTotalHeight();

            // Start scroll view
            var contentRect = new Rect(0, 0, viewRect.width - 16, totalHeight);

            using (var scrollScope = new GUI.ScrollViewScope(viewRect, scrollPosition, contentRect))
            {
                scrollPosition = scrollScope.scrollPosition;

                // Calculate visible range indices
                CalculateVisibleRange();

                // Draw only items within the visible range
                DrawVisibleItems(contentRect.width);
            }
        }

        /// <summary>
        /// Calculates the total content height.
        /// </summary>
        private float CalculateTotalHeight()
        {
            var height = items.Count * ItemHeight;

            if (ExpandedIndex >= 0 && ExpandedIndex < items.Count)
            {
                if (expandedHeights.TryGetValue(ExpandedIndex, out var expandedHeight))
                    height += expandedHeight;
                else
                    height += ExpandedExtraHeight;
            }

            return height;
        }

        /// <summary>
        /// Calculates the offset up to the specified index.
        /// </summary>
        private float CalculateOffsetForIndex(int index)
        {
            var offset = index * ItemHeight;

            // If an expanded item is before the target, add its extra height
            if (ExpandedIndex >= 0 && ExpandedIndex < index)
            {
                if (expandedHeights.TryGetValue(ExpandedIndex, out var expandedHeight))
                    offset += expandedHeight;
                else
                    offset += ExpandedExtraHeight;
            }

            return offset;
        }

        /// <summary>
        /// Calculates the indices of the visible range.
        /// </summary>
        private void CalculateVisibleRange()
        {
            // Calculate the first index from a position that accounts for expanded items
            var currentY = 0f;
            FirstVisibleIndex = 0;

            for (var i = 0; i < items.Count; i++)
            {
                var itemHeight = ItemHeight;
                if (i == ExpandedIndex)
                {
                    if (expandedHeights.TryGetValue(i, out var expandedHeight))
                        itemHeight += expandedHeight;
                    else
                        itemHeight += ExpandedExtraHeight;
                }

                if (currentY + itemHeight > scrollPosition.y)
                {
                    FirstVisibleIndex = Math.Max(0, i - BufferCount);
                    break;
                }

                currentY += itemHeight;
            }

            // Calculate the last index
            currentY = CalculateOffsetForIndex(FirstVisibleIndex);
            LastVisibleIndex = FirstVisibleIndex;

            var endY = scrollPosition.y + viewHeight;
            for (var i = FirstVisibleIndex; i < items.Count; i++)
            {
                if (currentY > endY + BufferCount * ItemHeight)
                    break;

                LastVisibleIndex = i;

                var itemHeight = ItemHeight;
                if (i == ExpandedIndex)
                {
                    if (expandedHeights.TryGetValue(i, out var expandedHeight))
                        itemHeight += expandedHeight;
                    else
                        itemHeight += ExpandedExtraHeight;
                }

                currentY += itemHeight;
            }

            LastVisibleIndex = Math.Min(LastVisibleIndex + BufferCount, items.Count - 1);
        }

        /// <summary>
        /// Draws items within the visible range.
        /// </summary>
        private void DrawVisibleItems(float width)
        {
            var currentY = CalculateOffsetForIndex(FirstVisibleIndex);

            for (var i = FirstVisibleIndex; i <= LastVisibleIndex; i++)
            {
                var isSelected = i == SelectedIndex;
                var isExpanded = i == ExpandedIndex;
                var item = items[i];

                // Draw the main row for the item
                var itemRect = new Rect(0, currentY, width, ItemHeight);

                // Click detection
                if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
                {
                    if (SelectedIndex != i)
                    {
                        SelectedIndex = i;
                        OnSelectionChanged?.Invoke(i);
                    }
                    Event.current.Use();
                }

                // Item drawing callback
                OnDrawItem?.Invoke(i, item, itemRect, isSelected, isExpanded);

                currentY += ItemHeight;

                // Draw additional content if expanded
                if (isExpanded)
                {
                    float extraHeight;
                    if (expandedHeights.TryGetValue(i, out var expandedHeight))
                        extraHeight = expandedHeight;
                    else
                        extraHeight = ExpandedExtraHeight;

                    var expandedRect = new Rect(0, currentY, width, extraHeight);
                    OnDrawExpandedContent?.Invoke(i, item, expandedRect);
                    currentY += extraHeight;
                }
            }
        }

        /// <summary>
        /// Expands or collapses an item.
        /// </summary>
        /// <param name="index">Index</param>
        public void ToggleExpand(int index)
        {
            if (ExpandedIndex == index)
                ExpandedIndex = -1;
            else
                ExpandedIndex = index;
        }

        /// <summary>
        /// Resets the scroll position.
        /// </summary>
        public void ResetScroll()
        {
            scrollPosition = Vector2.zero;
            FirstVisibleIndex = 0;
            LastVisibleIndex = 0;
        }

        /// <summary>
        /// Gets the current scroll position.
        /// </summary>
        public Vector2 GetScrollPosition() => scrollPosition;

        /// <summary>
        /// Sets the scroll position.
        /// </summary>
        public void SetScrollPosition(Vector2 position)
        {
            scrollPosition = position;
        }
    }
}
