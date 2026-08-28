using Nebulae.Collections;
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
    /// <see cref="DelegateSpecifier"/> 的参数稳定性
    /// </summary>
    /// <remarks><b>仅对 <see cref="DelegateSpecifier.Compiler{TDelegate}"/> 有效。</b></remarks>
    public enum DelegateSpecifierStability : byte
    {
        /// <summary>
        /// 可变，输入参数可能因每次运行变化
        /// </summary>
        /// <remarks>每个创建的委托将得到输入参数闭包的复制。</remarks>
        Mutable,

        /// <summary>
        /// 稳定，输入参数在每次运行时保持不变
        /// </summary>
        /// <remarks>每个创建的委托将尝试共享输入参数闭包。</remarks>
        Stable
    }


    /// <summary>
    /// 委托的引用说明符
    /// </summary>
    /// <remarks>用于描述一个闭包委托。</remarks>
    public readonly partial struct DelegateSpecifier : IEquatable<DelegateSpecifier>
    {
        //------------------------------------------------------
        //
        //  Private Constants
        //
        //------------------------------------------------------

        #region Private Constants

        private const int KindMask = 0B_0000_0010;
        private const int StabilityMask = 0B_0000_0001;

        #endregion


        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        /// <summary>
        /// 目标方法的 <see cref="MethodBase"/>
        /// </summary>
        /// <remarks>可能为 <see cref="ConstructorInfo"/> 或 <see cref="MethodInfo"/>。</remarks>
        public readonly MethodBase MemberInfo;

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
        /// 获取 <see cref="DelegateSpecifier"/> 的目标绑定状态
        /// </summary>
        /// <remarks>对于静态成员，此项默认为 <see cref="SpecifierBinding.Close"/>。</remarks>
        public SpecifierBinding Binding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetBinding(_flags);
        }

        /// <summary>
        /// 获取 <see cref="DelegateSpecifier"/> 的文化信息
        /// </summary>
        public SpecifierCulture Culture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetCulture(_flags);
        }

        /// <summary>
        /// 获取 <see cref="DelegateSpecifier"/> 的策略
        /// </summary>
        public SpecifierPolicy Policy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Specifier.GetPolicy(_flags);
        }

        /// <summary>
        /// 获取 <see cref="DelegateSpecifier"/> 的参数稳定性
        /// </summary>
        public DelegateSpecifierStability Stability
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DelegateSpecifierStability)(_flags & StabilityMask);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DelegateSpecifier(ConstructorInfo memberInfo, Closure closure, byte flags)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;

            _closure = closure;
            _flags = (byte)(Specifier.Close(flags) | KindMask);
        }

        internal DelegateSpecifier(MethodInfo memberInfo, object? target, Closure closure, byte flags)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;
            Target = target;

            _closure = closure;
            _flags = flags;
        }

        private DelegateSpecifier(MethodBase memberInfo, object? target, Closure closure, byte flags)
        {
            Debug.Assert(memberInfo.DeclaringType is not null, "DeclaringType cannot be null.");
            MemberInfo = memberInfo;
            Target = target;

            _closure = closure;
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
            return obj is DelegateSpecifier other
                && Equals(other);
        }

        /// <summary>
        /// 判断指定对象是否等于当前对象
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>若指定的对象等于当前对象，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public bool Equals(DelegateSpecifier other)
        {
            return _flags == other._flags
                && _closure == other._closure
                && Equals(Target, other.Target)
                && MemberInfo == other.MemberInfo;
        }

        /// <summary>
        /// 获取当前对象的哈希代码
        /// </summary>
        /// <returns>当前对象的哈希代码。</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(MemberInfo, Target, _closure, _flags);
        }

        /// <summary>
        /// 获取表示当前对象的字符串
        /// </summary>
        /// <returns>表示当前对象的字符串。</returns>
        public override string ToString()
        {
            return CompilerHelpers.GetNameBuilder(MemberInfo, IsConstructor)
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
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Bind(object? target)
        {
            return new DelegateSpecifier(
                MemberInfo,
                target,
                _closure,
                Specifier.Close(_flags));
        }

        /// <summary>
        /// 将引用说明符绑定的目标对象移除
        /// </summary>
        /// <returns>配置后的 <see cref="MethodSpecifier"/>。</returns>
        public DelegateSpecifier Open()
        {
            return new DelegateSpecifier(
                MemberInfo,
                target: null,
                _closure,
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
                new ArrayClosure(args.ToArray()),
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
                new Closure<T>(arg),
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
                new Closure<T1, T2>(arg1, arg2),
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
                new Closure<T1, T2, T3>(arg1, arg2, arg3),
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
                new Closure<T1, T2, T3, T4>(arg1, arg2, arg3, arg4),
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
                new Closure<T1, T2, T3, T4, T5>(arg1, arg2, arg3, arg4, arg5),
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
                new Closure<T1, T2, T3, T4, T5, T6>(arg1, arg2, arg3, arg4, arg5, arg6),
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
                new Closure<T1, T2, T3, T4, T5, T6, T7>(arg1, arg2, arg3, arg4, arg5, arg6, arg7),
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
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8),
                _flags);
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Lenient()
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                _closure,
                Specifier.Lenient(_flags));
        }

        /// <summary>
        /// 将引用说明符的策略配置为宽松
        /// </summary>
        /// <param name="culture"><see cref="IConvertible"/> 接口使用的 <see cref="IFormatProvider"/></param>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Lenient(SpecifierCulture culture)
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                _closure,
                Specifier.Lenient(_flags, culture));
        }

        /// <summary>
        /// 将引用说明符的参数稳定性配置为稳定
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Stable()
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                _closure,
                (byte)(_flags | StabilityMask));
        }

        /// <summary>
        /// 将引用说明符的参数稳定性配置为可变
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Mutable()
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                _closure,
                (byte)(_flags & ~StabilityMask));
        }

        /// <summary>
        /// 将引用说明符的策略配置为严格
        /// </summary>
        /// <returns>配置后的 <see cref="DelegateSpecifier"/>。</returns>
        public DelegateSpecifier Strict()
        {
            return new DelegateSpecifier(
                MemberInfo,
                Target,
                _closure,
                Specifier.Strict(_flags));
        }

        /// <summary>
        /// 创建指定类型的闭包委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>由此 <see cref="DelegateSpecifier"/> 编译的委托。</returns>
        public T Compile<T>() where T : Delegate
        {
            try
            {
                CompilerHelpers.VerifyBindingTarget(this);

                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this, out Closure closure);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return RequiresBinding
                    ? invoker.CreateDelegate<T>(closure.Bind(Target))
                    : invoker.CreateDelegate<T>(closure);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot compile member '{MemberInfo.AsLog()}' " +
                    $"to delegate type '{typeof(T).AsLog()}'.", e);
            }
        }

        /// <summary>
        /// 解析并创建指定委托类型的编译器
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <returns>与此 <see cref="DelegateSpecifier"/> 关联的 <see cref="Compiler{T}"/>。</returns>
        public Compiler<T> Resolve<T>() where T : Delegate
        {
            try
            {
                if (Target != Specifier.Defer)
                {
                    CompilerHelpers.VerifyBindingTarget(this);
                }

                SpecifierInvokerInfo invokerInfo = CompilerHelpers.VerifyDelegate<T>(this, out Closure closure);
                DynamicMethod invoker = CompilerHelpers.CreateInvoker(this, invokerInfo);

                return new Compiler<T>(this, invoker, closure);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot specify member '{MemberInfo.AsLog()}' as a compiler " +
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

        private bool IsConstructor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & KindMask) != 0;
        }

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

        private bool IsStable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & StabilityMask) != 0;
        }

        private ParameterInfo[] Parameters
        {
            get
            {
                if (IsConstructor)
                {
                    return MemberInfo.GetParameters();
                }

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

        private bool RequiresBinding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsOpen && !IsConstructor && !MemberInfo.IsStatic;
        }

        #endregion


        private readonly Closure _closure;

        // Bit layout:
        //
        //     7         6        5       4       3       2       1      0
        // +--------+---------+-------+-------+-------+--------+------+------+
        // | Policy | Binding |    Culture (3 bits)   | Unused | Mode | Kind |
        // +--------+---------+-------+-------+-------+--------+------+------+
        private readonly byte _flags;


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 判断两个 <see cref="DelegateSpecifier"/> 是否相等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="DelegateSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="DelegateSpecifier"/></param>
        /// <returns>若两个 <see cref="DelegateSpecifier"/> 相等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator ==(DelegateSpecifier left, DelegateSpecifier right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个 <see cref="DelegateSpecifier"/> 是否不等
        /// </summary>
        /// <param name="left">要比较的第一个 <see cref="DelegateSpecifier"/></param>
        /// <param name="right">要比较的第二个 <see cref="DelegateSpecifier"/></param>
        /// <returns>若两个 <see cref="DelegateSpecifier"/> 不等，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool operator !=(DelegateSpecifier left, DelegateSpecifier right)
        {
            return !left.Equals(right);
        }

        #endregion


        /// <summary>
        /// <see cref="DelegateSpecifier"/> 的编译器
        /// </summary>
        /// <typeparam name="TDelegate">委托类型</typeparam>
        public sealed class Compiler<TDelegate> : Specifier.ICompiler<TDelegate>
            where TDelegate : Delegate
        {
            /// <summary>
            /// 关联的 <see cref="DelegateSpecifier"/>
            /// </summary>
            public readonly DelegateSpecifier Specifier;


            internal Compiler(DelegateSpecifier specifier, DynamicMethod invoker, Closure closure)
            {
                Specifier = specifier;

                _invoker = invoker;

                if (!specifier.IsStable)
                {
                    _closure = closure;
                    _isStable = false;
                }
                else if (specifier.RequiresBinding && specifier.Target == Reflection.Specifier.Defer)
                {
                    _closure = closure;
                    _isStable = false;
                }
                else
                {
                    _closure = specifier.RequiresBinding ? closure.Bind(specifier.Target) : closure;
                    _isStable = true;
                }
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
                    SpecifierVerifier.ThrowIfDeferTarget(Specifier.Target);

                    return _isStable
                        ? _invoker.CreateDelegate<TDelegate>(_closure)
                        : _invoker.CreateDelegate<TDelegate>(_closure.Copy());
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile member '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(TDelegate).AsLog()}'.", e);
                }
            }

            /// <summary>
            /// 编译为指定的委托类型并绑定到目标对象
            /// </summary>
            /// <typeparam name="T">目标对象的类型</typeparam>
            /// <param name="target">要绑定的目标对象</param>
            /// <returns>按 <see cref="Specifier"/> 编译的委托。</returns>
            public TDelegate Compile<T>(T target)
            {
                if (Specifier.IsOpen)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile member '{Specifier.MemberInfo.AsLog()}' " +
                        $"with an explicit target because the specifier is open.");
                }

                if (Specifier.IsConstructor)
                {
                    throw new NotSupportedException(
                        $"Cannot bind constructor '{Specifier.MemberInfo.AsLog()}' to any target.");
                }

                try
                {
                    SpecifierVerifier.ThrowIfDeferTarget(target);

                    MethodBase member = Specifier.MemberInfo;
                    SpecifierVerifier.VerifyBindingTarget(target, member, member.IsStatic);

                    return Specifier.RequiresBinding
                        ? _invoker.CreateDelegate<TDelegate>(_closure.Copy().Bind(target))
                        : _invoker.CreateDelegate<TDelegate>(_isStable ? _closure : _closure.Copy());
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Cannot compile member '{Specifier.MemberInfo.AsLog()}' " +
                        $"to delegate type '{typeof(TDelegate).AsLog()}'.", e);
                }
            }

            #endregion


            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private readonly Closure _closure;

            private readonly DynamicMethod _invoker;

            private readonly bool _isStable;

            #endregion
        }


        private static class CompilerHelpers
        {
            public static DynamicMethod CreateInvoker(in DelegateSpecifier specifier, in SpecifierInvokerInfo invokerInfo)
            {
                SpecifierCulture culture = specifier.Culture;
                MethodBase member = specifier.MemberInfo;

                SpecifierInvokerInfo.ArgumentInfo[] arguments = invokerInfo.Arguments!;
                SpecifierInvokerInfo.DelegateInfo delegateInfo = invokerInfo.Delegate;

                Type[] parameterTypes = invokerInfo.ParameterTypes!;

                Type closureType = parameterTypes[0];
                FieldInfo[] closureFields = closureType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                bool isArrayClosure = closureType == typeof(ArrayClosure);

                DynamicMethod invoker = new(
                    GetNameBuilder(member, specifier.IsConstructor).ToString(),
                    delegateInfo.ReturnType,
                    parameterTypes,
                    restrictedSkipVisibility: true);
                ILGenerator il = invoker.GetILGenerator();

                if (!invokerInfo.IsStatic)
                {
                    if (specifier.IsOpen)
                    {
                        il.EmitLdtarg(parameterTypes[1], member.DeclaringType!, 1);
                    }
                    else
                    {
                        Type targetType = member.DeclaringType!;

                        il.Emit(OpCodes.Ldarg_0);

                        if (isArrayClosure)
                        {
                            il.Emit(OpCodes.Ldfld, closureFields[0]);
                            il.Emit(OpCodes.Ldc_I4_0);

                            if (targetType.IsValueType)
                            {
                                il.Emit(OpCodes.Ldelem_Ref);
                                il.Emit(OpCodes.Unbox, targetType);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldelem_Ref);
                                il.Emit(OpCodes.Castclass, targetType);
                            }
                        }
                        else
                        {
                            il.Emit(targetType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, closureFields[0]);
                        }
                    }
                }

                for (int i = 0; i < arguments.Length; i++)
                {
                    ref readonly SpecifierInvokerInfo.ArgumentInfo argument = ref arguments[i];

                    Type argumentType = argument.ArgumentType;
                    Type parameterType = argument.ParameterType;

                    switch (argument.Source)
                    {
                        case SpecifierInvokerInfo.ArgumentSource.Closure:
                            il.Emit(OpCodes.Ldarg_0);

                            if (isArrayClosure)
                            {
                                il.Emit(OpCodes.Ldfld, closureFields[0]);
                                il.EmitLdcI4(argument.SourceIndex);

                                if (argument.IsCompatible)
                                {
                                    if (parameterType.IsByRef)
                                    {
                                        parameterType = parameterType.GetElementType()!;

                                        if (parameterType == typeof(object))
                                        {
                                            il.Emit(OpCodes.Ldelema, parameterType);
                                        }
                                        else if (parameterType.IsValueType)
                                        {
                                            il.Emit(OpCodes.Ldelem_Ref);
                                            il.Emit(OpCodes.Unbox, parameterType);
                                        }
                                        else
                                        {
                                            il.Emit(OpCodes.Ldelem_Ref);
                                            il.Emit(OpCodes.Castclass, parameterType);
                                            il.EmitAsByRef(parameterType);
                                        }
                                    }
                                    else
                                    {
                                        il.Emit(OpCodes.Ldelem_Ref);

                                        if (parameterType != typeof(object))
                                        {
                                            il.Emit(parameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameterType);
                                        }
                                    }
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldelem_Ref);

                                    if (argumentType.IsValueType)
                                    {
                                        il.Emit(OpCodes.Unbox, argumentType);
                                        il.EmitConv(argumentType.MakeByRefType(), parameterType, culture);
                                    }
                                    else
                                    {
                                        il.Emit(OpCodes.Castclass, argumentType);
                                        il.EmitConv(argumentType, parameterType, culture);
                                    }
                                }
                            }
                            else
                            {
                                FieldInfo field = closureFields[argument.SourceIndex];

                                if (argument.IsCompatible)
                                {
                                    if (parameterType.IsByRef)
                                    {
                                        if (parameterType == argumentType)
                                        {
                                            il.Emit(OpCodes.Ldflda, field);
                                        }
                                        else
                                        {
                                            il.Emit(OpCodes.Ldfld, field);
                                            il.EmitAsByRef(parameterType.GetElementType()!);
                                        }
                                    }
                                    else
                                    {
                                        il.Emit(OpCodes.Ldfld, field);
                                    }
                                }
                                else
                                {
                                    if (argumentType.IsValueType)
                                    {
                                        il.Emit(OpCodes.Ldflda, closureFields[argument.SourceIndex]);
                                        il.EmitConv(argumentType.MakeByRefType(), parameterType, culture);
                                    }
                                    else
                                    {
                                        il.Emit(OpCodes.Ldfld, closureFields[argument.SourceIndex]);
                                        il.EmitConv(argumentType, parameterType, culture);
                                    }
                                }
                            }
                            break;
                        case SpecifierInvokerInfo.ArgumentSource.Constant:
                            il.EmitLdc(specifier._closure[argument.SourceIndex]!, argumentType);

                            if (argument.IsCompatible)
                            {
                                if (parameterType.IsByRef)
                                {
                                    il.EmitAsByRef(argumentType);
                                }
                            }
                            else
                            {
                                il.EmitConv(argumentType, argument.ParameterType, culture);
                            }

                            break;
                        case SpecifierInvokerInfo.ArgumentSource.Null:
                            il.EmitLdnull(parameterType);
                            break;
                        default:
                            throw new NotSupportedException(
                                $"Argument source '{argument.Source}' is not supported.");
                    }
                }

                if (specifier.IsConstructor)
                {
                    il.Emit(OpCodes.Newobj, (ConstructorInfo)member);
                }
                else
                {
                    il.Emit(invokerInfo.IsStatic ? OpCodes.Call : OpCodes.Callvirt, (MethodInfo)member);
                }

                if (!invokerInfo.Return.IsCompatible)
                {
                    il.EmitConv(invokerInfo.Return.Type, delegateInfo.ReturnType, culture);
                }

                il.Emit(OpCodes.Ret);
                return invoker;
            }

            public static StringBuilder GetNameBuilder(MethodBase member, bool isConstructor)
            {
                return new StringBuilder(128)
                    .Format(typeof(Reflector))
                    .Append(".Invoke<>")
                    .Format(member.DeclaringType!)
                    .Append('.')
                    .Append(isConstructor ? "ctor" : member.Name);
            }

            public static void VerifyBindingTarget(in DelegateSpecifier specifier)
            {
                if (specifier.IsOpen)
                {
                    if (specifier.IsConstructor)
                    {
                        throw new InvalidOperationException(
                            $"Cannot bind constructor '{specifier.MemberInfo.AsLog()}' to any target.");
                    }

                    return;
                }

                if (specifier.IsConstructor)
                {
                    if (specifier.Target is not null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot bind constructor '{specifier.MemberInfo.AsLog()}' to any target.");
                    }

                    return;
                }

                object? target = specifier.Target;

                SpecifierVerifier.ThrowIfDeferTarget(target);

                MethodBase member = specifier.MemberInfo;
                SpecifierVerifier.VerifyBindingTarget(target, member, member.IsStatic);
            }

            public static SpecifierInvokerInfo VerifyDelegate<T>(in DelegateSpecifier specifier, out Closure closure)
                where T : Delegate
            {
                SpecifierInvokerInfo.DelegateInfo delegateInfo = SpecifierVerifier.VerifyDelegate<T>();
                MethodBase member = specifier.MemberInfo;
                Closure input = specifier._closure;

                ParameterInfo[] delegateParameters = delegateInfo.Parameters;
                ParameterInfo[] memberParameters = member.GetParameters();

                if (specifier.IsOpen)
                {
                    if (delegateParameters.Length is not 1)
                    {
                        throw new ArgumentException(
                            $"Expects exactly 1 delegate parameter " +
                            $"when the target is not bound, " +
                            $"but received {delegateParameters.Length} parameter(s).");
                    }
                }
                else
                {
                    if (delegateParameters.Length is not 0)
                    {
                        throw new ArgumentException(
                            $"Expects exactly 0 delegate parameters " +
                            $"when the target is bound, " +
                            $"but received {delegateParameters.Length} parameter(s).");
                    }
                }

                if (memberParameters.Length != input.Length)
                {
                    throw new ArgumentException(
                        $"Expects exactly {memberParameters.Length} input argument(s), " +
                        $"but received {input.Length} argument(s).");
                }

                if (specifier.IsOpen && !member.IsStatic)
                {
                    SpecifierVerifier.VerifyTargetType(
                        delegateParameters[0].ParameterType,
                        member.DeclaringType!,
                        specifier.IsStrict);
                }

                bool requiresBinding = specifier.RequiresBinding;
                int closureIndex = requiresBinding ? 1 : 0;

                var arguments = new SpecifierInvokerInfo.ArgumentInfo[memberParameters.Length];
                var constants = new ValueCollector<int>((uint)memberParameters.Length);

                for (int i = 0; i < memberParameters.Length; i++)
                {
                    Type parameterType = memberParameters[i].ParameterType;
                    SpecifierInvokerInfo.ArgumentInfo argument = VerifyArgument(
                        input,
                        parameterType,
                        specifier.IsStrict,
                        ref closureIndex,
                        i);

                    if (argument.Source is SpecifierInvokerInfo.ArgumentSource.Constant)
                    {
                        constants.Collect(i);
                    }

                    arguments[i] = argument;
                }

                closure = Compress(input, constants.AsSpan());

                if (requiresBinding)
                {
                    closure = closure.BindType(member.DeclaringType!);
                }

                Type[] parameterTypes = specifier.IsOpen
                    ? [closure.GetType(), delegateParameters[0].ParameterType]
                    : [closure.GetType()];

                return new SpecifierInvokerInfo(
                    delegateInfo,
                    specifier.IsConstructor || member.IsStatic,
                    parameterTypes,
                    arguments,
                    SpecifierVerifier.VerifyReturn(
                        delegateInfo.ReturnType,
                        specifier.IsConstructor ? member.DeclaringType! : ((MethodInfo)member).ReturnType,
                        specifier.IsStrict));
            }


            private static Closure Compress(Closure source, scoped ReadOnlySpan<int> constants)
            {
                if (constants.IsEmpty)
                {
                    return source;
                }

                if (constants.Length == source.Length)
                {
                    return new ArrayClosure();
                }

                if (source is ArrayClosure array)
                {
                    object?[] args = array.Args;
                    object?[] values = new object?[args.Length - constants.Length];

                    int constantIndex = 0;
                    int valueIndex = 0;

                    for (int i = 0; i < args.Length; i++)
                    {
                        if (constantIndex < constants.Length && constants[constantIndex] == i)
                        {
                            constantIndex++;
                            continue;
                        }

                        values[valueIndex++] = args[i];
                    }

                    return new ArrayClosure(values);
                }

                Closure closure = source;

                for (int i = constants.Length - 1; i >= 0; i--)
                {
                    closure = closure.Compress(constants[i]);
                }

                return closure;
            }

            private static SpecifierInvokerInfo.ArgumentInfo VerifyArgument(
                Closure closure,
                Type parameterType,
                bool isStrict,
                ref int closureIndex,
                int position)
            {
                bool isNull = closure.IsNullAt(position);

                if (isNull)
                {
                    if (!parameterType.IsValueType)
                    {
                        return new SpecifierInvokerInfo.ArgumentInfo(
                            SpecifierInvokerInfo.ArgumentSource.Null,
                            -1,
                            parameterType,
                            parameterType,
                            true);
                    }

                    if (isStrict || !parameterType.IsNullable())
                    {
                        throw new ArgumentException(
                            $"Expects a non-null input argument of type '{parameterType.AsLog()}' " +
                            $"at position {position}, but received '{DiagnosticHelpers.Null}'.");
                    }

                    return new SpecifierInvokerInfo.ArgumentInfo(
                        SpecifierInvokerInfo.ArgumentSource.Null,
                        position,
                        parameterType,
                        parameterType,
                        true);
                }

                Type argumentType = closure.GetArgumentTypeAt(position);

                SpecifierInvokerInfo.ArgumentSource source;
                int sourceIndex;

                if (argumentType.IsConstant())
                {
                    source = SpecifierInvokerInfo.ArgumentSource.Constant;
                    sourceIndex = position;
                }
                else
                {
                    source = SpecifierInvokerInfo.ArgumentSource.Closure;
                    sourceIndex = closureIndex++;
                }

                Type parameterTypeToCompare = parameterType;

                if (parameterType.IsByRef)
                {
                    parameterTypeToCompare = parameterType.GetElementType()!;
                }

                if (Reflector.IsCompatible(parameterTypeToCompare, argumentType))
                {
                    return new SpecifierInvokerInfo.ArgumentInfo(
                        source,
                        sourceIndex,
                        argumentType,
                        parameterType,
                        true);
                }

                if (isStrict)
                {
                    throw new ArgumentException(
                        $"Expects an input argument of type '{parameterType.AsLog()}' " +
                        $"at position {position}, " +
                        $"but received '{argumentType.AsLog()}'.");
                }

                return new SpecifierInvokerInfo.ArgumentInfo(
                    source,
                    sourceIndex,
                    argumentType,
                    parameterType,
                    false);
            }
        }
    }
}
