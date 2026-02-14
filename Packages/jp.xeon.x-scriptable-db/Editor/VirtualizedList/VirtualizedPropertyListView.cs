using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SerializedPropertyを表示するための仮想スクロールリストビュー。
    /// </summary>
    public class VirtualizedPropertyListView
    {
        /// <summary>1行の高さ</summary>
        public float ItemHeight { get; set; } = 26f;

        /// <summary>展開時の追加高さを計算する関数</summary>
        public Func<SerializedProperty, float> CalculateExpandedHeight;

        /// <summary>選択中のインデックス</summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>展開中のインデックス</summary>
        public int ExpandedIndex { get; set; } = -1;

        /// <summary>バッファ行数</summary>
        public int BufferCount { get; set; } = 5;

        private SerializedProperty arrayProperty;
        private Vector2 scrollPosition;
        private float viewHeight;
        private Dictionary<int, float> cachedExpandedHeights = new();

        /// <summary>サマリー描画コールバック</summary>
        public Action<int, SerializedProperty, Rect> OnDrawSummary;

        /// <summary>選択変更コールバック</summary>
        public Action<int> OnSelectionChanged;

        /// <summary>
        /// 配列プロパティを設定する。
        /// </summary>
        public void SetProperty(SerializedProperty property)
        {
            arrayProperty = property;
            cachedExpandedHeights.Clear();
        }

        /// <summary>
        /// 展開高さをキャッシュする。
        /// </summary>
        public void CacheExpandedHeight(int index, float height)
        {
            cachedExpandedHeights[index] = height;
        }

        /// <summary>
        /// リストを描画する。
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

            // 総コンテンツ高さを計算
            var totalHeight = CalculateTotalHeight(arraySize);

            // スクロールビュー開始
            var contentRect = new Rect(0, 0, viewRect.width - 16, totalHeight);

            using (var scrollScope = new GUI.ScrollViewScope(viewRect, scrollPosition, contentRect))
            {
                scrollPosition = scrollScope.scrollPosition;

                // 表示範囲を計算
                var (firstVisible, lastVisible) = CalculateVisibleRange(arraySize);

                // 表示範囲のアイテムのみ描画
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

                // ヘッダー行の背景
                var headerRect = new Rect(0, currentY, width, ItemHeight);
                var bgColor = isSelected ? new Color(0.2f, 0.4f, 0.6f, 0.5f) : (i % 2 == 0 ? new Color(0.3f, 0.3f, 0.3f, 0.3f) : Color.clear);
                EditorGUI.DrawRect(headerRect, bgColor);

                // インデックスと展開ボタン
                var indexRect = new Rect(4, currentY + 3, 40, ItemHeight - 6);
                EditorGUI.LabelField(indexRect, $"[{i}]");

                var expandButtonRect = new Rect(44, currentY + 3, 25, ItemHeight - 6);
                if (GUI.Button(expandButtonRect, isExpanded ? "v" : ">"))
                    ToggleExpand(i);

                // クリック判定（展開ボタン領域を除外）
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

                // サマリー描画
                var summaryRect = new Rect(74, currentY, width - 78, ItemHeight);
                OnDrawSummary?.Invoke(i, element, summaryRect);

                currentY += ItemHeight;

                // 展開されている場合は詳細を描画
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
        /// 展開/折りたたみを切り替える。
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
        /// 指定インデックスまでスクロールする。
        /// </summary>
        public void ScrollToIndex(int index)
        {
            scrollPosition.y = CalculateOffsetForIndex(index);
        }

        /// <summary>
        /// スクロール位置をリセットする。
        /// </summary>
        public void ResetScroll()
        {
            scrollPosition = Vector2.zero;
        }

        /// <summary>
        /// 現在のスクロール位置を取得する。
        /// </summary>
        public Vector2 GetScrollPosition() => scrollPosition;

        /// <summary>
        /// スクロール位置を設定する。
        /// </summary>
        public void SetScrollPosition(Vector2 position)
        {
            scrollPosition = position;
        }

        /// <summary>
        /// キャッシュをクリアする。
        /// </summary>
        public void ClearCache()
        {
            cachedExpandedHeights.Clear();
        }
    }
}