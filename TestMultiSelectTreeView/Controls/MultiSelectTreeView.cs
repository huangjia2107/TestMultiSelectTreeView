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
    [TemplatePart(Name = ScrollHostPartName, Type = typeof(ScrollViewer))]
    public class MultiSelectTreeView : ItemsControl
    {
        private const string ScrollHostPartName = "PART_ScrollHost";
        private ScrollViewer _scrollHost = null;

        private readonly Dictionary<MultiSelectTreeViewItem, object> _selectedElements = null;
        private List<MultiSelectTreeViewItem> _lastSelectedContainers = null;
        private MultiSelectTreeViewItem _latestSelectedContainer = null;

        internal bool IsChangingSelection { get; private set; }

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

        public static readonly RoutedEvent SelectedItemsChangedEvent = EventManager.RegisterRoutedEvent("SelectedItemsChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<IList>), typeof(MultiSelectTreeView));
        public event RoutedPropertyChangedEventHandler<IList> SelectedItemsChanged
        {
            add { AddHandler(SelectedItemsChangedEvent, value); }
            remove { RemoveHandler(SelectedItemsChangedEvent, value); }
        }

        protected virtual void OnSelectedItemsChanged(RoutedPropertyChangedEventArgs<IList> e)
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
            var selectedContainers = _selectedElements.Keys.ToList();
            if ((_lastSelectedContainers == null || _lastSelectedContainers.Count == 0) && selectedContainers.Count == 0)
                return;

            if (_lastSelectedContainers != null && _lastSelectedContainers.Count == selectedContainers.Count)
            {
                if (_lastSelectedContainers.Except(selectedContainers).Count() == 0)
                    return;
            }

            var oldSelectedItems = SelectedItems;
            _lastSelectedContainers = selectedContainers;

            SetValue(SelectedItemsPropertyKey, _selectedElements.Values.ToList());

            OnSelectedItemsChanged(new RoutedPropertyChangedEventArgs<IList>(oldSelectedItems, SelectedItems, SelectedItemsChangedEvent));
        }

        #endregion

        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollHost = GetTemplateChild(ScrollHostPartName) as ScrollViewer;
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
                    {
                        return;
                    }
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (!HasItems && (e.OldItems == null || e.OldItems.Count == 0))
                            return;

                        RemoveSelectedElementsWithInvalidContainer(false);
                        return;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (!HasItems && (e.OldItems == null || e.OldItems.Count == 0))
                            return;

                        RemoveSelectedElementsWithInvalidContainer(true);
                        return;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        ResetSelectedElements();
                        return;
                    }
            }

            object[] action = new object[] { e.Action };
            throw new NotSupportedException("UnexpectedCollectionChangeAction" + action);
        }



        #endregion

        #region Focus Func

        internal bool FocusFirstItem()
        {
            var treeViewItem = ItemContainerGenerator.ContainerFromIndex(0) as MultiSelectTreeViewItem;
            if (treeViewItem == null)
                return false;

            if (treeViewItem.IsEnabled && treeViewItem.IsVisible && treeViewItem.Focus())
                return true;

            return treeViewItem.FocusDown();
        }

        internal bool FocusLastItem()
        {
            for (int i = base.Items.Count - 1; i >= 0; i--)
            {
                var treeViewItem = ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (treeViewItem != null && treeViewItem.IsEnabled && treeViewItem.IsVisible)
                    return MultiSelectTreeViewItem.FocusIntoItem(treeViewItem);
            }

            return false;
        }

        #endregion

        #region Selection

        private void RefreshSelectedElements(ItemsControl itemsControl)
        {
            if (itemsControl == null || !itemsControl.HasItems)
                return;

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (container != null)
                {
                    if (container.IsSelected)
                    {
                        _selectedElements.Add(container, itemsControl.Items[i]);
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

        internal void RemoveSelectedElementsWithInvalidContainer(bool isUpdateSelection)
        {
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

            if (flag && isUpdateSelection)
                SetSelectedItems();
        }

        private void SelectRange(double topOffset, double bottomOffset, ItemsControl itemsControl)
        {
            if (itemsControl == null || !itemsControl.HasItems)
                return;

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (container != null && container.IsEnabled && container.IsVisible)
                {
                    var curOffset = container.TranslatePoint(new Point(), this).Y;
                    if (DoubleUtil.LessThanOrClose(curOffset, bottomOffset) && DoubleUtil.GreaterThanOrClose(curOffset, topOffset))
                    {
                        container.IsSelected = true;

                        if (!_selectedElements.ContainsKey(container))
                            _selectedElements.Add(container, itemsControl.Items[i]);
                    }
                    else
                    {
                        container.IsSelected = false;

                        if (_selectedElements.ContainsKey(container))
                            _selectedElements.Remove(container);
                    }

                    if (!container.IsExpanded || !container.HasItems)
                        continue;

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
                    SelectRange(_latestSelectedContainer, container);
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
                    _latestSelectedContainer = _selectedElements.Count == 0 ? null : _selectedElements.Last().Key;

                if (!selected)
                    _latestSelectedContainer = container;

                SetSelectedItems();
            }

            IsChangingSelection = false;
        }

        #endregion
    }
}

