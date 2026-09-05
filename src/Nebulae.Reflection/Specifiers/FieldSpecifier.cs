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
        private const byte ModeMask = 0b_0000_0011;


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
            get => Specifier.GetBinding(_flags);
        }

        /// <summary>
        /// 获取 <see cref="FieldSpecifier"/> 的文化信息
        /// </summary>
        public SpecifierCulture Culture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetCulture(_flags);
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
            get => Specifier.GetPolicy(_flags);
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
                _flags = Specifier.Close(default);
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
                && Equals(this, other);
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(FieldSpecifier other)
        {
            return _flags == other._flags
                && Equals(Target, other.Target)
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
                target,
                Specifier.Close(_flags));
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
                Specifier.Open(_flags));
        }

        /// <summary>
        /// 将引用说明符的模式配置为获取字段值
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Get()
        {
            return new FieldSpecifier(
                MemberInfo,
                Target,
                (byte)((_flags & ~ModeMask) | (int)FieldSpecifierMode.Get));
        }

        /// <summary>
        /// 将引用说明符的模式配置为设置字段值
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Set()
        {
            return new FieldSpecifier(
                MemberInfo,
                Target,
                (byte)((_flags & ~ModeMask) | (int)FieldSpecifierMode.Set));
        }

        /// <summary>
        /// 将引用说明符的模式配置为获取字段引用
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Ref()
        {
            // FieldSpecifierMode.Ref == 0b_11;
            return new FieldSpecifier(
                MemberInfo,
                Target,
                (byte)(_flags | (int)FieldSpecifierMode.Ref));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Lenient()
        {
            return new FieldSpecifier(
                MemberInfo,
                Target,
                Specifier.Lenient(_flags));
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
                Target,
                Specifier.Lenient(_flags, culture));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="FieldSpecifier"/>。</returns>
        public FieldSpecifier Strict()
        {
            return new FieldSpecifier(
                MemberInfo,
                Target,
                Specifier.Strict(_flags));
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

                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return IsOpen ? invoker.CreateDelegate<T>() : invoker.CreateDelegate<T>(Target);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot compile field '{MemberInfo.AsLog()}' " +
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
                if (Target != Specifier.Defer)
                {
                    CompilerHelpers.VerifyBindingTarget(this);
                }

                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return new Compiler<T>(this, invoker);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot specify field '{MemberInfo.AsLog()}' as a compiler " +
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
            get => Specifier.IsOpen(_flags);
        }

        private bool IsStrict
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.IsStrict(_flags);
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
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个 <see cref="FieldSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="FieldSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="FieldSpecifier"/></param>
        /// <returns>若两个 <see cref="FieldSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(FieldSpecifier left, FieldSpecifier right)
        {
            return !left.Equals(right);
        }

        #endregion


        /// <summary>
        /// <see cref="FieldSpecifier"/> 的编译器
        /// </summary>
        /// <typeparam name="TDelegate">委托类型</typeparam>
        public sealed class Compiler<TDelegate> : Specifier.ICompiler<TDelegate>
            where TDelegate : Delegate
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
            /// <returns>按 <see cref="Specifier"/> 编译的委托。</returns>
            public TDelegate Compile()
            {
                try
                {
                    if (Specifier.IsOpen)
                    {
                        return _invoker.CreateDelegate<TDelegate>();
                    }

                    object? target = Specifier.Target;
                    SpecifierVerifier.ThrowIfDeferTarget(target);

                    return _invoker.CreateDelegate<TDelegate>(target);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile field '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(TDelegate).AsLog()}'.", e);
                }
            }

            /// <summary>
            /// 编译为指定的委托类型并绑定到目标对象
            /// </summary>
            /// <typeparam name="T">目标对象的类型</typeparam>
            /// <param name="target">要绑定的目标对象</param>
            /// <returns>按 <see cref="Specifier"/> 编译的委托。</returns>
            /// <remarks>只有 <see cref="Binding"/> 为 <see cref="SpecifierBinding.Close"/> 时，才能使用此方法。</remarks>
            public TDelegate Compile<T>(T target)
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

                    FieldInfo member = Specifier.MemberInfo;
                    SpecifierVerifier.VerifyBindingTarget(target, member, member.IsStatic);

                    return _invoker.CreateDelegate<TDelegate>(target);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile field '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(TDelegate).AsLog()}'.", e);
                }
            }

            #endregion


            private readonly DynamicMethod _invoker;
        }


        private static class CompilerHelpers
        {
            public static DynamicMethod CreateInvoker(in FieldSpecifier specifier, in SpecifierInvokerInfo invokerInfo)
            {
                Debug.Assert(invokerInfo.IsDynamic, "Field invokers must be dynamic.");

                SpecifierCulture culture = specifier.Culture;
                FieldInfo member = specifier.MemberInfo;
                FieldSpecifierMode mode = specifier.Mode;

                SpecifierInvokerInfo.DelegateInfo delegateInfo = invokerInfo.Delegate;
                SpecifierInvokerInfo.ArgumentInfo[] arguments = invokerInfo.Arguments!;

                Type[] parameterTypes = invokerInfo.ParameterTypes!;

                DynamicMethod invoker = new(
                    GetNameBuilder(member, mode).ToString(),
                    delegateInfo.ReturnType,
                    parameterTypes,
                    restrictedSkipVisibility: true);
                ILGenerator il = invoker.GetILGenerator();

                if (!invokerInfo.IsStatic)
                {
                    il.EmitLdtarg(parameterTypes[0], member.DeclaringType!, 0);
                }

                switch (mode)
                {
                    case FieldSpecifierMode.Get:
                        if (invokerInfo.Return.IsCompatible)
                        {
                            il.Emit(invokerInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, member);
                            break;
                        }

                        if (member.FieldType.IsValueType)
                        {
                            il.Emit(invokerInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, member);
                            il.EmitConv(member.FieldType.MakeByRefType(), delegateInfo.ReturnType, culture);
                        }
                        else
                        {
                            il.Emit(member.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, member);
                            il.EmitConv(invokerInfo.Return.Type, delegateInfo.ReturnType, culture);
                        }

                        break;
                    case FieldSpecifierMode.Set:
                        ref readonly SpecifierInvokerInfo.ArgumentInfo argument = ref arguments[0];

                        Debug.Assert(
                            argument.Source is SpecifierInvokerInfo.ArgumentSource.Parameter,
                            "Field set arguments must come from delegate parameters.");

                        short sourceIndex = (short)argument.SourceIndex;

                        if (argument.IsCompatible)
                        {
                            il.EmitLdarg(sourceIndex);
                        }
                        else
                        {
                            Type sourceType = argument.ArgumentType;

                            if (sourceType.IsValueType)
                            {
                                sourceType = sourceType.MakeByRefType();

                                il.Emit(OpCodes.Ldarga_S, sourceIndex);
                            }
                            else
                            {
                                il.EmitLdarg(sourceIndex);
                            }

                            il.EmitConv(sourceType, argument.ParameterType, culture);
                        }

                        il.Emit(invokerInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, member);
                        break;
                    case FieldSpecifierMode.Ref:
                        il.Emit(invokerInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, member);
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Compile mode '{mode}' is not supported.");
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(FieldInfo member, FieldSpecifierMode mode)
            {
                return new StringBuilder(128)
                    .Format(typeof(Reflector))
                    .Append('.')
                    .Append(mode)
                    .Append("<>")
                    .Format(member.DeclaringType!)
                    .Append('.')
                    .Format(member.Name);
            }

            public static void VerifyBindingTarget(in FieldSpecifier specifier)
            {
                if (specifier.IsOpen)
                {
                    return;
                }

                object? target = specifier.Target;
                SpecifierVerifier.ThrowIfDeferTarget(target);

                FieldInfo member = specifier.MemberInfo;
                SpecifierVerifier.VerifyBindingTarget(target, member, member.IsStatic);
            }

            public static SpecifierInvokerInfo VerifyDelegate<T>(in FieldSpecifier specifier)
                where T : Delegate
            {
                SpecifierInvokerInfo.DelegateInfo delegateInfo = SpecifierVerifier.VerifyDelegate<T>();
                FieldInfo member = specifier.MemberInfo;

                Type returnType = delegateInfo.ReturnType;
                Type memberType = member.FieldType;

                ParameterInfo[] delegateParameters = delegateInfo.Parameters;
                Type targetType;

                switch (specifier.Mode)
                {
                    case FieldSpecifierMode.Get:
                        if (specifier.IsOpen)
                        {
                            if (delegateParameters.Length is not 1)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 1 delegate parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Get)}' " +
                                    $"when the target is not bound, " +
                                    $"but received {delegateParameters.Length} parameter(s).");
                            }

                            targetType = delegateParameters[0].ParameterType;
                        }
                        else
                        {
                            if (delegateParameters.Length is not 0)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 0 delegate parameters " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Get)}' " +
                                    $"when the target is bound, " +
                                    $"but received {delegateParameters.Length} parameter(s).");
                            }

                            targetType = member.IsStatic
                                ? typeof(object)
                                : member.DeclaringType!;
                        }

                        return new SpecifierInvokerInfo(
                            delegateInfo,
                            isStatic: member.IsStatic,
                            parameterTypes: [targetType],
                            arguments: [],
                            SpecifierVerifier.VerifyReturn(
                                returnType, memberType, specifier.IsStrict));
                    case FieldSpecifierMode.Set:
                        Type valueType;

                        if (specifier.IsOpen)
                        {
                            if (delegateParameters.Length is not 2)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 2 delegate parameters " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Set)}' " +
                                    $"when the target is not bound, " +
                                    $"but received {delegateParameters.Length} parameter(s).");
                            }

                            targetType = delegateParameters[0].ParameterType;
                            valueType = delegateParameters[1].ParameterType;
                        }
                        else
                        {
                            if (delegateParameters.Length is not 1)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 1 delegate parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Set)}' " +
                                    $"when the target is bound, " +
                                    $"but received {delegateParameters.Length} parameter(s).");
                            }

                            targetType = member.IsStatic
                                ? typeof(object)
                                : member.DeclaringType!;
                            valueType = delegateParameters[0].ParameterType;
                        }

                        return new SpecifierInvokerInfo(
                            delegateInfo,
                            isStatic: member.IsStatic,
                            parameterTypes: [targetType, valueType],
                            arguments: [new SpecifierInvokerInfo.ArgumentInfo(
                                SpecifierInvokerInfo.ArgumentSource.Parameter,
                                1,
                                valueType,
                                memberType,
                                SpecifierVerifier.VerifyArgumentType(
                                    valueType, memberType, specifier.IsStrict, 1))],
                            SpecifierVerifier.VerifyReturn(
                                returnType, typeof(void), specifier.IsStrict));
                    case FieldSpecifierMode.Ref:
                        if (!memberType.IsByRef)
                        {
                            memberType = memberType.MakeByRefType();
                        }

                        if (specifier.IsOpen)
                        {
                            if (delegateParameters.Length is not 1)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 1 delegate parameter " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Ref)}' " +
                                    $"when the target is not bound, " +
                                    $"but received {delegateParameters.Length} parameter(s).");
                            }

                            targetType = delegateParameters[0].ParameterType;
                        }
                        else
                        {
                            if (delegateParameters.Length is not 0)
                            {
                                throw new ArgumentException(
                                    $"Expects exactly 0 delegate parameters " +
                                    $"in compile mode '{nameof(FieldSpecifierMode.Ref)}' " +
                                    $"when the target is bound, " +
                                    $"but received {delegateParameters.Length} parameter(s).");
                            }

                            targetType = member.IsStatic
                                ? typeof(object)
                                : member.DeclaringType!;
                        }

                        return new SpecifierInvokerInfo(
                            delegateInfo,
                            isStatic: member.IsStatic,
                            parameterTypes: [targetType],
                            arguments: [],
                            SpecifierVerifier.VerifyReturn(
                                returnType, memberType, specifier.IsStrict));
                    default:
                        throw new NotSupportedException(
                            $"Compile mode '{specifier.Mode}' is not supported.");
                }
            }
        }
    }
}
