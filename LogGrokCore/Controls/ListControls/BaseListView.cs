﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace LogGrokCore.Controls.ListControls
{
    public abstract class BaseListView : System.Windows.Controls.ListView
    {
        protected BaseListView()
        {
            CommandBindings.Add(new CommandBinding(RoutedCommands.CopyToClipboard,
                (_, args) =>
                {
                    Trace.TraceInformation("CopyToClipboard.Execute");
                    CopySelectedItemsToClipboard();
                    args.Handled = true;
                },
                (_, args) =>
                {
                    args.CanExecute = GetSelectedItems().Any();
                    args.Handled = true;
                }));
        }
               
        private void CopySelectedItemsToClipboard()
        {
            var items = GetSelectedItems();
            
            var  text = new StringBuilder();
            foreach (var line in items)
            {
                if (line == null)
                    continue;
                
                _ = text.Append(line.ToString()?.TrimEnd());
                _ = text.Append("\r\n");
            }
            _ = text.Replace("\0", string.Empty);

            TextCopy.ClipboardService.SetText(text.ToString());
        }

        protected abstract IEnumerable<object?> GetSelectedItems();
    }
}