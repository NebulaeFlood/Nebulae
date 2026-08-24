using Nebulae.Diagnostics;
using Nebulae.Reflection.Extensions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nebulae.Reflection.Specifiers
{
    /// <summary>
    /// <see cref="FieldSpecifier"/> 的模式
    /// </summary>
    public enum FieldSpecifierMode : byte
    {
        /// <summary>
        /// 未设置
        /// </summary>
        None,

        /// <summary>
        /// 获取字段值
        /// </summary>
        Get,

        /// <summary>
        /// 设置字段值
        /// </summary>
        Set,

        /// <summary>
        /// 获取字段引用
        /// </summary>
        Ref
    }


    /// <summary>
    /// 字段的引用说明符
    /// </summary>
    public readonly struct FieldSpecifier : IEquatable<FieldSpecifier>
    {
        private const byte ModeMask = 0B_0000_0011;


        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        /// <summary>
        /// 目标字段的 <see cref="FieldInfo"/>
        /// </summary>
        public readonly FieldInfo MemberInfo;

        /// <summary>
        /// 目标对象
        /// </summary>
        public readonly object? Target;

        #endregion


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 获取 <see cref="FieldSpecifier"/> 的目标绑定状态
        /// </summary>
        /// <remarks>对于静态成员，此项默认为 <see cref="SpecifierBinding.Close"/>。</remarks>
        public SpecifierBinding Binding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (SpecifierBinding)((_flags & Specifier.BindingMask) >> 6);
        }

        /// <summary>
        /// 获取 <see cref="FieldSpecifier"/> 的文化信息
        /// </summary>
        public SpecifierCulture Culture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (SpecifierCulture)((_flags & Specifier.CultureMask) >> 3);
        }

        /// <summary>
        /// 获取 <see cref="FieldSpecifier"/> 的模式
        /// </summary>
        public FieldSpecifierMode Mode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (FieldSpecifierMode)(_flags & ModeMask);
        }

        /// <summary>
        /// 获取 <see cref="FieldSpecifier"/> 的策略
        /// </summary>
        public SpecifierPolicy Policy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (SpecifierPolicy)((_flags & Specifier.PolicyMask) >> 7);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal FieldSpecifier(FieldInfo memberInfo)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;

            if (memberInfo.IsStatic)
            {
                _flags = Specifier.BindingMask;
            }
        }

        private FieldSpecifier(FieldInfo memberInfo, object? target, byte flags)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;
            Target = target;

            _flags = flags;
        }

        #endregion


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
            return obj is FieldSpecifier other
                && _flags == other._flags
                && Target == other.Target
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(FieldSpecifier other)
        {
            return _flags == other._flags
                && Target == other.Target
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 获取当前对象的哈希代码
        /// </summary>
        /// <returns>当前对象的哈希代码。</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(MemberInfo, Target, _flags);
        }

        /// <summary>
        /// 获取表示当前对象的字符串
        /// </summary>
        /// <returns>表示当前对象的字符串。</returns>
        public override string ToString()
        {
            FieldSpecifierMode mode = Mode;

            if (mode is FieldSpecifierMode.None)
            {
                return MemberInfo.AsLog();
            }
            else
            {
                return CompilerHelpers.GetNameBuilder(MemberInfo, mode)
                    .Append('(')
                    .Format(Parameters)
                    .Append(')')
                    .ToString();
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 将引用说明符绑定到指定的目标对象
        /// </summary>
        /// <param name="target">要绑定的目标对象</param>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Bind(object? target)
        {
            return new FieldSpecifier(
                MemberInfo,
                target: target,
                flags: (byte)(_flags | Specifier.BindingMask));
        }

        /// <summary>
        /// 将引用说明符绑定的目标对象移除
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Open()
        {
            return new FieldSpecifier(
                MemberInfo,
                target: null,
                flags: (byte)(_flags & ~Specifier.BindingMask));
        }

        /// <summary>
        /// 将引用说明符的模式配置为获取字段值
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Get()
        {
            return new FieldSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)((_flags & ~ModeMask) | (int)FieldSpecifierMode.Get));
        }

        /// <summary>
        /// 将引用说明符的模式配置为设置字段值
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Set()
        {
            return new FieldSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)((_flags & ~ModeMask) | (int)FieldSpecifierMode.Set));
        }

        /// <summary>
        /// 将引用说明符的模式配置为获取字段引用
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Ref()
        {
            // FieldSpecifierMode.Ref == 0B11;
            return new FieldSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)(_flags | (int)FieldSpecifierMode.Ref));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Lenient()
        {
            return new FieldSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)(_flags | Specifier.PolicyMask));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <param name="culture"><see cref="IConvertible"/> 接口使用的 <see cref="IFormatProvider"/></param>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Lenient(SpecifierCulture culture)
        {
            return new FieldSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)((_flags & ~Specifier.CultureMask) | ((int)culture << 3) | Specifier.PolicyMask));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Strict()
        {
            return new FieldSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)(_flags & ~Specifier.PolicyMask));
        }

        /// <summary>
        /// 创建指定类型的委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>由此 <see cref="FieldSpecifier"/> 编译的委托。</returns>
        /// <remarks>
        /// <para>
        /// 委托中目标对象的参数的类型不会被验证，
        /// 而是在运行时尝试用 <c>castclass</c> 指令转换不匹配的类型。
        /// </para>
        /// <para>
        /// 当 <see cref="Policy"/> 为 <see cref="SpecifierPolicy.Lenient"/> 时，
        /// 除了特殊情况，参数类型不匹配将不会立即抛出异常，
        /// 而是会在运行时尝试用 <c>castclass</c> 或 <see cref="IConvertible"/> 接口转换。
        /// </para>
        /// </remarks>
        public T Compile<T>() where T : Delegate
        {
            try
            {
                CompilerHelpers.VerifyBindingTarget(this);

                Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, verification);

                return IsOpen ? invoker.CreateDelegate<T>() : invoker.CreateDelegate<T>(Target);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot compile field '{MemberInfo.AsLog()}' " +
                    $"of type '{MemberInfo.FieldType.AsLog()}' " +
                    $"to delegate type '{typeof(T).AsLog()}'.", e);
            }
        }

        /// <summary>
        /// 解析并创建指定委托类型的编译器
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>与此 <see cref="FieldSpecifier"/> 关联的 <see cref="Compiler{T}"/>。</returns>
        public Compiler<T> Resolve<T>() where T : Delegate
        {
            try
            {
                if (IsOpen)
                {
                    Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                    DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, verification);
                    return new Compiler<T>(this, invoker);
                }
                else
                {
                    object? target = Target;

                    if (target != Specifier.Defer)
                    {
                        CompilerHelpers.VerifyBindingTarget(target, MemberInfo);
                    }

                    Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                    DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, verification);
                    return new Compiler<T>(this, invoker);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot specify field '{MemberInfo.AsLog()}' " +
                    $"of type '{MemberInfo.FieldType.AsLog()}' as a compiler " +
                    $"with delegate type '{typeof(T).AsLog()}'.", e);
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsOpen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & Specifier.BindingMask) == 0;
        }

        private bool IsStrict
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & Specifier.PolicyMask) == 0;
        }

        private ParameterInfo[] Parameters
        {
            get
            {
                if (IsOpen)
                {
                    return Mode switch
                    {
                        FieldSpecifierMode.Get or FieldSpecifierMode.Ref
                            => [new SpecifierParameterInfo("this", MemberInfo.IsStatic ? typeof(object) : MemberInfo.DeclaringType!)],
                        FieldSpecifierMode.Set
                            => [new SpecifierParameterInfo("this", MemberInfo.IsStatic ? typeof(object) :MemberInfo.DeclaringType!),
                                new SpecifierParameterInfo("value", MemberInfo.FieldType)],
                        _ => []
                    };
                }
                else
                {
                    return Mode switch
                    {
                        FieldSpecifierMode.Set
                            => [new SpecifierParameterInfo("value", MemberInfo.FieldType)],
                        _ => []
                    };
                }
            }
        }

        #endregion


        // Bit layout:
        //
        //     7         6        5       4       3       2        1       0
        // +--------+---------+-------+-------+-------+--------+-------+-------+
        // | Policy | Binding |    Culture (3 bits)   | Unused | Mode (2 bits) |
        // +--------+---------+-------+-------+-------+--------+-------+-------+
        private readonly byte _flags;


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 判断两个 <see cref="FieldSpecifier"/> 是否相等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="FieldSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="FieldSpecifier"/></param>
        /// <returns>若两个 <see cref="FieldSpecifier"/> 相等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator ==(FieldSpecifier left, FieldSpecifier right)
        {
            return left._flags == right._flags
                && left.Target == right.Target
                && left.MemberInfo == right.MemberInfo;
        }

        /// <summary>
        /// 判断两个 <see cref="FieldSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="FieldSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="FieldSpecifier"/></param>
        /// <returns>若两个 <see cref="FieldSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(FieldSpecifier left, FieldSpecifier right)
        {
            return left._flags != right._flags
                || left.Target != right.Target
                || left.MemberInfo != right.MemberInfo;
        }

        #endregion


        /// <summary>
        /// <see cref="FieldSpecifier"/> 的编译器
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        public sealed class Compiler<T> where T : Delegate
        {
            /// <summary>
            /// 关联的 <see cref="FieldSpecifier"/>
            /// </summary>
            public readonly FieldSpecifier Specifier;


            internal Compiler(FieldSpecifier specifier, DynamicMethod invoker)
            {
                Specifier = specifier;

                _invoker = invoker;
            }


            /// <summary>
            /// 获取表示当前对象的字符串
            /// </summary>
            /// <returns>表示当前对象的字符串。</returns>
            public override string ToString()
            {
                return Specifier.ToString();
            }


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            /// <summary>
            /// 编译为指定的委托类型
            /// </summary>
            /// <returns>由 <see cref="Specifier"/> 编译的委托。</returns>
            public T Compile()
            {
                try
                {
                    if (Specifier.IsOpen)
                    {
                        return _invoker.CreateDelegate<T>();
                    }
                    else
                    {
                        object? target = Specifier.Target;

                        SpecifierVerifier.ThrowIfDeferTarget(target);
                        return _invoker.CreateDelegate<T>(target);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile field '{Specifier.MemberInfo.AsLog()}' " +
                        $"of type '{Specifier.MemberInfo.FieldType.AsLog()}' " +
                        $"to delegate type '{typeof(T).AsLog()}'.", e);
                }
            }

            /// <summary>
            /// 编译为指定的委托类型并绑定到目标对象
            /// </summary>
            /// <param name="target">要绑定的目标对象</param>
            /// <returns>由 <see cref="Specifier"/> 编译的委托。</returns>
            /// <remarks>只有 <see cref="Binding"/> 为 <see cref="SpecifierBinding.Close"/> 时，才能使用此方法。</remarks>
            public T Compile(object? target)
            {
                if (Specifier.IsOpen)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile field '{Specifier.MemberInfo.AsLog()}' " +
                        $"with an explicit target because the specifier is open.");
                }

                try
                {
                    SpecifierVerifier.ThrowIfDeferTarget(target);
                    CompilerHelpers.VerifyBindingTarget(target, Specifier.MemberInfo);
                    return _invoker.CreateDelegate<T>(target);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile field '{Specifier.MemberInfo.AsLog()}' " +
                        $"of type '{Specifier.MemberInfo.FieldType.AsLog()}' " +
                        $"to delegate type '{typeof(T).AsLog()}'.", e);
                }
            }

            #endregion


            private readonly DynamicMethod _invoker;
        }

        private static class CompilerHelpers
        {
            public static DynamicMethod CreateInvoker(in FieldSpecifier specifier, Verification verification)
            {
                FieldInfo member = specifier.MemberInfo;
                FieldSpecifierMode mode = specifier.Mode;

                Type[] parameterTypes = verification.ParameterTypes;
                Type returnType = verification.ReturnType;

                DynamicMethod invoker = new(
                    GetNameBuilder(member, mode).ToString(),
                    returnType,
                    parameterTypes,
                    restrictedSkipVisibility: true);
                ILGenerator il = invoker.GetILGenerator();

                if (!member.IsStatic)
                {
                    il.EmitLdtarg(parameterTypes[0], member.DeclaringType!, 0);
                }

                switch (mode)
                {
                    case FieldSpecifierMode.Get:
                        if (verification.ReturnCompatible)
                        {
                            il.Emit(member.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, member);
                            break;
                        }

                        if (member.FieldType.IsValueType)
                        {
                            il.Emit(member.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, member);
                            il.EmitConv(member.FieldType.MakeByRefType(), returnType, specifier.Culture);
                        }
                        else
                        {
                            il.Emit(member.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, member);
                            il.EmitConv(member.FieldType, returnType, specifier.Culture);
                        }

                        break;
                    case FieldSpecifierMode.Set:
                        if (verification.ParameterCompatible)
                        {
                            il.Emit(OpCodes.Ldarg_1);
                        }
                        else
                        {
                            Type parameterType = parameterTypes[1];

                            if (parameterType.IsValueType)
                            {
                                parameterType = parameterType.MakeByRefType();

                                il.Emit(OpCodes.Ldarga_S, 1);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldarg_1);
                            }

                            il.EmitConv(parameterType, member.FieldType, specifier.Culture);
                        }

                        il.Emit(member.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, member);
                        break;
                    case FieldSpecifierMode.Ref:
                        il.Emit(member.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, member);
                        break;
                    default:
                        throw new NotSupportedException($"Cannot compile delegate because mode '{mode}' is not supported.");
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(FieldInfo field, FieldSpecifierMode mode)
            {
                return new StringBuilder(128)
                    .Format(field.FieldType)
                    .Append(' ')
                    .Format(typeof(Reflector))
                    .Append('.')
                    .Append(mode)
                    .Append("<>")
                    .Format(field.DeclaringType!)
                    .Append('.')
                    .Format(field.Name);
            }

            public static void VerifyBindingTarget(in FieldSpecifier specifier)
            {
                if (specifier.IsOpen)
                {
                    return;
                }

                object? target = specifier.Target;

                SpecifierVerifier.ThrowIfDeferTarget(target);
                VerifyBindingTarget(target, specifier.MemberInfo);
            }

            public static void VerifyBindingTarget(object? target, FieldInfo field)
            {
                if (target is null)
                {
                    if (!field.IsStatic)
                    {
                        throw new ArgumentException(
                            $"Expects a non-null target object " +
                            $"for instance field '{field.AsLog()}', " +
                            $"but received '{DiagnosticHelpers.Null}'.");
                    }
                }
                else if (field.IsStatic)
                {
                    throw new ArgumentException(
                        $"Expects a null target object " +
                        $"for static field '{field.AsLog()}', " +
                        $"but received object '{target.AsLog()}'.");
                }
                else
                {
                    Type targetType = target.GetType();
                    Type declaringType = field.DeclaringType!;

                    if (!declaringType.IsAssignableFrom(targetType))
                    {
                        throw new ArgumentException(
                            $"Expects a target object of type '{declaringType.AsLog()}' " +
                            $"for instance field '{field.AsLog()}', " +
                            $"but received object '{target.AsLog()}' of type '{targetType.AsLog()}'.");
                    }
                }
            }

            public static Verification VerifyDelegate<T>(in FieldSpecifier specifier)
                where T : Delegate
            {
                Type delegateType = typeof(T);

                if (delegateType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"Cannot compile to abstract delegate type '{delegateType.AsLog()}'.");
                }

                MethodInfo invocation = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)
                    ?? throw new NotSupportedException(
                        $"Cannot compile to delegate type '{delegateType.AsLog()}', " +
                        $"because it does not have any method named 'Invoke'.");

                Type returnType = invocation.ReturnType;
                Type memberType = specifier.MemberInfo.FieldType;

                ParameterInfo[] invokerParameters;
                Type targetType;

                switch (specifier.Mode)
                {
                    case FieldSpecifierMode.Get:
                        invokerParameters = invocation.GetParameters();

                        if (specifier.IsOpen)
                        {
                            if (invokerParameters.Length is not 1)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 1 parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Get)}' " +
                                    $"when the target is not bound, " +
                                    $"but received {invokerParameters.Length} parameter(s).");
                            }

                            targetType = specifier.MemberInfo.IsStatic
                                ? typeof(object)
                                : invokerParameters[0].ParameterType;
                        }
                        else
                        {
                            if (invokerParameters.Length is not 0)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 0 parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Get)}' " +
                                    $"when the target is bound, " +
                                    $"but received {invokerParameters.Length} parameter(s).");
                            }

                            targetType = specifier.MemberInfo.IsStatic
                                ? typeof(object)
                                : specifier.MemberInfo.DeclaringType!;
                        }

                        return new Verification(
                            [targetType],
                            true,
                            returnType,
                            SpecifierVerifier.VerifyReturnType(
                                returnType, memberType, specifier.IsStrict));
                    case FieldSpecifierMode.Set:
                        invokerParameters = invocation.GetParameters();
                        Type valueType;

                        if (specifier.IsOpen)
                        {
                            if (invokerParameters.Length is not 2)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 2 parameters " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Set)}' " +
                                    $"when the target is not bound, " +
                                    $"but received {invokerParameters.Length} parameter(s).");
                            }

                            targetType = specifier.MemberInfo.IsStatic
                                ? typeof(object)
                                : invokerParameters[0].ParameterType;
                            valueType = invokerParameters[1].ParameterType;
                        }
                        else
                        {
                            if (invokerParameters.Length is not 1)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 1 parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Set)}' " +
                                    $"when the target is bound, " +
                                    $"but received {invokerParameters.Length} parameter(s).");
                            }

                            targetType = specifier.MemberInfo.IsStatic
                                ? typeof(object)
                                : specifier.MemberInfo.DeclaringType!;
                            valueType = invokerParameters[0].ParameterType;
                        }

                        return new Verification(
                            [targetType, valueType],
                            SpecifierVerifier.VerifyArgumentType(
                                valueType, memberType, specifier.IsStrict, 1),
                            returnType,
                            SpecifierVerifier.VerifyReturnType(
                                returnType, typeof(void), specifier.IsStrict));
                    case FieldSpecifierMode.Ref:
                        if (!memberType.IsByRef)
                        {
                            memberType = memberType.MakeByRefType();
                        }

                        invokerParameters = invocation.GetParameters();

                        if (specifier.IsOpen)
                        {
                            if (invokerParameters.Length is not 1)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 1 parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Ref)}' " +
                                    $"when the target is not bound, " +
                                    $"but received {invokerParameters.Length} parameter(s).");
                            }

                            targetType = specifier.MemberInfo.IsStatic
                                ? typeof(object)
                                : invokerParameters[0].ParameterType;
                        }
                        else
                        {
                            if (invokerParameters.Length is not 0)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 0 parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Ref)}' " +
                                    $"when the target is bound, " +
                                    $"but received {invokerParameters.Length} parameter(s).");
                            }

                            targetType = specifier.MemberInfo.IsStatic
                                ? typeof(object)
                                : specifier.MemberInfo.DeclaringType!;
                        }

                        return new Verification(
                            [targetType],
                            true,
                            returnType,
                            SpecifierVerifier.VerifyReturnType(
                                returnType, memberType, specifier.IsStrict));
                    default:
                        throw new NotSupportedException(
                            $"Compile mode '{specifier.Mode}' is not supported.");
                }
            }
        }

        private readonly ref struct Verification(
            Type[] parameterTypes,
            bool parameterCompatible,
            Type returnType,
            bool returnCompatible)
        {
            public readonly Type[] ParameterTypes = parameterTypes;

            public readonly bool ParameterCompatible = parameterCompatible;

            public readonly Type ReturnType = returnType;

            public readonly bool ReturnCompatible = returnCompatible;
        }
    }
}
