using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace LogGrokCore.Controls
{
    public static class VisualTreeTools
    {
        public static IEnumerable<DependencyObject> GetDirectDescendands(this DependencyObject obj)
        {
            for (var i = 0; i< VisualTreeHelper.GetChildrenCount(obj); i++)
                yield return VisualTreeHelper.GetChild(obj, i);
        }

        public static IEnumerable<T> GetVisualChildren<T> (this DependencyObject obj) 
            where T : DependencyObject
        {
            foreach (DependencyObject descendant in GetDirectDescendands(obj))
            {
                if (descendant is T t)
                    yield return t;

                foreach (var d in GetVisualChildren<T>(descendant)) yield return d;
            }
        }
    }
}
