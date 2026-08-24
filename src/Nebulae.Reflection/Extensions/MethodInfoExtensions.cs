using System;
using System.Reflection;

namespace Nebulae.Reflection.Extensions
{
#if !NETCOREAPP1_0_OR_GREATER

    /// <summary>
    /// 提供 <see cref="MethodInfo"/> 的拓展方法
    /// </summary>
    public static class MethodInfoExtensions
    {

        /// <summary>
        /// 创建指定类型的委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="methodInfo">目标方法</param>
        /// <returns>由此方法创建的委托。</returns>
        public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate
        {
            return (T)methodInfo.CreateDelegate(typeof(T));
        }

        /// <summary>
        /// 创建指定类型的委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="methodInfo">目标方法</param>
        /// <param name="target">目标对象</param>
        /// <returns>由此方法创建的委托。</returns>
        public static T CreateDelegate<T>(this MethodInfo methodInfo, object? target) where T : Delegate
        {
            return (T)methodInfo.CreateDelegate(typeof(T), target);
        }
    }

#endif
}
