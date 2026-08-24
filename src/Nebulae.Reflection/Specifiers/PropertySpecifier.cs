using Nebulae.Diagnostics;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Nebulae.Reflection.Specifiers
{
    /// <summary>
    /// 属性的引用说明符
    /// </summary>
    public readonly struct PropertySpecifier : IEquatable<PropertySpecifier>
    {
        /// <summary>
        /// 目标属性的 <see cref="PropertyInfo"/>
        /// </summary>
        public readonly PropertyInfo MemberInfo;


        internal PropertySpecifier(PropertyInfo memberInfo)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;
        }


        //------------------------------------------------------
        //
        //  Basic Methods
        //
        //------------------------------------------------------

        #region Basic Methods

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public override bool Equals(object? obj)
        {
            return obj is PropertySpecifier other
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(PropertySpecifier other)
        {
            return MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 获取当前对象的哈希代码
        /// </summary>
        /// <returns>当前对象的哈希代码。</returns>
        public override int GetHashCode()
        {
            return MemberInfo.GetHashCode();
        }

        /// <summary>
        /// 获取表示当前对象的字符串
        /// </summary>
        /// <returns>表示当前对象的字符串。</returns>
        public override string ToString()
        {
            return MemberInfo.AsLog();
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 获取属性 <see langword="get"/> 方法的引用说明符
        /// </summary>
        /// <returns>属性 <see langword="get"/> 方法的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Get()
        {
            return new MethodSpecifier(
                MemberInfo.GetGetMethod(true) ?? throw new MissingMethodException(
                    $"Property '{MemberInfo.AsLog()}' does not have a get method."));
        }

        /// <summary>
        /// 获取属性 <see langword="set"/> 方法的引用说明符
        /// </summary>
        /// <returns>属性 <see langword="set"/> 方法的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Set()
        {
            return new MethodSpecifier(
                MemberInfo.GetSetMethod(true) ?? throw new MissingMethodException(
                    $"Property '{MemberInfo.AsLog()}' does not have a set method."));
        }

        #endregion


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 判断两个 <see cref="PropertySpecifier"/> 是否相等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="PropertySpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="PropertySpecifier"/></param>
        /// <returns>若两个 <see cref="PropertySpecifier"/> 相等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator ==(PropertySpecifier left, PropertySpecifier right)
        {
            return left.MemberInfo == right.MemberInfo;
        }

        /// <summary>
        /// 判断两个 <see cref="PropertySpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="PropertySpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="PropertySpecifier"/></param>
        /// <returns>若两个 <see cref="PropertySpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(PropertySpecifier left, PropertySpecifier right)
        {
            return left.MemberInfo != right.MemberInfo;
        }

        #endregion
    }
}
