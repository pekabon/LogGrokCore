using System;
using System.Windows;

namespace LogGrokCore.Controls.GridView
{
    public partial class LogGridViewCell
    {
        public LogGridViewCell()
        {
            InitializeComponent();
            DataContextChanged += (o, e) => UpdateValue();
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        public static DependencyProperty ValueGetterProperty = 
            DependencyProperty.Register(nameof(ValueGetter), 
                typeof(Func<LineViewModel, string>), 
                typeof(LogGridViewCell),
                new PropertyMetadata(OnValueGetterChanged));

        internal Func<LineViewModel, string>? ValueGetter
        {
            get => (Func<LineViewModel, string>?) GetValue(ValueGetterProperty);
            set => SetValue(ValueGetterProperty, value);
        }

        private static void OnValueGetterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is LogGridViewCell cell)
                cell.UpdateValue();
        }
        
        private void UpdateValue()
        {
            if (DataContext is LineViewModel lineVm && ValueGetter != null)
                Content = ValueGetter(lineVm);
        }
    }
}
