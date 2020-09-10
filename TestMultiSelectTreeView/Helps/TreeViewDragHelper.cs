using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using TestMultiSelectTreeView.Adorners;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Collections;
using System.Windows.Media;

namespace TestMultiSelectTreeView.Helps
{
    /// <typeparam name="T">TreeView or MultiSelectTreeView</typeparam>
    /// <typeparam name="U">TreeViewItem or MultiSelectTreeViewItem</typeparam>
    public class TreeViewDragHelper<TContainer, TItem>
        where TContainer : ItemsControl
        where TItem : HeaderedItemsControl
    {
        //TreeView
        private TContainer _treeView = null;

        //TreeViewItem
        private TItem _draggingContainer = null;

        private AdornerLayer _adornerLayer = null;
        private MousePanelAdorner _panelAdorner = null;

        //Mouse Down
        private Point _cacheMouseDownPosToChild;
        private Point? _mouseDownPos = null;
        private Rect? _lastDraggingRect = null;

        public double LeaveGroupMinOffset { get; set; }

        //1. TItem : the parent of draggingItem
        public Predicate<ItemsControl> AllowLeave { get; set; }

        //2. TItem : overlapItem
        public Predicate<TItem> IsValidOverlapItem { get; set; }

        //3. TItem : draggingItem, TItem : overlapItem
        public Func<TItem, TItem, bool> IsValidOverlapGroup { get; set; }

        //4. 
        public bool AutoRemoveEmptyGroup { get; set; }

        //5. Source Item, Source ItemsControl, Target ItemsControl
        public Action<object, ItemsControl, ItemsControl> UpdateGroup { get; set; }

        public Action<TItem> Successed { get; set; }

        public TreeViewDragHelper(TContainer treeView, bool manualHandleMouseLeftButtonDownUp)
        {
            _treeView = treeView;

            if (!manualHandleMouseLeftButtonDownUp)
            {
                _treeView.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                _treeView.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;

                _treeView.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
                _treeView.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            }
        }

        #region Public

        public void ManualHandleMouseLeftButtonDown(TItem treeViewItem, MouseButtonEventArgs e)
        {
            if (!_treeView.HasItems || e.OriginalSource.GetType().Name == "TextBoxView")
                return;

            _mouseDownPos = e.GetPosition(_treeView);
            Init(treeViewItem, e);
        }

        public void ManualHandleMouseLeftButtonUp()
        {
            _treeView.PreviewMouseMove -= OnMouseMove;
        }

        #endregion

        #region Event

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_treeView.HasItems || e.OriginalSource.GetType().Name == "TextBoxView")
                return;

            _mouseDownPos = e.GetPosition(_treeView);

            var result = VisualTreeHelper.HitTest(_treeView, _mouseDownPos.Value);
            if (result == null)
                return;

            var treeViewItem = Utils.FindVisualParent<TItem>(result.VisualHit);
            if (treeViewItem == null)
                return;

            Init(treeViewItem, e);
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ManualHandleMouseLeftButtonUp();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var curPos = e.GetPosition(_treeView);

