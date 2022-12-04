using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WpfBleApp.Widget
{
    public class AutoScrollListBox : ListBox
    {
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
            {
                return;
            }
            int count = e.NewItems.Count;
            if (count > 0)
            {
                ScrollIntoView(e.NewItems[count - 1]);
            }
            base.OnItemsChanged(e);
        }
    }
}
