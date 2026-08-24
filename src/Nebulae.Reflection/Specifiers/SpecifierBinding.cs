namespace Nebulae.Reflection.Specifiers
{
    /// <summary>
    /// 引用说明符的绑定类型
    /// </summary>
    public enum SpecifierBinding : byte
    {
        /// <summary>
        /// 未绑定目标
        /// </summary>
        Open,

        /// <summary>
        /// 已绑定目标
        /// </summary>
        Close
    }
}
