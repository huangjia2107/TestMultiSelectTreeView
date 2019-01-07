using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using TestMultiSelectTreeView.Controls;
using TestMultiSelectTreeView.Helps;
using TestMultiSelectTreeView.Adorners;
using System.Windows.Controls.Primitives;

namespace TestMultiSelectTreeView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TestSource _testSource { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _testSource = new TestSource
            {
                ModelCollection = new ObservableCollection<TestModel> 
                {
                    new TestModel { Name = "AAA" },
                    new TestModel 
                    { 
                        Name = "BBB", 
                        IsGroup = true,
                        ModelCollection = new ObservableCollection<TestModel>
                        {
                            new TestModel { Name = "Group_BBB_111" },
                            new TestModel { Name = "Group_BBB_222" },
                            new TestModel { Name = "Group_BBB_333" },
                        }
                    },
                    new TestModel { Name = "CCC" },
                    new TestModel { Name = "DDD" },
                    new TestModel { 
                        Name = "EEE", 
                        IsGroup = true,
                        ModelCollection = new ObservableCollection<TestModel>
                        {
                            new TestModel { Name = "Group_EEE_111" },
                            new TestModel { Name = "Group_EEE_222" },
                            new TestModel { Name = "Group_EEE_333" },
                        }
                    }
                }
            };

