using Nebulae.Runtime.Emit.Inline;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nebulae.Lifetime.WeakEvents
{
    [DebuggerDisplay("IsAlive = {IsAlive}, Method = {Method}")]
    internal readonly struct WeakEventHandler<TSender, TArgs>
#if NET9_0_OR_GREATER
        where TSender : allows ref struct
        where TArgs : allows ref struct
#endif
    {
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

        internal WeakEventHandler(EventHandler<TSender, TArgs> invocation)
        {
            Debug.Assert(invocation.Target is null);
            Debug.Assert(invocation.Method.DeclaringType is Type { IsValueType: false });

            _invocation = invocation;
        }

        internal WeakEventHandler(
            object target,
            EventHandlerInternal<TSender, TArgs> invocation)
        {
            Debug.Assert(target is not null);
            Debug.Assert(!target!.GetType().IsValueType);

            _targetRef = new WeakReference<object>(target);
            _invocation = invocation;
        }

        #endregion


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
                Unsafe.As<EventHandlerInternal<TSender, TArgs>>(_invocation)
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
        //  Internal Static Methods
        //
        //------------------------------------------------------

        #region Internal Static Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static WeakEventHandler<TSender, TArgs> Create(
            nint functionPointer)
        {
            IL.Emit.Ldnull();
            IL.Emit.Ldarg(functionPointer);
            IL.Emit.Newobj(
                IL.Ref(typeof(EventHandler<TSender, TArgs>))
                    .Constructor(typeof(object), typeof(nint)));
            IL.Emit.Newobj(
                IL.Ref(typeof(WeakEventHandler<TSender, TArgs>))
                    .Constructor(typeof(EventHandler<TSender, TArgs>)));
            IL.Emit.Ret();

            throw IL.Fail();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static WeakEventHandler<TSender, TArgs> Create(
            object target,
            nint functionPointer)
        {
            IL.Emit.Ldarg(target);
            IL.Emit.Ldnull();
            IL.Emit.Ldarg(functionPointer);
            IL.Emit.Newobj(
                IL.Ref(typeof(EventHandlerInternal<TSender, TArgs>))
                    .Constructor(typeof(object), typeof(nint)));
            IL.Emit.Newobj(
                IL.Ref(typeof(WeakEventHandler<TSender, TArgs>))
                    .Constructor(typeof(object), typeof(EventHandlerInternal<TSender, TArgs>)));
            IL.Emit.Ret();

            throw IL.Fail();
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
