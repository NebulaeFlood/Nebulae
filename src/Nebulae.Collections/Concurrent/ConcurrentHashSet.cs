using Nebulae.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Nebulae.Collections.Concurrent
{
    /// <summary>
    /// 可由多个线程同时访问的线程安全集
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="ConcurrentHashSet{T}"/> 的所有公共成员都是线程安全的。
    /// </para>
    /// <para>
    /// <see cref="ConcurrentHashSet{T}"/> 在枚举期间不会阻止其他线程对集合进行修改，因此枚举结果是某一时刻的快照。
    /// </para>
    /// <para>
    /// <see cref="ConcurrentHashSet{T}"/> 为了性能平衡，未实现 <see cref="ISet{T}"/> 接口。
    /// </para>
    /// <para>
    /// <b><see cref="ConcurrentHashSet{T}"/> 不保证任何拓展方法的线程安全性。</b>
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    public class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>, ICollectionDebugView<T> where T : notnull
    {
        //------------------------------------------------------
        //
        //  Private Constants
        //
        //------------------------------------------------------

        #region Private Constants

        private const int DefaultCapacity = 31;
        private const int MaxLockCount = 1024;

        #endregion


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 获取用于查找元素的 <see cref="IEqualityComparer{T}"/> 实现
        /// </summary>
        public IEqualityComparer<T> Comparer
        {
            get => _comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// 获取并发集中元素的数量
        /// </summary>
        /// <remarks>若是为了判断并发集是否为空，使用 <see cref="IsEmpty"/> 以提高性能。</remarks>
        public int Count
        {
            get
            {
                object[] locks = _tables.Locks;

                int locksCount = locks.Length;
                int locksTaken = 0;

                try
                {
                    for (; locksTaken < locksCount; locksTaken++)
                    {
                        Monitor.Enter(locks[locksTaken]);
                    }

                    return (int)Sum(_tables.LockReuseTimes);
                }
                finally
                {
                    for (int i = 0; i < locksTaken; i++)
                    {
                        Monitor.Exit(locks[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 获取一个值，该值指示并发集是否为空
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                uint[] lockReuseTimes = _tables.LockReuseTimes;

                for (int i = lockReuseTimes.Length - 1; i >= 0; i--)
                {
                    if (lockReuseTimes[i] > 0)
                    {
                        return false;
                    }
                }

                object[] locks = _tables.Locks;
                int locksTaken = 0;

                int count = locks.Length;

                try
                {
                    for (; locksTaken < count; locksTaken++)
                    {
                        Monitor.Enter(locks[locksTaken]);
                    }

                    lockReuseTimes = _tables.LockReuseTimes;

                    for (int i = lockReuseTimes.Length - 1; i >= 0; i--)
                    {
                        if (lockReuseTimes[i] > 0)
                        {
                            return false;
                        }
                    }

                    return true;
                }
                finally
                {
                    for (int i = 0; i < locksTaken; i++)
                    {
                        Monitor.Exit(locks[i]);
                    }
                }
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        public ConcurrentHashSet()
            : this(Environment.ProcessorCount, DefaultCapacity, locksIncreasable: true) { }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="items">要添加到并发集中的元素</param>
        public ConcurrentHashSet(IEnumerable<T> items)
            : this(Environment.ProcessorCount, items, locksIncreasable: true)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);
            Initailize(items);
        }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="items">要添加到并发集中的元素</param>
        /// <param name="comparer">集合内用于查找的 <see cref="IEqualityComparer{T}"/> 实现</param>
        public ConcurrentHashSet(IEnumerable<T> items, IEqualityComparer<T>? comparer)
            : this(Environment.ProcessorCount, items, locksIncreasable: true, comparer: comparer)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);
            Initailize(items);
        }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="comparer">集合内用于查找的 <see cref="IEqualityComparer{T}"/> 实现</param>
        public ConcurrentHashSet(IEqualityComparer<T>? comparer)
            : this(Environment.ProcessorCount, DefaultCapacity, locksIncreasable: true, comparer: comparer) { }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="concurrencyLevel">估计将同时更新并发集的线程数</param>
        /// <param name="capacity">并发集的初始容量</param>
        public ConcurrentHashSet(int concurrencyLevel, int capacity)
            : this(concurrencyLevel, capacity, locksIncreasable: false) { }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="concurrencyLevel">估计将同时更新并发集的线程数</param>
        /// <param name="items">要添加到并发集中的元素</param>
        /// <param name="comparer">集合内用于查找的 <see cref="IEqualityComparer{T}"/> 实现</param>
        public ConcurrentHashSet(int concurrencyLevel, IEnumerable<T> items, IEqualityComparer<T>? comparer)
            : this(concurrencyLevel, items, locksIncreasable: false, comparer: comparer)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);
            Initailize(items);
        }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="concurrencyLevel">估计将同时更新并发集的线程数</param>
        /// <param name="capacity">并发集的初始容量</param>
        /// <param name="comparer">集合内用于查找的 <see cref="IEqualityComparer{T}"/> 实现</param>
        public ConcurrentHashSet(int concurrencyLevel, int capacity, IEqualityComparer<T>? comparer)
            : this(concurrencyLevel, capacity, locksIncreasable: false, comparer: comparer) { }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="concurrencyLevel">估计将同时更新并发集的线程数</param>
        /// <param name="capacity">并发集的初始容量</param>
        /// <param name="items">要添加到并发集中的元素</param>
        public ConcurrentHashSet(int concurrencyLevel, int capacity, IEnumerable<T> items)
            : this(concurrencyLevel, capacity, locksIncreasable: false)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);
            Initailize(items);
        }

        /// <summary>
        /// 初始化 <see cref="ConcurrentHashSet{T}"/> 的新实例
        /// </summary>
        /// <param name="concurrencyLevel">估计将同时更新并发集的线程数</param>
        /// <param name="capacity">并发集的初始容量</param>
        /// <param name="items">要添加到并发集中的元素</param>
        /// <param name="comparer">集合内用于查找的 <see cref="IEqualityComparer{T}"/> 实现</param>
        public ConcurrentHashSet(int concurrencyLevel, int capacity, IEnumerable<T> items, IEqualityComparer<T>? comparer)
            : this(concurrencyLevel, capacity, locksIncreasable: false, comparer: comparer)
        {
            ThrowHelpers.ThrowIfArgumentNull(items);
            Initailize(items);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Constructors
        //
        //------------------------------------------------------

        #region Private Constructors

        private ConcurrentHashSet(int concurrencyLevel, IEnumerable<T> items, bool locksIncreasable, IEqualityComparer<T>? comparer = null)
        {
            ThrowHelpers.ThrowIfArgumentNotPositive(concurrencyLevel);
            ThrowHelpers.ThrowIfArgumentNull(items);

            int capacity = items.Count();

            if (capacity < concurrencyLevel)
            {
                capacity = concurrencyLevel;
            }

            capacity = HashHelpers.EnsurePrime(capacity);
            var locks = new object[concurrencyLevel];

            for (int i = concurrencyLevel - 1; i >= 0; i--)
            {
                locks[i] = new object();
            }

            _designedCapacity = capacity;

            _lockIncreasable = locksIncreasable;
            _lockReuseThreshold = Math.Max(1, capacity / concurrencyLevel);

            _tables = new Tables(
                new VolatileWrapper<Node>[capacity],
                locks,
                new uint[concurrencyLevel]);

            if (typeof(T).IsValueType)
            {
                _comparer = comparer;
            }
            else
            {
                _comparer = comparer ?? EqualityComparer<T>.Default;
            }
        }

        private ConcurrentHashSet(int concurrencyLevel, int capacity, bool locksIncreasable, IEqualityComparer<T>? comparer = null)
        {
            ThrowHelpers.ThrowIfArgumentNotPositive(concurrencyLevel);

            if (capacity < concurrencyLevel)
            {
                capacity = concurrencyLevel;
            }

            capacity = HashHelpers.EnsurePrime(capacity);
            var locks = new object[concurrencyLevel];

            for (int i = concurrencyLevel - 1; i >= 0; i--)
            {
                locks[i] = new object();
            }

            _designedCapacity = capacity;

            _lockIncreasable = locksIncreasable;
            _lockReuseThreshold = Math.Max(1, capacity / concurrencyLevel);

            _tables = new Tables(
                new VolatileWrapper<Node>[capacity],
                locks,
                new uint[concurrencyLevel]);

            if (typeof(T).IsValueType)
            {
                _comparer = comparer;
            }
            else
            {
                _comparer = comparer ?? EqualityComparer<T>.Default;
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 将元素添加到并发集
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>若 <paramref name="item"/> 成功添加到并发集，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Add(T item)
        {
            ThrowHelpers.ThrowIfArgumentNull(item);
            return TryAddInternal(item);
        }

        /// <summary>
        /// 移除所有元素
        /// </summary>
        public void Clear()
        {
            object[] locks = _tables.Locks;

            int locksCount = locks.Length;
            int locksTaken = 0;

            try
            {
                for (; locksTaken < locksCount; locksTaken++)
                {
                    Monitor.Enter(locks[locksTaken]);
                }

                uint[] lockReuseTimes = _tables.LockReuseTimes;

                for (int i = locksTaken - 1; i >= 0; i--)
                {
                    if (lockReuseTimes[i] > 0)
                    {
                        Tables tables = _tables;

                        _tables = new Tables(
                            new VolatileWrapper<Node>[_designedCapacity],
                            tables.Locks,
                            new uint[locksTaken]);

                        _lockReuseThreshold = Math.Max(1, _designedCapacity / locksTaken);
                        return;
                    }
                }
            }
            finally
            {
                for (int i = 0; i < locksTaken; i++)
                {
                    Monitor.Exit(locks[i]);
                }
            }
        }

        /// <summary>
        /// 确定并发集是否包含指定的元素
        /// </summary>
        /// <param name="item">要查找的元素</param>
        /// <returns>若并发集包含 <paramref name="item"/>，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Contains(T item)
        {
            if (item is null)
            {
                return false;
            }

            return TryGetInternal(item, out _);
        }

        /// <summary>
        /// 从目标数组的指定索引处开始，复制并发集内的元素
        /// </summary>
        /// <param name="array">接收并发集内元素的数组</param>
        /// <param name="arrayIndex">开始复制的索引</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowHelpers.ThrowIfArgumentNull(array);
            ThrowHelpers.ThrowIfArgumentNegative(arrayIndex);

            int arrayLength = array.Length;

            CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(arrayIndex, arrayLength);

            object[] locks = _tables.Locks;
            int locksCount = locks.Length;
            int locksTaken = 0;

            try
            {
                for (; locksTaken < locksCount; locksTaken++)
                {
                    Monitor.Enter(locks[locksTaken]);
                }

                int count = (int)Sum(_tables.LockReuseTimes);

                CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(arrayIndex, arrayLength, count);

                arrayIndex = arrayIndex + count - 1;
                VolatileWrapper<Node>[] buckets = _tables.Buckets;

                for (int i = buckets.Length - 1; i >= 0; i--)
                {
                    for (Node? current = buckets[i].Value; current is not null; current = current.Next)
                    {
                        array[arrayIndex--] = current.Value;
                    }
                }
            }
            finally
            {
                for (int i = 0; i < locksTaken; i++)
                {
                    Monitor.Exit(locks[i]);
                }
            }
        }

        /// <summary>
        /// 获取循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的枚举器。</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 在并发集中移除指定元素
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>若在并发集中找到并删除了 <paramref name="item"/>，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Remove(T item)
        {
            ThrowHelpers.ThrowIfArgumentNull(item);
            return TryRemoveInternal(item);
        }

        /// <summary>
        /// 将并发集的元素复制到新数组中
        /// </summary>
        /// <returns>一个包含并发集中所有元素的数组。</returns>
        public T[] ToArray()
        {
            object[] locks = _tables.Locks;

            int locksCount = locks.Length;
            int locksTaken = 0;

            try
            {
                for (; locksTaken < locksCount; locksTaken++)
                {
                    Monitor.Enter(locks[locksTaken]);
                }

                int count = (int)Sum(_tables.LockReuseTimes);

                if (count < 1)
                {
                    return [];
                }

                var array = new T[count];
                int index = count - 1;

                VolatileWrapper<Node>[] buckets = _tables.Buckets;

                for (int i = buckets.Length - 1; i >= 0; i--)
                {
                    for (Node? current = buckets[i].Value; current is not null; current = current.Next)
                    {
                        array[index--] = current.Value;
                    }
                }

                return array;
            }
            finally
            {
                for (int i = 0; i < locksTaken; i++)
                {
                    Monitor.Exit(locks[i]);
                }
            }
        }

        /// <summary>
        /// 在并发集中查找指定元素
        /// </summary>
        /// <param name="equalValue">要查找的元素</param>
        /// <param name="actualValue">并发集中与 <paramref name="equalValue"/> 相等的元素</param>
        /// <returns>若在并发集中找到了 <paramref name="actualValue"/>，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool TryGetValue(in T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            ThrowHelpers.ThrowIfArgumentNull(equalValue);
            return TryGetInternal(equalValue, out actualValue);
        }

        #endregion


        //------------------------------------------------------
        //
        //  ICollection<TKey>
        //
        //------------------------------------------------------

        #region ICollection<T>

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item)
        {
            ThrowHelpers.ThrowIfArgumentNull(item);
            TryAddInternal(item);
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
        //  Private Static Methods
        //
        //------------------------------------------------------

        #region Private Static Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetHashCode(T item, IEqualityComparer<T>? comparer)
        {
            if (typeof(T).IsValueType && comparer is null)
            {
                return (uint)EqualityComparer<T>.Default.GetHashCode(item);
            }

            return (uint)comparer!.GetHashCode(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T left, T right, IEqualityComparer<T>? comparer)
        {
            if (typeof(T).IsValueType && comparer is null)
            {
                return EqualityComparer<T>.Default.Equals(left, right);
            }

            return comparer!.Equals(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Node? GetBucket(Tables tables, uint hashCode)
        {
            VolatileWrapper<Node>[] buckets = tables.Buckets;

            if (HashHelpers.Bit64)
            {
                return ref buckets[HashHelpers.Modulo(hashCode, (uint)buckets.Length, tables.FastBucketsModuloMultiplier)].Value;
            }
            else
            {
                return ref buckets[hashCode % (uint)buckets.Length].Value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Node? GetBucketWithLockIndex(Tables tables, uint hashCode, out uint lockIndex)
        {
            VolatileWrapper<Node>[] buckets = tables.Buckets;

            if (HashHelpers.Bit64)
            {
                uint index = HashHelpers.Modulo(hashCode, (uint)buckets.Length, tables.FastBucketsModuloMultiplier);
                lockIndex = HashHelpers.Modulo(index, (uint)tables.Locks.Length, tables.FastLocksModuloMultiplier);

                return ref buckets[index].Value;
            }
            else
            {
                uint bucketIndex = hashCode % (uint)buckets.Length;
                lockIndex = hashCode % (uint)tables.Locks.Length;

                return ref buckets[bucketIndex].Value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Sum(uint[] lockReuseTimes)
        {
            uint count = 0;

            for (int i = 0; i < lockReuseTimes.Length; i++)
            {
                count += lockReuseTimes[i];
            }

            return count;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void Grow()
        {
            Tables tables = _tables;
            uint locksTaken = 1;

            try
            {
                // 仅允许持有第一个锁的线程执行该方法
                Monitor.Enter(tables.Locks[0]);

                // 是否已经有其它线程改变了大小
                if (tables != _tables)
                {
                    return;
                }


                // 计算当前集合元素的近似数量
                uint approxCount = Sum(tables.LockReuseTimes);

                // 计算负载因子 approxCount / tables.Buckets.Length，
                // 判断其是否小于 1 / 4
                if (approxCount < tables.Buckets.Length / 4)
                {
                    // 此时说明负载因子过低，扩容的收益太低，
                    // 因此这里只提高每个锁能管理的元素数量。
                    _lockReuseThreshold = CollectionHelpers.Grow(_lockReuseThreshold);
                    return;
                }


                // 尝试扩容
                if (!HashHelpers.Expand(tables.Buckets.Length, out int space))
                {
                    // 此时说明大小已经达到极限，无法扩容。
                    // 将 _lockReuseThreshold 设置为 int.MaxValue，
                    // 防止再次调用 Grow()。
                    _lockReuseThreshold = int.MaxValue;
                    return;
                }

                if (space is HashHelpers.MaxSize)
                {
                    // 此时说明空间将要拓展到极限，这次扩容后无法再扩容。
                    // 将 _lockReuseThreshold 设置为 int.MaxValue，
                    // 防止再次调用 Grow()。
                    _lockReuseThreshold = int.MaxValue;
                }


                object[] locks = tables.Locks;
                int oldLockCount = locks.Length;

                if (_lockIncreasable)
                {
                    int lockCount = CollectionHelpers.Grow(oldLockCount, MaxLockCount);

                    Array.Resize(ref locks, lockCount);

                    for (int i = oldLockCount; i < lockCount; i++)
                    {
                        locks[i] = new object();
                    }

                    if (lockCount == MaxLockCount)
                    {
                        // 锁的数量已经达到极限。
                        _lockIncreasable = false;
                    }
                }

                uint[] lockReuseTimes = new uint[locks.Length];
                var newBuckets = new VolatileWrapper<Node>[space];

                // lockCount 为旧的锁数量，
                // i > 0 以跳过第一个锁。
                for (int i = oldLockCount - 1; i > 0; i--)
                {
                    // Array.Resize() 不会修改数组元素的顺序，
                    // 因此这里可以直接使用本地变量 locks[i]。
                    Monitor.Enter(locks[i]);
                    locksTaken++;
                }

                uint newLockCount = (uint)locks.Length;
                VolatileWrapper<Node>[] oldBuckets = tables.Buckets;

                var newTables = new Tables(newBuckets, locks, lockReuseTimes);

                for (int i = 0; i < oldBuckets.Length; i++)
                {
                    for (Node? current = oldBuckets[i].Value; current is not null; current = current.Next)
                    {
                        ref Node? newNode = ref GetBucketWithLockIndex(newTables, current.HashCode, out uint lockIndex);
                        newNode = new Node(current.Value, current.HashCode, newNode);

                        checked
                        {
                            lockReuseTimes[lockIndex]++;
                        }
                    }
                }

                // 每个锁能管理的元素数量 = 新的 Buckets.Length / 新的锁数量
                _lockReuseThreshold = Math.Max(1, space / (int)newLockCount);
                _tables = newTables;
            }
            finally
            {
                object[] locks = tables.Locks;

                for (int i = 0; i < locksTaken; i++)
                {
                    Monitor.Exit(locks[i]);
                }
            }
        }

        private void Initailize(IEnumerable<T> items)
        {
            IEqualityComparer<T>? comparer = _comparer;
            uint[] lockReuseTimes = _tables.LockReuseTimes;

            foreach (T item in items)
            {
                if (item is null)
                {
                    throw new ArgumentNullException(
                        $"Cannot create {typeof(ConcurrentHashSet<T>).AsLog()} " +
                        $"from a collection that contains null elements.");
                }

                uint hashCode = GetHashCode(item, comparer);
                ref Node? node = ref GetBucketWithLockIndex(_tables, hashCode, out uint lockIndex);

                for (var current = node; current is not null; current = current.Next)
                {
                    if (current.HashCode == hashCode && Equals(current.Value, item, comparer))
                    {
                        continue;
                    }
                }

                node = new Node(item, hashCode, node);

                if (checked(++lockReuseTimes[lockIndex]) > _lockReuseThreshold)
                {
                    Grow();
                }
            }
        }

        private bool TryAddInternal(T item)
        {
            uint hashCode = GetHashCode(item, _comparer);

        Retry:
            Tables tables = _tables;
            ref Node? bucket = ref GetBucketWithLockIndex(tables, hashCode, out uint lockIndex);

            bool lockOverloaded = false;

            lock (tables.Locks[lockIndex])
            {
                if (tables != _tables)
                {
                    goto Retry;
                }

                for (Node? current = bucket; current is not null; current = current.Next)
                {
                    if (current.HashCode == hashCode && Equals(current.Value, item, _comparer))
                    {
                        return false;
                    }
                }

                Volatile.Write(ref bucket, new Node(item, hashCode, bucket));

                if (checked(++tables.LockReuseTimes[lockIndex]) > _lockReuseThreshold)
                {
                    lockOverloaded = true;
                }
            }

            if (lockOverloaded)
            {
                Grow();
            }

            return true;
        }

        private bool TryGetInternal(T item, [MaybeNullWhen(false)] out T value)
        {
            uint hashCode = GetHashCode(item, _comparer);
            Tables tables = _tables;

            for (Node? current = GetBucket(tables, hashCode); current is not null; current = current.Next)
            {
                if (current.HashCode == hashCode && Equals(current.Value, item, _comparer))
                {
                    value = current.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private bool TryRemoveInternal(T item)
        {
            uint hashCode = GetHashCode(item, _comparer);

        Retry:
            Tables tables = _tables;
            ref Node? bucket = ref GetBucketWithLockIndex(tables, hashCode, out uint lockIndex);

            lock (tables.Locks[lockIndex])
            {
                if (tables != _tables)
                {
                    goto Retry;
                }

                Node? current = bucket;
                Node? previous = null;

                while (current is not null)
                {
                    if (current.HashCode == hashCode && Equals(current.Value, item, _comparer))
                    {
                        if (previous is null)
                        {
                            Volatile.Write(ref bucket, current.Next);
                        }
                        else
                        {
                            previous.Next = current.Next;
                        }

                        tables.LockReuseTimes[lockIndex]--;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly IEqualityComparer<T>? _comparer;
        private readonly int _designedCapacity;

        private bool _lockIncreasable;
        private int _lockReuseThreshold;

        private volatile Tables _tables;

        #endregion


        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        private sealed class Enumerator(ConcurrentHashSet<T> set) : IEnumerator<T>
        {
            public T Current => _currentValue!;


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            public void Dispose() { }

            public bool MoveNext()
            {
                while (_current is null)
                {
                    if ((uint)++_index >= _buckets.Length)
                    {
                        return false;
                    }

                    _current = _buckets[_index].Value;
                }

                _currentValue = _current.Value;
                _current = _current.Next;

                return true;
            }

            public void Reset()
            {
                _buckets = _set._tables.Buckets;

                _current = null;
                _currentValue = default;

                _index = -1;
            }

            #endregion


            object? IEnumerator.Current => _currentValue;


            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private VolatileWrapper<Node>[] _buckets = set._tables.Buckets;
            private readonly ConcurrentHashSet<T> _set = set;

            private Node? _current;
            private T? _currentValue;

            private int _index = -1;

            #endregion
        }

        private sealed class Node(T value, uint hashCode, Node? next)
        {
            public readonly T Value = value;
            public readonly uint HashCode = hashCode;

            public volatile Node? Next = next;


            public override string ToString()
            {
                return Value.AsLog();
            }
        }

        private sealed class Tables
        {
            public readonly VolatileWrapper<Node>[] Buckets;

            public readonly object[] Locks;
            public readonly uint[] LockReuseTimes;

            public readonly ulong FastBucketsModuloMultiplier;
            public readonly ulong FastLocksModuloMultiplier;

            public Tables(VolatileWrapper<Node>[] buckets, object[] locks, uint[] lockReuseTimes)
            {
                Buckets = buckets;

                Locks = locks;
                LockReuseTimes = lockReuseTimes;

                if (HashHelpers.Bit64)
                {
                    FastBucketsModuloMultiplier = HashHelpers.CalculateMultiplier((uint)buckets.Length);
                    FastLocksModuloMultiplier = HashHelpers.CalculateMultiplier((uint)locks.Length);
                }
            }
        }

        #endregion
    }
}
