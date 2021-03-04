using System.Windows;
using System.Windows.Controls;
using LogGrokCore.Controls;
using ListView = LogGrokCore.Controls.ListView;

namespace LogGrokCore.MarkedLines
{
    public class MarkedLinesListView : ListView
    {
        public override ListViewItem GetContainerForItem()
        {
            return new LogListViewItem();
        }

      
    }
}