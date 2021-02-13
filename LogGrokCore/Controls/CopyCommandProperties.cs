using System.Linq;
using System.Windows.Input;

namespace LogGrokCore.Controls
{
    public static class CopyCommandProperties
    {
        public static string Text { get; } = ApplicationCommands.Copy.Text;

        public static string GestureText { get; } =
            ApplicationCommands.Copy.InputGestures.OfType<KeyGesture>().First().DisplayString;
    }
}