using System;
using System.Runtime.CompilerServices;

namespace Nebulae.Runtime.Emit.Inline
{
#pragma warning disable IDE0060

    /// <summary>
    /// 提供用于内联 IL 代码的占位方法和拓展方法
    /// </summary>
    public static partial class IL
    {
        private const string PlaceholderMessage =
            "An inline IL placeholder was executed because current method was not rewritten correctly.";


        /// <summary>
        /// 引用类型
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>表示类型引用的 <see cref="TypeRef"/>。</returns>
        [Reference(ReferenceType.Type)]
        public static TypeRef Ref(Type type)
        {
            return Throw<TypeRef>();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Inline this method to throw the exception directly at the call site.
        internal static void Throw()
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Inline this method to throw the exception directly at the call site.
        internal static T Throw<T>()
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }
    }
}
