using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Controls.FilterPopup
{
    public static class TextBoxCommands
    {
        public static ICommand Clear = new DelegateCommand(
            textBox => ((TextBox)textBox).Text = string.Empty,
            textBox => !string.IsNullOrEmpty(((TextBox)textBox).Text));
    }
}
