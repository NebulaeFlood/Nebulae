using Nebulae.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Nebulae.Lifetime.WeakEvents
{
    /// <summary>
    /// 弱事件
    /// </summary>
    /// <typeparam name="TSender">事件源类型</typeparam>
    /// <typeparam name="TArgs">事件数据类型</typeparam>
    /// <remarks>
    /// <para>
    /// 保存订阅者的弱引用，使订阅者在订阅期间可被回收。
    /// </para>
    /// <para>
    /// 对于包含多个处理器的 <see cref="MulticastDelegate"/>，只会转换其中最后被添加的处理器。
    /// </para>
    /// <para>
    /// <b>该<see cref="WeakEvent{TSender, TArgs}"/> 中的所有公共成员都是线程安全的。</b>
    /// </para>
    /// </remarks>
    /// <example>
    /// <![CDATA[
    ///    private WeakEvent<object, EventArgs> _exampleEvent = new WeakEvent<object, EventArgs>();
    ///
    ///    public event EventHandler ExampleEvent
    ///    {
    ///        add { _exampleEvent.Subscribe(value); }
    ///        remove { _exampleEvent.Unsubscribe(value); }
    ///     }
    ///
    ///    public event EventHandler<object, EventArgs> ExampleEvent2
    ///    {
    ///        add { _exampleEvent += value; }
    ///        remove { _exampleEvent -= value; }
    ///     }
    /// ]]>
    /// </example>
    public sealed class WeakEvent<TSender, TArgs>
#if NET9_0_OR_GREATER
        where TSender : allows ref struct
        where TArgs : allows ref struct
#endif
    {
#if NET7_0_OR_GREATER
        private const string AotMessage =
            "Weak-event subscription constructs delegates from runtime method pointers " +
            "and is not supported by NativeAOT.";
#endif

        /// <summary>
        /// 初始化 <see cref="WeakEvent{TSender, TArgs}"/> 的新实例
        /// </summary>
        public WeakEvent() { }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// 调用所有事件处理器
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="args">事件参数</param>
        public void Invoke(TSender sender, TArgs args)
        {
            State state = _state;
            var invocationList = state.InvocationList ?? Rebuild(state);

            for (int i = 0; i < invocationList.Length; i++)
            {
                invocationList[i].Invoke(sender, args);
            }
        }

        /// <summary>
        /// 订阅事件处理器
        /// </summary>
        /// <param name="handler">要订阅的处理器</param>
#if NET7_0_OR_GREATER
        [RequiresDynamicCode(AotMessage)]
#endif
        public void Subscribe(EventHandler<TSender, TArgs> handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(handler);
            SubscribeCore(handler.AsWeak());
        }

        /// <summary>
        /// 订阅事件处理器
        /// </summary>
        /// <param name="handler">要订阅的处理器</param>
#if NET7_0_OR_GREATER
        [RequiresDynamicCode(AotMessage)]
#endif
        public void Subscribe(Delegate handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(handler);
            SubscribeCore(handler.AsWeak<TSender, TArgs>());
        }

        /// <summary>
        /// 取消订阅事件处理器
        /// </summary>
        /// <param name="handler">要取消订阅的处理器</param>
        public void Unsubscribe(Delegate handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(handler);
            UnsubscribeCore(handler);
        }

        /// <summary>
        /// 移除所有引用对象已被回收的事件处理器
        /// </summary>
        public void Purge()
        {
        Retry:
            State observed = _state;

            if (!AnyDeath(observed.Head))
            {
                return;
            }

            Node? head = null;
            Node? tail = null;

            int count = 0;

            for (var current = observed.Head; current is not null; current = current.Next)
            {
                if (current.Handler.IsAlive)
                {
                    var node = new Node(current.Handler);

                    if (tail is null)
                    {
                        head = node;
                    }
                    else
                    {
                        tail.Next = node;
                    }

                    tail = node;
                    count++;
                }
            }

            State state = new(head, count);

            if (Interlocked.CompareExchange(ref _state, state, observed) != observed)
            {
                goto Retry;
            }

            return;


            static bool AnyDeath(Node? head)
            {
                for (var node = head; node is not null; node = node.Next)
                {
                    if (!node.Handler.IsAlive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private WeakEventHandler<TSender, TArgs>[] Rebuild(State state)
        {
            var handlers = state.Count is 0
                ? []
                : new WeakEventHandler<TSender, TArgs>[state.Count];

            int index = handlers.Length;
            bool anyDeath = false;

            for (var node = state.Head; node is not null; node = node.Next)
            {
                if (node.Handler.IsAlive)
                {
                    handlers[--index] = node.Handler;
                }
                else
                {
                    anyDeath = true;
                }
            }

            if (anyDeath)
            {
                handlers = new ReadOnlySpan<WeakEventHandler<TSender, TArgs>>(
                    handlers,
                    index,
                    handlers.Length - index).ToArray();

                Node? head = null;

                for (int i = 0; i < handlers.Length; i++)
                {
                    head = new Node(handlers[i], head);
                }

                if (Interlocked.CompareExchange(ref _state, new(head, handlers), state) == state)
                {
                    return handlers;
                }
            }

            return Interlocked.CompareExchange(
                ref state.InvocationList,
                handlers,
                null) ?? handlers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubscribeCore(WeakEventHandler<TSender, TArgs> handler)
        {
        Retry:
            State observed = _state;
            State state = new(
                new Node(handler, observed.Head),
                observed.Count + 1);

            if (Interlocked.CompareExchange(ref _state, state, observed) != observed)
            {
                goto Retry;
            }
        }

        private void UnsubscribeCore(Delegate target)
        {
        Retry:
            State observed = _state;

            for (var node = observed.Head; node is not null; node = node.Next)
            {
                if (!node.Handler.IsAlive || node.Handler.Matches(target))
                {
                    goto Start;
                }
            }

            return;
        Start:
            Node? head = null;
            Node? tail = null;

            int count = 0;
            bool removed = false;

            for (var current = observed.Head; current is not null; current = current.Next)
            {
                WeakEventHandler<TSender, TArgs> handler = current.Handler;

                if (!handler.IsAlive)
                {
                    continue;
                }

                if (!removed && handler.Matches(target))
                {
                    removed = true;
                    continue;
                }

                var node = new Node(current.Handler);

                if (tail is null)
                {
                    head = node;
                }
                else
                {
                    tail.Next = node;
                }

                tail = node;
                count++;
            }

            State state = new(head, count);

            if (Interlocked.CompareExchange(ref _state, state, observed) != observed)
            {
                goto Retry;
            }
        }

        #endregion


        private volatile State _state = State.Empty;


        //------------------------------------------------------
        //
        //  Operators
        //
        //------------------------------------------------------

        #region Operators

        /// <summary>
        /// 添加事件处理器
        /// </summary>
        /// <param name="event">目标弱事件</param>
        /// <param name="handler">要添加的处理器</param>
        /// <returns>添加处理器后的 <paramref name="event"/>。</returns>
#if NET7_0_OR_GREATER
        [RequiresDynamicCode(AotMessage)]
#endif
        public static WeakEvent<TSender, TArgs> operator +(
            WeakEvent<TSender, TArgs> @event,
            EventHandler<TSender, TArgs> handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(@event);
            ThrowHelpers.ThrowIfArgumentNull(handler);

            @event.SubscribeCore(handler.AsWeak());
            return @event;
        }

        /// <summary>
        /// 添加事件处理器
        /// </summary>
        /// <param name="event">目标弱事件</param>
        /// <param name="handler">要添加的处理器</param>
        /// <returns>添加处理器后的 <paramref name="event"/>。</returns>
#if NET7_0_OR_GREATER
        [RequiresDynamicCode(AotMessage)]
#endif
        public static WeakEvent<TSender, TArgs> operator +(
            WeakEvent<TSender, TArgs> @event,
            Delegate handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(@event);
            ThrowHelpers.ThrowIfArgumentNull(handler);

            @event.SubscribeCore(handler.AsWeak<TSender, TArgs>());
            return @event;
        }

        /// <summary>
        /// 移除事件处理器
        /// </summary>
        /// <param name="event">目标弱事件</param>
        /// <param name="handler">要移除的处理器</param>
        /// <returns>移除处理器后的 <paramref name="event"/>。</returns>
        public static WeakEvent<TSender, TArgs> operator -(
            WeakEvent<TSender, TArgs> @event,
            Delegate handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(@event);
            ThrowHelpers.ThrowIfArgumentNull(handler);

            @event.UnsubscribeCore(handler);
            return @event;
        }

        #endregion


        private sealed class Node
        {
            public readonly WeakEventHandler<TSender, TArgs> Handler;
            public Node? Next;

            public Node(WeakEventHandler<TSender, TArgs> handler)
            {
                Handler = handler;
            }

            public Node(WeakEventHandler<TSender, TArgs> handler, Node? next)
            {
                Handler = handler;
                Next = next;
            }
        }

        private sealed class State
        {
            public static readonly State Empty = new();

            public readonly Node? Head;
            public readonly int Count;

            public volatile WeakEventHandler<TSender, TArgs>[]? InvocationList;


            private State() { }


            public State(Node? head, int count)
            {
                Head = head;
                Count = count;
            }

            public State(Node? head, WeakEventHandler<TSender, TArgs>[] invocationList)
            {
                Head = head;
                Count = invocationList.Length;

                InvocationList = invocationList;
            }
        }
    }
}
