using System;
using System.ComponentModel;

namespace InWit.Core.Collections
{
    public class CollectionChangingEventArgs<T> : EventArgs
    {
        public CollectionChangingEventArgs(CollectionChangeAction action, T element)
        {
            Action = action;
            Element = element;

            Cancel = false;
        }

        public CollectionChangeAction Action { get; private set; }
        public T Element { get; private set; }
        public bool Cancel { get; set; }
    }
}
