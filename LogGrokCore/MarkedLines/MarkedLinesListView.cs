using System.Windows;
using LogGrokCore.Controls;
using ListView = System.Windows.Controls.ListView;

namespace LogGrokCore.MarkedLines
{
    public class MarkedLinesListView : ListView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new LogListViewItem();
        }
    }
}