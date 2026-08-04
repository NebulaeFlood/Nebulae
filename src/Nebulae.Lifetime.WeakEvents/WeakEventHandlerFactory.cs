using Nebulae.Diagnostics;
using Nebulae.Runtime.Emit.Inline;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nebulae.Lifetime.WeakEvents
{
    internal static class WeakEventHandlerFactory
    {
        public static WeakEventHandler<TSender, TArgs> AsWeak<TSender, TArgs>(this EventHandler<TSender, TArgs> handler)
#if NET9_0_OR_GREATER
            where TSender : allows ref struct
            where TArgs : allows ref struct
#endif
        {
            object? target = handler.Target;

            if (target is null)
            {
                return new WeakEventHandler<TSender, TArgs>(handler);
            }

            if (target.GetType().IsValueType)
            {
                throw new ArgumentException(
                    $"Cannot convert method '{handler.Method.AsLog()}' to a weak event handler " +
                    $"because it is an instance method of a value type.");
            }

            return WeakEventHandler<TSender, TArgs>.Create(
                target,
                handler.Method.MethodHandle.GetFunctionPointer());
        }

        public static WeakEventHandler<TSender, TArgs> AsWeak<TSender, TArgs>(this Delegate @delegate)
#if NET9_0_OR_GREATER
            where TSender : allows ref struct
            where TArgs : allows ref struct
#endif
        {
            MethodInfo method = @delegate.Method;
            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length is not 2
                || parameters[0].ParameterType != typeof(TSender)
                || parameters[1].ParameterType != typeof(TArgs))
            {
                throw new ArgumentException(
                    $"Cannot convert the given method to a weak event handler. " +
                    $"A valid method must have exactly two parameters '({typeof(TSender).AsLog()} sender, {typeof(TArgs).AsLog()} args)', " +
                    $"while the given method is '({parameters.AsLog()})'.");
            }

            object? target = @delegate.Target;

            if (target is null)
            {
                return WeakEventHandler<TSender, TArgs>.Create(
                    method.MethodHandle.GetFunctionPointer());
            }

            if (target.GetType().IsValueType)
            {
                throw new ArgumentException(
                    $"Cannot convert method '{method.AsLog()}' to a weak event handler " +
                    $"because it is an instance method of a value type.");
            }

            return WeakEventHandler<TSender, TArgs>.Create(
                target,
                method.MethodHandle.GetFunctionPointer());
        }
    }
}
