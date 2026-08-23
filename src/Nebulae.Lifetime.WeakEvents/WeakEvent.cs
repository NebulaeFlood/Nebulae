using Nebulae.Diagnostics;
using System;
using System.Diagnostics;
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
    /// 对于动态创建的委托，如表达式树、动态方法等，若绑定了目标实例，
    /// 将无法作为 <see cref="WeakEvent{TSender, TArgs}"/> 的处理器。
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
    public sealed partial class WeakEvent<TSender, TArgs>
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
            Handler[] invocationList = state.InvocationList ?? Rebuild(state);

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
            SubscribeCore(handler);
        }

        /// <summary>
        /// 取消订阅事件处理器
        /// </summary>
        /// <param name="handler">要取消订阅的处理器</param>
        public void Unsubscribe(EventHandler<TSender, TArgs> handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(handler);
            UnsubscribeCore(handler);
        }

        /// <summary>
        /// 预先构建事件处理器调用列表缓存
        /// </summary>
        /// <remarks>
        /// 当订阅状态发生变化后，调用列表缓存会失效，
        /// 若期间未调用 <see cref="Prepare"/>，
        /// 将在下次调用 <see cref="Invoke(TSender, TArgs)"/> 时重新构建。
        /// </remarks>
        public void Prepare()
        {
            State state = _state;

            if (state.InvocationList is not null)
            {
                return;
            }

            Handler[] handlers = state.Count is 0
                ? []
                : new Handler[state.Count];

            int index = handlers.Length;
            bool anyDeath = false;

            for (Node? node = state.Head; node is not null; node = node.Next)
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

            if (!anyDeath)
            {
                Interlocked.CompareExchange(ref state.InvocationList, handlers, null);
                return;
            }

            handlers = new ReadOnlySpan<Handler>(
                handlers,
                index,
                handlers.Length - index).ToArray();

            Node? head = null;

            for (int i = 0; i < handlers.Length; i++)
            {
                head = new Node(handlers[i], head);
            }

            Interlocked.CompareExchange(ref _state, new State(head, handlers), state);
        }

        /// <summary>
        /// 移除所有目标对象已被回收的事件处理器
        /// </summary>
        public void Purge()
        {
        Retry:
            State state = _state;

            if (!AnyDeath(state.Head))
            {
                return;
            }

            Node? head = null;
            Node? tail = null;

            int count = 0;

            for (var current = state.Head; current is not null; current = current.Next)
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

            if (Interlocked.CompareExchange(ref _state, new State(head, count), state) != state)
            {
                goto Retry;
            }


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

        private Handler[] Rebuild(State state)
        {
            Handler[] handlers = state.Count is 0
                ? []
                : new Handler[state.Count];

            int index = handlers.Length;
            bool anyDeath = false;

            for (Node? node = state.Head; node is not null; node = node.Next)
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
                handlers = new ReadOnlySpan<Handler>(
                    handlers,
                    index,
                    handlers.Length - index).ToArray();

                Node? head = null;

                for (int i = 0; i < handlers.Length; i++)
                {
                    head = new Node(handlers[i], head);
                }

                if (Interlocked.CompareExchange(ref _state, new State(head, handlers), state) == state)
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
        private void SubscribeCore(EventHandler<TSender, TArgs> handler)
        {
#if NET9_0_OR_GREATER
            if (handler.HasSingleTarget)
            {
                var node = new Node(Handler.Create(handler));
                SubscribeCore(node, node, 1);
            }
            else
            {
                var enumerator = Delegate.EnumerateInvocationList(handler).GetEnumerator();
                enumerator.MoveNext();

                int count = 1;

                Node tail = new(Handler.Create(enumerator.Current));
                Node head = tail;

                while (enumerator.MoveNext())
                {
                    count++;
                    head = new Node(Handler.Create(enumerator.Current), head);
                }

                SubscribeCore(head, tail, count);
            }
#else
            Delegate[] invocationList = handler.GetInvocationList();

            Node tail = new(Handler.Create(Unsafe.As<EventHandler<TSender, TArgs>>(invocationList[0])));
            Node head = tail;

            for (int i = 1; i < invocationList.Length; i++)
            {
                head = new Node(
                    Handler.Create(Unsafe.As<EventHandler<TSender, TArgs>>(invocationList[i])),
                    head);
            }

            SubscribeCore(head, tail, invocationList.Length);
#endif
        }

        private void SubscribeCore(Node head, Node tail, int count)
        {
        Retry:
            State state = _state;
            tail.Next = state.Head;

            State newState = new(head, state.Count + count);

            if (Interlocked.CompareExchange(ref _state, newState, state) != state)
            {
                goto Retry;
            }
        }

        private void UnsubscribeCore(EventHandler<TSender, TArgs> handler)
        {
#if NET9_0_OR_GREATER
            if (handler.HasSingleTarget)
            {
                UnsubscribeCore((Delegate)handler);
            }
            else
            {
                UnsubscribeCore(handler.GetInvocationList());
            }
#else
            Delegate[] handlers = handler.GetInvocationList();

            if (handlers.Length is 1)
            {
                UnsubscribeCore((Delegate)handler);
            }
            else
            {
                UnsubscribeCore(handlers);
            }
#endif
        }

        private void UnsubscribeCore(Delegate handler)
        {
        Retry:
            State state = _state;

            bool anyDeath = false;
            Node? cadidate = null;

            for (Node? node = state.Head; node is not null; node = node.Next)
            {
                if (!node.Handler.IsAlive)
                {
                    anyDeath = true;
                    continue;
                }

                if (node.Handler.Matches(handler))
                {
                    cadidate = node;
                    break;
                }
            }

            if (!anyDeath && cadidate is null)
            {
                return;
            }

            // The tail of invocation list.
            Node? head = null;
            // The head of invocation list.
            Node? tail = null;

            int count = 0;

            for (Node? current = state.Head; current is not null; current = current.Next)
            {
                if (cadidate == current)
                {
                    continue;
                }

                if (!current.Handler.IsAlive)
                {
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

            State newState = count is 0
                ? State.Empty
                : new State(head, count);

            if (Interlocked.CompareExchange(ref _state, newState, state) != state)
            {
                goto Retry;
            }
        }

        private void UnsubscribeCore(Delegate[] handlers)
        {
        Retry:
            State state = _state;

            Search(state.Head, handlers, out bool anyDeath, out Node? cadidate);

            if (!anyDeath && cadidate is null)
            {
                return;
            }

            // The tail of invocation list.
            Node? head = null;
            // The head of invocation list.
            Node? tail = null;

            int count = 0;

            for (Node? current = state.Head; current is not null; current = current.Next)
            {
                if (cadidate == current)
                {
                    for (int i = 1; i < handlers.Length; i++)
                    {
                        current = current!.Next;
                    }

                    if (current is null)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!current.Handler.IsAlive)
                {
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

                count++;
                tail = node;
            }

            State newState = count is 0
                ? State.Empty
                : new State(head, count);

            if (Interlocked.CompareExchange(ref _state, newState, state) != state)
            {
                goto Retry;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Search(Node? head, Delegate[] handlers, out bool anyDeath, out Node? cadidate)
            {
                anyDeath = false;

                for (Node? node = head; node is not null; node = node.Next)
                {
                    if (!node.Handler.IsAlive)
                    {
                        anyDeath = true;
                        continue;
                    }

                    cadidate = node;

                    for (int i = handlers.Length - 1; i >= 0; i--)
                    {
                        if (!node.Handler.Matches(handlers[i]))
                        {
                            break;
                        }

                        if (i is 0)
                        {
                            return;
                        }

                        node = node.Next;

                        if (node is null)
                        {
                            cadidate = null;
                            return;
                        }

                        if (!node.Handler.IsAlive)
                        {
                            anyDeath = true;
                            break;
                        }
                    }
                }

                cadidate = null;
            }
        }

        #endregion


        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
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

            @event.SubscribeCore(handler);
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
            EventHandler<TSender, TArgs> handler)
        {
            ThrowHelpers.ThrowIfArgumentNull(@event);
            ThrowHelpers.ThrowIfArgumentNull(handler);

            @event.UnsubscribeCore(handler);
            return @event;
        }

        #endregion


        private sealed class Node
        {
            public readonly Handler Handler;
            public Node? Next;

            public Node(Handler handler)
            {
                Handler = handler;
            }

            public Node(Handler handler, Node? next)
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

            public volatile Handler[]? InvocationList;


            private State() { }


            public State(Node? head, int count)
            {
                Head = head;
                Count = count;
            }

            public State(Node? head, Handler[] invocationList)
            {
                Head = head;
                Count = invocationList.Length;

                InvocationList = invocationList;
            }
        }
    }
}
