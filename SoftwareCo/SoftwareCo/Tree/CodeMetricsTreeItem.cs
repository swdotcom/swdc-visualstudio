using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SoftwareCo
{
    public class CodeMetricsTreeItem : TreeViewItem
    {

        public string ItemId { get; set; }
        public CodeMetricsTreeItem(string ItemId)
        {
            this.ItemId = ItemId;

        }
    }
}
