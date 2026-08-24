using Nebulae.Diagnostics;
using System;
using System.Reflection;

namespace Nebulae.Reflection.Extensions
{
    /// <summary>
    /// 提供 <see cref="PropertyInfo"/> 的拓展方法
    /// </summary>
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// 判断属性是否为静态属性
        /// </summary>
        /// <param name="propertyInfo">要判断的属性</param>
        /// <returns>若判断的属性为静态属性，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool IsStatic(this PropertyInfo propertyInfo)
        {
            MethodInfo method = propertyInfo.GetGetMethod(true)
                ?? propertyInfo.GetSetMethod(true)
                ?? throw new NotSupportedException(
                    $"Cannot determine whether " +
                    $"property '{propertyInfo.AsLog()}' is static " +
                    $"because it does not have a get or set method.");

            return method.IsStatic;
        }
    }
}
