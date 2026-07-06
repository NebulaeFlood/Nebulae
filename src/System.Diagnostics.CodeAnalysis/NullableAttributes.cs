namespace System.Diagnostics.CodeAnalysis
{
#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable IDE0290
    /// <summary>
    /// 指明不允许 <see langword="null"/> 的类型可以被赋值为 <see langword="null"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute { }

    /// <summary>
    /// 指明方法永远不会返回
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute { }

    /// <summary>
    /// 指明参数在方法返回指定值时可能为 <see langword="null"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        /// <summary>
        /// 初始化 <see cref="MaybeNullWhenAttribute"/> 的新实例
        /// </summary>
        /// <param name="returnValue">目标返回值</param>
        public MaybeNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }


        /// <summary>
        /// 获取设定的目标返回值
        /// </summary>
        public bool ReturnValue { get; }
    }

    /// <summary>
    /// 指明参数或输出永远不会为 <see langword="null"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute { }

    /// <summary>
    /// 指明参数在方法返回指定值时永远不会为 <see langword="null"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>
        /// 初始化 <see cref="NotNullWhenAttribute"/> 的新实例
        /// </summary>
        /// <param name="returnValue">目标返回值</param>
        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }


        /// <summary>
        /// 获取设定的目标返回值
        /// </summary>
        public bool ReturnValue { get; }
    }
#endif
}