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
        internal const byte BindingMask = 0B_0100_0000;
        internal const byte CultureMask = 0B_0011_1000;
        internal const byte PolicyMask = 0B_1000_0000;

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

            if (declaringType.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"Cannot specify constructor '{constructorInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"is a generic type definition.");
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

            if (declaringType.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"Cannot specify event '{eventInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"is a generic type definition.");
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

            if (declaringType.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"Cannot specify field '{fieldInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"is a generic type definition.");
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

            if (declaringType.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"Cannot specify method '{methodInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"is a generic type definition.");
            }

            if (methodInfo.IsGenericMethodDefinition)
            {
                throw new ArgumentException(
                    $"Cannot specify method '{methodInfo.AsLog()}' " +
                    $"because it is a generic method definition.");
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

            if (declaringType.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"Cannot specify property '{propertyInfo.AsLog()}' " +
                    $"because its declaring type '{declaringType.AsLog()}' " +
                    $"is a generic type definition.");
            }
            return new PropertySpecifier(propertyInfo);
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