            if (DoubleUtil.GreaterThanOrClose(Math.Abs(_mouseDownPos.Value.Y - curPos.Y), SystemParameters.MinimumVerticalDragDistance)
                || DoubleUtil.GreaterThanOrClose(Math.Abs(_mouseDownPos.Value.X - curPos.X), SystemParameters.MinimumHorizontalDragDistance))
            {
                Start();
            }
        }

        //Dragging
        private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            _panelAdorner.Update();
            Move();
        }

        #endregion

        #region Private

        private void Init(TItem treeViewItem, MouseButtonEventArgs e)
        {
            _cacheMouseDownPosToChild = e.GetPosition(treeViewItem);
            _draggingContainer = treeViewItem;

            _treeView.PreviewMouseMove -= OnMouseMove;
            _treeView.PreviewMouseMove += OnMouseMove;
        }

        private MousePanelAdorner ConstructMousePanelAdorner(UIElement panel, UIElement draggingContainer)
        {
            if (panel == null || draggingContainer == null)
                return null;

            return new MousePanelAdorner(panel, draggingContainer as FrameworkElement, Mouse.GetPosition(draggingContainer));
        }

        private MousePanelAdorner GetPanelAdorner(UIElement panel, FrameworkElement draggingContainer)
        {
            return ConstructMousePanelAdorner(panel, draggingContainer);
        }

        private bool IsMoveUp(Point dragedItemOriginalPos, Rect dragedRect)
        {
            return DoubleUtil.LessThan(dragedRect.Y, dragedItemOriginalPos.Y);
        }

        private bool IsGroup(ItemsControl parentItemsControl)
        {
            return parentItemsControl != null && parentItemsControl is TItem;
        }

        private Size GetOverlapSize(Rect rect1, Rect rect2)
        {
            return Rect.Intersect(rect1, rect2).Size;
        }

        private DependencyObject ContainerFromIndex(ItemsControl parentItemsControl, int index)
        {
            var container = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(index);
            if (container == null)
            {
                parentItemsControl.UpdateLayout();
                container = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(index);
            }

            return container;
        }

        private void Start()
        {
            if (_panelAdorner != null || _draggingContainer == null)
                return;

            if (_adornerLayer == null)
                _adornerLayer = AdornerLayer.GetAdornerLayer(_treeView);

            if (_panelAdorner == null)
            {
                _panelAdorner = GetPanelAdorner(_treeView, _draggingContainer);
                _adornerLayer.Add(_panelAdorner);

                _draggingContainer.Opacity = 0.2;
            }

            DragDrop.AddQueryContinueDragHandler(_treeView, OnQueryContinueDrag);
            DragDrop.DoDragDrop(_treeView, _draggingContainer.DataContext, DragDropEffects.Move);
            DragDrop.RemoveQueryContinueDragHandler(_treeView, OnQueryContinueDrag);

            End();
        }

        private void End()
        {
            _treeView.PreviewMouseMove -= OnMouseMove;

            _adornerLayer.Remove(_panelAdorner);
            _panelAdorner = null;

            _draggingContainer.Opacity = 1;
            _draggingContainer = null;
            _lastDraggingRect = null;
        }

        private void Move()
        {
            if ((DoubleUtil.IsZero(_draggingContainer.ActualWidth) || DoubleUtil.IsZero(_draggingContainer.ActualHeight))
                && _draggingContainer.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            var screenPos = new Win32.POINT();
            if (!Win32.GetCursorPos(ref screenPos))
                return;

            var posToPanel = _treeView.PointFromScreen(new Point(screenPos.X, screenPos.Y));
            var draggingRect = new Rect(posToPanel.X - _cacheMouseDownPosToChild.X, posToPanel.Y - _cacheMouseDownPosToChild.Y, _draggingContainer.ActualWidth, _draggingContainer.ActualHeight);

            if (DoubleUtil.GreaterThan(draggingRect.X, _treeView.ActualWidth)
                || DoubleUtil.LessThan(draggingRect.Right, 0)
                || DoubleUtil.LessThan(draggingRect.Bottom, 0)
                || DoubleUtil.GreaterThan(draggingRect.Y, _treeView.ActualHeight))
                return;

            if (_lastDraggingRect == null)
            {
                _lastDraggingRect = draggingRect;
                return;
            }

            var areClose = DoubleUtil.AreClose(draggingRect.Y, _lastDraggingRect.Value.Y);
            if (areClose)
                return;

            var isMouseUp = DoubleUtil.LessThan(draggingRect.Y, _lastDraggingRect.Value.Y);
            _lastDraggingRect = draggingRect;

            var sourcePos = _draggingContainer.TranslatePoint(new Point(), _treeView);
            var isSuccess = false;

            if (isMouseUp)
            {
                isSuccess = MoveUp(_treeView, draggingRect, ref _draggingContainer);
            }
            else
            {
                isSuccess = MoveDown(_treeView, draggingRect, ref _draggingContainer);
            }

            if (isSuccess && _draggingContainer != null)
            {
                _draggingContainer.Focus();
                _draggingContainer.Opacity = 0.2;

                if (Successed != null)
                    Successed(_draggingContainer);
            }
        }

        private bool MoveUp(TContainer rootTreeView, Rect draggingRect, ref TItem draggingContainer)
        {
            var overlapSize = new Size();
            var overlapItemRect = new Rect();
            TItem overlapContainer = null;

            var sourceItemsControl = ItemsControl.ItemsControlFromItemContainer(draggingContainer);
            if (sourceItemsControl == null)
                return false;

            var sourceIndex = sourceItemsControl.ItemContainerGenerator.IndexFromContainer(draggingContainer);
            var sourceCollection = sourceItemsControl.ItemsSource as IList;
            var sourceItem = sourceCollection[sourceIndex];

            var sourcePos = draggingContainer.TranslatePoint(new Point(), rootTreeView);

            //从组中分离
            if (IsGroup(sourceItemsControl) && sourceIndex == 0 && DoubleUtil.LessThan(draggingRect.Bottom, sourcePos.Y - LeaveGroupMinOffset))
            {
                if (AllowLeave != null && !AllowLeave(sourceItemsControl))
                    return false;

                //加入上层集合
                var superItemsControl = ItemsControl.ItemsControlFromItemContainer(sourceItemsControl);
                var superIndex = superItemsControl.ItemContainerGenerator.IndexFromContainer(sourceItemsControl);
                var superCollection = superItemsControl.ItemsSource as IList;

                sourceCollection.RemoveAt(sourceIndex);
                superCollection.Insert(superIndex, sourceItem);

                if (sourceItemsControl.Items.Count == 0 && AutoRemoveEmptyGroup)
                    superCollection.RemoveAt(superIndex + 1);

                if (UpdateGroup != null)
                    UpdateGroup(sourceItem, sourceItemsControl, superItemsControl);

                draggingContainer = ContainerFromIndex(superItemsControl, superIndex) as TItem;

                return true;
            }

            if (!FindFirstUpItemWithValidOverlap(rootTreeView, sourceItemsControl, sourceIndex - 1, draggingContainer, draggingRect, ref overlapSize, ref overlapItemRect, ref overlapContainer))
                return false;

            if (IsValidOverlapItem != null && !IsValidOverlapItem(overlapContainer))
                return false;

            var overlapContainerItemsControl = ItemsControl.ItemsControlFromItemContainer(overlapContainer);
            if (sourceItemsControl != overlapContainerItemsControl)
                return false;

            //Group with specified conditions from IsValidOverlapGroup(...)
            if (IsValidOverlapGroup != null && IsValidOverlapGroup(draggingContainer, overlapContainer))
            {
                if (DoubleUtil.LessThan(draggingRect.Bottom, sourcePos.Y + draggingContainer.ActualHeight / 2))
                {
                    var overlapCollection = overlapContainer.ItemsSource as IList;

                    sourceCollection.RemoveAt(sourceIndex);
                    overlapCollection.Add(sourceItem);

                    if (UpdateGroup != null)
                        UpdateGroup(sourceItem, sourceItemsControl, overlapContainer);

                    draggingContainer = ContainerFromIndex(overlapContainer, overlapCollection.Count - 1) as TItem;

                    return true;
                }
            }

            if (DoubleUtil.LessThan(draggingRect.Bottom, overlapItemRect.Y + overlapItemRect.Height * 3 / 4)) //Top -> 3/4 height 移动
            {
                var targetIndex = sourceItemsControl.ItemContainerGenerator.IndexFromContainer(overlapContainer);

                sourceCollection.RemoveAt(sourceIndex);
                sourceCollection.Insert(targetIndex, sourceItem);

                draggingContainer = ContainerFromIndex(sourceItemsControl, targetIndex) as TItem;

                return true;
            }

            return false;
        }

        private bool MoveDown(TContainer rootTreeView, Rect draggingRect, ref TItem draggingContainer)
        {
            var overlapSize = new Size();
            var overlapItemRect = new Rect();
            TItem overlapContainer = null;

            var sourceItemsControl = ItemsControl.ItemsControlFromItemContainer(draggingContainer);
            if (sourceItemsControl == null)
                return false;

            var sourceCollection = sourceItemsControl.ItemsSource as IList;
            var sourceIndex = sourceItemsControl.ItemContainerGenerator.IndexFromContainer(draggingContainer);
            var sourceItem = sourceCollection[sourceIndex];

            var sourcePos = draggingContainer.TranslatePoint(new Point(), rootTreeView);

            //从组中分离 
            if (IsGroup(sourceItemsControl) && sourceIndex == sourceItemsControl.Items.Count - 1)
            {
                if (AllowLeave != null && !AllowLeave(sourceItemsControl))
                    return false;

                var parentIsGroupAndLast = false;

                var parentItemsControl = ItemsControl.ItemsControlFromItemContainer(sourceItemsControl);
                if (parentItemsControl != null && IsGroup(parentItemsControl))
                {
                    var i = parentItemsControl.ItemContainerGenerator.IndexFromContainer(sourceItemsControl);
                    parentIsGroupAndLast = i == parentItemsControl.Items.Count - 1;
                }

                if (!parentIsGroupAndLast && DoubleUtil.GreaterThan(draggingRect.Y, sourcePos.Y + draggingContainer.ActualHeight * 3 / 4)
                    || parentIsGroupAndLast && DoubleUtil.GreaterThan(draggingRect.Y, sourcePos.Y + draggingContainer.ActualHeight / 2))
                {
                    //加入上层集合
                    var superItemsControl = ItemsControl.ItemsControlFromItemContainer(sourceItemsControl);
                    var superCollection = superItemsControl.ItemsSource as IList;
                    var superIndex = superItemsControl.ItemContainerGenerator.IndexFromContainer(sourceItemsControl);

                    sourceCollection.RemoveAt(sourceIndex);

                    if (superIndex + 1 < superItemsControl.Items.Count)
                    {
                        superCollection.Insert(superIndex + 1, sourceItem);
                        draggingContainer = ContainerFromIndex(superItemsControl, superIndex + 1) as TItem;
                    }
                    else
                    {
                        superCollection.Add(sourceItem);
                        draggingContainer = ContainerFromIndex(superItemsControl, superCollection.Count - 1) as TItem;
                    }

                    if (sourceItemsControl.Items.Count == 0 && AutoRemoveEmptyGroup)
                        superCollection.RemoveAt(superIndex);

                    if (UpdateGroup != null)
                        UpdateGroup(sourceItem, sourceItemsControl, superItemsControl);

                    return true;
                }
            }

            if (!FindFirstDownItemWithValidOverlap(rootTreeView, sourceItemsControl, sourceIndex + 1, draggingContainer, draggingRect, ref overlapSize, ref overlapItemRect, ref overlapContainer))
                return false;

            if (IsValidOverlapItem != null && !IsValidOverlapItem(overlapContainer))
                return false;

            var overlapContainerItemsControl = ItemsControl.ItemsControlFromItemContainer(overlapContainer);
            if (sourceItemsControl != overlapContainerItemsControl)
                return false;

            //Group with specified conditions from IsValidOverlapGroup(...)
            if (IsValidOverlapGroup != null && IsValidOverlapGroup(draggingContainer, overlapContainer))
            {
                var overlapCollection = overlapContainer.ItemsSource as IList;

                sourceCollection.RemoveAt(sourceIndex);
                overlapCollection.Insert(0, sourceItem);

                if (UpdateGroup != null)
                    UpdateGroup(sourceItem, sourceItemsControl, overlapContainer);

                draggingContainer = ContainerFromIndex(overlapContainer, 0) as TItem;

                return true;
            }

            if (DoubleUtil.GreaterThan(draggingRect.Y, overlapItemRect.Y + overlapItemRect.Height / 4)) //Top -> 1/4 height 移动
            {
                var targetIndex = sourceItemsControl.ItemContainerGenerator.IndexFromContainer(overlapContainer);

                sourceCollection.RemoveAt(sourceIndex);
                sourceCollection.Insert(targetIndex, sourceItem);

                draggingContainer = ContainerFromIndex(sourceItemsControl, targetIndex) as TItem;

                return true;
            }

            return false;
        }

        private bool FindFirstUpItemWithValidOverlap(TContainer rootTreeView, ItemsControl parentItemsControl, int startIndex, TItem draggingContainer, Rect draggingRect, ref Size overlapSize, ref Rect overlapContainerRect, ref TItem overlapContainer)
        {
            while (startIndex > -1)
            {
                var upTreeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(startIndex--) as TItem;
                if (upTreeViewItem == null)
                    continue;

                if (CalculateValidOverlap(rootTreeView, upTreeViewItem, draggingRect, ref overlapSize, ref overlapContainerRect, ref overlapContainer))
                    return true;
            };

            var superIC = ItemsControl.ItemsControlFromItemContainer(parentItemsControl);
            if (superIC == null)
                return false;

            var topIndex = superIC.ItemContainerGenerator.IndexFromContainer(parentItemsControl);
            return FindFirstUpItemWithValidOverlap(rootTreeView, superIC, topIndex - 1, draggingContainer, draggingRect, ref overlapSize, ref  overlapContainerRect, ref overlapContainer);
        }

        private bool FindFirstDownItemWithValidOverlap(TContainer rootTreeView, ItemsControl parentItemsControl, int startIndex, TItem draggingContainer, Rect draggingRect, ref Size overlapSize, ref Rect overlapContainerRect, ref TItem overlapContainer)
        {
            while (startIndex < parentItemsControl.Items.Count)
            {
                var downTreeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(startIndex++) as TItem;
                if (downTreeViewItem == null)
                    continue;

                if (CalculateValidOverlap(rootTreeView, downTreeViewItem, draggingRect, ref overlapSize, ref overlapContainerRect, ref overlapContainer))
                    return true;
            };

            var superIC = ItemsControl.ItemsControlFromItemContainer(parentItemsControl);
            if (superIC == null)
                return false;

            var topIndex = superIC.ItemContainerGenerator.IndexFromContainer(parentItemsControl);
            return FindFirstDownItemWithValidOverlap(rootTreeView, superIC, topIndex + 1, draggingContainer, draggingRect, ref overlapSize, ref  overlapContainerRect, ref overlapContainer);
        }

        private bool CalculateValidOverlap(TContainer rootTreeView, TItem treeViewItem, Rect draggingRect, ref Size overlapSize, ref Rect overlapContainerRect, ref TItem overlapContainer)
        {
            var itemPos = treeViewItem.TranslatePoint(new Point(), rootTreeView);
            var itemOverlapSize = GetOverlapSize(new Rect(itemPos, new Point(itemPos.X + treeViewItem.ActualWidth, itemPos.Y + treeViewItem.ActualHeight)), draggingRect);

            if (itemOverlapSize.IsEmpty || DoubleUtil.IsZero(itemOverlapSize.Width) || DoubleUtil.IsZero(itemOverlapSize.Height))
                return false;

            overlapSize = itemOverlapSize;
            overlapContainerRect = new Rect(itemPos.X, itemPos.Y, treeViewItem.ActualWidth, treeViewItem.ActualHeight);
            overlapContainer = treeViewItem;

            return true;
        }

        #endregion
    }
}
