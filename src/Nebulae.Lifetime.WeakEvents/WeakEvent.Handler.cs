using Nebulae.Diagnostics;
using Nebulae.Runtime.Emit.Inline;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nebulae.Lifetime.WeakEvents
{
    public sealed partial class WeakEvent<TSender, TArgs>
    {
        [DebuggerDisplay("IsAlive = {IsAlive}, Method = {Method}")]
        private readonly struct Handler
        {
            private delegate void EventHandler(object target, TSender sender, TArgs args);


            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------

            #region Public Properties

            public bool IsAlive
            {
                get
                {
                    return _targetRef is null
                        || _targetRef.TryGetTarget(out _);
                }
            }

            public bool IsStatic
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _targetRef is null;
            }

            public MethodInfo Method
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _invocation.Method;
            }

            public object? Target
            {
                get
                {
                    if (_targetRef is not null
                        && _targetRef.TryGetTarget(out object? target))
                    {
                        return target;
                    }

                    return null;
                }
            }

            #endregion


            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            private Handler(EventHandler<TSender, TArgs> invocation)
            {
                Debug.Assert(invocation.Target is null);

                _invocation = invocation;
            }

            private Handler(object target, EventHandler invocation)
            {
                Debug.Assert(target is not null and not ValueType);

                _targetRef = new WeakReference<object>(target!);
                _invocation = invocation;
            }

            #endregion


            public static Handler Create(EventHandler<TSender, TArgs> handler)
            {
                object? target = handler.Target;

                if (target is null)
                {
                    return new Handler(handler);
                }

                if (target is ValueType)
                {
                    throw new ArgumentException(
                        $"Cannot convert method '{handler.Method.AsLog()}' to a weak event handler, " +
                        $"because it is an instance method of a value type.");
                }

                MethodInfo method = handler.Method;

#if !NETSTANDARD2_0
                if (method is DynamicMethod)
                {
                    throw new NotSupportedException(
                        $"Cannor convert method '{method.AsLog()}' to a weak event handler, " +
                        $"because it is a dynamic method.");
                }
#endif

                return new Handler(
                    target,
                    CreateDelegate(method.MethodHandle.GetFunctionPointer()));


                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static EventHandler CreateDelegate(nint functionPointer)
                {
                    IL.Emit.Ldnull();
                    IL.Emit.Ldarg(functionPointer);
                    IL.Emit.Newobj(
                        IL.Ref(typeof(EventHandler))
                            .Constructor(typeof(object), typeof(nint)));
                    return IL.Ret<EventHandler>();
                }
            }


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            public bool Matches(Delegate target)
            {
                if (Method != target.Method)
                {
                    return false;
                }

                if (_targetRef is null)
                {
                    return target.Target is null;
                }

                return _targetRef.TryGetTarget(out object? instance)
                    && ReferenceEquals(instance, target.Target);
            }

            public void Invoke(TSender sender, TArgs args)
            {
                if (_targetRef is null)
                {
                    Unsafe.As<EventHandler<TSender, TArgs>>(_invocation)
                        .Invoke(sender, args);
                    return;
                }

                if (_targetRef.TryGetTarget(out object? target))
                {
                    Unsafe.As<EventHandler>(_invocation)
                        .Invoke(target, sender, args);
                }
            }

            public override string? ToString()
            {
                return _invocation.ToString();
            }

            #endregion


            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private readonly WeakReference<object>? _targetRef;
            private readonly Delegate _invocation;

            #endregion
        }
    }
}
