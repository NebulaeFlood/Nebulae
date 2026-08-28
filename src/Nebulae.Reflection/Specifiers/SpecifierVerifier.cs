using Nebulae.Diagnostics;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nebulae.Reflection.Specifiers
{
    internal static class SpecifierVerifier
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfDeferTarget(object? target)
        {
            if (target == Specifier.Defer)
            {
                throw new ArgumentException(
                    $"Cannot bind to the target '{Specifier.Defer}', " +
                    $"as it represents a deferred target.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfInputEmpty(scoped ReadOnlySpan<object?> args)
        {
            if (args.IsEmpty)
            {
                throw new ArgumentException($"Expects at least 1 argument, but received 0.");
            }
        }


        public static bool VerifyArgumentType(Type argumentType, Type parameterType, bool isStrict, int position)
        {
            if (Reflector.IsCompatible(parameterType, argumentType))
            {
                return true;
            }

            if (isStrict)
            {
                throw new ArgumentException(
                    $"Expects delegate parameter type '{parameterType.AsLog()}' " +
                    $"at position {position}, " +
                    $"but received '{argumentType.AsLog()}'.");
            }

            return false;
        }

        public static void VerifyBindingTarget(object? target, MemberInfo member, bool isStatic)
        {
            if (target is null)
            {
                if (!isStatic)
                {
                    throw new ArgumentException(
                        $"Expects a non-null binding target " +
                        $"for instance member '{member.AsLog()}', " +
                        $"but received '{DiagnosticHelpers.Null}'.");
                }
            }
            else if (isStatic)
            {
                throw new ArgumentException(
                    $"Expects a null binding target " +
                    $"for static member '{member.AsLog()}', " +
                    $"but received '{target.AsLog()}'.");
            }
            else
            {
                Type targetType = target.GetType();
                Type declaringType = member.DeclaringType!;

                if (!declaringType.IsAssignableFrom(targetType))
                {
                    throw new ArgumentException(
                        $"Expects a target object of type '{declaringType.AsLog()}' " +
                        $"for instance member '{member.AsLog()}', " +
                        $"but received '{target.AsLog()}' of type '{targetType.AsLog()}'.");
                }
            }
        }

        public static SpecifierInvokerInfo.DelegateInfo VerifyDelegate<T>() where T : Delegate
        {
            Type delegateType = typeof(T);

            if (delegateType.IsAbstract)
            {
                throw new ArgumentException(
                    $"Cannot compile to abstract delegate type '{delegateType.AsLog()}'.");
            }

            MethodInfo invoker = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)
                ?? throw new NotSupportedException(
                    $"Cannot compile to delegate type '{delegateType.AsLog()}', " +
                    $"because it does not have any method named 'Invoke'.");

            return new SpecifierInvokerInfo.DelegateInfo(
                delegateType,
                invoker,
                invoker.GetParameters());
        }

        public static SpecifierInvokerInfo.ReturnInfo VerifyReturn(Type returnType, Type memberType, bool isStrict)
        {
            if (typeof(void) == memberType || memberType.IsByRef)
            {
                if (returnType != memberType)
                {
                    throw new ArgumentException(
                        $"Expects return type '{memberType.AsLog()}', " +
                        $"but received '{returnType.AsLog()}'.");
                }

                return new SpecifierInvokerInfo.ReturnInfo(memberType, true);
            }

            if (Reflector.IsCompatible(returnType, memberType))
            {
                return new SpecifierInvokerInfo.ReturnInfo(memberType, true);
            }

            if (isStrict)
            {
                throw new ArgumentException(
                    $"Expects return type '{memberType.AsLog()}', " +
                    $"but received '{returnType.AsLog()}'.");
            }

            return new SpecifierInvokerInfo.ReturnInfo(memberType, false);
        }

        public static bool VerifyTargetType(Type argumentType, Type targetType, bool isStrict)
        {
            if (targetType.IsValueType)
            {
                if (argumentType.IsByRef && targetType == argumentType.GetElementType())
                {
                    return true;
                }
            }
            else if (Reflector.IsCompatible(targetType, argumentType))
            {
                return true;
            }

            if (isStrict)
            {
                throw new ArgumentException(
                    $"Expects parameter type '{targetType.AsLog()}', " +
                    $"at position 0," +
                    $"but received '{argumentType.AsLog()}'.");
            }

            return false;
        }
    }
}
