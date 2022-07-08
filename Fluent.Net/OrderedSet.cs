using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Fluent.Net
{
    class OrderedSet<T> : ICollection<T>
    {
        readonly Dictionary<T, LinkedListNode<T>> _valuesByKey =
            new Dictionary<T, LinkedListNode<T>>();
        readonly LinkedList<T> _values = new LinkedList<T>();

        public int Count => _values.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (!_valuesByKey.ContainsKey(item))
            {
                _valuesByKey.Add(item, _values.AddLast(item));
            }
        }

        public void Clear()
        {
            _values.Clear();
            _valuesByKey.Clear();
        }

        public bool Contains(T item) => _valuesByKey.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public bool Remove(T item)
        {
            if (_valuesByKey.TryGetValue(item, out LinkedListNode<T> entry))
            {
                _valuesByKey.Remove(item);
                _values.Remove(entry);
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_values).GetEnumerator();
        }
    }
}
