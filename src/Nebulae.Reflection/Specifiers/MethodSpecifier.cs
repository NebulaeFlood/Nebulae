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
            get => (SpecifierBinding)((_flags & Specifier.BindingMask) >> 6);
        }

        /// <summary>
        /// 获取 <see cref="MethodSpecifier"/> 的文化信息
        /// </summary>
        public SpecifierCulture Culture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (SpecifierCulture)((_flags & Specifier.CultureMask) >> 3);
        }

        /// <summary>
        /// 获取 <see cref="MethodSpecifier"/> 的策略
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

        internal MethodSpecifier(MethodInfo memberInfo)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;

            if (memberInfo.IsStatic)
            {
                _flags = Specifier.BindingMask;
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
                && _flags == other._flags
                && Target == other.Target
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(MethodSpecifier other)
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
                target: target,
                flags: (byte)(_flags | Specifier.BindingMask));
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
                flags: (byte)(_flags & ~Specifier.BindingMask));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Lenient()
        {
            return new MethodSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)(_flags | Specifier.PolicyMask));
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
                target: Target,
                flags: (byte)((_flags & ~Specifier.CultureMask) | ((int)culture << 3) | Specifier.PolicyMask));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public MethodSpecifier Strict()
        {
            return new MethodSpecifier(
                MemberInfo,
                target: Target,
                flags: (byte)(_flags & ~Specifier.PolicyMask));
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

                Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                MethodInfo invoker = CompilerHelpers.CreateInvoker(this, verification);

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
                if (IsOpen)
                {
                    Verification verification = CompilerHelpers.VerifyDelegate<T>(this);
                    MethodInfo invoker = CompilerHelpers.CreateInvoker(this, verification);
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
                    MethodInfo invoker = CompilerHelpers.CreateInvoker(this, verification);
                    return new Compiler<T>(this, invoker);
                }
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
            return left._flags == right._flags
                && left.Target == right.Target
                && left.MemberInfo == right.MemberInfo;
        }

        /// <summary>
        /// 判断两个 <see cref="MethodSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="MethodSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="MethodSpecifier"/></param>
        /// <returns>若两个 <see cref="MethodSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(MethodSpecifier left, MethodSpecifier right)
        {
            return left._flags != right._flags
                || left.Target != right.Target
                || left.MemberInfo != right.MemberInfo;
        }

        #endregion


        /// <summary>
        /// <see cref="MethodSpecifier"/> 的编译器
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        public sealed class Compiler<T> where T : Delegate
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
                        $"Cannot compile method '{Specifier.MemberInfo.AsLog()}' " +
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
                        $"Cannot compile method '{Specifier.MemberInfo.AsLog()}' " +
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
                        $"Cannot compile method '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(T).AsLog()}'.", e);
                }
            }

            #endregion


            private readonly MethodInfo _invoker;
        }


        private static class CompilerHelpers
        {
            public static MethodInfo CreateInvoker(in MethodSpecifier specifier, in Verification verification)
            {
                if (verification.SignatureCompatible)
                {
                    return specifier.MemberInfo;
                }

                MethodInfo member = specifier.MemberInfo;
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

                if (!member.IsStatic)
                {
                    il.EmitLdtarg(argumentTypes[0], parameterTypes[0], 0);
                }

                bool[] compability = verification.SignatureCompability;

                for (short i = 1; i < argumentTypes.Length; i++)
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

                il.Emit(member.IsStatic ? OpCodes.Call : OpCodes.Callvirt, member);

                if (!verification.ReturnCompatiable)
                {
                    il.EmitConv(member.ReturnType, returnType, culture);
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(MethodInfo method)
            {
                return new StringBuilder(128)
                    .Format(method.ReturnType)
                    .Append(' ')
                    .Format(typeof(Reflector))
                    .Append(".Invoke<>")
                    .Format(method.DeclaringType!)
                    .Append('.')
                    .Format(method.Name);
            }

            public static void VerifyBindingTarget(in MethodSpecifier specifier)
            {
                if (specifier.IsOpen)
                {
                    return;
                }

                object? target = specifier.Target;

                SpecifierVerifier.ThrowIfDeferTarget(target);
                VerifyBindingTarget(target, specifier.MemberInfo);
            }

            public static void VerifyBindingTarget(object? target, MethodInfo method)
            {
                if (target is null)
                {
                    if (!method.IsStatic)
                    {
                        throw new ArgumentException(
                            $"Expects a non-null target object " +
                            $"for instance method '{method.AsLog()}', " +
                            $"but received '{DiagnosticHelpers.Null}'.");
                    }
                }
                else if (method.IsStatic)
                {
                    throw new ArgumentException(
                        $"Expects a null target object " +
                        $"for static method '{method.AsLog()}', " +
                        $"but received object '{target.AsLog()}'.");
                }
                else
                {
                    Type targetType = target.GetType();
                    Type declaringType = method.DeclaringType!;

                    if (!declaringType.IsAssignableFrom(targetType))
                    {
                        throw new ArgumentException(
                            $"Expects a target object of type '{declaringType.AsLog()}' " +
                            $"for instance method '{method.AsLog()}', " +
                            $"but received object '{target.AsLog()}' of type '{targetType.AsLog()}'.");
                    }
                }
            }

            public static Verification VerifyDelegate<T>(in MethodSpecifier specifier)
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
                Type memberType = specifier.MemberInfo.ReturnType;

                bool returnCompatible = SpecifierVerifier.VerifyReturnType(
                    returnType, memberType, specifier.IsStrict);

                ParameterInfo[] invokerParameters = invocation.GetParameters();
                ParameterInfo[] memberParameters = specifier.MemberInfo.GetParameters();

                Type[] argumentTypes;
                Type[] parameterTypes;

                bool[] signatureCompability;
                bool signatureCompatible = returnCompatible;

                if (specifier.IsOpen)
                {
                    if (memberParameters.Length + 1 != invokerParameters.Length)
                    {
                        throw new ArgumentException(
                            $"Expects exactly {memberParameters.Length + 1} parameter(s) " +
                            $"when the target is not bound, " +
                            $"but received {invokerParameters.Length} parameter(s).");
                    }

                    argumentTypes = new Type[invokerParameters.Length];
                    parameterTypes = new Type[invokerParameters.Length];

                    if (specifier.MemberInfo.IsStatic)
                    {
                        signatureCompatible = false;

                        Type type = typeof(object);

                        argumentTypes[0] = type;
                        parameterTypes[0] = type;
                    }
                    else
                    {
                        Type argumentType = invokerParameters[0].ParameterType;
                        Type parameterType = specifier.MemberInfo.DeclaringType!;

                        signatureCompatible = SpecifierVerifier.VerifyTargetType(
                            argumentType, parameterType, specifier.IsStrict)
                            && signatureCompatible;

                        argumentTypes[0] = argumentType;
                        parameterTypes[0] = parameterType;
                    }

                    signatureCompability = new bool[invokerParameters.Length];

                    for (int i = 1; i < invokerParameters.Length; i++)
                    {
                        Type argumentType = invokerParameters[i].ParameterType;
                        Type parameterType = memberParameters[i - 1].ParameterType;

                        bool compability = SpecifierVerifier.VerifyArgumentType(
                            argumentType, parameterType, specifier.IsStrict, i);

                        signatureCompability[i] = compability;
                        signatureCompatible = signatureCompatible && compability;

                        argumentTypes[i] = argumentType;
                        parameterTypes[i] = parameterType;
                    }
                }
                else
                {
                    if (memberParameters.Length != invokerParameters.Length)
                    {
                        throw new ArgumentException(
                            $"Expects exactly {memberParameters.Length} parameter(s) " +
                            $"when the target is bound, " +
                            $"but received {invokerParameters.Length} parameter(s).");
                    }

                    int parameterCount = invokerParameters.Length + 1;

                    argumentTypes = new Type[parameterCount];
                    parameterTypes = new Type[parameterCount];

                    if (specifier.MemberInfo.IsStatic)
                    {
                        Type type = typeof(object);

                        argumentTypes[0] = type;
                        parameterTypes[0] = type;
                    }
                    else
                    {
                        Type declaringType = specifier.MemberInfo.DeclaringType!;

                        argumentTypes[0] = declaringType;
                        parameterTypes[0] = declaringType;
                    }

                    signatureCompability = new bool[parameterCount];

                    for (int i = 0; i < invokerParameters.Length; i++)
                    {
                        Type argumentType = invokerParameters[i].ParameterType;
                        Type parameterType = memberParameters[i].ParameterType;

                        bool compability = SpecifierVerifier.VerifyArgumentType(
                            argumentType, parameterType, specifier.IsStrict, i);

                        int index = i + 1;

                        signatureCompability[index] = compability;
                        signatureCompatible = signatureCompatible && compability;

                        argumentTypes[index] = argumentType;
                        parameterTypes[index] = parameterType;
                    }
                }

                return new Verification(
                    argumentTypes,
                    parameterTypes,
                    returnType,
                    returnCompatible,
                    signatureCompability,
                    signatureCompatible);
            }
        }

        private readonly ref struct Verification(
            Type[] argumentTypes,
            Type[] parameterTypes,
            Type returnType,
            bool returnTypeCompatible,
            bool[] signatureCompability,
            bool signatureCompatible)
        {
            public readonly Type[] ArgumentTypes = argumentTypes;

            public readonly Type[] ParameterTypes = parameterTypes;

            public readonly Type ReturnType = returnType;

            public readonly bool ReturnCompatiable = returnTypeCompatible;

            public readonly bool[] SignatureCompability = signatureCompability;

            public readonly bool SignatureCompatible = signatureCompatible;
        }
    }
}
