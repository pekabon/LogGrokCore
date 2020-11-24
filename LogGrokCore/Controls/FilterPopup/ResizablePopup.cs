using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace LogGrokCore.Controls.FilterPopup
{
    [ContentProperty("Child")]
    public class ResizablePopup : Popup
    {
        static ResizablePopup()
        {
            var defaultMetadata = Popup.ChildProperty.GetMetadata(typeof(Popup));

            Popup.ChildProperty.OverrideMetadata(typeof(ResizablePopup),
                new FrameworkPropertyMetadata(
                    defaultMetadata.DefaultValue,
                    defaultMetadata.PropertyChangedCallback,
                    CoerceChild));
        }        
        
        public ResizablePopup()
        {
            void OnDragDelta(object _, RoutedEventArgs args)
            {
                if (!(args is DragDeltaEventArgs e) || 
                    !(e.OriginalSource is Thumb thumb) ||
                    thumb.Name != "PART_Thumb") return;
                Height = Math.Max(Height + e.VerticalChange, MinHeight);
                Width = Math.Max(Width + e.HorizontalChange, MinWidth);
            }
            
            void OnDragStarted(object _, RoutedEventArgs e)
            {
                if (e.OriginalSource is Thumb thumb && thumb.Name == "PART_Thumb")
                    thumb.Cursor = Cursors.SizeNWSE;
            }

            void OnDragCompleted(object _, RoutedEventArgs e)
            {
                if (e.OriginalSource is Thumb thumb && thumb.Name == "PART_Thumb")
                    thumb.Cursor = Cursors.SizeNWSE;
            }
   
            AddHandler(Thumb.DragStartedEvent, new RoutedEventHandler(OnDragStarted));
            AddHandler(Thumb.DragCompletedEvent, new RoutedEventHandler(OnDragCompleted));
            AddHandler(Thumb.DragDeltaEvent, new RoutedEventHandler(OnDragDelta));
        }

        private static object CoerceChild(DependencyObject _, object value)
        {
            return new ResizeablePopupContent {Content = value};
        }
    }
}
