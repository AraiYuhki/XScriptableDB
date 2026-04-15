using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Virtualized scroll list view for displaying SerializedProperty.
    /// </summary>
    public class VirtualizedPropertyListView
    {
        /// <summary>Height of a single row</summary>
        public float ItemHeight { get; set; } = 26f;

        /// <summary>Function to calculate additional height when expanded</summary>
        public Func<SerializedProperty, float> CalculateExpandedHeight;

        /// <summary>Currently selected index</summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>Currently expanded index</summary>
        public int ExpandedIndex { get; set; } = -1;

        /// <summary>Number of buffer rows</summary>
        public int BufferCount { get; set; } = 5;

        private SerializedProperty arrayProperty;
        private Vector2 scrollPosition;
        private float viewHeight;
        private Dictionary<int, float> cachedExpandedHeights = new();

        /// <summary>Summary drawing callback</summary>
        public Action<int, SerializedProperty, Rect> OnDrawSummary;

        /// <summary>Selection change callback</summary>
        public Action<int> OnSelectionChanged;

        /// <summary>
        /// Sets the array property.
        /// </summary>
        public void SetProperty(SerializedProperty property)
        {
            arrayProperty = property;
            cachedExpandedHeights.Clear();
        }

        /// <summary>
        /// Caches the expanded height.
        /// </summary>
        public void CacheExpandedHeight(int index, float height)
        {
            cachedExpandedHeights[index] = height;
        }

        /// <summary>
        /// Draws the list.
        /// </summary>
        public void Draw(Rect viewRect)
        {
            if (arrayProperty == null || !arrayProperty.isArray)
            {
                EditorGUI.LabelField(viewRect, "No property set");
                return;
            }

            var arraySize = arrayProperty.arraySize;
            if (arraySize == 0)
            {
                EditorGUI.LabelField(viewRect, "Empty array");
                return;
            }

            viewHeight = viewRect.height;

            // Calculate total content height
            var totalHeight = CalculateTotalHeight(arraySize);

            // Start scroll view
            var contentRect = new Rect(0, 0, viewRect.width - 16, totalHeight);

            using (var scrollScope = new GUI.ScrollViewScope(viewRect, scrollPosition, contentRect))
            {
                scrollPosition = scrollScope.scrollPosition;

                // Calculate visible range
                var (firstVisible, lastVisible) = CalculateVisibleRange(arraySize);

                // Draw only items within the visible range
                DrawVisibleItems(firstVisible, lastVisible, contentRect.width, arraySize);
            }
        }

        private float CalculateTotalHeight(int arraySize)
        {
            var height = arraySize * ItemHeight;

            if (ExpandedIndex >= 0 && ExpandedIndex < arraySize)
            {
                if (cachedExpandedHeights.TryGetValue(ExpandedIndex, out var expandedHeight))
                    height += expandedHeight;
                else if (CalculateExpandedHeight != null)
                {
                    var element = arrayProperty.GetArrayElementAtIndex(ExpandedIndex);
                    var expandedH = CalculateExpandedHeight(element);
                    cachedExpandedHeights[ExpandedIndex] = expandedH;
                    height += expandedH;
                }
            }

            return height;
        }

        private (int first, int last) CalculateVisibleRange(int arraySize)
        {
            var currentY = 0f;
            var firstVisible = 0;

            for (var i = 0; i < arraySize; i++)
            {
                var itemHeight = GetItemHeight(i);
                if (currentY + itemHeight > scrollPosition.y)
                {
                    firstVisible = Math.Max(0, i - BufferCount);
                    break;
                }
                currentY += itemHeight;
            }

            currentY = CalculateOffsetForIndex(firstVisible);
            var lastVisible = firstVisible;
            var endY = scrollPosition.y + viewHeight;

            for (var i = firstVisible; i < arraySize; i++)
            {
                if (currentY > endY + BufferCount * ItemHeight)
                    break;
                lastVisible = i;
                currentY += GetItemHeight(i);
            }

            return (firstVisible, Math.Min(lastVisible + BufferCount, arraySize - 1));
        }

        private float GetItemHeight(int index)
        {
            var height = ItemHeight;
            if (index == ExpandedIndex)
            {
                if (cachedExpandedHeights.TryGetValue(index, out var expandedHeight))
                    height += expandedHeight;
                else
                    height += 100f;
            }
            return height;
        }

        private float CalculateOffsetForIndex(int index)
        {
            var offset = index * ItemHeight;
            if (ExpandedIndex >= 0 && ExpandedIndex < index)
            {
                if (cachedExpandedHeights.TryGetValue(ExpandedIndex, out var expandedHeight))
                    offset += expandedHeight;
            }
            return offset;
        }

        private void DrawVisibleItems(int firstVisible, int lastVisible, float width, int arraySize)
        {
            var currentY = CalculateOffsetForIndex(firstVisible);

            for (var i = firstVisible; i <= lastVisible && i < arraySize; i++)
            {
                var isSelected = i == SelectedIndex;
                var isExpanded = i == ExpandedIndex;
                var element = arrayProperty.GetArrayElementAtIndex(i);

                // Header row background
                var headerRect = new Rect(0, currentY, width, ItemHeight);
                var bgColor = isSelected ? new Color(0.2f, 0.4f, 0.6f, 0.5f) : (i % 2 == 0 ? new Color(0.3f, 0.3f, 0.3f, 0.3f) : Color.clear);
                EditorGUI.DrawRect(headerRect, bgColor);

                // Index and expand button
                var indexRect = new Rect(4, currentY + 3, 40, ItemHeight - 6);
                EditorGUI.LabelField(indexRect, $"[{i}]");

                var expandButtonRect = new Rect(44, currentY + 3, 25, ItemHeight - 6);
                if (GUI.Button(expandButtonRect, isExpanded ? "v" : ">"))
                    ToggleExpand(i);

                // Click detection (excluding expand button area)
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                    && headerRect.Contains(Event.current.mousePosition)
                    && !expandButtonRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.clickCount == 2)
                    {
                        ToggleExpand(i);
                    }
                    else
                    {
                        SelectedIndex = i;
                        OnSelectionChanged?.Invoke(i);
                    }
                    Event.current.Use();
                }

                // Summary drawing
                var summaryRect = new Rect(74, currentY, width - 78, ItemHeight);
                OnDrawSummary?.Invoke(i, element, summaryRect);

                currentY += ItemHeight;

                // Draw detail if expanded
                if (isExpanded)
                {
                    float expandedHeight;
                    if (!cachedExpandedHeights.TryGetValue(i, out expandedHeight))
                    {
                        if (CalculateExpandedHeight != null)
                            expandedHeight = CalculateExpandedHeight(element);
                        else
                            expandedHeight = EditorGUI.GetPropertyHeight(element, true);
                        cachedExpandedHeights[i] = expandedHeight;
                    }

                    var expandedRect = new Rect(20, currentY, width - 24, expandedHeight);
                    EditorGUI.DrawRect(new Rect(0, currentY, width, expandedHeight), new Color(0.2f, 0.2f, 0.2f, 0.3f));
                    EditorGUI.PropertyField(expandedRect, element, GUIContent.none, true);
                    currentY += expandedHeight;
                }
            }
        }

        /// <summary>
        /// Toggles expand/collapse.
        /// </summary>
        public void ToggleExpand(int index)
        {
            if (ExpandedIndex == index)
            {
                ExpandedIndex = -1;
            }
            else
            {
                ExpandedIndex = index;
                cachedExpandedHeights.Remove(index);
            }
        }

        /// <summary>
        /// Scrolls to the specified index.
        /// </summary>
        public void ScrollToIndex(int index)
        {
            scrollPosition.y = CalculateOffsetForIndex(index);
        }

        /// <summary>
        /// Resets the scroll position.
        /// </summary>
        public void ResetScroll()
        {
            scrollPosition = Vector2.zero;
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

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void ClearCache()
        {
            cachedExpandedHeights.Clear();
        }
    }
}