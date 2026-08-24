using Nebulae.Diagnostics;
using System;
using System.Reflection;

namespace Nebulae.Reflection.Extensions
{
    /// <summary>
    /// 为 <see cref="EventInfo"/> 提供拓展方法
    /// </summary>
    public static class EventInfoExtensions
    {
        /// <summary>
        /// 判断事件是否为静态事件
        /// </summary>
        /// <param name="eventInfo">要判断的事件</param>
        /// <returns>若判断的事件为静态事件，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool IsStatic(this EventInfo eventInfo)
        {
            MethodInfo method = eventInfo.GetAddMethod(true)
                ?? eventInfo.GetRemoveMethod(true)
                ?? throw new NotSupportedException(
                    $"Cannot determine whether " +
                    $"event '{eventInfo.AsLog()}' is static " +
                    $"because it does not have an add or remove method.");

            return method.IsStatic;
        }
    }
}
