using System;
using System.Collections;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// An array nobody can change after it is made: the copy is taken at construction and never handed out. Used for the
    /// shared static tables of the sim (ware groups, recipe amounts, directions) so that two sims in one process can never
    /// edit each other's rules (M0 static audit). The indexer is a plain array read, so hot loops stay fast.
    /// </summary>
    public sealed class ReadOnlyArray<T> : IReadOnlyList<T>
    {
        private readonly T[] _items;

        public ReadOnlyArray(params T[] items)
        {
            _items = items == null ? Array.Empty<T>() : (T[])items.Clone();
        }

        public ReadOnlyArray(IEnumerable<T> items)
        {
            _items = items == null ? Array.Empty<T>() : new List<T>(items).ToArray();
        }

        public T this[int index] => _items[index];
        public int Length => _items.Length;
        public int Count => _items.Length;

        public int IndexOf(T item) => Array.IndexOf(_items, item);
        public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;

        /// <summary>A fresh copy the caller may change.</summary>
        public T[] ToArray() => (T[])_items.Clone();

        /// <summary>Allocation-free foreach.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_items);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public struct Enumerator
        {
            private readonly T[] _a;
            private int _i;
            internal Enumerator(T[] a) { _a = a; _i = -1; }
            public bool MoveNext() => ++_i < _a.Length;
            public T Current => _a[_i];
        }
    }
}
