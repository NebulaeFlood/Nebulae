using Nebulae.Diagnostics;
using Nebulae.Reflection.Specifiers;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Nebulae.Reflection
{
    /// <summary>
    /// 提供用于创建引用说明符的扩展方法
    /// </summary>
    /// <remarks>
    /// 不支持如下成员：
    /// <list type="bullet">
    /// <item>
    /// <see cref="MemberInfo.DeclaringType"/> 为 <see langword="null"/> 的成员。
    /// </item>
    /// <item>
    /// <see cref="MemberInfo.DeclaringType"/> 的 <see cref="Type.ContainsGenericParameters"/> 为 <see langword="true"/> 的成员。
    /// </item>
    /// <item>
    /// <see cref="MethodBase.ContainsGenericParameters"/> 为 <see langword="true"/> 的成员。
    /// </item>
    /// </list>
    /// </remarks>
    [DebuggerStepThrough]
    public static class Specifier
    {
        //------------------------------------------------------
        //
        //  Internal Constants
        //
        //------------------------------------------------------

        #region Internal Constants

        // Bit layout:
        //
        //     7         6        5       4       3       2       1       0
        // +--------+---------+-------+-------+-------+-------+-------+-------+
        // | Policy | Binding |    Culture (3 bits)   |    Unused (3 bits)    |
        // +--------+---------+-------+-------+-------+-------+-------+-------+
        private const byte BindingMask = 0b_0100_0000;
        private const byte BindingOffset = 6;
        private const byte CultureMask = 0b_0011_1000;
        private const byte CultureOffset = 3;
        private const byte PolicyMask = 0b_1000_0000;
        private const byte PolicyOffset = 7;

        #endregion


        //------------------------------------------------------
        //
        //  Flags Helpers
        //
        //------------------------------------------------------

        #region Flags Helpers

        internal static byte Close(byte flags)
        {
            return (byte)(flags | BindingMask);
        }

        internal static byte Open(byte flags)
        {
            return (byte)(flags & ~BindingMask);
        }

        internal static byte Lenient(byte flags)
        {
            return (byte)((flags & ~(CultureMask | PolicyMask)) | PolicyMask);
        }

        internal static byte Lenient(byte flags, SpecifierCulture culture)
        {
            return (byte)(
                (flags & ~(CultureMask | PolicyMask))
                | (((int)culture << CultureOffset) & CultureMask)
                | PolicyMask);
        }

        internal static byte Strict(byte flags)
        {
            return (byte)(flags & ~(CultureMask | PolicyMask));
        }

        internal static SpecifierBinding GetBinding(byte flags)
        {
            return (SpecifierBinding)((flags & BindingMask) >> BindingOffset);
        }

        internal static SpecifierCulture GetCulture(byte flags)
        {
            return (SpecifierCulture)((flags & CultureMask) >> CultureOffset);
        }

        internal static SpecifierPolicy GetPolicy(byte flags)
        {
            return (SpecifierPolicy)((flags & PolicyMask) >> PolicyOffset);
        }

        internal static bool IsOpen(byte flags)
        {
            return (flags & BindingMask) == 0;
        }

        internal static bool IsStrict(byte flags)
        {
            return (flags & PolicyMask) == 0;
        }

        #endregion


        /// <summary>
        /// 表示延迟绑定目标对象的占位符
        /// </summary>
        /// <remarks>
        /// 使用此对象作为成员占位符的目标对象时，表示实际目标对象将在创建委托时提供。
        /// </remarks>
        public static readonly object Defer = new DeferObject();


        /// <summary>
        /// 创建指定构造函数的引用说明符
        /// </summary>
        /// <param name="constructorInfo">目标构造函数的 <see cref="ConstructorInfo"/></param>
        /// <returns>指定构造函数的 <see cref="ConstructorSpecifier"/>。</returns>
        public static ConstructorSpecifier Specify(this ConstructorInfo constructorInfo)
        {
            ThrowHelpers.ThrowIfArgumentNull(constructorInfo);

            if (constructorInfo.IsStatic)
            {
                throw new ArgumentException(
                    $"Cannot specify constructor '{constructorInfo.AsLog()}' " +
                    $"because it is a static constructor.");
            }

            Type declaringType = constructorInfo.DeclaringType ?? throw new ArgumentException(
                $"Cannot specify constructor '{constructorInfo.AsLog()}' " +
                $"because it does not have a declaring type.");

            if (declaringType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"Cannot specify constructor '{constructorInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"contains generic parameters that " +
                    $"have not been replaced with concrete types.");
            }

            return new ConstructorSpecifier(constructorInfo);
        }

        /// <summary>
        /// 创建指定事件的引用说明符
        /// </summary>
        /// <param name="eventInfo">目标事件的 <see cref="EventInfo"/></param>
        /// <returns>指定事件的 <see cref="EventSpecifier"/>。</returns>
        public static EventSpecifier Specify(this EventInfo eventInfo)
        {
            ThrowHelpers.ThrowIfArgumentNull(eventInfo);

            Type declaringType = eventInfo.DeclaringType ?? throw new ArgumentException(
                $"Cannot specify event '{eventInfo.AsLog()}' " +
                $"because it does not have a declaring type.");

            if (declaringType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"Cannot specify event '{eventInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"contains generic parameters that " +
                    $"have not been replaced with concrete types.");
            }

            return new EventSpecifier(eventInfo);
        }

        /// <summary>
        /// 创建指定字段的引用说明符
        /// </summary>
        /// <param name="fieldInfo">目标字段的 <see cref="FieldInfo"/></param>
        /// <returns>指定字段的 <see cref="FieldSpecifier"/>。</returns>
        public static FieldSpecifier Specify(this FieldInfo fieldInfo)
        {
            ThrowHelpers.ThrowIfArgumentNull(fieldInfo);

            Type declaringType = fieldInfo.DeclaringType ?? throw new ArgumentException(
                $"Cannot specify field '{fieldInfo.AsLog()}' " +
                $"because it does not have a declaring type.");

            if (declaringType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"Cannot specify field '{fieldInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"contains generic parameters that " +
                    $"have not been replaced with concrete types.");
            }

            return new FieldSpecifier(fieldInfo);
        }

        /// <summary>
        /// 创建指定方法的引用说明符
        /// </summary>
        /// <param name="methodInfo">目标方法的 <see cref="MethodInfo"/></param>
        /// <returns>指定方法的 <see cref="MethodSpecifier"/>。</returns>
        public static MethodSpecifier Specify(this MethodInfo methodInfo)
        {
            ThrowHelpers.ThrowIfArgumentNull(methodInfo);

            Type declaringType = methodInfo.DeclaringType ?? throw new ArgumentException(
                $"Cannot specify method '{methodInfo.AsLog()}' " +
                $"because it does not have a declaring type.");

            if (declaringType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"Cannot specify method '{methodInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"contains generic parameters that " +
                    $"have not been replaced with concrete types.");
            }

            if (methodInfo.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"Cannot specify method '{methodInfo.AsLog()}' " +
                    $"because it contains generic parameters that " +
                    $"have not been replaced with concrete types.");
            }

            return new MethodSpecifier(methodInfo);
        }

        /// <summary>
        /// 创建指定属性的引用说明符
        /// </summary>
        /// <param name="propertyInfo">目标属性的 <see cref="PropertyInfo"/></param>
        /// <returns>指定属性的 <see cref="PropertySpecifier"/>。</returns>
        public static PropertySpecifier Specify(this PropertyInfo propertyInfo)
        {
            ThrowHelpers.ThrowIfArgumentNull(propertyInfo);

            Type declaringType = propertyInfo.DeclaringType ?? throw new ArgumentException(
                $"Cannot specify property '{propertyInfo.AsLog()}' " +
                $"because it does not have a declaring type.");

            if (declaringType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"Cannot specify property '{propertyInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"contains generic parameters that " +
                    $"have not been replaced with concrete types.");
            }

            return new PropertySpecifier(propertyInfo);
        }


        /// <summary>
        /// 成员说明符的编译器
        /// </summary>
        /// <typeparam name="TDelegate">委托类型</typeparam>
        public interface ICompiler<out TDelegate> where TDelegate : Delegate
        {
            /// <summary>
            /// 编译为指定的委托类型
            /// </summary>
            /// <returns>按成员说明符编译的委托。</returns>
            public TDelegate Compile();

            /// <summary>
            /// 编译为指定的委托类型并绑定到目标对象
            /// </summary>
            /// <typeparam name="T">目标对象的类型</typeparam>
            /// <param name="target">要绑定的目标对象</param>
            /// <returns>按成员说明符编译的委托。</returns>
            /// <remarks>只有成员说明符的 <see cref="SpecifierBinding"/> 为 <see cref="SpecifierBinding.Close"/> 时，才能使用此方法。</remarks>
            public TDelegate Compile<T>(T target);
        }


        [DebuggerDisplay(Display)]
        private sealed class DeferObject
        {
            private const string Display =
                $"{nameof(Nebulae)}.{nameof(Reflection)}.{nameof(Specifier)}.{nameof(Defer)}";


            public override string ToString()
            {
                return Display;
            }
        }
    }
}
