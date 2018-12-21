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
        class ContainerItemPair
        {
            public MultiSelectTreeViewItem Container { get; set; }
            public object Item { get; set; }

            public ContainerItemPair(MultiSelectTreeViewItem container, object item)
            {
                Container = container;
                Item = item;
            }
        }

        private const string ScrollHostPartName = "PART_ScrollHost";
        private ScrollViewer _scrollHost = null;

        private readonly List<ContainerItemPair> _selectedElements = null;
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
            _selectedElements = new List<ContainerItemPair>();
        }

        #endregion

        #region Events

        public static readonly RoutedEvent SelectedItemChangedEvent = EventManager.RegisterRoutedEvent("SelectedItemChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(MultiSelectTreeView));
        public event RoutedPropertyChangedEventHandler<object> SelectedItemChanged
        {
            add { AddHandler(SelectedItemChangedEvent, value); }
            remove { RemoveHandler(SelectedItemChangedEvent, value); }
        }

        protected virtual void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            RaiseEvent(e);
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(SelectionMode), typeof(MultiSelectTreeView), new UIPropertyMetadata(SelectionMode.Extended));
        public SelectionMode SelectionMode
        {
            get { return (SelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        private readonly static DependencyPropertyKey SelectedItemPropertyKey =
            DependencyProperty.RegisterReadOnly("SelectedItem", typeof(object), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(null));

        public readonly static DependencyProperty SelectedItemProperty = SelectedItemPropertyKey.DependencyProperty;
        public object SelectedItem
        {
            get { return GetValue(TreeView.SelectedItemProperty); }
        }

        private static readonly DependencyPropertyKey SelectedItemsPropertyKey =
            DependencyProperty.RegisterReadOnly("SelectedItems", typeof(IList), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata((IList)null));

        private static readonly DependencyProperty SelectedItemsProperty = SelectedItemsPropertyKey.DependencyProperty;
        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
        }

        private void SetSelectedItems()
        {
            var newSelecedItem = (_selectedElements == null || _selectedElements.Count == 0) ? null : _selectedElements[0].Item;
            var oldSelectedItem = (SelectedItems == null || SelectedItems.Count == 0) ? null : SelectedItems[0];

            SetValue(SelectedItemsPropertyKey, _selectedElements.Select(p => p.Item).ToList());

            if (oldSelectedItem != newSelecedItem)
            {
                SetValue(SelectedItemPropertyKey, newSelecedItem);
                OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<object>(oldSelectedItem, newSelecedItem, SelectedItemChangedEvent));
            }
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

        //called before a container is used
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
        }

        //called when a container is thrown away or recycled
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            var container = element as MultiSelectTreeViewItem;

            if (container != null)
                RemoveSelectedElement(container, true);
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
                        _selectedElements.Add(new ContainerItemPair(container, itemsControl.Items[i]));

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

        private bool RemoveFromSelectedElements(MultiSelectTreeViewItem treeViewItem)
        {
            var index = _selectedElements.FindIndex(p => p.Container == treeViewItem);

            if (index >= 0)
            {
                if (treeViewItem.IsSelected)
                    treeViewItem.IsSelected = false;

                _selectedElements.RemoveAt(index);
                return true;
            }

            return false;
        }

        private bool AddToSelectedElements(MultiSelectTreeViewItem treeViewItem, object item)
        {
            var index = _selectedElements.FindIndex(p => p.Container == treeViewItem);

            if (index < 0)
            {
                if (!treeViewItem.IsSelected)
                    treeViewItem.IsSelected = true;

                _selectedElements.Add(new ContainerItemPair(treeViewItem, item));
                return true;
            }

            return false;
        }

        private void ForearchTreeViewItem(MultiSelectTreeViewItem treeViewItem, bool walkIntoSubtree, Action<MultiSelectTreeViewItem> doAction)
        {
            if (doAction != null)
                doAction(treeViewItem);

            if (!treeViewItem.HasItems || !walkIntoSubtree)
                return;

            for (int i = 0; i < treeViewItem.Items.Count; i++)
            {
                var container = treeViewItem.ItemContainerGenerator.ContainerFromIndex(i) as MultiSelectTreeViewItem;
                if (container != null && doAction != null)
                {
                    doAction(container);
                    ForearchTreeViewItem(container, walkIntoSubtree, doAction);
                }
            }
        }

        internal void RemoveSelectedElement(MultiSelectTreeViewItem treeViewItem, bool walkIntoSubtree)
        {
            bool flag = false;
            ForearchTreeViewItem(treeViewItem, walkIntoSubtree,
                container =>
                {
                    if (RemoveFromSelectedElements(container))
                        flag = true;
                });

            if (flag)
                SetSelectedItems();
        }

        internal void RemoveSelectedElementsWithInvalidContainer(bool isUpdateSelection)
        {
            var flag = false;
            for (int i = 0; i < _selectedElements.Count; i++)
            {
                var pair = _selectedElements[i];

                var itemsControl = ItemsControl.ItemsControlFromItemContainer(pair.Container);
                if (itemsControl == null)
                {
                    _selectedElements.RemoveAt(i);
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
                        if (!container.IsSelected)
                            container.IsSelected = true;

                        AddToSelectedElements(container, itemsControl.Items[i]);
                    }
                    else
                    {
                        if (container.IsSelected)
                            container.IsSelected = false;

                        RemoveFromSelectedElements(container);
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

            var mode = SelectionMode == SelectionMode.Multiple ? SelectionMode.Multiple : (SelectionMode)Math.Min((int)SelectionMode, (int)selectionMode);

            if (selected)
            {
                if (mode == SelectionMode.Single)
                {
                    _selectedElements.ForEach(p => p.Container.IsSelected = false);
                    _selectedElements.Clear();
                }

                if (mode == SelectionMode.Extended)
                {
                    SelectRange(_latestSelectedContainer, container);
                    flag = true;
                }
                else
                {
                    var pair = _selectedElements.FirstOrDefault(p => p.Container == container);
                    if (pair == null)
                    {
                        if (!container.IsSelected)
                            container.IsSelected = true;

                        _selectedElements.Add(new ContainerItemPair(container, data));
                        flag = true;
                    }
                    else if (pair.Item != data)
                    {
                        pair.Item = data;
                        flag = true;
                    }
                }
            }
            else
            {
                if (RemoveFromSelectedElements(container))
                    flag = true;
            }

            if (flag)
            {
                if (selected && mode != SelectionMode.Extended)
                    _latestSelectedContainer = (_selectedElements == null || _selectedElements.Count == 0) ? null : _selectedElements.Last().Container;

                if (!selected)
                    _latestSelectedContainer = container;

                SetSelectedItems();
            }

            IsChangingSelection = false;
        }

        #endregion
    }
}

