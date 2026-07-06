namespace System.Runtime.CompilerServices
{
#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable IDE0290

    /// <summary>
    /// 指明参数捕获指定参数传递的表达式作为字符串
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        /// <summary>
        /// 初始化 <see cref="CallerArgumentExpressionAttribute"/> 的新实例
        /// </summary>
        /// <param name="parameterName">目标参数的名称</param>
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }


        /// <summary>
        /// 获取目标参数的名称
        /// </summary>
        public string ParameterName { get; }
    }
#endif
}
