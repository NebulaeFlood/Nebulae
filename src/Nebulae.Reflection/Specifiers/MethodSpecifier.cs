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
    /// 方法的引用说明符
    /// </summary>
    public readonly struct MethodSpecifier : IEquatable<MethodSpecifier>
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        /// <summary>
        /// 目标方法的 <see cref="MethodInfo"/>
        /// </summary>
        public readonly MethodInfo MemberInfo;

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
        /// 获取 <see cref="MethodSpecifier"/> 的目标绑定状态
        /// </summary>
        /// <remarks>对于静态成员，此项默认为 <see cref="SpecifierBinding.Close"/>。</remarks>
        public SpecifierBinding Binding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetBinding(_flags);
        }

        /// <summary>
        /// 获取 <see cref="MethodSpecifier"/> 的文化信息
        /// </summary>
        public SpecifierCulture Culture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetCulture(_flags);
        }

        /// <summary>
        /// 获取 <see cref="MethodSpecifier"/> 的策略
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

        internal MethodSpecifier(MethodInfo memberInfo)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;

            if (memberInfo.IsStatic)
            {
                _flags = Specifier.Close(default);
            }
        }

        private MethodSpecifier(MethodInfo memberInfo, object? target, byte flags)
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
            return obj is MethodSpecifier other
                && Equals(other);
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(MethodSpecifier other)
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
            return CompilerHelpers.GetNameBuilder(MemberInfo)
                .Append('(')
                .Format(Parameters)
                .Append(')')
                .ToString();
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
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Bind(object? target)
        {
            return new MethodSpecifier(
                MemberInfo,
                target,
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符绑定的目标对象移除
        /// </summary>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Open()
        {
            return new MethodSpecifier(
                MemberInfo,
                target: null,
                Specifier.Open(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input(params scoped ReadOnlySpan<object?> args)
        {
            SpecifierVerifier.ThrowIfInputEmpty(args);

            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.ArrayClosure(args.ToArray()),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T>(T arg)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T>(arg),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2>(T1 arg1, T2 arg2)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2>(arg1, arg2),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2, T3>(arg1, arg2, arg3),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2, T3, T4>(arg1, arg2, arg3, arg4),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Lenient()
        {
            return new MethodSpecifier(
                MemberInfo,
                Target,
                Specifier.Lenient(_flags));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <param name="culture"><see cref="IConvertible"/> 接口使用的 <see cref="IFormatProvider"/></param>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Lenient(SpecifierCulture culture)
        {
            return new MethodSpecifier(
                MemberInfo,
                Target,
                Specifier.Lenient(_flags, culture));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Strict()
        {
            return new MethodSpecifier(
                MemberInfo,
                Target,
                Specifier.Strict(_flags));
        }

        /// <summary>
        /// 创建指定类型的委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>由此 <see cref="MethodSpecifier"/> 编译的委托。</returns>
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
                MethodInfo invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return IsOpen ? invoker.CreateDelegate<T>() : invoker.CreateDelegate<T>(Target);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot compile method '{MemberInfo.AsLog()}' " +
                    $"to delegate type '{typeof(T).AsLog()}'.", e);
            }
        }

        /// <summary>
        /// 解析并创建指定委托类型的编译器
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>与此 <see cref="MethodSpecifier"/> 关联的 <see cref="Compiler{T}"/>。</returns>
        public Compiler<T> Resolve<T>() where T : Delegate
        {
            try
            {
                if (Target != Specifier.Defer)
                {
                    CompilerHelpers.VerifyBindingTarget(this);
                }

                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this);
                MethodInfo invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return new Compiler<T>(this, invoker);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot specify method '{MemberInfo.AsLog()}' as a compiler " +
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
                ParameterInfo[] parameters = MemberInfo.GetParameters();

                if (IsOpen)
                {
                    return [
                        new SpecifierParameterInfo(
                            "this",
                            MemberInfo.IsStatic ? typeof(object) : MemberInfo.DeclaringType!),
                        ..
                        parameters];
                }
                else
                {
                    return parameters;
                }
            }
        }

        #endregion


        // Bit layout:
        //
        //     7         6        5       4       3       2       1       0
        // +--------+---------+-------+-------+-------+-------+-------+-------+
        // | Policy | Binding |    Culture (3 bits)   |    Unused (3 bits)    |
        // +--------+---------+-------+-------+-------+-------+-------+-------+
        private readonly byte _flags;


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 判断两个 <see cref="MethodSpecifier"/> 是否相等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="MethodSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="MethodSpecifier"/></param>
        /// <returns>若两个 <see cref="MethodSpecifier"/> 相等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator ==(MethodSpecifier left, MethodSpecifier right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个 <see cref="MethodSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="MethodSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="MethodSpecifier"/></param>
        /// <returns>若两个 <see cref="MethodSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(MethodSpecifier left, MethodSpecifier right)
        {
            return !left.Equals(right);
        }

        #endregion


        /// <summary>
        /// <see cref="MethodSpecifier"/> 的编译器
        /// </summary>
        /// <typeparam name="TDelegate">委托类型</typeparam>
        public sealed class Compiler<TDelegate> : Specifier.ICompiler<TDelegate>
            where TDelegate : Delegate
        {
            /// <summary>
            /// 关联的 <see cref="MethodSpecifier"/>
            /// </summary>
            public readonly MethodSpecifier Specifier;


            internal Compiler(MethodSpecifier specifier, MethodInfo invoker)
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
                        $"Cannot compile method '{Specifier.MemberInfo.AsLog()}' " +
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
                        $"Cannot compile method '{Specifier.MemberInfo.AsLog()}' " +
                        $"with an explicit target because the specifier is open.");
                }

                try
                {
                    SpecifierVerifier.ThrowIfDeferTarget(target);

                    MethodInfo member = Specifier.MemberInfo;
                    SpecifierVerifier.VerifyBindingTarget(target, member, member.IsStatic);

                    return _invoker.CreateDelegate<TDelegate>(target);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile method '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(TDelegate).AsLog()}'.", e);
                }
            }

            #endregion


            private readonly MethodInfo _invoker;
        }


        private static class CompilerHelpers
        {
            public static MethodInfo CreateInvoker(in MethodSpecifier specifier, in SpecifierInvokerInfo invokerInfo)
            {
                if (!invokerInfo.IsDynamic)
                {
                    return specifier.MemberInfo;
                }

                SpecifierCulture culture = specifier.Culture;
                MethodInfo member = specifier.MemberInfo;

                SpecifierInvokerInfo.DelegateInfo delegateInfo = invokerInfo.Delegate;

                SpecifierInvokerInfo.ArgumentInfo[] arguments = invokerInfo.Arguments;
                Type[] parameterTypes = invokerInfo.ParameterTypes!;

                DynamicMethod invoker = new(
                    GetNameBuilder(member).ToString(),
                    delegateInfo.ReturnType,
                    parameterTypes,
                    restrictedSkipVisibility: true);
                ILGenerator il = invoker.GetILGenerator();

                if (!invokerInfo.IsStatic)
                {
                    il.EmitLdtarg(parameterTypes[0], member.DeclaringType!, 0);
                }

                for (int i = 0; i < arguments.Length; i++)
                {
                    ref readonly SpecifierInvokerInfo.ArgumentInfo argument = ref arguments[i];

                    Debug.Assert(
                        argument.Source is SpecifierInvokerInfo.ArgumentSource.Parameter,
                        "Method arguments must come from delegate parameters.");

                    short sourceIndex = (short)argument.SourceIndex;

                    if (argument.IsCompatible)
                    {
                        il.EmitLdarg(sourceIndex);
                    }
                    else
                    {
                        Type argumentType = argument.ArgumentType;

                        if (argumentType.IsValueType)
                        {
                            argumentType = argumentType.MakeByRefType();

                            il.Emit(OpCodes.Ldarga_S, sourceIndex);
                        }
                        else
                        {
                            il.EmitLdarg(sourceIndex);
                        }

                        il.EmitConv(argumentType, argument.ParameterType, culture);
                    }
                }

                il.Emit(invokerInfo.IsStatic ? OpCodes.Call : OpCodes.Callvirt, member);

                if (!invokerInfo.Return.IsCompatible)
                {
                    il.EmitConv(invokerInfo.Return.Type, delegateInfo.ReturnType, culture);
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(MethodInfo member)
            {
                return new StringBuilder(128)
                    .Format(typeof(Reflector))
                    .Append(".Invoke<>")
                    .Format(member.DeclaringType!)
                    .Append('.')
                    .Format(member.Name);
            }

            public static void VerifyBindingTarget(in MethodSpecifier specifier)
            {
                if (specifier.IsOpen)
                {
                    return;
                }

                object? target = specifier.Target;
                SpecifierVerifier.ThrowIfDeferTarget(target);

                MethodInfo member = specifier.MemberInfo;
                SpecifierVerifier.VerifyBindingTarget(target, member, member.IsStatic);
            }

            public static SpecifierInvokerInfo VerifyDelegate<T>(in MethodSpecifier specifier)
                where T : Delegate
            {
                SpecifierInvokerInfo.DelegateInfo delegateInfo = SpecifierVerifier.VerifyDelegate<T>();

                MethodInfo member = specifier.MemberInfo;

                ParameterInfo[] delegateParameters = delegateInfo.Parameters;
                ParameterInfo[] memberParameters = member.GetParameters();

                int delegateOffset;

                if (specifier.IsOpen)
                {
                    if (delegateParameters.Length != memberParameters.Length + 1)
                    {
                        throw new ArgumentException(
                            $"Expects exactly {memberParameters.Length + 1} delegate parameter(s) " +
                            $"when the target is not bound, " +
                            $"but received {delegateParameters.Length} delegate parameter(s).");
                    }

                    delegateOffset = 1;
                }
                else
                {
                    if (delegateParameters.Length != memberParameters.Length)
                    {
                        throw new ArgumentException(
                            $"Expects exactly {memberParameters.Length} delegate parameter(s) " +
                            $"when the target is bound, " +
                            $"but received {delegateParameters.Length} delegate parameter(s).");
                    }

                    delegateOffset = 0;
                }

                SpecifierInvokerInfo.ReturnInfo returnInfo = SpecifierVerifier.VerifyReturn(
                    delegateInfo.ReturnType, member.ReturnType, specifier.IsStrict);
                bool signatureCompatible = returnInfo.IsCompatible;

                if (specifier.IsOpen)
                {
                    signatureCompatible &= !member.IsStatic
                        && SpecifierVerifier.VerifyTargetType(
                            delegateParameters[0].ParameterType, member.DeclaringType!, specifier.IsStrict);
                }

                for (int i = 0; i < memberParameters.Length; i++)
                {
                    int delegateIndex = i + delegateOffset;

                    signatureCompatible &= SpecifierVerifier.VerifyArgumentType(
                        delegateParameters[delegateIndex].ParameterType,
                        memberParameters[i].ParameterType,
                        specifier.IsStrict,
                        delegateIndex);
                }

                if (signatureCompatible)
                {
                    return new SpecifierInvokerInfo(
                        delegateInfo,
                        member.IsStatic,
                        returnInfo);
                }

                var arguments = new SpecifierInvokerInfo.ArgumentInfo[memberParameters.Length];
                Type[] parameterTypes = new Type[memberParameters.Length + 1];

                parameterTypes[0] = specifier.IsOpen
                    ? delegateParameters[0].ParameterType
                    : (member.IsStatic ? typeof(object) : member.DeclaringType!);

                for (int i = 0; i < memberParameters.Length; i++)
                {
                    int delegateIndex = i + delegateOffset;
                    int sourceIndex = i + 1;

                    Type argumentType = delegateParameters[delegateIndex].ParameterType;
                    Type parameterType = memberParameters[i].ParameterType;

                    parameterTypes[sourceIndex] = argumentType;
                    arguments[i] = new SpecifierInvokerInfo.ArgumentInfo(
                        SpecifierInvokerInfo.ArgumentSource.Parameter,
                        sourceIndex,
                        argumentType,
                        parameterType,
                        SpecifierVerifier.VerifyArgumentType(
                            argumentType, parameterType, specifier.IsStrict, delegateIndex));
                }

                return new SpecifierInvokerInfo(
                    delegateInfo,
                    member.IsStatic,
                    parameterTypes,
                    arguments,
                    returnInfo);
            }
        }
    }
}
