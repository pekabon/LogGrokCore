using System.Collections.Generic;
using System.Windows;

namespace LogGrokCore.Controls
{
    public static class NavigateToLineBehavior
    {
        public static DependencyProperty NavigateToLineRequestProperty =
            DependencyProperty.RegisterAttached(
                "NavigateToLineRequest",
                typeof(NavigateToLineRequest), 
                typeof(NavigateToLineBehavior),
                new PropertyMetadata(null, OnRequestChanged));

        public static void SetNavigateToLineRequest(ListView listView, NavigateToLineRequest request)
        {
            listView.SetValue(NavigateToLineRequestProperty, request);
        }

        public static NavigateToLineRequest? GetNavigateToLineRequest(ListView listView)
        {
            return (NavigateToLineRequest) listView.GetValue(NavigateToLineRequestProperty);
        }

        private static void OnRequestChanged(DependencyObject? d , DependencyPropertyChangedEventArgs args)
        {
            if (!(d is ListView listView)) return;
            
            if (args.OldValue != null)
            {
                var request = (NavigateToLineRequest) (args.OldValue);
                SubscribersMap.Remove(request);
            }

            if (args.NewValue != null)
            {
                var request = (NavigateToLineRequest) (args.NewValue);
                SubscribersMap[request] = listView;
                request.Navigate += i => RequestNavigate(request, i);
            }
        }

        private static void RequestNavigate(NavigateToLineRequest request, int lineNumber)
        {
            var listView = SubscribersMap[request];
            listView.BringIndexIntoView(lineNumber);
        }

        private static readonly Dictionary<NavigateToLineRequest, ListView> SubscribersMap 
            = new();
    }
}
