using Nebulae.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nebulae.Collections
{
    /// <summary>
    /// 数组列表存储
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <remarks>名称以 <c>Raw</c> 开头的方法不检查参数、索引或集合状态，调用方必须保证所有前置条件成立。</remarks>
    [DebuggerDisplay("Count = {count}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public abstract class ArrayStorageBase<T> : IEnumerable<T>, ICollectionDebugView<T>
    {
        private const int DefaultCapacity = 4;


        //------------------------------------------------------
        //
        //  Protected Internal Fields
        //
        //------------------------------------------------------

        #region Protected Internal Fields

        /// <summary>
        /// 列表元素数量
        /// </summary>
        protected internal int count;

        /// <summary>
        /// 列表元素数组
        /// </summary>
        protected internal T[] items;

        #endregion


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 获取该列表中包含元素的数量
        /// </summary>
        public int Count
        {
            get => count;
        }

        /// <summary>
        /// 获取一个值，该值指示此列表是否为空
        /// </summary>
        public bool IsEmpty
        {
            get => count is 0;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// 为 <see cref="ArrayStorageBase{T}"/> 派生类实现基本初始化
        /// </summary>
        protected ArrayStorageBase()
        {
            items = new T[DefaultCapacity];
        }

        /// <summary>
        /// 为 <see cref="ArrayStorageBase{T}"/> 派生类实现基本初始化
        /// </summary>
        /// <param name="capacity">列表的初始容量</param>
        protected ArrayStorageBase(int capacity)
        {
            ThrowHelpers.ThrowIfArgumentNegative(capacity);
            items = capacity is 0 ? [] : new T[capacity];
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 复制列表内的元素到目标数组
        /// </summary>
        /// <param name="array">接收列表内元素的数组</param>
        public void CopyTo(T[] array)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            Array.Copy(items, 0, array, 0, count);
        }

        /// <summary>
        /// 从目标数组的指定索引处开始，复制列表内的元素
        /// </summary>
        /// <param name="array">接收列表内元素的数组</param>
        /// <param name="arrayIndex">开始复制的索引</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            Array.Copy(items, 0, array, arrayIndex, count);
        }

        /// <summary>
        /// 从目标数组的指定索引处开始，复制列表内指定索引处开始的元素
        /// </summary>
        /// <param name="index">列表中开始复制的索引</param>
        /// <param name="array">接收列表内元素的数组</param>
        /// <param name="arrayIndex">开始复制的索引</param>
        /// <param name="count">要复制的元素数量</param>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            ThrowHelpers.ThrowIfArgumentNegative(count);
            CollectionHelpers.ThrowHelpers.ThrowIfCollectionElementNotEnough(this.count, count);
            CollectionHelpers.ThrowHelpers.ThrowIfCollectionElementNotEnough(this.count, index, count);
            Array.Copy(items, index, array, arrayIndex, count);
        }

        /// <summary>
        /// 获取循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的枚举器。</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 将列表的元素复制到新数组中
        /// </summary>
        /// <returns>一个包含列表中所有元素的数组。</returns>
        public T[] ToArray()
        {
            return new Span<T>(items, 0, count).ToArray();
        }

        /// <summary>
        /// 将列表中指定索引处开始的元素复制到新数组中
        /// </summary>
        /// <param name="index">列表中开始复制的索引</param>
        /// <param name="count">要复制的元素数量</param>
        /// <returns>一个包含指定范围内元素的数组。</returns>
        public T[] ToArray(int index, int count)
        {
            ThrowHelpers.ThrowIfArgumentNegative(count);
            CollectionHelpers.ThrowHelpers.ThrowIfCollectionElementNotEnough(this.count, count);
            CollectionHelpers.ThrowHelpers.ThrowIfCollectionElementNotEnough(this.count, index, count);
            return new Span<T>(items, index, count).ToArray();
        }

        #endregion


        //------------------------------------------------------
        //
        //  IEnumerable
        //
        //------------------------------------------------------

        #region IEnumerable

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        #endregion


        //------------------------------------------------------
        //
        //  Protected Internal Methods
        //
        //------------------------------------------------------

        #region Protected Internal Methods

        /// <summary>
        /// 将元素添加到指定索引之后
        /// </summary>
        /// <param name="index">目标索引</param>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddAfter(int index, T item)
        {
            RawAddBefore(index + 1, item);
        }

        /// <summary>
        /// 将元素添加到指定索引之前
        /// </summary>
        /// <param name="index">目标索引</param>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddBefore(int index, T item)
        {
            if (count == items.Length)
            {
                Array.Resize(ref items, CollectionHelpers.Grow(items.Length));
            }

            Array.Copy(items, index, items, index + 1, count - index);

            items[index] = item;
            count++;
        }

        /// <summary>
        /// 将元素添加到列表的头部
        /// </summary>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddFirst(T item)
        {
            if (count == items.Length)
            {
                Array.Resize(ref items, CollectionHelpers.Grow(items.Length));
            }

            Array.Copy(items, 0, items, 1, count);

            items[0] = item;
            count++;
        }

        /// <summary>
        /// 将元素添加到列表的尾部
        /// </summary>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddLast(T item)
        {
            if (count == items.Length)
            {
                Array.Resize(ref items, CollectionHelpers.Grow(items.Length));
            }

            items[count++] = item;
        }

        /// <summary>
        /// 将指定索引处的元素移动到列表的头部
        /// </summary>
        /// <param name="index">目标索引</param>
        protected internal void RawMoveToHead(int index)
        {
            if (index == 0)
            {
                return;
            }

            T item = items[index];

            Array.Copy(items, 0, items, 1, index);
            items[0] = item;
        }

        /// <summary>
        /// 将指定索引处的元素移动到列表的尾部
        /// </summary>
        /// <param name="index">目标索引</param>
        protected internal void RawMoveToTail(int index)
        {
            if (index == count - 1)
            {
                return;
            }

            T item = items[index];

            Array.Copy(items, index + 1, items, index, count - index - 1);
            items[count - 1] = item;
        }

        /// <summary>
        /// 移除列表指定索引处的元素
        /// </summary>
        /// <param name="index">目标索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawRemoveAt(int index)
        {
            count--;

            if (index < count)
            {
                Array.Copy(items, index + 1, items, index, count - index);
            }

#if !NETSTANDARD2_0
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                items[count] = default!;
            }
#else
            items[count] = default!;
#endif
        }

        /// <summary>
        /// 清空列表中的元素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawReset()
        {
#if !NETSTANDARD2_0
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(items, 0, count);
            }
#else
            Array.Clear(items, 0, count);
#endif
            count = 0;
        }

#endregion


        /// <summary>
        /// <see cref="ArrayStorageBase{T}"/> 子类的枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            /// <summary>
            /// 获取枚举器当前指向的元素
            /// </summary>
            public readonly T Current => _currentValue!;


            internal Enumerator(ArrayStorageBase<T> list)
            {
                _list = list;
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
                if (_currentIndex < _list.count)
                {
                    _currentValue = _list.items[_currentIndex++];
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

            private readonly ArrayStorageBase<T> _list;

            #endregion
        }
    }
}
