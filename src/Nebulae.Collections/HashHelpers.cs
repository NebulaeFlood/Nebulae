using System;
using System.Runtime.CompilerServices;

namespace Nebulae.Collections
{
    /// <summary>
    /// 哈希值相关操作的工具类
    /// </summary>
    public static class HashHelpers
    {
        /// <summary>
        /// 最大哈希表大小
        /// </summary>
        public const int MaxSize = 0x7FFFFFC3;


        //------------------------------------------------------
        //
        //  Private Constants
        //
        //------------------------------------------------------

        #region Private Constants

        private const int Count = 72;

        private const int HashPrime = 101;
        private const ulong HashPrimeMultiplier = ulong.MaxValue / 101ul + 1;

        #endregion


        /// <summary>
        /// 是否为 64 位进程
        /// </summary>
        public static readonly bool Bit64 = IntPtr.Size is 8;


        //------------------------------------------------------
        //
        //  Public Static Methods
        //
        //------------------------------------------------------

        #region Public Static Methods

        /// <summary>
        /// 计算用于快速取模运算的乘数
        /// </summary>
        /// <param name="divisor">除数</param>
        /// <returns>快速取模运算的乘数。</returns>
        /// <remarks>仅适用于 64 位系统。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CalculateMultiplier(uint divisor)
        {
            return ulong.MaxValue / divisor + 1;
        }

        /// <summary>
        /// 确保哈希表大小为合适的质数
        /// </summary>
        /// <param name="size">哈希表当前的大小</param>
        /// <returns>大于或等于 <paramref name="size"/> 的适合哈希表的质数。</returns>
        /// <remarks>当找不到适合的质数时，返回 <see cref="MaxSize"/>。</remarks>
        public static int EnsurePrime(int size)
        {
            for (int i = 0; i < Count; i++)
            {
                var num = Primes[i];

                if (num >= size)
                {
                    return num;
                }
            }

            if (Bit64)
            {
                for (int i = size | 1; i < MaxSize; i += 2)
                {
                    if (IsPrime(i) && Modulo((uint)(i - 1u), HashPrime, HashPrimeMultiplier) != 0)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = size | 1; i < MaxSize; i += 2)
                {
                    if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                    {
                        return i;
                    }
                }
            }

            return MaxSize;
        }

        /// <summary>
        /// 以一定的算法增长哈希表大小
        /// </summary>
        /// <param name="size">哈希表当前的大小</param>
        /// <param name="newSize">新的哈希表大小</param>
        /// <returns>若 <paramref name="newSize"/> 大于 <paramref name="size"/>，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        /// <remarks>
        /// <para>
        /// 新值将不会大于 <see cref="MaxSize"/>。
        /// </para>
        /// <para>
        /// <paramref name="size"/> 为负数时，<paramref name="newSize"/> 将被设为 <see cref="MaxSize"/>，并返回 <see langword="false"/>。
        /// </para>
        /// </remarks>
        public static bool Expand(int size, out int newSize)
        {
            if ((uint)size >= MaxSize)
            {
                newSize = MaxSize;
                return false;
            }

            if (size < 4)
            {
                newSize = size << 1;
            }
            else
            {
                newSize = (int)MathF.Ceiling(size * CollectionHelpers.GoldenRatio);
            }

            if ((uint)newSize > MaxSize)
            {
                newSize = MaxSize;
                return true;
            }

            newSize = GetPrime(size);
            return true;
        }

        /// <summary>
        /// 获取适合作为哈希表大小的质数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <returns>大于 <paramref name="min"/> 的适合作为哈希表大小的质数。</returns>
        /// <remarks>当找不到适合的质数时，返回 <see cref="MaxSize"/>。</remarks>
        public static int GetPrime(int min)
        {
            for (int i = 0; i < Count; i++)
            {
                var num = Primes[i];

                if (num > min)
                {
                    return num;
                }
            }

            if (Bit64)
            {
                for (int i = min | 1; i < MaxSize; i += 2)
                {
                    if (IsPrime(i) && Modulo((uint)(i - 1U), HashPrime, HashPrimeMultiplier) != 0)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = min | 1; i < MaxSize; i += 2)
                {
                    if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                    {
                        return i;
                    }
                }
            }

            return MaxSize;
        }

        /// <summary>
        /// 判断一个数是否为质数
        /// </summary>
        /// <param name="candidate">要判断的数</param>
        /// <returns>若 <paramref name="candidate"/> 是质数，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) is 0)
            {
                return candidate is 2;
            }

            int limit = (int)Math.Sqrt(candidate);

            for (int divisor = 3; divisor <= limit; divisor += 2)
            {
                if ((candidate % divisor) is 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 快速取模运算
        /// </summary>
        /// <param name="value">要取模的值</param>
        /// <param name="divisor">除数</param>
        /// <param name="multiplier">快速取模运算的乘数</param>
        /// <returns>对 <paramref name="value"/> 取模后的结果。</returns>
        /// <remarks>仅适用于 64 位系统。</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Modulo(uint value, uint divisor, ulong multiplier)
        {
            return (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);
        }

        #endregion


        private static readonly int[] Primes =
        [
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71,
            89, 107, 131, 163, 197, 239, 293, 353, 431, 521,
            631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371,
            4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023,
            25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363,
            156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
            968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559,
            5999471, 7199369
        ];
    }
}
