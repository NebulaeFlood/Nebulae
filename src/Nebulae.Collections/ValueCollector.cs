using Nebulae.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nebulae.Collections
{
    /// <summary>
    /// 收集器
    /// </summary>
    /// <typeparam name="T">收集的元素类型</typeparam>
    [DebuggerDisplay("Count = {_count}")]
    [DebuggerTypeProxy(typeof(ValueCollector<>.DebugView))]
    public ref struct ValueCollector<T>
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 获取元素数量
        /// </summary>
        public readonly int Count
        {
            get => _count;
        }

        /// <summary>
        /// 获取一个值，该值指示此列表是否为空
        /// </summary>
        public readonly bool IsEmpty
        {
            get => _count is 0;
        }

        #endregion


        /// <summary>
        /// 获取指定索引处的元素
        /// </summary>
        /// <param name="index">目标索引</param>
        /// <returns>指定索引处的元素。</returns>
        /// <remarks>索引范围由内部数组的长度决定，而非 <see cref="Count"/>。</remarks>
        public readonly T? this[uint index]
        {
            get => _items[index];
        }


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// 初始化 <see cref="ValueCollector{T}"/> 的新实例
        /// </summary>
        public ValueCollector()
        {
            _items = new T[4];
        }

        /// <summary>
        /// 初始化 <see cref="ValueCollector{T}"/> 的新实例
        /// </summary>
        /// <param name="capacity">初始容量</param>
        public ValueCollector(uint capacity)
        {
            _items = new T[capacity];
        }

        /// <summary>
        /// 初始化 <see cref="ValueCollector{T}"/> 的新实例
        /// </summary>
        /// <param name="items">要收集的元素</param>
        /// <remarks>
        /// <para>
        /// 此构造函数直接将 <paramref name="items"/> 作为内部数组且默认其不含空元素。
        /// </para>
        /// <para>
        /// 扩容后，传入的 <paramref name="items"/> 将不再作为内部数组。
        /// </para>
        /// </remarks>
        public ValueCollector(T[] items)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);

            _count = items.Length;
            _items = items;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 创建一个包含所有收集的元素的 <see cref="Memory{T}"/>
        /// </summary>
        /// <returns>包含所有此 <see cref="ValueCollector{T}"/> 收集的元素的 <see cref="Memory{T}"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Memory<T> AsMemory()
        {
            return new Memory<T>(_items, 0, _count);
        }

        /// <summary>
        /// 创建一个包含所有收集的元素的 <see cref="Span{T}"/>
        /// </summary>
        /// <returns>包含所有此 <see cref="ValueCollector{T}"/> 收集的元素的 <see cref="Span{T}"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            return new Span<T>(_items, 0, _count);
        }

        /// <summary>
        /// 收集元素
        /// </summary>
        /// <param name="item">要收集的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Collect(T item)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, CollectionHelpers.Grow(_count));
            }

            CollectionHelpers.Unsafe.Ref(_items, _count++) = item;
        }

        /// <summary>
        /// 收集一组元素
        /// </summary>
        /// <param name="items">要收集的元素</param>
        public void CollectRange(ICollection<T> items)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);

            if (_count == _items.Length || _count + items.Count > _items.Length)
            {
                Array.Resize(ref _items, CollectionHelpers.Grow(_count + items.Count));
            }

            items.CopyTo(_items, _count);
            _count += items.Count;
        }

        /// <summary>
        /// 收集一组元素
        /// </summary>
        /// <param name="items">要收集的元素</param>
        public void CollectRange(IEnumerable<T> items)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);

            foreach (var item in items)
            {
                Collect(item);
            }
        }

        /// <summary>
        /// 收集一组元素
        /// </summary>
        /// <param name="items">要收集的元素</param>
        public void CollectRange(scoped ReadOnlySpan<T> items)
        {
            if (_count == _items.Length || _count + items.Length > _items.Length)
            {
                Array.Resize(ref _items, CollectionHelpers.Grow(_count + items.Length));
            }

            items.CopyTo(new Span<T>(_items, _count, items.Length));
            _count += items.Length;
        }

        /// <summary>
        /// 将收集的元素复制到指定数组
        /// </summary>
        /// <param name="array">目标数组</param>
        /// <param name="arrayIndex">目标数组中的起始索引</param>
        public readonly void CopyTo(T[] array, int arrayIndex)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            ThrowHelpers.ThrowIfArgumentNegative(arrayIndex);
            Array.Copy(_items, 0, array, arrayIndex, _count);
        }

        /// <summary>
        /// 获取循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的枚举器。</returns>
        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 将 <see cref="ValueCollector{T}"/> 转换为数组
        /// </summary>
        /// <returns>此 <see cref="ValueCollector{T}"/> 用于收集元素的数组。</returns>
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
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private int _count;
        private T[] _items;

        #endregion


        /// <summary>
        /// <see cref="ValueCollector{T}"/> 的枚举器
        /// </summary>
        public ref struct Enumerator : IEnumerator<T>
        {
            /// <summary>
            /// 获取枚举器当前指向的元素
            /// </summary>
            public readonly T Current => _currentValue!;


            internal Enumerator(ValueCollector<T> collector)
            {
                _collector = collector;
            }


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            /// <summary>
            /// 释放枚举器占用的非托管资源
            /// </summary>
            public readonly void Dispose() { }

            /// <summary>
            /// 令枚举器指向下一个元素
            /// </summary>
            /// <returns>若枚举器成功指向下一个元素，返回 <see langword="true"/>；若将要指向集合末尾，返回 <see langword="false"/>。</returns>
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

            /// <summary>
            /// 重置枚举器到初始位置
            /// </summary>
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

            private readonly ValueCollector<T> _collector;

            #endregion
        }


        private sealed class DebugView(ValueCollector<T> collector)
        {
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items => _items;


            private readonly T[] _items = collector.ToArray();
        }
    }
}
