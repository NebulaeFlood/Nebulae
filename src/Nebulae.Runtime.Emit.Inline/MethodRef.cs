using System;

namespace Nebulae.Runtime.Emit.Inline
{
#if NET10_0_OR_GREATER
#pragma warning disable CA1822
#endif
#pragma warning disable IDE0060

    /// <summary>
    /// 方法引用
    /// </summary>
    [Reference(ReferenceType.Method)]
    public sealed class MethodRef
    {
        private MethodRef() { }


        /// <summary>
        /// 使用指定的类型参数创建泛型方法引用
        /// </summary>
        /// <param name="genericArguments">泛型参数</param>
        /// <returns>由指定的类型参数创建的泛型方法引用。</returns>
        [Reference(ReferenceType.MethodMakeGeneric)]
        public MethodRef MakeGeneric(params Type[] genericArguments)
        {
            return IL.Throw<MethodRef>();
        }
    }
}
