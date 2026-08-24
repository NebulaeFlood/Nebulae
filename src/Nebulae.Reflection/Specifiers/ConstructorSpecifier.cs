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
            get => (SpecifierCulture)((_flags & Specifier.CultureMask) >> 3);
        }

        /// <summary>
        /// 获取 <see cref="ConstructorSpecifier"/> 的策略
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
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="ConstructorSpecifier"/>。</returns>
        public ConstructorSpecifier Lenient()
        {
            return new ConstructorSpecifier(
                MemberInfo,
                flags: (byte)(_flags | Specifier.PolicyMask));
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
                flags: (byte)((_flags & ~Specifier.CultureMask) | ((int)culture << 3) | Specifier.PolicyMask));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="ConstructorSpecifier"/>。</returns>
        public ConstructorSpecifier Strict()
        {
            return new ConstructorSpecifier(
                MemberInfo,
                flags: (byte)(_flags & ~Specifier.PolicyMask));
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
                Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, verification);

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
                Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, verification);
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
            get => (_flags & Specifier.PolicyMask) == 0;
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
        /// <typeparam name="T">委托类型</typeparam>
        public sealed class Compiler<T> where T : Delegate
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
            /// <returns>由 <see cref="Specifier"/> 编译的委托。</returns>
            public T Compile()
            {
                try
                {
                    return _invoker.CreateDelegate<T>();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile constructor '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(T).AsLog()}'.", e);
                }
            }


            private readonly DynamicMethod _invoker;
        }


        private static class CompilerHelpers
        {
            public static DynamicMethod CreateInvoker(in ConstructorSpecifier specifier, in Verification verification)
            {
                ConstructorInfo member = specifier.MemberInfo;
                SpecifierCulture culture = specifier.Culture;

                Type[] argumentTypes = verification.ArgumentTypes;
                Type[] parameterTypes = verification.ParameterTypes;
                Type returnType = verification.ReturnType;

                DynamicMethod invoker = new(
                    GetNameBuilder(specifier.MemberInfo).ToString(),
                    returnType,
                    argumentTypes,
                    restrictedSkipVisibility: true);
                ILGenerator il = invoker.GetILGenerator();

                bool[] compability = verification.SignatureCompability;

                for (short i = 0; i < argumentTypes.Length; i++)
                {
                    if (compability[i])
                    {
                        il.EmitLdarg(i);
                    }
                    else
                    {
                        Type argumentType = argumentTypes[i];

                        if (argumentType.IsValueType)
                        {
                            argumentType = argumentType.MakeByRefType();

                            il.Emit(OpCodes.Ldarga_S, i);
                        }
                        else
                        {
                            il.EmitLdarg(i);
                        }

                        il.EmitConv(argumentType, parameterTypes[i], culture);
                    }
                }

                il.Emit(OpCodes.Newobj, member);

                if (!verification.ReturnCompatiable)
                {
                    il.EmitConv(member.DeclaringType!, returnType, culture);
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(ConstructorInfo constructor)
            {
                return new StringBuilder(128)
                    .Format(constructor.IsStatic ? typeof(void) : constructor.DeclaringType!)
                    .Append(' ')
                    .Format(typeof(Reflector))
                    .Append(".Invoke<>")
                    .Format(constructor.DeclaringType!)
                    .Append(".ctor");
            }

            public static Verification VerifyDelegate<T>(in ConstructorSpecifier specifier)
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
                Type memberType = specifier.MemberInfo.DeclaringType!;

                bool returnCompatible = SpecifierVerifier.VerifyReturnType(
                    returnType, memberType, specifier.IsStrict);

                ParameterInfo[] invokerParameters = invocation.GetParameters();
                ParameterInfo[] memberParameters = specifier.MemberInfo.GetParameters();

                Type[] argumentTypes;
                Type[] parameterTypes;

                bool[] signatureCompability;

                if (memberParameters.Length != invokerParameters.Length)
                {
                    throw new ArgumentException(
                        $"Expects exactly {memberParameters.Length} parameter(s), " +
                        $"but received  {invokerParameters.Length}  parameter(s).");
                }

                argumentTypes = new Type[invokerParameters.Length];
                parameterTypes = new Type[invokerParameters.Length];

                signatureCompability = new bool[invokerParameters.Length];

                for (int i = 0; i < invokerParameters.Length; i++)
                {
                    Type argumentType = invokerParameters[i].ParameterType;
                    Type parameterType = memberParameters[i].ParameterType;

                    bool compability = SpecifierVerifier.VerifyArgumentType(
                        argumentType, parameterType, specifier.IsStrict, i);

                    signatureCompability[i] = compability;

                    argumentTypes[i] = argumentType;
                    parameterTypes[i] = parameterType;
                }

                return new Verification(
                    argumentTypes,
                    parameterTypes,
                    returnType,
                    returnCompatible,
                    signatureCompability);
            }
        }

        private readonly ref struct Verification(
            Type[] argumentTypes,
            Type[] parameterTypes,
            Type returnType,
            bool returnTypeCompatible,
            bool[] signatureCompability)
        {
            public readonly Type[] ArgumentTypes = argumentTypes;

            public readonly Type[] ParameterTypes = parameterTypes;

            public readonly Type ReturnType = returnType;

            public readonly bool ReturnCompatiable = returnTypeCompatible;

            public readonly bool[] SignatureCompability = signatureCompability;
        }
    }
}
