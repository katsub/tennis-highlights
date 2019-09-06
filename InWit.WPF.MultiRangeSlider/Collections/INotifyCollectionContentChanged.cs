using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace InWit.Core.Collections
{
    interface INotifyCollectionContentChanged : INotifyCollectionChanged
    {
        event NotifyCollectionContentChangedEventHandler CollectionContentChanged;
    }

    public delegate void NotifyCollectionContentChangedEventHandler(Object sender, PropertyChangedEventArgs e);
}
