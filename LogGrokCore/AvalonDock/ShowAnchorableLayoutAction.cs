using System.Windows;
using Microsoft.Xaml.Behaviors;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LogGrokCore.AvalonDock
{
    public class ShowAnchorableLayoutAction : TriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty DockingManagerProperty = DependencyProperty.Register(
            "DockingManager", typeof(DockingManager), typeof(ShowAnchorableLayoutAction), new PropertyMetadata(default(DockingManager)));

        public DockingManager DockingManager
        {
            get => (DockingManager) GetValue(DockingManagerProperty);
            set => SetValue(DockingManagerProperty, value);
        }

        public static readonly DependencyProperty LayoutAnchorableProperty = DependencyProperty.Register(
            "LayoutAnchorable", typeof(LayoutAnchorable), typeof(ShowAnchorableLayoutAction), new PropertyMetadata(default(LayoutAnchorable)));

        public LayoutAnchorable LayoutAnchorable
        {
            get => (LayoutAnchorable) GetValue(LayoutAnchorableProperty);
            set => SetValue(LayoutAnchorableProperty, value);
        }

        public static readonly DependencyProperty LayoutAnchorablePaneProperty = DependencyProperty.Register(
            "LayoutAnchorablePane", typeof(LayoutAnchorablePane), typeof(ShowAnchorableLayoutAction), new PropertyMetadata(default(LayoutAnchorablePane)));

        public LayoutAnchorablePane LayoutAnchorablePane
        {
            get => (LayoutAnchorablePane) GetValue(LayoutAnchorablePaneProperty);
            set => SetValue(LayoutAnchorablePaneProperty, value);
        }
        
        protected override void Invoke(object parameter)
        {
            if (LayoutAnchorable.Parent != null)
            {
                LayoutAnchorable.Show();
                return;
            }

            LayoutAnchorablePane.Children.Add(LayoutAnchorable);
            DockingManager.Layout.RootPanel.Children.Add(LayoutAnchorablePane);
        }
    }
}
