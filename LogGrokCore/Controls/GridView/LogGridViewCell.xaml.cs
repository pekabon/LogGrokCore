using System;
using System.Windows;
using System.Windows.Controls;

namespace LogGrokCore.Controls.GridView
{
    internal record CellContentViewModel
    {
        public string Text { get; }
        
        public LineViewModel LineViewModel { get; }

        public CellContentViewModel(string text, LineViewModel lineViewModel) => (Text, LineViewModel) = (text, lineViewModel);
    }

    public class LogGridViewCellTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? NormalTemplate { get; set; }
        public DataTemplate? SelectableTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is  CellContentViewModel { LineViewModel: var lineViewModel })
            {
                return lineViewModel.Mode switch
                {
                    LogLineMode.Normal => NormalTemplate,
                    LogLineMode.Selectable => SelectableTemplate,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return base.SelectTemplate(item, container);
        }
    }

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

        public static readonly DependencyProperty LogLineModeProperty = DependencyProperty.Register(
            "LogLineMode", typeof(LogLineMode), typeof(LogGridViewCell), 
            new PropertyMetadata(default(LogLineMode)));

        public LogLineMode LogLineMode
        {
            get => (LogLineMode) GetValue(LogLineModeProperty);
            set => SetValue(LogLineModeProperty, value);
        }

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
                Content = new CellContentViewModel(ValueGetter(lineVm), lineVm);
        }
    }
}
