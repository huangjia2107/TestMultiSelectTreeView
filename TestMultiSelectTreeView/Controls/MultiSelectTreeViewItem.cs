﻿using System;
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
    [TemplatePart(Name = HeaderPartName, Type = typeof(FrameworkElement))]
    public class MultiSelectTreeViewItem : HeaderedItemsControl
    {
        private const string HeaderPartName = "PART_Header";
        private FrameworkElement _headerElement = null;

        private bool CanExpand
        {
            get { return HasItems; }
        }

        private bool CanExpandOnInput
        {
            get { return !this.CanExpand ? false : IsEnabled; }
        }

        private ItemsControl ParentItemsControl
        {
            get { return ItemsControl.ItemsControlFromItemContainer(this); }
        }

        private MultiSelectTreeView ParentTreeView
        {
            get
            {
                for (var i = ParentItemsControl; i != null; i = ItemsControl.ItemsControlFromItemContainer(i))
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

        private MultiSelectTreeViewItem ParentTreeViewItem
        {
            get { return ParentItemsControl as MultiSelectTreeViewItem; }
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

            treeViewItem.Select(newValue, SelectionMode.Multiple);

            if (newValue)
            {
                treeViewItem.OnSelected(new RoutedEventArgs(TreeViewItem.SelectedEvent, treeViewItem));
                return;
            }

            treeViewItem.OnUnselected(new RoutedEventArgs(TreeViewItem.UnselectedEvent, treeViewItem));
        }

        #endregion

        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _headerElement = GetTemplateChild("PART_Header") as FrameworkElement;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiSelectTreeViewItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiSelectTreeViewItem();
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
                case NotifyCollectionChangedAction.Replace:
                    {
                        var parentTreeView = ParentTreeView;
                        if (parentTreeView == null || !HasItems && (e.OldItems == null || e.OldItems.Count == 0))
                            return;

                        parentTreeView.RemoveItemsWithChildrenSelection(e.OldItems);
                        return;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        var parentTreeView = ParentTreeView;
                        if (parentTreeView == null || !HasItems)
                            return;

                        parentTreeView.ResetSelectedElements();
                        return;
                    }
            }

            object[] action = new object[] { e.Action };
            throw new NotSupportedException("UnexpectedCollectionChangeAction, " + action);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                Focus();

                if (e.ClickCount > 1)
                {
                    this.IsExpanded = !this.IsExpanded;
                }
                else
                {
                    if (IsControlKeyDown || IsShiftKeyDown || !IsSelected || IsSelected && ParentTreeView.SelectedItems.Count > 1)
                        Select(IsControlKeyDown ? (!IsSelected) : true, GetSelectionMode());
                }

                e.Handled = true;
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                Focus();

                Select(true, SelectionMode.Single);
                e.Handled = true;
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            Select(true, GetSelectionMode());
            base.OnGotFocus(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                var key = e.Key;
                switch (key)
                {
                    case Key.Left:
                    case Key.Right:
                        {
                            if (!this.LogicalLeft(e.Key))
                            {
                                if (IsControlKeyDown || !CanExpandOnInput)
                                    break;

                                if (!this.IsExpanded)
                                {
                                    this.IsExpanded = true;
                                    e.Handled = true;
                                    return;
                                }

                                if (!HandleDownKey())
                                    break;

                                e.Handled = true;
                                return;
                            }
                            else
                            {
                                if (IsControlKeyDown || !CanExpandOnInput || !this.IsExpanded)
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
                            if (IsControlKeyDown || !HandleUpKey())
                                break;

                            e.Handled = true;
                            break;
                        }
                    case Key.Down:
                        {
                            if (IsControlKeyDown || !HandleDownKey())
                                break;

                            e.Handled = true;
                            return;
                        }
                    case Key.Home:
                        {
                            if (IsControlKeyDown || !HandleHomeKey())
                                break;

                            e.Handled = true;
                            return;
                        }
                    case Key.End:
                        {
                            if (IsControlKeyDown || !HandleEndKey())
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

        #endregion

        #region Static

        private static bool IsControlKeyDown
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control; }
        }

        private static bool IsShiftKeyDown
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift; }
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

        private static MultiSelectTreeViewItem FindLastFocusableItem(MultiSelectTreeViewItem item)
        {
            MultiSelectTreeViewItem treeViewItem = null;
            int count = -1;
            MultiSelectTreeViewItem treeViewItem1 = null;

            while (item != null)
            {
                if (!item.IsEnabled || !item.IsVisible)
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

        #region Handle Key Func

        internal bool FocusDown()
        {
            var treeViewItem = FindNextFocusableItem(true);
            if (treeViewItem == null)
                return true;

            return treeViewItem.Focus();
        }

        private bool HandleDownKey()
        {
            return FocusDown();
        }

        private bool HandleUpKey()
        {
            var itemsControl = FindPreviousFocusableItem();
            if (itemsControl != null)
            {
                if (itemsControl == ParentItemsControl && itemsControl == ParentTreeView)
                    return true;

                return itemsControl.Focus();
            }

            return false;
        }

        private bool HandleHomeKey()
        {
            var parentTreeView = ParentTreeView;
            if (parentTreeView == null)
                return false;

            return parentTreeView.FocusFirstItem();
        }

        private bool HandleEndKey()
        {
            var parentTreeView = ParentTreeView;
            if (parentTreeView == null)
                return false;

            return parentTreeView.FocusLastItem();
        }

        private MultiSelectTreeViewItem FindNextFocusableItem(bool walkIntoSubtree)
        {
            if (walkIntoSubtree && CanExpand && IsExpanded)
            {
                var treeViewItem1 = ItemContainerGenerator.ContainerFromIndex(0) as MultiSelectTreeViewItem;
                if (treeViewItem1 != null)
                {
                    if (treeViewItem1.IsEnabled && treeViewItem1._headerElement.IsVisible)
                        return treeViewItem1;

                    return treeViewItem1.FindNextFocusableItem(false);
                }
            }

            var parentItemsControl = ParentItemsControl;
            if (parentItemsControl != null)
            {
                int num = parentItemsControl.ItemContainerGenerator.IndexFromContainer(this);
                int count = parentItemsControl.Items.Count;

                MultiSelectTreeViewItem treeViewItem;
                while (num < count)
                {
                    num++;

                    treeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(num) as MultiSelectTreeViewItem;
                    if (treeViewItem == null || !treeViewItem.IsEnabled || !treeViewItem.IsVisible)
                        continue;

                    if (!treeViewItem._headerElement.IsVisible)
                        return treeViewItem.FindNextFocusableItem(true);

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
            var parentItemsControl = ParentItemsControl;
            if (parentItemsControl == null)
                return null;

            int num = parentItemsControl.ItemContainerGenerator.IndexFromContainer(this);
            if (num == 0)
            {
                var parentTreeViewItem = ParentTreeViewItem;
                if (parentTreeViewItem != null && !parentTreeViewItem._headerElement.IsVisible)
                    return parentTreeViewItem.FindPreviousFocusableItem();
            }

            while (num > 0)
            {
                num--;

                var treeViewItem = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(num) as MultiSelectTreeViewItem;
                if (treeViewItem == null || !treeViewItem.IsEnabled || !treeViewItem.IsVisible)
                    continue;

                var treeViewItem1 = FindLastFocusableItem(treeViewItem);
                if (treeViewItem1 == null)
                    continue;

                return treeViewItem1;
            }

            return parentItemsControl;
        }

        private bool LogicalLeft(Key key)
        {
            bool flowDirection = (FlowDirection == FlowDirection.RightToLeft);
            if (!flowDirection && key == Key.Left)
                return true;

            if (!flowDirection)
                return false;

            return key == Key.Right;
        }

        #endregion

        #region Func

        private SelectionMode GetSelectionMode()
        {
            if (IsControlKeyDown)
                return SelectionMode.Multiple;

            if (IsShiftKeyDown)
                return SelectionMode.Extended;

            return SelectionMode.Single;
        }

        private object GetItemOrContainerFromContainer(ItemsControl itemsControl, DependencyObject container)
        {
            object obj = itemsControl.ItemContainerGenerator.ItemFromContainer(container);
            if (obj == DependencyProperty.UnsetValue && ItemsControl.ItemsControlFromItemContainer(container) == this && container is UIElement)
            {
                obj = container;
            }
            return obj;
        }

        private void Select(bool selected, SelectionMode selectionMode)
        {
            var parentTreeView = ParentTreeView;
            var parentItemsControl = ParentItemsControl;

            if (parentTreeView != null && parentItemsControl != null && !parentTreeView.IsChangingSelection)
            {
                parentTreeView.ChangeSelection(GetItemOrContainerFromContainer(parentItemsControl, this), this, selected, selectionMode);
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

        private void HandleBringIntoView(RequestBringIntoViewEventArgs e)
        {
            for (var i = ParentTreeViewItem; i != null; i = i.ParentTreeViewItem)
            {
                if (!i.IsExpanded)
                    i.IsExpanded = true;
            }

            if (e.TargetRect.IsEmpty)
            {
                var headerElement = GetFirstHeaderElement();
                if (headerElement != null)
                {
                    headerElement.BringIntoView();
                    e.Handled = true;
                }
            }
        }

        private FrameworkElement GetFirstHeaderElement()
        {
            var headerElement = _headerElement;
            if (headerElement != null)
            {
                if (headerElement.IsVisible)
                    return headerElement;

                if (HasItems)
                {
                    var count = Items.Count;
                    var num = 0;

                    while (num < count)
                    {
                        var treeViewItem = ItemContainerGenerator.ContainerFromIndex(num) as MultiSelectTreeViewItem;
                        if (treeViewItem == null || !treeViewItem.IsVisible)
                        {
                            num++;
                            continue;
                        }

                        return treeViewItem.GetFirstHeaderElement();
                    }
                }
            }

            return null;
        }

        private DependencyObject InternalPredictFocus(FocusNavigationDirection direction)
        {
            switch (direction)
            {
                case FocusNavigationDirection.Left:
                case FocusNavigationDirection.Up:
                    {
                        return FindPreviousFocusableItem();
                    }
                case FocusNavigationDirection.Right:
                case FocusNavigationDirection.Down:
                    {
                        return FindNextFocusableItem(true);
                    }
            }

            return null;
        }

        #endregion
    }
}
