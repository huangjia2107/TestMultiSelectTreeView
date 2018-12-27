using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using TestMultiSelectTreeView.Controls;

namespace TestMultiSelectTreeView.Helps
{
    public class TopLineVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Contains(null) || values.Contains(DependencyProperty.UnsetValue))
                return Visibility.Hidden;

            var item = (HeaderedItemsControl)values[0];
            var tag = int.Parse(values[1].ToString());

            if (!item.IsVisible || tag == 1)
                return Visibility.Hidden;

            var ic = ItemsControl.ItemsControlFromItemContainer(item);
            if (ic == null || ic is MultiSelectTreeView)
                return Visibility.Hidden;

            var curIndex = ic.ItemContainerGenerator.IndexFromContainer(item);

            int i = curIndex - 1;
            for (; i >= 0; i--)
            {
                var curItem = (HeaderedItemsControl)ic.ItemContainerGenerator.ContainerFromItem(ic.Items[i]);
                if (curItem.IsVisible)
                    return Visibility.Visible;
            }

            return Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BottomLineVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Contains(null) || values.Contains(DependencyProperty.UnsetValue))
                return Visibility.Hidden;

            var item = (HeaderedItemsControl)values[0];
            var tag = int.Parse(values[1].ToString());

            if (!item.IsVisible || tag == 1)
                return Visibility.Hidden;

            var ic = ItemsControl.ItemsControlFromItemContainer(item);
            if (ic == null || ic is MultiSelectTreeView)
                return Visibility.Hidden;

            var curIndex = ic.ItemContainerGenerator.IndexFromContainer(item);

            int i = curIndex + 1;
            for (; i < ic.Items.Count; i++)
            {
                var curItem = (HeaderedItemsControl)ic.ItemContainerGenerator.ContainerFromItem(ic.Items[i]);
                if (curItem.IsVisible)
                    return Visibility.Visible;
            }

            return Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MiddleLineVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Contains(null) || values.Contains(DependencyProperty.UnsetValue))
                return Visibility.Hidden;

            var item = (HeaderedItemsControl)values[0];
            var tag = int.Parse(values[1].ToString());

            if (!item.IsVisible || tag == 1)
                return Visibility.Hidden;

            var ic = ItemsControl.ItemsControlFromItemContainer(item);
            if (ic == null || ic.Items.Count == 1 || ic is MultiSelectTreeView)
                return Visibility.Hidden;

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FirstOrLastItemMarginMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Contains(null) || values.Contains(DependencyProperty.UnsetValue))
                return new Thickness(); ;

            var item = (HeaderedItemsControl)values[0];

            var ic = ItemsControl.ItemsControlFromItemContainer(item);
            if (ic != null)
            {
                var index = ic.ItemContainerGenerator.IndexFromContainer(item);

                if (index == 0)
                    return new Thickness(0, 0, 0, 5);

                if (index == ic.Items.Count - 1)
                    return new Thickness(0, 5, 0, 0);
            }

            return new Thickness(0, 5, 0, 5);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
