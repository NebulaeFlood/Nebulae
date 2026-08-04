using System;
using System.Collections.Generic;
using System.Text;

namespace Nebulae.Lifetime.WeakEvents
{
#if !NET9_0_OR_GREATER
    /// <summary>
    /// 表示一个事件处理程序
    /// </summary>
    /// <typeparam name="TSender">事件源的类型</typeparam>
    /// <typeparam name="TArgs">事件参数的类型</typeparam>
    /// <param name="sender">事件源</param>
    /// <param name="args">事件参数</param>
    public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
#endif

    internal delegate void EventHandlerInternal<TSender, TArgs>(object target, TSender sender, TArgs args)
#if NET9_0_OR_GREATER
        where TSender : allows ref struct
        where TArgs : allows ref struct
#endif
        ;
}
