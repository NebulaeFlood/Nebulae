using Nebulae.Diagnostics;
using System;
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
                    $"'{Specifier.Defer}' cannot be used as an actual binding target " +
                    $"because it represents a deferred target.");
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
                    $"Expects parameter type '{parameterType.AsLog()}' " +
                    $"at position {position}, " +
                    $"but received '{argumentType.AsLog()}'.");
            }

            return false;
        }

        public static bool VerifyReturnType(Type returnType, Type memberType, bool isStrict)
        {
            if (typeof(void) == memberType || memberType.IsByRef)
            {
                if (returnType != memberType)
                {
                    throw new ArgumentException(
                        $"Expects return type '{memberType.AsLog()}', " +
                        $"but received '{returnType.AsLog()}'.");
                }

                return true;
            }

            if (Reflector.IsCompatible(returnType, memberType))
            {
                return true;
            }

            if (isStrict)
            {
                throw new ArgumentException(
                    $"Expects return type '{memberType.AsLog()}', " +
                    $"but received '{returnType.AsLog()}'.");
            }

            return false;
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
