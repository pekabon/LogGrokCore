using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AvalonDock.Controls;

namespace LogGrokCore.Controls
{
    public static class VisualTreeTools
    {
        public static IEnumerable<DependencyObject> GetDirectDescendants(this DependencyObject obj)
        {
            for (var i = 0; i< VisualTreeHelper.GetChildrenCount(obj); i++)
                yield return VisualTreeHelper.GetChild(obj, i);
        }

        public static IEnumerable<T> GetVisualChildren<T> (this DependencyObject obj) 
            where T : DependencyObject
        {
            foreach (DependencyObject descendant in GetDirectDescendants(obj))
            {
                if (descendant is T t)
                    yield return t;

                foreach (var d in GetVisualChildren<T>(descendant)) yield return d;
            }
        }

        public static T? GetItemUnderPoint<T>(this Visual obj, Point p) where T : class
        {
            HitTestFilterBehavior VisibilityHitTestFilter(DependencyObject target)
            {
                if (!(target is UIElement uiElement)) 
                    return HitTestFilterBehavior.Continue;
                if(!uiElement.IsHitTestVisible || !uiElement.IsVisible)
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                return HitTestFilterBehavior.Continue;
            }
            
            T? hitTestResult = null; 
            VisualTreeHelper.HitTest(obj, 
                VisibilityHitTestFilter,
                t =>
                {
                    var foundItem =  t.VisualHit.FindVisualAncestor<T>();
                    if (foundItem == null)
                        return HitTestResultBehavior.Continue;
                    hitTestResult = foundItem;
                    return HitTestResultBehavior.Stop;

                }, 
                new PointHitTestParameters(p));

            return hitTestResult;
        }
    }
}
