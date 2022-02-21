using System;
using System.Collections.Generic;
using System.Windows;
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender;

public static class ClippingRectProviderBehavior
{
    private static readonly Dictionary<int, Dictionary<int, (Rect rect, WeakReference<IClippingRectChangesAware>)>>
        Subscriptions = new();

    public static readonly DependencyProperty ClippingRectProviderProperty = DependencyProperty.RegisterAttached(
        "ClippingRectProvider", typeof(FrameworkElement), typeof(ClippingRectProviderBehavior),
        new FrameworkPropertyMetadata(default(FrameworkElement),
            FrameworkPropertyMetadataOptions.Inherits, ClippingRectProviderChanged));
   
    public static Rect GetClippingRect(FrameworkElement container, FrameworkElement element)
    {
        var elementRect = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
        var containerRectInElementCoordinates = new Rect(
            container.TranslatePoint(new Point(0, 0), element),
            container.TranslatePoint(new Point(container.ActualWidth, container.ActualHeight), element));
        containerRectInElementCoordinates.Intersect(elementRect);
        return containerRectInElementCoordinates;
    }

    public static void SetClippingRectProvider(DependencyObject element, FrameworkElement value)
        => element.SetValue(ClippingRectProviderProperty, value);

    public static FrameworkElement? GetClippingRectProvider(DependencyObject element)
        => (FrameworkElement?)element.GetValue(ClippingRectProviderProperty);

    private static void ClippingRectProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not (IClippingRectChangesAware targetObject and FrameworkElement targetElement))
        {
            return;
        }

        foreach (var (_, subscriptionsList) in Subscriptions)
        {
            var targetHashCode = d.GetHashCode();
            _ = subscriptionsList.Remove(targetHashCode);
        }

        if (e.NewValue is not FrameworkElement containerElement)
        {
            return;
        }

        if (!Subscriptions.TryGetValue(containerElement.GetHashCode(), out var subscriptionList))
        {
            subscriptionList = new Dictionary<int, (Rect, WeakReference<IClippingRectChangesAware>)>();
            containerElement.LayoutUpdated += (_, _) =>
            {
                using var changedRects =
                    new PooledList<(int, (Rect, WeakReference<IClippingRectChangesAware>))>();
                
                using var toDelete = new PooledList<int>();

                foreach (var (code, (rect, weakRef)) in subscriptionList)
                {
                    if (!weakRef.TryGetTarget(out var target))
                    {
                        toDelete.Add(code);
                        continue;
                    }

                    var clippingRect = GetClippingRect(containerElement, (FrameworkElement)target);
                    if (rect != clippingRect)
                    {
                        changedRects.Add((code, (clippingRect, weakRef)));
                        target.OnChildRectChanged(rect);
                    }
                }

                foreach (var code in toDelete)
                {
                    _ = subscriptionList.Remove(code);
                }

                foreach (var (code, value) in changedRects)
                {
                    subscriptionList[code] = value;
                }
            };
            Subscriptions[containerElement.GetHashCode()] = subscriptionList;
        }

        var clippingRect = GetClippingRect(containerElement, targetElement);  
        subscriptionList[targetObject.GetHashCode()] =
            (clippingRect,
                new WeakReference<IClippingRectChangesAware>(targetObject));
        targetObject.OnChildRectChanged(clippingRect);
    }
}