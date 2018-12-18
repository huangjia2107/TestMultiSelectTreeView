using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Collections;
using System.Windows.Automation.Peers;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TestMultiSelectTreeView.Helps;
using System.Windows.Data;

namespace TestMultiSelectTreeView.Controls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MultiSelectTreeViewItem))]
    [TemplatePart(Name = "PART_ScrollHost", Type = typeof(ScrollViewer))]
    public class MultiSelectTreeView : ItemsControl
    {
        private ScrollViewer _scrollHost = null;

        private readonly Dictionary<MultiSelectTreeViewItem, object> _selectedElements = null;

        private MultiSelectTreeViewItem _lastSelectedContainer = null;

        private static bool IsControlKeyDown
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control; }
        }

        private static bool IsShiftKeyDown
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift; }
        }

        #region Constructors

        static MultiSelectTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(typeof(MultiSelectTreeView)));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(MultiSelectTreeView), new FrameworkPropertyMetadata((object)KeyboardNavigationMode.Contained));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(MultiSelectTreeView), new FrameworkPropertyMetadata((object)KeyboardNavigationMode.None));
        }

        public MultiSelectTreeView()
        {
            _selectedElements = new Dictionary<MultiSelectTreeViewItem, object>();
        }

        #endregion

        #region Events

        public static readonly RoutedEvent SelectedItemsChangedEvent = EventManager.RegisterRoutedEvent("SelectedItemsChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<IList<object>>), typeof(MultiSelectTreeView));
        public event RoutedPropertyChangedEventHandler<IList<object>> SelectedItemsChanged
        {
            add { AddHandler(SelectedItemsChangedEvent, value); }
            remove { RemoveHandler(SelectedItemsChangedEvent, value); }
        }

        protected virtual void OnSelectedItemsChanged(RoutedPropertyChangedEventArgs<IList<object>> e)
        {
            RaiseEvent(e);
        }

        #endregion

        #region Properties

        private static readonly DependencyPropertyKey SelectedItemsPropertyKey =
                DependencyProperty.RegisterReadOnly("SelectedItems", typeof(IList), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata((IList)null));

        private static readonly DependencyProperty SelectedItemsProperty = SelectedItemsPropertyKey.DependencyProperty;
        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
        }

        private void SetSelectedItems()
        {
            SetValue(SelectedItemsPropertyKey, _selectedElements.Values.ToList());

            ShowSelectedItems();
            //OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<IList<object>>(selectedItem, obj, SelectedItemChangedEvent));
        }

        private void ShowSelectedItems()
        {
            if (SelectedItems != null)
            {
                string s = string.Empty;
                foreach (var d in SelectedItems)
                {
                    s += (d as TestModel).Name + " | ";
                }

                if (!string.IsNullOrEmpty(s))
                    System.Diagnostics.Trace.TraceInformation("Time = {0}, SelectedItems = {1}", DateTime.Now.ToString("HH:mm:ss,fff"), s.TrimEnd(" | ".ToCharArray()));
                else
                    System.Diagnostics.Trace.TraceInformation("SelectedItems is empty");
            }
        }

        #endregion

        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollHost = GetTemplateChild("PART_ScrollHost") as ScrollViewer;
        }

        //创建容器前都用这个方法检查它是不是就是容器本身
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiSelectTreeViewItem;
        }

        //为Items中每一个item创建它的容器用于在UI上显示
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
                        RemoveItemsWithChildrenSelection(e.OldItems);
                        SetSelectedItems();
                        return;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        ResetSelectedElements();
                        SetSelectedItems();
                        return;
                    }
            }

            object[] action = new object[] { e.Action };
            throw new NotSupportedException("UnexpectedCollectionChangeAction" + action);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                if (!IsControlKeyDown)
                {
                    Key key = e.Key;
                    if (key != Key.Tab)
                    {
                        switch (key)
                        {
                            case Key.End:
                                {
                                    if (!this.FocusLastItem())
                                    {
                                        break;
                                    }
                                    e.Handled = true;
                                    return;
                                }
                            case Key.Home:
                                {
                                    if (!this.FocusFirstItem())
                                    {
                                        break;
                                    }
                                    e.Handled = true;
                                    return;
                                }
                            case Key.Left:
                            case Key.Right:
                                {
                                    break;
                                }
                            case Key.Up:
                            case Key.Down:
                                {
                                    if (_selectedElements.Count > 0 || !this.FocusFirstItem())
                                    {
                                        break;
                                    }
                                    e.Handled = true;
                                    return;
                                }
                            default:
                                {
                                    return;
                                }
                        }
                    }
                    else if (IsShiftKeyDown && base.IsKeyboardFocusWithin && this.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)))
                    {
                        e.Handled = true;
                    }
                }
                else
                {
                    switch (e.Key)
                    {
                        case Key.Prior:
                        case Key.Next:
                        case Key.End:
                        case Key.Home:
                        case Key.Left:
                        case Key.Up:
                        case Key.Right:
                        case Key.Down:
                            {
                                if (!this.HandleScrollKeys(e.Key))
                                {
                                    break;
                                }
                                e.Handled = true;
                                return;
                            }
                        default:
                            {
                                return;
                            }
                    }
                }
            }
        }

        #endregion

        #region Func

        private static DependencyObject FindParent(DependencyObject o)
        {
            ContentElement contentElement;
            Visual visual = o as Visual;

            if (visual == null)
                contentElement = o as ContentElement;
            else
                contentElement = null;

            ContentElement contentElement1 = contentElement;
            if (contentElement1 != null)
            {
                o = ContentOperations.GetParent(contentElement1);
                if (o != null)
                    return o;

                FrameworkContentElement frameworkContentElement = contentElement1 as FrameworkContentElement;
                if (frameworkContentElement != null)
                {
                    return frameworkContentElement.Parent;
                }
            }
            else if (visual != null)
                return VisualTreeHelper.GetParent(visual);

            return null;
        }

        private bool FocusFirstItem()
        {
            var treeViewItem = base.ItemContainerGenerator.ContainerFromIndex(0) as MultiSelectTreeViewItem;
            if (treeViewItem == null)
                return false;

            if (treeViewItem.IsEnabled && treeViewItem.Focus())
                return true;

            return treeViewItem.FocusDown();
        }

        private bool FocusLastItem()
        {
            for (int i = base.Items.Count - 1; i >= 0; i--)
            {
                var treeViewItem = base.ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (treeViewItem != null && treeViewItem.IsEnabled)
                    return MultiSelectTreeViewItem.FocusIntoItem(treeViewItem);
            }

            return false;
        }

        private bool GetFirstVisibleItem(ref object item, ref MultiSelectTreeViewItem container)
        {
            if (!base.HasItems)
            {
                item = null;
                container = null;
                return false;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                container = ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (container.Visibility != Visibility.Visible)
                    continue;

                item = Items[i];
            }

            if (item == null)
                return false;

            return container != null;
        }

        internal void HandleMouseButtonDown()
        {
            if (!base.IsKeyboardFocusWithin)
            {
                if (_selectedElements.Count == 0)
                {
                    base.Focus();
                }
                else if (!_selectedElements.Any(e => e.Key.IsKeyboardFocused))
                {
                    _selectedElements.First().Key.Focus();
                    return;
                }
            }
        }

        private bool HandleScrollKeys(Key key)
        {
            ScrollViewer scrollHost = _scrollHost;
            if (scrollHost != null)
            {
                bool flowDirection = base.FlowDirection == FlowDirection.RightToLeft;
                switch (key)
                {
                    case Key.Prior:
                        {
                            if (!DoubleUtil.GreaterThan(scrollHost.ExtentHeight, scrollHost.ViewportHeight))
                            {
                                scrollHost.PageLeft();
                            }
                            else
                            {
                                scrollHost.PageUp();
                            }
                            return true;
                        }
                    case Key.Next:
                        {
                            if (!DoubleUtil.GreaterThan(scrollHost.ExtentHeight, scrollHost.ViewportHeight))
                            {
                                scrollHost.PageRight();
                            }
                            else
                            {
                                scrollHost.PageDown();
                            }
                            return true;
                        }
                    case Key.End:
                        {
                            scrollHost.ScrollToBottom();
                            return true;
                        }
                    case Key.Home:
                        {
                            scrollHost.ScrollToTop();
                            return true;
                        }
                    case Key.Left:
                        {
                            if (!flowDirection)
                            {
                                scrollHost.LineLeft();
                            }
                            else
                            {
                                scrollHost.LineRight();
                            }
                            return true;
                        }
                    case Key.Up:
                        {
                            scrollHost.LineUp();
                            return true;
                        }
                    case Key.Right:
                        {
                            if (!flowDirection)
                            {
                                scrollHost.LineRight();
                            }
                            else
                            {
                                scrollHost.LineLeft();
                            }
                            return true;
                        }
                    case Key.Down:
                        {
                            scrollHost.LineDown();
                            return true;
                        }
                }
            }
            return false;
        }

        #endregion

        #region Selection

        private void RefreshSelectedElements(ItemsControl itemsControl)
        {
            if (itemsControl == null || itemsControl.Items == null || itemsControl.Items.Count == 0)
                return;
             
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var item = itemsControl.Items[i];
                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as MultiSelectTreeViewItem;

                if (container != null)
                {
                    if (container.IsSelected)
                    {
                        _selectedElements.Add(container, item); 
                    }

                    RefreshSelectedElements(container);
                }
            }
        }

        internal void ResetSelectedElements()
        {
            _selectedElements.Clear(); 

            RefreshSelectedElements(this);
            SetSelectedItems();
        }

        internal void RemoveItemsWithChildrenSelection(IList items)
        {
            if (items == null || items.Count == 0)
                return;

            var flag = false;
            for (int i = 0; i < _selectedElements.Count; i++)
            {
                var kvp = _selectedElements.ElementAt(i);

                var itemsControl = ItemsControl.ItemsControlFromItemContainer(kvp.Key);
                if (itemsControl == null)
                {
                    _selectedElements.Remove(kvp.Key);
                    i--;

                    flag = true;
                }
            }

            if (flag)
                SetSelectedItems();
        }

        private void SelectRange(double topOffset, double bottomOffset, ItemsControl itemsControl)
        {
            if (itemsControl == null || itemsControl.Items == null || itemsControl.Items.Count == 0)
                return;

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var item = itemsControl.Items[i];
                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as MultiSelectTreeViewItem;

                if (container != null)
                {
                    var curOffset = container.TranslatePoint(new Point(), this).Y;
                    if (DoubleUtil.LessThanOrClose(curOffset, bottomOffset) && DoubleUtil.GreaterThanOrClose(curOffset, topOffset))
                    {
                        container.IsSelected = true;

                        if (!_selectedElements.ContainsKey(container))
                            _selectedElements.Add(container, item);
                    }
                    else
                    {
                        container.IsSelected = false;

                        if (_selectedElements.ContainsKey(container))
                            _selectedElements.Remove(container);
                    }

                    SelectRange(topOffset, bottomOffset, container);
                }
            }
        }

        private void SelectRange(MultiSelectTreeViewItem beginContainer, MultiSelectTreeViewItem endContainer)
        {
            if (Items == null || Items.Count == 0)
                return;

            var beginYOffset = beginContainer == null ? 0d : beginContainer.TranslatePoint(new Point(), this).Y;
            var endYOffset = endContainer == null ? 0d : endContainer.TranslatePoint(new Point(), this).Y;

            var topOffset = Math.Min(beginYOffset, endYOffset);
            var bottomOffset = Math.Max(beginYOffset, endYOffset);

            SelectRange(topOffset, bottomOffset, this);
        }

        internal bool IsChangingSelection { get; private set; }

        internal void ChangeSelection(object data, MultiSelectTreeViewItem container, bool selected, SelectionMode selectionMode)
        {
            IsChangingSelection = true;
            bool flag = false;

            if (selected)
            {
                if (selectionMode == SelectionMode.Single)
                {
                    foreach (var kvp in _selectedElements)
                    {
                        kvp.Key.IsSelected = false;
                    }

                    _selectedElements.Clear();
                }

                if (selectionMode == SelectionMode.Extended)
                {
                    SelectRange(_lastSelectedContainer, container);
                    flag = true;
                }
                else
                {
                    if (!_selectedElements.ContainsKey(container))
                    {
                        container.IsSelected = true;
                        _selectedElements.Add(container, data);

                        flag = true;
                    }
                }
            }
            else if (_selectedElements.ContainsKey(container))
            {
                container.IsSelected = false;
                _selectedElements.Remove(container);

                flag = true;
            }

            if (flag)
            {
                if (selected && selectionMode != SelectionMode.Extended)
                    _lastSelectedContainer = _selectedElements.Count == 0 ? null : _selectedElements.Last().Key;

                if (!selected)
                    _lastSelectedContainer = container;

                SetSelectedItems();
            }

            IsChangingSelection = false;
        }

        #endregion

        private struct ContainerSize
        {
            public double Size;

            public uint NumContainers;

            public ContainerSize(double size)
            {
                this.Size = size;
                this.NumContainers = 1;
            }

            public bool IsCloseTo(ContainerSize size)
            {
                return Math.Abs(this.Size - size.Size) < 0.5;
            }
        }
    }
}

