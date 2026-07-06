using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nebulae.Diagnostics
{
    /// <summary>
    /// 异常抛出的工具类
    /// </summary>
    public static class ThrowHelpers
    {
        /// <summary>
        /// 当参数为 <see langword="null"/> 时引发异常
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="argument">要检查的参数</param>
        /// <param name="paramName">参数名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfArgumentNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// 当参数为负数时引发异常
        /// </summary>
        /// <param name="argument">要检查的参数</param>
        /// <param name="paramName">参数名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfArgumentNegative(int argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, $"Argument cannot be a negative number, but the given value was '{argument}'.");
            }
        }

        /// <summary>
        /// 当参数不为正数时引发异常
        /// </summary>
        /// <param name="argument">要检查的参数</param>
        /// <param name="paramName">参数名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfArgumentNotPositive(int argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument < 1)
            {
                throw new ArgumentOutOfRangeException(paramName, $"Argument must be a positive number, but the given value was '{argument}'.");
            }
        }
    }
}
