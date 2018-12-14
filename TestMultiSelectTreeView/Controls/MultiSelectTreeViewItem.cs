using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using TestMultiSelectTreeView.Helps;
using System.Collections.Specialized;

namespace TestMultiSelectTreeView.Controls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MultiSelectTreeViewItem))]
    [TemplatePart(Name = "PART_Header", Type = typeof(FrameworkElement))]
    public class MultiSelectTreeViewItem : HeaderedItemsControl
    {
        private const string HeaderPartName = "PART_Header";

        private bool CanExpand
        {
            get { return base.HasItems; }
        }

        private bool CanExpandOnInput
        {
            get { return !this.CanExpand ? false : base.IsEnabled; }
        }

        private FrameworkElement HeaderElement
        {
            get { return base.GetTemplateChild("PART_Header") as FrameworkElement; }
        }

        internal ItemsControl ParentItemsControl
        {
            get { return ItemsControl.ItemsControlFromItemContainer(this); }
        }

        internal MultiSelectTreeView ParentTreeView
        {
            get
            {
                for (var i = this.ParentItemsControl; i != null; i = ItemsControl.ItemsControlFromItemContainer(i))
                {
                    var treeView = i as MultiSelectTreeView;
                    if (treeView != null)
                    {
                        return treeView;
                    }
                }
                return null;
            }
        }

        internal MultiSelectTreeViewItem ParentTreeViewItem
        {
            get { return this.ParentItemsControl as MultiSelectTreeViewItem; }
        }

        static MultiSelectTreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectTreeViewItem), new FrameworkPropertyMetadata(typeof(MultiSelectTreeViewItem)));
            IsTabStopProperty.OverrideMetadata(typeof(MultiSelectTreeViewItem), new FrameworkPropertyMetadata(false));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(MultiSelectTreeViewItem), new FrameworkPropertyMetadata((object)KeyboardNavigationMode.Continue));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(MultiSelectTreeViewItem), new FrameworkPropertyMetadata((object)KeyboardNavigationMode.None));

            EventManager.RegisterClassHandler(typeof(MultiSelectTreeViewItem), FrameworkElement.RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(OnRequestBringIntoView));
            EventManager.RegisterClassHandler(typeof(MultiSelectTreeViewItem), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
        }

        public MultiSelectTreeViewItem()
        {
        }

        #region Event

        public readonly static RoutedEvent ExpandedEvent = EventManager.RegisterRoutedEvent("Expanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MultiSelectTreeViewItem));
        public event RoutedEventHandler Expanded
        {
            add { base.AddHandler(TreeViewItem.ExpandedEvent, value); }
            remove { base.RemoveHandler(TreeViewItem.ExpandedEvent, value); }
        }

        public readonly static RoutedEvent CollapsedEvent = EventManager.RegisterRoutedEvent("Collapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MultiSelectTreeViewItem));
        public event RoutedEventHandler Collapsed
        {
            add { base.AddHandler(TreeViewItem.CollapsedEvent, value); }
            remove { base.RemoveHandler(TreeViewItem.CollapsedEvent, value); }
        }

        public readonly static RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MultiSelectTreeViewItem));
        public event RoutedEventHandler Selected
        {
            add { base.AddHandler(TreeViewItem.SelectedEvent, value); }
            remove { base.RemoveHandler(TreeViewItem.SelectedEvent, value); }
        }

        public readonly static RoutedEvent UnselectedEvent = EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MultiSelectTreeViewItem));
        public event RoutedEventHandler Unselected
        {
            add { base.AddHandler(TreeViewItem.UnselectedEvent, value); }
            remove { base.RemoveHandler(TreeViewItem.UnselectedEvent, value); }
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(MultiSelectTreeViewItem), new FrameworkPropertyMetadata(false, OnIsExpandedChanged));
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeViewItem = (MultiSelectTreeViewItem)d;
            var newValue = (bool)e.NewValue;
            //             if (!newValue)
            //             {
            //                 var parentTreeView = treeViewItem.ParentTreeView;
            //                 if (parentTreeView != null)
            //                 {
            //                     parentTreeView.HandleSelectionAndCollapsed(treeViewItem);
            //                 }
            //             }

            //             var treeViewItemAutomationPeer = UIElementAutomationPeer.FromElement(treeViewItem) as TreeViewItemAutomationPeer;
            //             if (treeViewItemAutomationPeer != null)
            //             {
            //                 treeViewItemAutomationPeer.RaiseExpandCollapseAutomationEvent((bool)e.OldValue, newValue);
            //             }

            if (newValue)
            {
                treeViewItem.OnExpanded(new RoutedEventArgs(TreeViewItem.ExpandedEvent, treeViewItem));
                return;
            }

            treeViewItem.OnCollapsed(new RoutedEventArgs(TreeViewItem.CollapsedEvent, treeViewItem));
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(MultiSelectTreeViewItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeViewItem = (MultiSelectTreeViewItem)d;
            var newValue = (bool)e.NewValue;

            treeViewItem.Select(newValue, true);

            if (newValue)
            {
                treeViewItem.OnSelected(new RoutedEventArgs(TreeViewItem.SelectedEvent, treeViewItem));
                return;
            }

            treeViewItem.OnUnselected(new RoutedEventArgs(TreeViewItem.UnselectedEvent, treeViewItem));
        } 

        #endregion

        #region Override

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                Focus();

                if (e.ClickCount % 2 == 0)
                {
                    System.Diagnostics.Trace.TraceInformation("[ OnMouseLeftButtonDown ] Click Double");

                    this.IsExpanded = !this.IsExpanded;
                    Select(true, false);

                    e.Handled = true;
                }
                else
                {
                    System.Diagnostics.Trace.TraceInformation("[ OnMouseLeftButtonDown ] Click Single");

                    this.Select(IsControlKeyDown ? (!IsSelected) : true, IsControlKeyDown);
                    e.Handled = true;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                Focus();

                this.Select(true, false);
                e.Handled = true;

            }
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {  
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Move:
                    {
                        return;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        var treeView = this.ParentTreeView;
                        if (treeView == null)
                            return;

                        treeView.ClearItemsWithChildsSelection(e.OldItems);
                        return;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        var treeView = this.ParentTreeView;
                        if (treeView == null)
                            return;

                        treeView.ClearItemsWithChildsSelection(this.Items);
                        return;
                    }
                case NotifyCollectionChangedAction.Replace:
                    {
                        var treeView = this.ParentTreeView;
                        if (treeView == null)
                            return;

                        treeView.ClearItemsWithChildsSelection(e.OldItems);
                        return;
                    }
            }

            object[] action = new object[] { e.Action };
            throw new NotSupportedException("UnexpectedCollectionChangeAction, " + action);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                Key key = e.Key;
                switch (key)
                {
                    case Key.Left:
                    case Key.Right:
                        {
                            if (!this.LogicalLeft(e.Key))
                            {
                                if (IsControlKeyDown || !this.CanExpandOnInput)
                                    break;

                                if (!this.IsExpanded)
                                {
                                    this.IsExpanded = true;
                                    e.Handled = true;
                                    return;
                                }

                                if (!this.HandleDownKey())
                                    break;

                                e.Handled = true;
                                return;
                            }
                            else
                            {
                                if (IsControlKeyDown || !this.CanExpandOnInput || !this.IsExpanded)
                                    break;

                                if (!base.IsFocused)
                                    base.Focus();
                                else
                                    this.IsExpanded = false;

                                e.Handled = true;
                                return;
                            }
                        }
                    case Key.Up:
                        {
                            if (IsControlKeyDown || !this.HandleUpKey())
                                break;

                            e.Handled = true;
                            break;
                        }
                    case Key.Down:
                        {
                            if (IsControlKeyDown || !this.HandleDownKey())
                                break;

                            e.Handled = true;
                            return;
                        }
                    default:
                        {
                            switch (key)
                            {
                                case Key.Add:
                                    {
                                        if (!this.CanExpandOnInput || this.IsExpanded)
                                            return;

                                        this.IsExpanded = true;
                                        e.Handled = true;
                                        return;
                                    }
                                case Key.Separator:
                                    {
                                        break;
                                    }
                                case Key.Subtract:
                                    {
                                        if (!this.CanExpandOnInput || !this.IsExpanded)
                                            return;

                                        this.IsExpanded = false;
                                        e.Handled = true;
                                        return;
                                    }
                                default:
                                    {
                                        return;
                                    }
                            }
                            break;
                        }
                }
            }
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiSelectTreeViewItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiSelectTreeViewItem();
        }

        #endregion

        #region Static

        private static bool IsControlKeyDown
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control; }
        }

        private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentTreeView = ((MultiSelectTreeViewItem)sender).ParentTreeView;
            if (parentTreeView != null)
            {
                parentTreeView.HandleMouseButtonDown();
            }
        }

        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (e.TargetObject == sender)
            {
                ((MultiSelectTreeViewItem)sender).HandleBringIntoView(e);
            }
        }

        private static double CalculateDelta(bool up, FrameworkElement item, ScrollViewer scroller, double startTop, double startBottom)
        {
            double num;
            return CalculateDelta(up, item, scroller, startTop, startBottom, out num);
        }

        private static double CalculateDelta(bool up, FrameworkElement item, ScrollViewer scroller, double startTop, double startBottom, out double closeEdge)
        {
            double num;
            double num1;
            GetTopAndBottom(item, scroller, out num, out num1);
            if (up)
            {
                closeEdge = startBottom - num1;
                return startBottom - num;
            }
            closeEdge = num - startTop;
            return num1 - startTop;
        }

        private static void GetTopAndBottom(FrameworkElement item, Visual parent, out double top, out double bottom)
        {
            Point point;
            Point point1;
            GeneralTransform ancestor = item.TransformToAncestor(parent);

            if (!ancestor.TryTransform(new Point(0, 0), out point))
                top = 0;
            else
                top = point.Y;

            if (ancestor.TryTransform(new Point(0, item.RenderSize.Height), out point1))
            {
                bottom = point1.Y;
                return;
            }

            bottom = top + item.RenderSize.Height;
        }

        private static MultiSelectTreeViewItem FindLastFocusableItem(MultiSelectTreeViewItem item)
        {
            MultiSelectTreeViewItem treeViewItem = null;
            int count = -1;
            MultiSelectTreeViewItem treeViewItem1 = null;

            while (item != null)
            {
                if (!item.IsEnabled)
                {
                    if (count <= 0)
                        break;

                    count--;
                }
                else
                {
                    if (!item.IsExpanded || !item.CanExpand)
                        return item;

                    treeViewItem = item;
                    treeViewItem1 = item;
                    count = item.Items.Count - 1;
                }

                item = treeViewItem1.ItemContainerGenerator.ContainerFromIndex(count) as MultiSelectTreeViewItem;
            }

            if (treeViewItem != null)
                return treeViewItem;

            return null;
        }

        internal static bool FocusIntoItem(MultiSelectTreeViewItem item)
        {
            var treeViewItem = FindLastFocusableItem(item);
            if (treeViewItem == null)
                return false;

            return treeViewItem.Focus();
        }

        #endregion

        #region Func

        private object GetItemOrContainerFromContainer(ItemsControl itemsControl, DependencyObject container)
        {
            object obj = itemsControl.ItemContainerGenerator.ItemFromContainer(container);
            if (obj == DependencyProperty.UnsetValue && ItemsControl.ItemsControlFromItemContainer(container) == this && container is UIElement)
            {
                obj = container;
            }
            return obj;
        }

        private void Select(bool selected, bool isMultiSelectMode)
        {
            var parentTreeView = this.ParentTreeView;
            var parentItemsControl = this.ParentItemsControl;

            if (parentTreeView != null && parentItemsControl != null)
            {
                parentTreeView.ChangeSelection(GetItemOrContainerFromContainer(parentItemsControl, this), this, selected, isMultiSelectMode);
            }
        }

        protected virtual void OnSelected(RoutedEventArgs e)
        {
            base.RaiseEvent(e);
        }

        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            base.RaiseEvent(e);
        }

        protected virtual void OnCollapsed(RoutedEventArgs e)
        {
            base.RaiseEvent(e);
        }

        protected virtual void OnExpanded(RoutedEventArgs e)
        {
            base.RaiseEvent(e);
        }

        private bool AllowHandleKeyEvent(FocusNavigationDirection direction)
        {
            if (!this.IsSelected)
                return false;

            var focusedElement = Keyboard.FocusedElement as DependencyObject;
            if (focusedElement != null && focusedElement is UIElement)
            {
                var parent = (focusedElement as UIElement).PredictFocus(direction);
                if (parent != focusedElement)
                {
                    while (parent != null)
                    {
                        var treeViewItem = parent as MultiSelectTreeViewItem;
                        if (treeViewItem == this)
                        {
                            return false;
                        }
                        if (treeViewItem != null || parent is TreeView)
                        {
                            return true;
                        }
                        parent = VisualTreeHelper.GetParent(parent);
                    }
                }
            }
            return true;
        }

        private MultiSelectTreeViewItem FindNextFocusableItem(bool walkIntoSubtree)
        {
            MultiSelectTreeViewItem treeViewItem;
            if (walkIntoSubtree && this.IsExpanded && this.CanExpand)
            {
                var treeViewItem1 = base.ItemContainerGenerator.ContainerFromIndex(0) as MultiSelectTreeViewItem;
                if (treeViewItem1 != null)
                {
                    if (treeViewItem1.IsEnabled)
                        return treeViewItem1;

                    return treeViewItem1.FindNextFocusableItem(false);
                }
            }

            var parentItemsControl = this.ParentItemsControl;
            if (parentItemsControl != null)
            {
                int num = parentItemsControl.ItemContainerGenerator.IndexFromContainer(this);
                int count = parentItemsControl.Items.Count;

                while (num < count)
                {
                    num++;
                    treeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(num) as MultiSelectTreeViewItem;
                    if (treeViewItem == null || !treeViewItem.IsEnabled)
                    {
                        continue;
                    }
                    return treeViewItem;
                }

                treeViewItem = parentItemsControl as MultiSelectTreeViewItem;
                if (treeViewItem != null)
                {
                    return treeViewItem.FindNextFocusableItem(false);
                }
            }

            return null;
        }

        private ItemsControl FindPreviousFocusableItem()
        {
            var parentItemsControl = this.ParentItemsControl;
            if (parentItemsControl == null)
                return null;

            int num = parentItemsControl.ItemContainerGenerator.IndexFromContainer(this);

            while (num > 0)
            {
                num--;
                MultiSelectTreeViewItem treeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(num) as MultiSelectTreeViewItem;
                if (treeViewItem == null || !treeViewItem.IsEnabled)
                    continue;

                var treeViewItem1 = FindLastFocusableItem(treeViewItem);
                if (treeViewItem1 == null)
                    continue;

                return treeViewItem1;
            }

            return parentItemsControl;
        }

        internal bool FocusDown()
        {
            var treeViewItem = this.FindNextFocusableItem(true);
            if (treeViewItem == null)
                return false;

            return treeViewItem.Focus();
        }

        internal void GetTopAndBottom(Visual parent, out double top, out double bottom)
        {
            var headerElement = this.HeaderElement;
            if (headerElement != null)
            {
                GetTopAndBottom(headerElement, parent, out top, out bottom);
                return;
            }

            GetTopAndBottom(this, parent, out top, out bottom);
        }

        private void HandleBringIntoView(RequestBringIntoViewEventArgs e)
        {
            for (var i = this.ParentTreeViewItem; i != null; i = i.ParentTreeViewItem)
            {
                if (!i.IsExpanded)
                    i.IsExpanded = true;
            }

            if (e.TargetRect.IsEmpty)
            {
                var headerElement = this.HeaderElement;
                if (headerElement != null)
                {
                    e.Handled = true;
                    headerElement.BringIntoView();
                }
            }
        }

        internal bool HandleDownKey()
        {
            if (!this.AllowHandleKeyEvent(FocusNavigationDirection.Down))
                return false;

            return this.FocusDown();
        }

        internal bool HandleScrollByPage(bool up, ScrollViewer scroller, double viewportHeight, double startTop, double startBottom, out double currentDelta)
        {
            double num;
            double num1;
            int num2 = 0;
            int num3 = 0;
            currentDelta = CalculateDelta(up, this, scroller, startTop, startBottom, out num);

            if (DoubleUtil.GreaterThan(num, viewportHeight))
                return false;

            if (DoubleUtil.LessThanOrClose(currentDelta, viewportHeight))
                return false;

            bool flag = false;
            FrameworkElement headerElement = this.HeaderElement;

            if (headerElement != null && DoubleUtil.LessThanOrClose(CalculateDelta(up, headerElement, scroller, startTop, startBottom), viewportHeight))
                flag = true;

            MultiSelectTreeViewItem treeViewItem = null;
            int count = base.Items.Count;
            bool flag1 = (!up ? false : this.IsSelected);

            for (int i = (up ? count - 1 : 0); 0 <= i && i < count; i = num2 + num3)
            {
                var treeViewItem1 = base.ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (treeViewItem1 != null && treeViewItem1.IsEnabled)
                {
                    if (flag1)
                    {
                        if (!treeViewItem1.IsSelected)
                        {
                            //                             if (!treeViewItem1.ContainsSelection)
                            //                                 goto Label0;

                            flag1 = false;
                        }
                        else
                        {
                            flag1 = false;
                            goto Label0;
                        }
                    }

                    if (treeViewItem1.HandleScrollByPage(up, scroller, viewportHeight, startTop, startBottom, out num1))
                        return true;

                    if (DoubleUtil.GreaterThan(num1, viewportHeight))
                        break;

                    treeViewItem = treeViewItem1;
                }
            Label0:
                num2 = i;
                num3 = (up ? -1 : 1);
            }

            if (treeViewItem == null)
            {
                if (!flag)
                    return false;

                return base.Focus();
            }

            if (up)
                return treeViewItem.Focus();

            return FocusIntoItem(treeViewItem);
        }

        internal bool HandleUpKey()
        {
            if (this.AllowHandleKeyEvent(FocusNavigationDirection.Up))
            {
                ItemsControl itemsControl = this.FindPreviousFocusableItem();
                if (itemsControl != null)
                {
                    if (itemsControl == this.ParentItemsControl && itemsControl == this.ParentTreeView)
                        return true;

                    return itemsControl.Focus();
                }
            }

            return false;
        }

        private DependencyObject InternalPredictFocus(FocusNavigationDirection direction)
        {
            switch (direction)
            {
                case FocusNavigationDirection.Left:
                case FocusNavigationDirection.Up:
                    {
                        return this.FindPreviousFocusableItem();
                    }
                case FocusNavigationDirection.Right:
                case FocusNavigationDirection.Down:
                    {
                        return this.FindNextFocusableItem(true);
                    }
            }
            return null;
        }

        private bool LogicalLeft(Key key)
        {
            bool flowDirection = base.FlowDirection == FlowDirection.RightToLeft;
            if (!flowDirection && key == Key.Left)
                return true;

            if (!flowDirection)
                return false;

            return key == Key.Right;
        }

        #endregion
    }
}
