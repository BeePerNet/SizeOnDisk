using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace SizeOnDisk.ViewModel
{
    public class NotifyCollection<T> : Collection<T>, INotifyCollectionChanged
    {
        public void OnCollectionChanged()
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
