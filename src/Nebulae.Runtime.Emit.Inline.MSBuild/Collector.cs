using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nebulae.Runtime.Emit.Inline.MSBuild
{
    [DebuggerTypeProxy(typeof(CollectorDebugView<>))]
    internal sealed class Collector<T>(int capacity) : IEnumerable<T>
    {
        public int Count
        {
            get => _count;
        }


        public T this[int index]
        {
            get => _items[index];
        }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return new Span<T>(_items, 0, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Collect(T item)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _count << 1);
            }

            _items[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Collect(ICollection<T> items)
        {
            if (_count + items.Count > _items.Length)
            {
                Array.Resize(ref _items, Math.Max(_count + items.Count, _count << 1));
            }

            items.CopyTo(_items, _count);
            _count += items.Count;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public T[] ToArray()
        {
            if (_count < _items.Length)
            {
                Array.Resize(ref _items, _count);
            }

            return _items;
        }

        #endregion


        //------------------------------------------------------
        //
        //  IEnumerable
        //
        //------------------------------------------------------

        #region IEnumerable

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private int _count;
        private T[] _items = new T[capacity];

        #endregion


        public struct Enumerator : IEnumerator<T>
        {
            public readonly T Current => _currentValue!;


            internal Enumerator(Collector<T> collector)
            {
                _collector = collector;
            }


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            public readonly void Dispose() { }

            public bool MoveNext()
            {
                if (_currentIndex < _collector._count)
                {
                    _currentValue = _collector._items[_currentIndex++];
                    return true;
                }

                _currentValue = default;
                return false;
            }
            public void Reset()
            {
                _currentIndex = 0;
                _currentValue = default;
            }

            #endregion


            readonly object? IEnumerator.Current => _currentValue;


            #region Private Fields

            private int _currentIndex;
            private T? _currentValue;

            private readonly Collector<T> _collector;

            #endregion
        }
    }


    internal sealed class CollectorDebugView<T>(Collector<T> collector)
    {
#pragma warning disable IDE0305

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => collector.ToArray();
#pragma warning restore IDE0305
    }
}