            this.DataContext = _testSource;
        }

        int j = 0;
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            _testSource.ModelCollection[0].ModelCollection.Add(new TestModel { Name = "Test Add " + j, IsSelected = true });
            j++;

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
        }

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_testSource.ModelCollection[0].ModelCollection.Count > 0)
            {
                _testSource.ModelCollection[0].ModelCollection.RemoveAt(0);
                _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
            }
        }

        private void Move_Button_Click(object sender, RoutedEventArgs e)
        {
            _testSource.ModelCollection[1].ModelCollection.Move(0, 2);

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
        }

        int i = 0;
        private void Replace_Button_Click(object sender, RoutedEventArgs e)
        {
            _testSource.ModelCollection[1].ModelCollection[0] = new TestModel { Name = "Test Replace " + i, IsSelected = true };
            i++;

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
        }

        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DataContext = null;

            this.DataContext = _testSource;
        }

        private void ShowSelectedItems()
        {
            if (treeView.SelectedItems != null)
            {
                string s = string.Empty;
                foreach (var d in treeView.SelectedItems)
                {
                    s += (d as TestModel).Name + " | ";
                }

                if (!string.IsNullOrEmpty(s))
                    System.Diagnostics.Trace.TraceInformation("SelectedItems = {0}", s.TrimEnd(" | ".ToCharArray()));
                else
                    System.Diagnostics.Trace.TraceInformation("SelectedItems is empty");
            }
        }

        private void treeView_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            System.Diagnostics.Trace.TraceInformation(" [ Selection ] Item = {0}, Value = {1}", e.NewValue == null ? "null" : (e.NewValue as TestModel).Name, treeView.SelectedValue);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (treeView.ItemContainerGenerator.ContainerFromIndex(treeView.Items.Count - 1) as FrameworkElement).BringIntoView();

            ShowSelectedItems();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _adornerLayer = AdornerLayer.GetAdornerLayer(treeView);
            this.AddHandler(MultiSelectTreeView.MouseLeftButtonDownEvent, new MouseButtonEventHandler(MouseLeftButtonDownEventHandler), true);
        }

        private Point _cacheMouseDownPosToChild;
        private MultiSelectTreeViewItem _draggingContainer = null;

        private MultiSelectTreeViewItem _lastOverlapContainer = null;
        private DateTime _startOverlapTime = DateTime.MinValue;

        private AdornerLayer _adornerLayer = null;
        private MousePanelAdorner _panelAdorner = null;

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

        private bool SelectAllItems(ItemsControl itemsControl)
        {
            if (itemsControl == null || !itemsControl.HasItems)
                return false;

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem);
                if (container != null)
                    container.IsSelected = true;
            }

            return true;
        }

        #region Group

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var element = (sender as FrameworkElement);
            foreach (var item in element.ContextMenu.Items)
            {
                if (item is MenuItem)
                {
                    var menu = item as MenuItem;
                    switch (menu.Name)
                    {
                        case "GroupMenuItem":
                            {
                                menu.IsEnabled = CanGroup();
                                break;
                            }
                        case "UnGroupMenuItem":
                            {
                                menu.IsEnabled = CanUnGroup();
                                break;
                            }

                    }
                }
            }
        }

        private void GroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GroupFromItems();
        }

        private void UnGroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UnGroupToItems();
        }

        private bool CanGroup()
        {
            if (treeView.SelectedItems == null || treeView.SelectedItems.Count < 2)
                return false;

            foreach (TestModel model in treeView.SelectedItems)
            {
                if (model.IsGroup || !_testSource.ModelCollection.Contains(model))
                    return false;
            }

            return true;
        }

        private void GroupFromItems()
        {
            if (!CanGroup())
                return;

            var selectedItems = treeView.SelectedItems;

            var group = new TestModel { Name = "New Group", IsGroup = true };

            int insertIndex = -1;
            for (int i = 0; i < _testSource.ModelCollection.Count; i++)
            {
                var model = _testSource.ModelCollection[i];
                if (model.IsGroup)
                    continue;

                if (selectedItems.Contains(model))
                {
                    if (insertIndex < 0)
                        insertIndex = i;

                    _testSource.ModelCollection.RemoveAt(i);

                    model.IsSelected = true;
                    group.ModelCollection.Add(model);
                    i--;
                }
            }

            if (insertIndex >= _testSource.ModelCollection.Count)
                _testSource.ModelCollection.Add(group);
            else
                _testSource.ModelCollection.Insert(insertIndex, group);
        }

        private bool CanUnGroup()
        {
            if (treeView.SelectedItems == null || treeView.SelectedItems.Count == 0)
                return false;

            foreach (TestModel model in treeView.SelectedItems)
            {
                if (model.IsGroup || _testSource.ModelCollection.Contains(model))
                    return false;
            }

            return true;
        }

        private void UnGroupToItems()
        {
            if (!CanUnGroup())
                return;

            var selectedItems = treeView.SelectedItems;

            for (int i = 0; i < _testSource.ModelCollection.Count; i++)
            {
                var model = _testSource.ModelCollection[i];
                if (!model.IsGroup || model.ModelCollection.Count == 0)
                    continue;

                int newCount = 0;

                for (int j = model.ModelCollection.Count - 1; j >= 0; j--)
                {
                    var childModel = model.ModelCollection[j];
                    if (!selectedItems.Contains(childModel))
                        continue;

                    model.ModelCollection.RemoveAt(j);

                    childModel.IsSelected = true;
                    if (i + 1 < _testSource.ModelCollection.Count)
                        _testSource.ModelCollection.Insert(i + 1, childModel);
                    else
                        _testSource.ModelCollection.Add(childModel);

                    newCount++;
                }

                if (model.ModelCollection.Count < 2)
                {
                    if (model.ModelCollection.Count == 1)
                    {
                        var childModel = model.ModelCollection[0];

                        childModel.IsSelected = true;
                        _testSource.ModelCollection.Insert(i + 1, childModel);
                        model.ModelCollection.Clear();

                        newCount++;
                    }

                    newCount--;
                    _testSource.ModelCollection.RemoveAt(i);
                }

                i += newCount;
            }

            _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
        }

        #endregion
        private void MouseLeftButtonDownEventHandler(object sender, MouseButtonEventArgs e)
        {
            if (!treeView.HasItems)
                return;

            var result = VisualTreeHelper.HitTest(treeView, e.GetPosition(treeView));
            if (result == null)
                return;

            var treeViewItem = Utils.FindVisualParent<MultiSelectTreeViewItem>(result.VisualHit);
            if (treeViewItem == null)
                return;

            _cacheMouseDownPosToChild = e.GetPosition(treeViewItem);
            _draggingContainer = treeViewItem;

            treeView.PreviewMouseMove += OnMouseMove;

        }

        //Mouse Move
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            StartDrag();
        }

        //Start Darg
        private void StartDrag()
        {
            if (_panelAdorner != null || _draggingContainer == null)
                return;

            if (_panelAdorner == null)
            {
                _draggingContainer.Tag = 1;
                _panelAdorner = GetPanelAdorner(treeView, _draggingContainer);
                _draggingContainer.Tag = 0;
            }

            _adornerLayer.Add(_panelAdorner);
            _draggingContainer.Opacity = 0.2;

            DragDrop.AddQueryContinueDragHandler(treeView, OnQueryContinueDrag);
            DragDrop.DoDragDrop(treeView, _draggingContainer, DragDropEffects.Move);
            DragDrop.RemoveQueryContinueDragHandler(treeView, OnQueryContinueDrag);

            EndDrag();
        }

        //Dragging
        private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            _panelAdorner.Update();

            try { Move(); }
            catch (Exception ex)
            {
                Trace.TraceError(ex.StackTrace);
            }

        }

        //End Drag
        private void EndDrag()
        {
            treeView.PreviewMouseMove -= OnMouseMove;

            _adornerLayer.Remove(_panelAdorner);
            _panelAdorner = null;

            _draggingContainer.Opacity = 1;
            _draggingContainer = null;

            _lastOverlapContainer = null;
            _startOverlapTime = DateTime.MinValue;
        }

        /// <returns>true:up  false:down</returns>
        private bool MoveDirection(Point dragedItemOriginalPos, Rect dragedRect)
        {
            return DoubleUtil.LessThan(dragedRect.Y, dragedItemOriginalPos.Y);
        }

        private bool FindFirstUpItemWithValidOverlap(MultiSelectTreeView rootTreeView, ItemsControl parentItemsControl, int startIndex, Rect draggingRect, ref Size overlapSize, ref Rect overlapContainerRect, ref MultiSelectTreeViewItem overlapContainer)
        {
            while (startIndex > -1)
            {
                var upTreeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(startIndex--) as MultiSelectTreeViewItem;
                if (upTreeViewItem == null)
                    continue;

                if (upTreeViewItem.HasItems)
                {
                    if (FindFirstUpItemWithValidOverlap(rootTreeView, upTreeViewItem, upTreeViewItem.Items.Count - 1, draggingRect, ref overlapSize, ref  overlapContainerRect, ref overlapContainer))
                        return true;
                }
                else
                {
                    var upItemPos = upTreeViewItem.TranslatePoint(new Point(), rootTreeView);
                    var upItemOverlapSize = GetOverlapSize(new Rect(upItemPos, new Point(upItemPos.X + upTreeViewItem.ActualWidth, upItemPos.Y + upTreeViewItem.ActualHeight)), draggingRect);

                    if (upItemOverlapSize.IsEmpty || DoubleUtil.IsZero(upItemOverlapSize.Width) || DoubleUtil.IsZero(upItemOverlapSize.Height))
                        continue;

                    overlapSize = upItemOverlapSize;
                    overlapContainerRect = new Rect(upItemPos.X, upItemPos.Y, upTreeViewItem.ActualWidth, upTreeViewItem.ActualHeight);
                    overlapContainer = upTreeViewItem;

                    return true;
                }
            };

            var superIC = ItemsControl.ItemsControlFromItemContainer(parentItemsControl);
            if (superIC == null)
                return false;

            var topIndex = superIC.ItemContainerGenerator.IndexFromContainer(parentItemsControl);
            return FindFirstUpItemWithValidOverlap(rootTreeView, superIC, topIndex - 1, draggingRect, ref overlapSize, ref  overlapContainerRect, ref overlapContainer);
        }

        private bool FindFirstDownItemWithValidOverlap(MultiSelectTreeView rootTreeView, ItemsControl parentItemsControl, int startIndex, Rect draggingRect, ref Size overlapSize, ref Rect overlapContainerRect, ref MultiSelectTreeViewItem overlapContainer)
        {
            while (startIndex < parentItemsControl.Items.Count)
            {
                var downTreeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(startIndex++) as MultiSelectTreeViewItem;
                if (downTreeViewItem == null)
                    continue;

                if (downTreeViewItem.HasItems)
                {
                    if (FindFirstDownItemWithValidOverlap(rootTreeView, downTreeViewItem, 0, draggingRect, ref overlapSize, ref overlapContainerRect, ref overlapContainer))
                        return true;
                }
                else
                {
                    var downItemPos = downTreeViewItem.TranslatePoint(new Point(), rootTreeView);
                    var downItemOverlapSize = GetOverlapSize(new Rect(downItemPos, new Point(downItemPos.X + downTreeViewItem.ActualWidth, downItemPos.Y + downTreeViewItem.ActualHeight)), draggingRect);

                    if (downItemOverlapSize.IsEmpty || DoubleUtil.IsZero(downItemOverlapSize.Width) || DoubleUtil.IsZero(downItemOverlapSize.Height))
                        continue;

                    overlapSize = downItemOverlapSize;
                    overlapContainerRect = new Rect(downItemPos.X, downItemPos.Y, downTreeViewItem.ActualWidth, downTreeViewItem.ActualHeight);
                    overlapContainer = downTreeViewItem;

                    return true;
                }
            };

            var superIC = ItemsControl.ItemsControlFromItemContainer(parentItemsControl);
            if (superIC == null)
                return false;

            var topIndex = superIC.ItemContainerGenerator.IndexFromContainer(parentItemsControl);
            return FindFirstDownItemWithValidOverlap(rootTreeView, superIC, topIndex + 1, draggingRect, ref overlapSize, ref  overlapContainerRect, ref overlapContainer);
        }

        private bool IsFromGroup(ItemsControl parentItemsControl)
        {
            return parentItemsControl != null && !(parentItemsControl is MultiSelectTreeView);
        }

        private bool DealHover(
            ItemsControl targetItemsControl, IList<TestModel> targetCollection, int targetIndex,
            IList<TestModel> sourceCollection, int sourceIndex, TestModel sourceItem,
            int newGroupIndex, MultiSelectTreeViewItem overlapContainer,
            ref MultiSelectTreeViewItem lastOverlapContainer, ref DateTime startOverlapTime)
        {
            if (!(targetItemsControl is MultiSelectTreeView))
                return false;

            if (lastOverlapContainer != overlapContainer)
            {
                lastOverlapContainer = overlapContainer;
                startOverlapTime = DateTime.Now;
            }

            if ((DateTime.Now - startOverlapTime).TotalMilliseconds > 1200)
            {
                var targetItem = targetCollection[targetIndex];

                var group = new TestModel { Name = "New Group", IsGroup = true };
                group.ModelCollection.Add(targetItem);
                group.ModelCollection.Add(sourceItem);

                targetCollection.RemoveAt(targetIndex);
                targetCollection.Insert(targetIndex, group);

                sourceCollection.RemoveAt(sourceIndex);

                var newGroupGenerator = (ContainerFromIndex(targetItemsControl, newGroupIndex) as ItemsControl).ItemContainerGenerator;
                CheckNewGroupContainerGenerator(newGroupGenerator);

                lastOverlapContainer = null;
            }

            return true;
        }

        private void CheckNewGroupContainerGenerator(ItemContainerGenerator newGroupGenerator)
        {
            if (newGroupGenerator.Status < GeneratorStatus.ContainersGenerated)
            {
                EventHandler handler = null;
                handler = (s, e) =>
                {
                    var generator = (ItemContainerGenerator)s;

                    if (_draggingContainer == null)
                        generator.StatusChanged -= handler;
                    else
                    {
                        if (generator.Status == GeneratorStatus.ContainersGenerated)
                        {
                            _draggingContainer = generator.ContainerFromIndex(1) as MultiSelectTreeViewItem;
                            if (_draggingContainer != null)
                            {
                                _draggingContainer.Focus();
                                _draggingContainer.Opacity = 0.2;
                            }

                            generator.StatusChanged -= handler;
                        }
                    }
                };

                newGroupGenerator.StatusChanged += handler;
            }
            else
                _draggingContainer = newGroupGenerator.ContainerFromIndex(1) as MultiSelectTreeViewItem;
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

        private bool MoveUp(MultiSelectTreeView rootTreeView, Rect draggingRect, ref MultiSelectTreeViewItem draggingContainer, ref MultiSelectTreeViewItem lastOverlapContainer, ref DateTime startOverlapTime)
        {
            Size overlapSize = new Size();
            Rect overlapItemRect = new Rect();
            MultiSelectTreeViewItem overlapContainer = null;

            var sourceItemsControl = ItemsControl.ItemsControlFromItemContainer(draggingContainer);
            if (sourceItemsControl == null)
                return false;

            var sourceCollection = sourceItemsControl.ItemsSource as IList<TestModel>;
            var sourceIndex = sourceItemsControl.ItemContainerGenerator.IndexFromContainer(draggingContainer);
            var sourceItem = sourceCollection[sourceIndex];

            var sourcePos = draggingContainer.TranslatePoint(new Point(), rootTreeView);

            //从组中分离
            if (IsFromGroup(sourceItemsControl) && sourceIndex == 0 && DoubleUtil.LessThan(draggingRect.Bottom, sourcePos.Y + draggingContainer.ActualHeight / 2))
            {
                //加入上层集合
                var superItemsControl = ItemsControl.ItemsControlFromItemContainer(sourceItemsControl);
                var superCollection = superItemsControl.ItemsSource as IList<TestModel>;
                var superIndex = superItemsControl.ItemContainerGenerator.IndexFromContainer(sourceItemsControl);

                sourceCollection.RemoveAt(sourceIndex);
                superCollection.Insert(superIndex, sourceItem);

                //原组若剩余一项，则将该项加入上层，移除组
                if (sourceItemsControl.Items.Count == 1)
                {
                    sourceItem = sourceCollection[0];
                    sourceCollection.RemoveAt(0);

                    superCollection.Insert(superIndex + 1, sourceItem);
                    superCollection.RemoveAt(superIndex + 2);
                }

                lastOverlapContainer = null;
                draggingContainer = ContainerFromIndex(superItemsControl, superIndex) as MultiSelectTreeViewItem;

                return true;
            }

            if (!FindFirstUpItemWithValidOverlap(rootTreeView, sourceItemsControl, sourceIndex - 1, draggingRect, ref overlapSize, ref overlapItemRect, ref overlapContainer))
                return false;

            var targetItemsControl = ItemsControl.ItemsControlFromItemContainer(overlapContainer);
            var targetCollection = targetItemsControl.ItemsSource as IList<TestModel>;
            var targetIndex = targetItemsControl.ItemContainerGenerator.IndexFromContainer(overlapContainer);

            if (DoubleUtil.GreaterThan(draggingRect.Y, overlapItemRect.Y + overlapItemRect.Height / 2))
            {
                //do noting
            }
            else if (DoubleUtil.GreaterThan(draggingRect.Y, overlapItemRect.Y + overlapItemRect.Height / 4)) // 3/4 -> 1/4 height
            {
                //非同组，则加入
                if (IsFromGroup(targetItemsControl) && targetItemsControl != sourceItemsControl)
                {
                    sourceCollection.RemoveAt(sourceIndex);
                    targetCollection.Add(sourceItem);

                    lastOverlapContainer = null;
                    draggingContainer = ContainerFromIndex(targetItemsControl, targetCollection.Count - 1) as MultiSelectTreeViewItem;
                }
            }
            else if (DoubleUtil.LessThan(draggingRect.Bottom, overlapItemRect.Y + overlapItemRect.Height * 3 / 4)) //Top -> 3/4 height 移动
            {
                sourceCollection.RemoveAt(sourceIndex);
                targetCollection.Insert(targetIndex, sourceItem);

                lastOverlapContainer = null;
                draggingContainer = ContainerFromIndex(targetItemsControl, targetIndex) as MultiSelectTreeViewItem;
            }
            else //处理悬停，创建新组
            {
                if (!DealHover(
                        targetItemsControl, targetCollection, targetIndex,
                        sourceCollection, sourceIndex, sourceItem,
                        targetIndex, overlapContainer,
                        ref lastOverlapContainer, ref startOverlapTime))
                    return false;
            }

            return true;
        }

        private bool MoveDown(MultiSelectTreeView rootTreeView, Rect draggingRect, ref MultiSelectTreeViewItem draggingContainer, ref MultiSelectTreeViewItem lastOverlapContainer, ref DateTime startOverlapTime)
        {
            Size overlapSize = new Size();
            Rect overlapItemRect = new Rect();
            MultiSelectTreeViewItem overlapContainer = null;

            var sourceItemsControl = ItemsControl.ItemsControlFromItemContainer(draggingContainer);
            if (sourceItemsControl == null)
                return false;

            var sourceCollection = sourceItemsControl.ItemsSource as IList<TestModel>;
            var sourceIndex = sourceItemsControl.ItemContainerGenerator.IndexFromContainer(draggingContainer);
            var sourceItem = sourceCollection[sourceIndex];

            var sourcePos = draggingContainer.TranslatePoint(new Point(), rootTreeView);

            //从组中分离
            if (IsFromGroup(sourceItemsControl) && sourceIndex == sourceItemsControl.Items.Count - 1 && DoubleUtil.GreaterThan(draggingRect.Y, sourcePos.Y + draggingContainer.ActualHeight / 2))
            {
                //加入上层集合
                var superItemsControl = ItemsControl.ItemsControlFromItemContainer(sourceItemsControl);
                var superCollection = superItemsControl.ItemsSource as IList<TestModel>;
                var superIndex = superItemsControl.ItemContainerGenerator.IndexFromContainer(sourceItemsControl);

                sourceCollection.RemoveAt(sourceIndex);
                if (superIndex + 1 < superItemsControl.Items.Count)
                {
                    superCollection.Insert(superIndex + 1, sourceItem);
                    draggingContainer = ContainerFromIndex(superItemsControl, superIndex + 1) as MultiSelectTreeViewItem;
                }
                else
                {
                    superCollection.Add(sourceItem);
                    draggingContainer = ContainerFromIndex(superItemsControl, superCollection.Count - 1) as MultiSelectTreeViewItem;
                }

                //原组若剩余一项，则将该项加入上层集合，移除组
                if (sourceItemsControl.Items.Count == 1)
                {
                    sourceItem = sourceCollection[0];
                    sourceCollection.RemoveAt(0);

                    superCollection.Insert(superIndex + 1, sourceItem);
                    superCollection.RemoveAt(superIndex);
                }

                lastOverlapContainer = null;
                return true;
            }

            if (!FindFirstDownItemWithValidOverlap(rootTreeView, sourceItemsControl, sourceIndex + 1, draggingRect, ref overlapSize, ref overlapItemRect, ref overlapContainer))
                return false;

            var targetItemsControl = ItemsControl.ItemsControlFromItemContainer(overlapContainer);
            var targetCollection = targetItemsControl.ItemsSource as IList<TestModel>;
            var targetIndex = targetItemsControl.ItemContainerGenerator.IndexFromContainer(overlapContainer);

            if (DoubleUtil.LessThan(draggingRect.Bottom, overlapItemRect.Y + overlapItemRect.Height / 2))
            {
                //Do noting
            }
            else if (DoubleUtil.LessThan(draggingRect.Bottom, overlapItemRect.Y + overlapItemRect.Height * 3 / 4)) // 1/4 -> 3/4 height
            {
                //非同组，则加入
                if (IsFromGroup(targetItemsControl) && targetItemsControl != sourceItemsControl)
                {
                    sourceCollection.RemoveAt(sourceIndex);
                    targetCollection.Insert(0, sourceItem);

                    lastOverlapContainer = null;
                    draggingContainer = ContainerFromIndex(targetItemsControl, 0) as MultiSelectTreeViewItem;
                }
            }
            else if (DoubleUtil.GreaterThan(draggingRect.Y, overlapItemRect.Y + overlapItemRect.Height / 4)) //Top -> 1/4 height 移动
            {
                sourceCollection.RemoveAt(sourceIndex);
                targetCollection.Insert(targetIndex, sourceItem);

                lastOverlapContainer = null;
                draggingContainer = ContainerFromIndex(targetItemsControl, targetIndex) as MultiSelectTreeViewItem;
            }
            else //处理悬停，创建新组
            {
                if (!DealHover(
                        targetItemsControl, targetCollection, targetIndex,
                        sourceCollection, sourceIndex, sourceItem,
                        targetIndex - 1, overlapContainer,
                        ref lastOverlapContainer, ref startOverlapTime))
                    return false;
            }

            return true;
        }

        private void Move()
        {
            var screenPos = new Win32.POINT();
            if (!Win32.GetCursorPos(ref screenPos))
                return;

            var posToPanel = treeView.PointFromScreen(new Point(screenPos.X, screenPos.Y));
            var draggingRect = new Rect(posToPanel.X - _cacheMouseDownPosToChild.X, posToPanel.Y - _cacheMouseDownPosToChild.Y, _draggingContainer.ActualWidth, _draggingContainer.ActualHeight);

            if (DoubleUtil.GreaterThan(draggingRect.X, treeView.ActualWidth * 3 / 4)
                || DoubleUtil.LessThan(draggingRect.Right, treeView.ActualWidth / 4)
                || DoubleUtil.LessThan(draggingRect.Bottom, 0)
                || DoubleUtil.GreaterThan(draggingRect.Y, treeView.ActualHeight))
                return;

            var sourcePos = _draggingContainer.TranslatePoint(new Point(), treeView);
            var isSuccess = false;

            if (MoveDirection(sourcePos, draggingRect))
            {
                isSuccess = MoveUp(treeView, draggingRect, ref _draggingContainer, ref _lastOverlapContainer, ref _startOverlapTime);
            }
            else
            {
                isSuccess = MoveDown(treeView, draggingRect, ref _draggingContainer, ref _lastOverlapContainer, ref _startOverlapTime);
            }

            if (isSuccess && _draggingContainer != null)
            {
                _draggingContainer.Focus();
                _draggingContainer.Opacity = 0.2;

                _testSource.ItemsChangedFlag = !_testSource.ItemsChangedFlag;
            }
        }

        private Size GetOverlapSize(Rect rect1, Rect rect2)
        {
            return Rect.Intersect(rect1, rect2).Size;
        }

        private void treeView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.G:
                    // Ctrl-Fowardslash = Select All
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        UnGroupToItems();
                    }
                    else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        GroupFromItems();
                    }

                    break;
            }
        }
    }
}

