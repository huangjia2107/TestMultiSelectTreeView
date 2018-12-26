using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace TestMultiSelectTreeView.Helps
{
    public static class Utils
    {
        public static T FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            if (!(obj is Visual) && !(obj is Visual3D))
                return null;

            while (obj != null)
            {
                if (obj is T)
                    return obj as T;

                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }

            return null;
        }

    }
}
