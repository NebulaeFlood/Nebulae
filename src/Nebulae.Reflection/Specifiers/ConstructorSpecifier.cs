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
    /// 构造函数的引用说明符
    /// </summary>
    public readonly struct ConstructorSpecifier : IEquatable<ConstructorSpecifier>
    {
        /// <summary>
        /// 目标构造函数的 <see cref="ConstructorInfo"/>
        /// </summary>
        public readonly ConstructorInfo MemberInfo;


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 获取 <see cref="ConstructorSpecifier"/> 的文化信息
        /// </summary>
        public SpecifierCulture Culture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetCulture(_flags);
        }

        /// <summary>
        /// 获取 <see cref="ConstructorSpecifier"/> 的策略
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

        internal ConstructorSpecifier(ConstructorInfo memberInfo)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            Debug.Assert(!memberInfo.IsStatic, "Static constructors are not supported.");
            MemberInfo = memberInfo;
        }

        private ConstructorSpecifier(ConstructorInfo memberInfo, byte flags)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            Debug.Assert(!memberInfo.IsStatic, "Static constructors are not supported.");
            MemberInfo = memberInfo;

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
            return obj is ConstructorSpecifier other
                && _flags == other._flags
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(ConstructorSpecifier other)
        {
            return _flags == other._flags
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 获取当前对象的哈希代码
        /// </summary>
        /// <returns>当前对象的哈希代码。</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(MemberInfo, _flags);
        }

        /// <summary>
        /// 获取表示当前对象的字符串
        /// </summary>
        /// <returns>表示当前对象的字符串。</returns>
        public override string ToString()
        {
            return CompilerHelpers.GetNameBuilder(MemberInfo)
                .Append('(')
                .Format(MemberInfo.GetParameters())
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
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input(params scoped ReadOnlySpan<object?> args)
        {
            SpecifierVerifier.ThrowIfInputEmpty(args);

            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.ArrayClosure(args.ToArray()),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T>(T arg)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T>(arg),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2>(T1 arg1, T2 arg2)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2>(arg1, arg2),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2, T3>(arg1, arg2, arg3),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2, T3, T4>(arg1, arg2, arg3, arg4),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的参数配置为指定的值
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Input<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return new DelegateSpecifier(
                MemberInfo,
                new DelegateSpecifier.Closure<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8),
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="ConstructorSpecifier"/>。</returns>
        public ConstructorSpecifier Lenient()
        {
            return new ConstructorSpecifier(
                MemberInfo,
                Specifier.Lenient(_flags));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <param name="culture"><see cref="IConvertible"/> 接口使用的 <see cref="IFormatProvider"/></param>
        /// <returns>配置后的 <see cref="ConstructorSpecifier"/>。</returns>
        public ConstructorSpecifier Lenient(SpecifierCulture culture)
        {
            return new ConstructorSpecifier(
                MemberInfo,
                Specifier.Lenient(_flags, culture));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="ConstructorSpecifier"/>。</returns>
        public ConstructorSpecifier Strict()
        {
            return new ConstructorSpecifier(
                MemberInfo,
                Specifier.Strict(_flags));
        }

        /// <summary>
        /// 创建指定类型的委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>由此 <see cref="ConstructorSpecifier"/> 编译的委托。</returns>
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
                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return invoker.CreateDelegate<T>();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot compile constructor '{MemberInfo.AsLog()}' " +
                    $"to delegate type '{typeof(T).AsLog()}'.", e);
            }
        }

        /// <summary>
        /// 解析并创建指定委托类型的编译器
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>与此 <see cref="ConstructorSpecifier"/> 关联的 <see cref="Compiler{T}"/>。</returns>
        public Compiler<T> Resolve<T>() where T : Delegate
        {
            try
            {
                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return new Compiler<T>(this, invoker);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot specify constructor '{MemberInfo.AsLog()}' as a compiler " +
                    $"with delegate type '{typeof(T).AsLog()}'.", e);
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


        private bool IsStrict
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.IsStrict(_flags);
        }


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 判断两个 <see cref="ConstructorSpecifier"/> 是否相等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="ConstructorSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="ConstructorSpecifier"/></param>
        /// <returns>若两个 <see cref="ConstructorSpecifier"/> 相等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator ==(ConstructorSpecifier left, ConstructorSpecifier right)
        {
            return left._flags == right._flags
                && left.MemberInfo == right.MemberInfo;
        }

        /// <summary>
        /// 判断两个 <see cref="ConstructorSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="ConstructorSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="ConstructorSpecifier"/></param>
        /// <returns>若两个 <see cref="ConstructorSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(ConstructorSpecifier left, ConstructorSpecifier right)
        {
            return left._flags != right._flags
                || left.MemberInfo != right.MemberInfo;
        }

        #endregion


        /// <summary>
        /// <see cref="ConstructorSpecifier"/> 的编译器
        /// </summary>
        /// <typeparam name="TDelegate">委托类型</typeparam>
        public sealed class Compiler<TDelegate> : Specifier.ICompiler<TDelegate>
            where TDelegate : Delegate
        {
            /// <summary>
            /// 关联的 <see cref="ConstructorSpecifier"/>
            /// </summary>
            public readonly ConstructorSpecifier Specifier;


            internal Compiler(ConstructorSpecifier specifier, DynamicMethod invoker)
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


            /// <summary>
            /// 编译为指定的委托类型
            /// </summary>
            /// <returns>按 <see cref="Specifier"/> 编译的委托。</returns>
            public TDelegate Compile()
            {
                try
                {
                    return _invoker.CreateDelegate<TDelegate>();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile constructor '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(TDelegate).AsLog()}'.", e);
                }
            }


            TDelegate Specifier.ICompiler<TDelegate>.Compile<T>(T target)
            {
                throw new NotSupportedException(
                    $"Cannot compile constructor '{Specifier.MemberInfo.AsLog()}' " +
                    $"to delegate type '{typeof(TDelegate).AsLog()}', " +
                    $"because it cannot bind to any target.");
            }


            private readonly DynamicMethod _invoker;
        }


        private static class CompilerHelpers
        {
            public static DynamicMethod CreateInvoker(in ConstructorSpecifier specifier, in SpecifierInvokerInfo invokerInfo)
            {
                Debug.Assert(invokerInfo.IsDynamic, "Constructor invokers must be dynamic.");

                ConstructorInfo member = specifier.MemberInfo;
                SpecifierCulture culture = specifier.Culture;

                SpecifierInvokerInfo.DelegateInfo delegateInfo = invokerInfo.Delegate;

                SpecifierInvokerInfo.ArgumentInfo[] arguments = invokerInfo.Arguments!;
                Type[] parameterTypes = invokerInfo.ParameterTypes!;

                DynamicMethod invoker = new(
                    GetNameBuilder(member).ToString(),
                    delegateInfo.ReturnType,
                    parameterTypes,
                    restrictedSkipVisibility: true);
                ILGenerator il = invoker.GetILGenerator();

                for (int i = 0; i < arguments.Length; i++)
                {
                    ref readonly SpecifierInvokerInfo.ArgumentInfo argument = ref arguments[i];

                    Debug.Assert(
                        argument.Source is SpecifierInvokerInfo.ArgumentSource.Parameter,
                        "Constructor arguments must come from delegate parameters.");

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
                }

                il.Emit(OpCodes.Newobj, member);

                if (!invokerInfo.Return.IsCompatible)
                {
                    il.EmitConv(invokerInfo.Return.Type, delegateInfo.ReturnType, culture);
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(ConstructorInfo meber)
            {
                return new StringBuilder(128)
                    .Format(typeof(Reflector))
                    .Append(".Invoke<>")
                    .Format(meber.DeclaringType!)
                    .Append(".ctor");
            }

            public static SpecifierInvokerInfo VerifyDelegate<T>(in ConstructorSpecifier specifier)
                where T : Delegate
            {
                SpecifierInvokerInfo.DelegateInfo delegateInfo = SpecifierVerifier.VerifyDelegate<T>();
                ConstructorInfo member = specifier.MemberInfo;

                Type returnType = delegateInfo.ReturnType;

                ParameterInfo[] delegateParameters = delegateInfo.Parameters;
                ParameterInfo[] memberParameters = member.GetParameters();

                if (memberParameters.Length != delegateParameters.Length)
                {
                    throw new ArgumentException(
                        $"Expects exactly {memberParameters.Length} delegate parameter(s), " +
                        $"but received  {delegateParameters.Length} delegate parameter(s).");
                }

                Type[] parameterTypes = new Type[delegateParameters.Length];
                var arguments = new SpecifierInvokerInfo.ArgumentInfo[delegateParameters.Length];

                for (int i = 0; i < delegateParameters.Length; i++)
                {
                    Type argumentType = delegateParameters[i].ParameterType;
                    Type parameterType = memberParameters[i].ParameterType;

                    bool compatibility = SpecifierVerifier.VerifyArgumentType(
                        argumentType, parameterType, specifier.IsStrict, i);

                    parameterTypes[i] = argumentType;
                    arguments[i] = new SpecifierInvokerInfo.ArgumentInfo(
                        SpecifierInvokerInfo.ArgumentSource.Parameter,
                        i,
                        argumentType,
                        parameterType,
                        compatibility);
                }

                return new SpecifierInvokerInfo(
                    delegateInfo,
                    isStatic: true,
                    parameterTypes,
                    arguments,
                    SpecifierVerifier.VerifyReturn(
                        returnType, member.DeclaringType!, specifier.IsStrict));
            }
        }
    }
}
