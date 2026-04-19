using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// 仮想スクロールをサポートするリストビュー。
    /// 表示領域内のアイテムのみをレンダリングし、大量のアイテムを効率的に表示します。
    /// </summary>
    public class VirtualizedListView<T>
    {
        /// <summary>1行の高さ</summary>
        public float ItemHeight { get; set; } = 24f;

        /// <summary>展開時の追加の高さ</summary>
        public float ExpandedExtraHeight { get; set; } = 100f;

        /// <summary>バッファ行数（表示領域外に事前取得される行数）</summary>
        public int BufferCount { get; set; } = 5;

        /// <summary>現在選択されているインデックス</summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>現在展開されているインデックス</summary>
        public int ExpandedIndex { get; set; } = -1;

        /// <summary>アイテムの総数</summary>
        public int TotalCount => items?.Count ?? 0;

        /// <summary>最初の表示インデックス</summary>
        public int FirstVisibleIndex { get; private set; }

        /// <summary>最後の表示インデックス</summary>
        public int LastVisibleIndex { get; private set; }

        private IList<T> items;
        private Vector2 scrollPosition;
        private float viewHeight;
        private Dictionary<int, float> expandedHeights = new();

        /// <summary>アイテム描画コールバック</summary>
        public Action<int, T, Rect, bool, bool> OnDrawItem;

        /// <summary>展開されたアイテムの追加コンテンツを描画するコールバック</summary>
        public Action<int, T, Rect> OnDrawExpandedContent;

        /// <summary>選択変更コールバック</summary>
        public Action<int> OnSelectionChanged;

        /// <summary>
        /// データソースを設定します。
        /// </summary>
        /// <param name="newItems">表示するアイテムのリスト</param>
        public void SetItems(IList<T> newItems)
        {
            items = newItems;
            expandedHeights.Clear();
        }

        /// <summary>
        /// 展開時の高さを設定します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <param name="height">高さ</param>
        public void SetExpandedHeight(int index, float height)
        {
            expandedHeights[index] = height;
        }

        /// <summary>
        /// 指定されたインデックスまでスクロールします。
        /// </summary>
        /// <param name="index">スクロール先のインデックス</param>
        public void ScrollToIndex(int index)
        {
            if (items == null || index < 0 || index >= items.Count)
                return;

            var targetY = CalculateOffsetForIndex(index);
            scrollPosition.y = targetY;
        }

        /// <summary>
        /// リストを描画します。
        /// </summary>
        /// <param name="viewRect">表示領域</param>
        public void Draw(Rect viewRect)
        {
            if (items == null || items.Count == 0)
            {
                EditorGUI.LabelField(viewRect, "No items to display");
                return;
            }

            viewHeight = viewRect.height;

            // コンテンツの合計の高さを計算する
            var totalHeight = CalculateTotalHeight();

            // スクロールビューを開始する
            var contentRect = new Rect(0, 0, viewRect.width - 16, totalHeight);

            using (var scrollScope = new GUI.ScrollViewScope(viewRect, scrollPosition, contentRect))
            {
                scrollPosition = scrollScope.scrollPosition;

                // 表示範囲のインデックスを計算する
                CalculateVisibleRange();

                // 表示範囲内のアイテムのみを描画する
                DrawVisibleItems(contentRect.width);
            }
        }

        /// <summary>
        /// コンテンツの合計の高さを計算します。
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
        /// 指定されたインデックスまでのオフセットを計算します。
        /// </summary>
        private float CalculateOffsetForIndex(int index)
        {
            var offset = index * ItemHeight;

            // 展開されたアイテムがターゲットより前にある場合は、その追加の高さを加算する
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
        /// 表示範囲のインデックスを計算します。
        /// </summary>
        private void CalculateVisibleRange()
        {
            // 展開されたアイテムを考慮した位置から最初のインデックスを計算する
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

            // 最後のインデックスを計算する
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
        /// 表示範囲内のアイテムを描画します。
        /// </summary>
        private void DrawVisibleItems(float width)
        {
            var currentY = CalculateOffsetForIndex(FirstVisibleIndex);

            for (var i = FirstVisibleIndex; i <= LastVisibleIndex; i++)
            {
                var isSelected = i == SelectedIndex;
                var isExpanded = i == ExpandedIndex;
                var item = items[i];

                // アイテムのメイン行を描画する
                var itemRect = new Rect(0, currentY, width, ItemHeight);

                // クリック検出
                if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
                {
                    if (SelectedIndex != i)
                    {
                        SelectedIndex = i;
                        OnSelectionChanged?.Invoke(i);
                    }
                    Event.current.Use();
                }

                // アイテム描画コールバック
                OnDrawItem?.Invoke(i, item, itemRect, isSelected, isExpanded);

                currentY += ItemHeight;

                // 展開されている場合は追加のコンテンツを描画する
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
        /// アイテムを展開または折りたたみます。
        /// </summary>
        /// <param name="index">インデックス</param>
        public void ToggleExpand(int index)
        {
            if (ExpandedIndex == index)
                ExpandedIndex = -1;
            else
                ExpandedIndex = index;
        }

        /// <summary>
        /// スクロール位置をリセットします。
        /// </summary>
        public void ResetScroll()
        {
            scrollPosition = Vector2.zero;
            FirstVisibleIndex = 0;
            LastVisibleIndex = 0;
        }

        /// <summary>
        /// 現在のスクロール位置を取得します。
        /// </summary>
        public Vector2 GetScrollPosition() => scrollPosition;

        /// <summary>
        /// スクロール位置を設定します。
        /// </summary>
        public void SetScrollPosition(Vector2 position)
        {
            scrollPosition = position;
        }
    }
}
