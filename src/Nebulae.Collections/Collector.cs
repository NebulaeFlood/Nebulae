using Nebulae.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Nebulae.Collections
{
    /// <summary>
    /// 收集器
    /// </summary>
    /// <typeparam name="T">收集的元素类型</typeparam>
    [DebuggerTypeProxy(typeof(CollectorDebugView<>))]
    public struct Collector<T> : IEnumerable<T>
    {
        /// <summary>
        /// 获取元素数量
        /// </summary>
        public readonly int Count
        {
            get => _count;
        }


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// 初始化 <see cref="Collector{T}"/> 的新实例
        /// </summary>
        public Collector()
        {
            _items = new T[4];
        }

        /// <summary>
        /// 初始化 <see cref="Collector{T}"/> 的新实例
        /// </summary>
        /// <param name="capacity">初始容量</param>
        public Collector(uint capacity)
        {
            _items = new T[capacity];
        }

        /// <summary>
        /// 初始化 <see cref="Collector{T}"/> 的新实例
        /// </summary>
        /// <param name="items">要收集的元素</param>
        /// <remarks>此构造函数默认 <paramref name="items"/> 是满的。</remarks>
        public Collector(T[] items)
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
        /// <returns>包含所有此 <see cref="Collector{T}"/> 收集的元素的 <see cref="Memory{T}"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Memory<T> AsMemory()
        {
            return new Memory<T>(_items, 0, _count);
        }

        /// <summary>
        /// 创建一个包含所有收集的元素的 <see cref="Span{T}"/>
        /// </summary>
        /// <returns>包含所有此 <see cref="Collector{T}"/> 收集的元素的 <see cref="Span{T}"/>。</returns>
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
        /// 将收集的元素复制到指定数组
        /// </summary>
        /// <param name="array">目标数组</param>
        /// <param name="arrayIndex">目标数组中的起始索引</param>
        public readonly void CopyTo(T[] array, int arrayIndex)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            ThrowHelpers.ThrowIfArgumentNegative(arrayIndex);

            int arrayLength = array.Length;

            CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(arrayIndex, arrayLength);
            CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(arrayIndex, arrayLength, _count);

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
        /// 将 <see cref="Collector{T}"/> 转换为数组
        /// </summary>
        /// <returns>此 <see cref="Collector{T}"/> 用于收集元素的数组。</returns>
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

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
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
        private T[] _items;

        #endregion


        /// <summary>
        /// <see cref="Collector{T}"/> 的枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            /// <summary>
            /// 获取枚举器当前指向的元素
            /// </summary>
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

            private readonly Collector<T> _collector;

            #endregion
        }
    }


    internal sealed class CollectorDebugView<T>(Collector<T> collector)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
#pragma warning disable IDE0305
        public T[] Items => collector.ToArray();
#pragma warning restore IDE0305
    }
}
