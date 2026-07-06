using Nebulae.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nebulae.Collections
{
    /// <summary>
    /// 提供集合操作的工具类
    /// </summary>
    public static class CollectionHelpers
    {
        /// <summary>
        /// 黄金比例的近似值
        /// </summary>
        public const float GoldenRatio = 1.6180340F;


        /// <summary>
        /// 以一定的算法增长值的大小
        /// </summary>
        /// <param name="value">要增长的值</param>
        /// <returns>增长后的新值。</returns>
        /// <remarks>
        /// <para>
        /// 新值将不会大于 <see cref="int.MaxValue"/>。
        /// </para>
        /// <para>
        /// 无法处理负值。
        /// </para>
        /// </remarks>
        public static int Grow(int value)
        {
            if ((uint)value >= int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < 4)
            {
                return (value | 1) << 1;
            }

            value = (int)MathF.Ceiling(value * GoldenRatio);

            if ((uint)value > int.MaxValue)
            {
                return int.MaxValue;
            }

            return value;
        }

        /// <summary>
        /// 以一定的算法增长值的大小
        /// </summary>
        /// <param name="value">要增长的值</param>
        /// <param name="maxValue">最大值</param>
        /// <returns>增长后的新值。</returns>
        /// <remarks>
        /// <para>
        /// 新值将不会大于 <paramref name="maxValue"/>。
        /// </para>
        /// <para>
        /// 无法处理负值。
        /// </para>
        /// </remarks>
        public static int Grow(int value, int maxValue)
        {
            if ((uint)value >= maxValue)
            {
                return maxValue;
            }

            if (value < 4)
            {
                return (value | 1) << 1;
            }

            value = (int)MathF.Ceiling(value * GoldenRatio);

            if ((uint)value > maxValue)
            {
                return maxValue;
            }

            return value;
        }


        /// <summary>
        /// 集合异常抛出的工具类
        /// </summary>
        public static class ThrowHelpers
        {
            /// <summary>
            /// 抛出集合容量达到最大值时的异常
            /// </summary>
            /// <param name="collection">集合对象或信息</param>
            /// <param name="capacity">集合的容量</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowCapacityReachAbility(object? collection, int capacity)
            {
                throw new InvalidOperationException($"Capacity of collection '{collection.AsLog()}' has reached its maximum limit of '{capacity}'.");
            }

            /// <summary>
            /// 抛出对空集合执行操作时的异常
            /// </summary>
            /// <typeparam name="T">集合类型</typeparam>
            /// <param name="operation">操作名称</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowOperateEmptyCollection<T>([CallerMemberName] string operation = "Unknown")
            {
                throw new InvalidOperationException($"Cannot perform '{operation}' on an empty collection of type '{typeof(T).AsLog()}'.");
            }

            /// <summary>
            /// 当数组索引大于数组长度时引发异常
            /// </summary>
            /// <param name="arrayIndex">数组索引</param>
            /// <param name="arrayLength">数组长度</param>
            /// <param name="paramName">参数名称</param>
            /// <remarks><b>数组参数应命名为 <see langword="array"/>。</b></remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfArrayNotLongEnough(int arrayIndex, int arrayLength, [CallerArgumentExpression(nameof(arrayIndex))] string? paramName = null)
            {
                if (arrayIndex >= arrayLength)
                {
                    throw new ArgumentException($"Destination array was not long enough. The destination index was '{arrayIndex}' while the array's length was '{arrayLength}'.", paramName);
                }
            }

            /// <summary>
            /// 当数组在索引后的可用长度不足时引发异常
            /// </summary>
            /// <param name="arrayIndex">数组索引</param>
            /// <param name="arrayLength">数组长度</param>
            /// <param name="requiredLength">所需长度</param>
            /// <param name="paramName">参数名称</param>
            /// <remarks><b>数组参数应命名为 <see langword="array"/>。</b></remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfArrayNotLongEnough(int arrayIndex, int arrayLength, int requiredLength, [CallerArgumentExpression(nameof(arrayIndex))] string? paramName = null)
            {
                if (arrayIndex + requiredLength > arrayLength)
                {
                    throw new ArgumentException($"Destination array was not long enough. The destination index was '{arrayIndex + requiredLength}' while the array's length was '{arrayLength}'.", paramName);
                }
            }

            /// <summary>
            /// 当集合中的元素数量不足时引发异常
            /// </summary>
            /// <param name="collectionCount">集合中的元素数量</param>
            /// <param name="requiredCount">所需元素数量</param>
            /// <param name="paramName">参数名称</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfCollectionElementNotEnough(int collectionCount, int requiredCount, [CallerArgumentExpression(nameof(requiredCount))] string? paramName = null)
            {
                if (collectionCount < requiredCount)
                {
                    throw new ArgumentException($"Collection does not contain enough elements. The collection contains '{collectionCount}' elements while the required count is '{requiredCount}'.", paramName);
                }
            }

            /// <summary>
            /// 当集合中从指定索引开始的元素数量不足时引发异常
            /// </summary>
            /// <param name="collectionCount">集合中的元素数量</param>
            /// <param name="index">起始索引</param>
            /// <param name="requiredCount">所需元素数量</param>
            /// <param name="paramName">参数名称</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfCollectionElementNotEnough(int collectionCount, int index, int requiredCount, [CallerArgumentExpression(nameof(index))] string? paramName = null)
            {
                if (collectionCount - index < requiredCount)
                {
                    throw new ArgumentException($"Collection does not contain enough elements. The collection contains '{collectionCount - index}' elements from the specified index '{index}' to the end while the required count is '{requiredCount}'.", paramName);
                }
            }
        }

        /// <summary>
        /// 集合不安全操作的工具类
        /// </summary>
        public static class Unsafe
        {
            /// <summary>
            /// 获取数组的第一个元素的引用
            /// </summary>
            /// <typeparam name="T">数组元素类型</typeparam>
            /// <param name="array">目标数组</param>
            /// <returns>数组第一个元素的引用。</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T Ref<T>(T[] array)
            {
#if NET5_0_OR_GREATER
                return ref MemoryMarshal.GetArrayDataReference(array);
#else
                return ref MemoryMarshal.GetReference(new Span<T>(array));
#endif
            }

            /// <summary>
            /// 获取数组中指定索引处元素的引用
            /// </summary>
            /// <typeparam name="T">数组元素类型</typeparam>
            /// <param name="array">目标数组</param>
            /// <param name="index">索引位置</param>
            /// <returns>指定索引处元素的引用。</returns>

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T Ref<T>(T[] array, int index)
            {
#if NET5_0_OR_GREATER
                return ref System.Runtime.CompilerServices.Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
                return ref System.Runtime.CompilerServices.Unsafe.Add(ref MemoryMarshal.GetReference(new Span<T>(array)), index);
#endif
            }

            /// <summary>
            /// 获取数组中指定索引处元素的引用
            /// </summary>
            /// <typeparam name="T">数组元素类型</typeparam>
            /// <param name="array">目标数组</param>
            /// <param name="index">索引位置</param>
            /// <returns>指定索引处元素的引用。</returns>

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T Ref<T>(T[] array, nuint index)
            {
#if NET5_0_OR_GREATER
                return ref System.Runtime.CompilerServices.Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
                return ref System.Runtime.CompilerServices.Unsafe.Add(ref MemoryMarshal.GetReference(new Span<T>(array)), index);
#endif
            }
        }
    }
}