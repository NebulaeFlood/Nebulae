using Nebulae.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nebulae.Collections
{
    /// <summary>
    /// 双向链表存储
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <remarks>名称以 <c>Raw</c> 开头的方法不检查参数、索引、节点归属或集合状态，用方必须保证所有前置条件成立。</remarks>
    [DebuggerDisplay("Count = {count}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public abstract class LinkedStorageBase<T> : IEnumerable<T>, ICollectionDebugView<T>
    {
        //------------------------------------------------------
        //
        //  Protected Internal Fields
        //
        //------------------------------------------------------

        #region Protected Internal Fields

        /// <summary>
        /// 链表元素数量
        /// </summary>
        protected internal int count;

        /// <summary>
        /// 链表的头节点
        /// </summary>
        protected internal Node? head;

        /// <summary>
        /// 链表的尾节点
        /// </summary>
        protected internal Node? tail;

        #endregion


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 获取该链表中包含元素的数量
        /// </summary>
        public int Count
        {
            get => count;
        }

        /// <summary>
        /// 获取一个值，该值指示此链表是否为空
        /// </summary>
        public bool IsEmpty
        {
            get => head is null;
        }

        #endregion


        /// <summary>
        /// 为 <see cref="LinkedStorageBase{T}"/> 派生类实现基本初始化
        /// </summary>
        protected LinkedStorageBase() { }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 从目标数组的指定索引处开始，复制链表内的元素
        /// </summary>
        /// <param name="array">接收链表内元素的数组</param>
        /// <param name="arrayIndex">开始复制的索引</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            ThrowHelpers.ThrowIfArgumentNegative(arrayIndex);

            int arrayLength = array.Length;

            CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(arrayIndex, arrayLength);
            CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(arrayIndex, arrayLength, count);

            for (var node = head; node is not null; node = node.Next)
            {
                array[arrayIndex++] = node.Item;
            }
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
        /// 将链表的元素复制到新数组中
        /// </summary>
        /// <returns>一个包含链表中所有元素的数组。</returns>
        public T[] ToArray()
        {
            if (count < 1)
            {
                return [];
            }

            var array = new T[count];
            int index = 0;

            for (var node = head; node is not null; node = node.Next)
            {
                array[index++] = node.Item;
            }

            return array;
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
        /// 将元素添加到指定节点之后
        /// </summary>
        /// <param name="index">目标节点</param>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddAfter(Node index, T item)
        {
            var node = new Node(index, item, index.Next);

            if (index.Next is null)
            {
                tail = node;
            }
            else
            {
                index.Next.Prev = node;
            }

            index.Next = node;
            count++;
        }

        /// <summary>
        /// 将节点添加到指定节点之后
        /// </summary>
        /// <param name="index">作为索引的节点</param>
        /// <param name="node">要添加的节点</param>
        /// <remarks>需保证 <paramref name="node"/> 不在该链表中。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddAfter(Node index, Node node)
        {
            if (index.Next is null)
            {
                tail = node;
            }
            else
            {
                index.Next.Prev = node;
                node.Next = index.Next;
            }

            index.Next = node;
            node.Prev = index;

            count++;
        }

        /// <summary>
        /// 将元素添加到指定节点之前
        /// </summary>
        /// <param name="node">目标节点</param>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddBefore(Node node, T item)
        {
            var newNode = new Node(node.Prev, item, node);

            if (node.Prev is null)
            {
                head = newNode;
            }
            else
            {
                node.Prev.Next = newNode;
            }

            node.Prev = newNode;
            count++;
        }

        /// <summary>
        /// 将节点添加到指定节点之前
        /// </summary>
        /// <param name="index">作为索引的节点</param>
        /// <param name="node">要添加的节点</param>
        /// <remarks>需保证 <paramref name="node"/> 不在该链表中。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddBefore(Node index, Node node)
        {
            if (index.Prev is null)
            {
                head = node;
            }
            else
            {
                index.Prev.Next = node;
                node.Prev = index.Prev;
            }

            index.Prev = node;
            node.Next = index;

            count++;
        }

        /// <summary>
        /// 将元素添加到链表的头部
        /// </summary>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddFirst(T item)
        {
            var node = new Node(item, head);

            if (head is null)
            {
                head = node;
                tail = node;
            }
            else
            {
                head.Prev = node;
                head = node;
            }

            count++;
        }

        /// <summary>
        /// 将节点添加到链表的头部
        /// </summary>
        /// <param name="node">要添加的节点</param>
        /// <remarks>需保证 <paramref name="node"/> 不在该链表中。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddFirst(Node node)
        {
            if (head is null)
            {
                head = node;
                tail = node;
            }
            else
            {
                node.Next = head;

                head.Prev = node;
                head = node;
            }

            count++;
        }

        /// <summary>
        /// 将元素添加到链表的尾部
        /// </summary>
        /// <param name="item">要添加的元素</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddLast(T item)
        {
            var node = new Node(tail, item);

            if (tail is null)
            {
                head = node;
                tail = node;
            }
            else
            {
                tail.Next = node;
                tail = node;
            }

            count++;
        }

        /// <summary>
        /// 将节点添加到链表的尾部
        /// </summary>
        /// <param name="node">要添加的节点</param>
        /// <remarks>需保证 <paramref name="node"/> 不在该链表中。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawAddLast(Node node)
        {
            if (tail is null)
            {
                head = node;
                tail = node;
            }
            else
            {
                node.Prev = tail;

                tail.Next = node;
                tail = node;
            }

            count++;
        }

        /// <summary>
        /// 将节点从链表中移除
        /// </summary>
        /// <param name="node">要移除的节点</param>
        /// <remarks>需保证链表包含 <paramref name="node"/>。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawDetach(Node node)
        {
            if (node.Prev is null)
            {
                head = node.Next;
            }
            else
            {
                node.Prev.Next = node.Next;
            }

            if (node.Next is null)
            {
                tail = node.Prev;
            }
            else
            {
                node.Next.Prev = node.Prev;
            }

            node.Prev = null;
            node.Next = null;

            count--;
        }

        /// <summary>
        /// 清空链表中的元素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawReset()
        {
            head = null;
            tail = null;

            count = 0;
        }

        /// <summary>
        /// 使用指定的方法对链表中的元素排序
        /// </summary>
        /// <param name="comparison">比较元素时使用的方法</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawSort(Comparison<T> comparison)
        {
            if (count < 2)
            {
                return;
            }

            int half = count >> 1;

            var dummy = new Node { Next = head };
            Node lastGroupTail = dummy;

            for (int step = 1; step < count; step = step > half ? count : step << 1)
            {
                lastGroupTail = dummy;
                Node? currentHead = dummy.Next;

                while (currentHead is not null)
                {
                    var left = MergeSort.Split(
                        currentHead,
                        step,
                        out var rest);

                    var right = MergeSort.Split(
                        rest,
                        step,
                        out rest);

                    lastGroupTail = MergeSort.Merge(
                        lastGroupTail,
                        left,
                        right,
                        comparison);

                    currentHead = rest;
                }
            }

            head = dummy.Next;
            head!.Prev = null;

            tail = lastGroupTail;
            tail.Next = null;
        }

        /// <summary>
        /// 使用指定的比较器对链表中的元素排序
        /// </summary>
        /// <param name="comparer">比较元素时使用的比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void RawSort(IComparer<T> comparer)
        {
            if (count < 2)
            {
                return;
            }

            int half = count >> 1;

            var dummy = new Node { Next = head };
            Node lastGroupTail = dummy;

            for (int step = 1; step < count; step = step > half ? count : step << 1)
            {
                lastGroupTail = dummy;
                Node? currentHead = dummy.Next;

                while (currentHead is not null)
                {
                    var left = MergeSort.Split(
                        currentHead,
                        step,
                        out var rest);

                    var right = MergeSort.Split(
                        rest,
                        step,
                        out rest);

                    lastGroupTail = MergeSort.Merge(
                        lastGroupTail,
                        left,
                        right,
                        comparer);

                    currentHead = rest;
                }
            }

            head = dummy.Next;
            head!.Prev = null;

            tail = lastGroupTail;
            tail.Next = null;
        }

        #endregion


        /// <summary>
        /// <see cref="LinkedStorageBase{T}"/> 子类的枚举器
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            /// <summary>
            /// 获取枚举器当前指向的元素
            /// </summary>
            public readonly T Current => _currentValue!;


            internal Enumerator(LinkedStorageBase<T> list)
            {
                _currentNode = list.head;
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
                if (_currentNode is not null)
                {
                    _currentValue = _currentNode.Item;
                    _currentNode = _currentNode.Next;

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
                _currentNode = _list.head;
                _currentValue = default;
            }

            #endregion


            readonly object? IEnumerator.Current => _currentValue;


            #region Private Fields

            private Node? _currentNode;
            private T? _currentValue;

            private readonly LinkedStorageBase<T> _list;

            #endregion
        }


        /// <summary>
        /// <see cref="LinkedStorageBase{T}"/> 的节点
        /// </summary>
        protected internal sealed class Node
        {
            //------------------------------------------------------
            //
            //  Public Fields
            //
            //------------------------------------------------------

            #region Public Fields

            /// <summary>
            /// 节点元素
            /// </summary>
            [AllowNull]
            public T Item;

            /// <summary>
            /// 前驱节点
            /// </summary>
            public Node? Prev;

            /// <summary>
            /// 后继节点
            /// </summary>
            public Node? Next;

            #endregion


            /// <summary>
            /// 初始化 <see cref="Node"/> 的新实例
            /// </summary>
            /// <param name="item">节点元素</param>
            public Node(T item)
            {
                Item = item;
            }


            //------------------------------------------------------
            //
            //  Internal Constructors
            //
            //------------------------------------------------------

            #region Internal Constructors

            internal Node() { }

            internal Node(T item, Node? next)
            {
                Item = item;

                Next = next;
            }

            internal Node(Node? prev, T item)
            {
                Item = item;

                Prev = prev;
            }

            internal Node(Node? prev, T item, Node? next)
            {
                Item = item;

                Prev = prev;
                Next = next;
            }

            #endregion


            //------------------------------------------------------
            //
            //  Basic Methods
            //
            //------------------------------------------------------

            #region Basic Methods

            /// <summary>
            /// 判断指定对象是否等于当前对象
            /// </summary>
            /// <param name="obj">要比较的对象</param>
            /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
            public override bool Equals(object? obj)
            {
                return obj is Node other
                    && Prev == other.Prev
                    && Next == other.Next
                    && EqualityComparer<T>.Default.Equals(Item, other.Item);
            }

            /// <summary>
            /// 判断指定对象是否等于当前对象
            /// </summary>
            /// <param name="other">要比较的对象</param>
            /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
            public bool Equals(Node? other)
            {
                return other is not null
                    && Prev == other.Prev
                    && Next == other.Next
                    && EqualityComparer<T>.Default.Equals(Item, other.Item);
            }

            /// <summary>
            /// 获取当前对象的哈希代码
            /// </summary>
            /// <returns>当前对象的哈希代码。</returns>
            public override int GetHashCode()
            {
                return HashCode.Combine(Prev, Item, Next);
            }

            /// <summary>
            /// 获取表示当前对象的字符串
            /// </summary>
            /// <returns>表示当前对象的字符串。</returns>
            public override string ToString()
            {
                return Item.AsLog();
            }

            #endregion


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            /// <summary>
            /// 判断该节点是否在另一个节点之后
            /// </summary>
            /// <param name="node">要比较的节点</param>
            /// <returns>若该节点位于 <paramref name="node"/> 之后，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
            public bool After(Node? node)
            {
                while (node is not null)
                {
                    if (this == node.Next)
                    {
                        return true;
                    }

                    node = node.Next;
                }

                return false;
            }

            /// <summary>
            /// 判断该节点是否在另一个节点之前
            /// </summary>
            /// <param name="node">要比较的节点</param>
            /// <returns>若该节点位于 <paramref name="node"/> 之前，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
            public bool Before(Node? node)
            {
                while (node is not null)
                {
                    if (this == node.Prev)
                    {
                        return true;
                    }

                    node = node.Prev;
                }

                return false;
            }

            #endregion


            //------------------------------------------------------
            //
            //  Operators
            //
            //------------------------------------------------------

            #region Operators

            public static bool operator ==(Node? left, Node? right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                return left.Prev == right.Prev
                    && left.Next == right.Next
                    && EqualityComparer<T>.Default.Equals(left.Item, right.Item);
            }

            public static bool operator !=(Node? left, Node? right)
            {
                if (ReferenceEquals(left, right))
                {
                    return false;
                }

                if (left is null || right is null)
                {
                    return true;
                }

                return left.Prev != right.Prev
                    || left.Next != right.Next
                    || !EqualityComparer<T>.Default.Equals(left.Item, right.Item);
            }

            #endregion
        }


        private static class MergeSort
        {
            public static Node Merge(Node previousTail, Node? left, Node? right, Comparison<T> comparison)
            {
                var tail = previousTail;

                while (left is not null && right is not null)
                {
                    Node selected;

                    if (comparison(left.Item, right.Item) <= 0)
                    {
                        selected = left;
                        left = left.Next;
                    }
                    else
                    {
                        selected = right;
                        right = right.Next;
                    }

                    tail.Next = selected;
                    selected.Prev = tail;

                    tail = selected;
                }

                var remainder = left ?? right;

                if (remainder is not null)
                {
                    tail.Next = remainder;
                    remainder.Prev = tail;

                    do
                    {
                        tail = remainder;
                        remainder = remainder.Next;
                    }
                    while (remainder is not null);
                }

                tail.Next = null;
                return tail;
            }

            public static Node Merge(Node previousTail, Node? left, Node? right, IComparer<T> comparer)
            {
                var tail = previousTail;

                while (left is not null && right is not null)
                {
                    Node selected;

                    if (comparer.Compare(left.Item, right.Item) <= 0)
                    {
                        selected = left;
                        left = left.Next;
                    }
                    else
                    {
                        selected = right;
                        right = right.Next;
                    }

                    tail.Next = selected;
                    selected.Prev = tail;

                    tail = selected;
                }

                var remainder = left ?? right;

                if (remainder is not null)
                {
                    tail.Next = remainder;
                    remainder.Prev = tail;

                    do
                    {
                        tail = remainder;
                        remainder = remainder.Next;
                    }
                    while (remainder is not null);
                }

                tail.Next = null;
                return tail;
            }

            public static Node? Split(Node? head, int length, out Node? rest)
            {
                if (head is null)
                {
                    rest = null;
                    return null;
                }

                var tail = head;

                for (int i = 1; i < length && tail.Next is not null; i++)
                {
                    tail = tail.Next;
                }

                rest = tail.Next;
                tail.Next = null;

                rest?.Prev = null;
                return head;
            }
        }
    }
}
