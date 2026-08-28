using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nebulae.Reflection.Specifiers
{
    internal readonly ref struct SpecifierInvokerInfo
    {
        public readonly DelegateInfo Delegate;

        public readonly bool IsStatic;

        public readonly Type[]? ParameterTypes;

        public readonly ArgumentInfo[]? Arguments;

        public readonly ReturnInfo Return;


        [MemberNotNullWhen(true, nameof(ParameterTypes), nameof(Arguments))]
        public bool IsDynamic
        {
            get => ParameterTypes is not null;
        }


        public SpecifierInvokerInfo(
            DelegateInfo delegateInfo,
            bool isStatic,
            ReturnInfo returnInfo)
        {
            Delegate = delegateInfo;
            IsStatic = isStatic;
            Return = returnInfo;
        }

        public SpecifierInvokerInfo(
            DelegateInfo delegateInfo,
            bool isStatic,
            Type[] parameterTypes,
            ArgumentInfo[] arguments,
            ReturnInfo returnInfo)
        {
            Delegate = delegateInfo;
            IsStatic = isStatic;
            ParameterTypes = parameterTypes;
            Arguments = arguments;
            Return = returnInfo;
        }


        public enum ArgumentSource : byte
        {
            Parameter,
            Closure,
            Constant,
            Null
        }

        public readonly struct ArgumentInfo(
            ArgumentSource source,
            int sourceIndex,
            Type argumentType,
            Type parameterType,
            bool isCompatible)
        {
            public readonly ArgumentSource Source = source;

            public readonly int SourceIndex = sourceIndex;

            public readonly Type ArgumentType = argumentType;

            public readonly Type ParameterType = parameterType;

            public readonly bool IsCompatible = isCompatible;
        }

        public readonly struct DelegateInfo(
            Type type,
            MethodInfo invoker,
            ParameterInfo[] parameters)
        {
            public readonly Type Type = type;

            public readonly MethodInfo Invoker = invoker;

            public readonly ParameterInfo[] Parameters = parameters;

            public readonly Type ReturnType = invoker.ReturnType;
        }

        public readonly struct ReturnInfo(
            Type type,
            bool isCompatible)
        {
            public readonly Type Type = type;

            public readonly bool IsCompatible = isCompatible;
        }
    }
}
