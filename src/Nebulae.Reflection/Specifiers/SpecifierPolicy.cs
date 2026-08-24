using System;

namespace Nebulae.Reflection.Specifiers
{
    /// <summary>
    /// 引用说明符的策略
    /// </summary>
    public enum SpecifierPolicy : byte
    {
        /// <summary>
        /// 严格策略，要求引用说明符的配置的参数类型必须完全匹配目标成员
        /// </summary>
        Strict,

        /// <summary>
        /// 宽松策略，允许引用说明符的配置的参数类型与目标成员不完全匹配
        /// </summary>
        /// <remarks>
        /// 在宽松策略下，会尝试使用
        /// <see cref="ConvertHelpers.ChangeType{TFrom, TTo}(TFrom, IFormatProvider?)"/>
        /// 转换参数，以适应目标成员的要求。
        /// </remarks>
        Lenient
    }
}
