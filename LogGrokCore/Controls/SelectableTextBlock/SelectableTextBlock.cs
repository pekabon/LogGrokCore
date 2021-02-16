using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LogGrokCore.Controls.SelectableTextBlock
{
    #pragma warning disable CS0169
    public class SelectableTextBlock : TextBlock
    {
        public static readonly DependencyProperty SelectionBrushProperty =
            TextBoxBase.SelectionBrushProperty.AddOwner(typeof(SelectableTextBlock), 
                new PropertyMetadata(GetDefaultSelectionTextBrush()));

        public static readonly DependencyProperty SelectedTextProperty = DependencyProperty.Register(
            "SelectedText", typeof(string), typeof(SelectableTextBlock), 
            new PropertyMetadata(string.Empty));
        
        public string SelectedText
        {
            get => (string) GetValue(SelectedTextProperty);
            set => SetValue(SelectedTextProperty, value);
        }

        private static Brush GetDefaultSelectionTextBrush()
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush(SystemColors.HighlightColor);
            solidColorBrush.Freeze();
            return solidColorBrush;
        }

        public Brush SelectionBrush
        {
            get => (Brush) GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        private bool _subscribedToSelectionChanges;

        static SelectableTextBlock()
        {
            FocusableProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata(true));
            TextEditorWrapper.RegisterCommandHandlers(typeof(SelectableTextBlock), true, true, true);

            // remove the focus rectangle around the control
            FocusVisualStyleProperty.OverrideMetadata(typeof(SelectableTextBlock), 
                new FrameworkPropertyMetadata((object?)null));
        }

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly TextEditorWrapper _editor;

        public SelectableTextBlock()
        {
            _editor = TextEditorWrapper.CreateFor(this);
            _editor.SelectionChanged += (sender, args) =>
            {
                SelectedText = _editor.GetSelectedText();
            };
        }
    }
}