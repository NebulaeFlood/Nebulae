using Nebulae.Diagnostics;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Nebulae.Reflection.Specifiers
{
    /// <summary>
    /// 事件的引用说明符
    /// </summary>
    public readonly struct EventSpecifier : IEquatable<EventSpecifier>
    {
        /// <summary>
        /// 目标事件的 <see cref="EventInfo"/>
        /// </summary>
        public readonly EventInfo MemberInfo;


        internal EventSpecifier(EventInfo memberInfo)
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
            return obj is EventSpecifier other
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(EventSpecifier other)
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
        /// 获取事件 <see langword="add"/> 方法的引用说明符
        /// </summary>
        /// <returns>事件 <see langword="add"/> 方法的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Add()
        {
            return new MethodSpecifier(
                MemberInfo.GetAddMethod(true) ?? throw new MissingMethodException(
                    $"Event '{MemberInfo.AsLog()}' does not have an add method."));
        }

        /// <summary>
        /// 获取事件 <c>raise</c> 方法的引用说明符
        /// </summary>
        /// <returns>事件 <c>raise</c> 方法的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Raise()
        {
            return new MethodSpecifier(
                MemberInfo.GetRaiseMethod(true) ?? throw new MissingMethodException(
                    $"Event '{MemberInfo.AsLog()}' does not have a raise method."));
        }

        /// <summary>
        /// 获取事件 <see langword="remove"/> 方法的引用说明符
        /// </summary>
        /// <returns>事件 <see langword="remove"/> 方法的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Remove()
        {
            return new MethodSpecifier(
                MemberInfo.GetRemoveMethod(true) ?? throw new MissingMethodException(
                    $"Event '{MemberInfo.AsLog()}' does not have a remove method."));
        }

        #endregion


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 判断两个 <see cref="EventSpecifier"/> 是否相等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="EventSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="EventSpecifier"/></param>
        /// <returns>若两个 <see cref="EventSpecifier"/> 相等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator ==(EventSpecifier left, EventSpecifier right)
        {
            return left.MemberInfo == right.MemberInfo;
        }

        /// <summary>
        /// 判断两个 <see cref="EventSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="EventSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="EventSpecifier"/></param>
        /// <returns>若两个 <see cref="EventSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(EventSpecifier left, EventSpecifier right)
        {
            return left.MemberInfo != right.MemberInfo;
        }

        #endregion
    }
}
