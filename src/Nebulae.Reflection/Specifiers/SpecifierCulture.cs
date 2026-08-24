using System.Globalization;

namespace Nebulae.Reflection.Specifiers
{
    /// <summary>
    /// 引用说明符的文化信息
    /// </summary>
    public enum SpecifierCulture : byte
    {
        /// <summary>
        /// 对应 <see cref="CultureInfo.CurrentCulture"/>
        /// </summary>
        ///
        CurrentCulture,
        /// <summary>
        /// 对应 <see cref="CultureInfo.CurrentUICulture"/>
        /// </summary>
        CurrentUICulture,

        /// <summary>
        /// 对应 <see cref="CultureInfo.DefaultThreadCurrentCulture"/>
        /// </summary>
        DefaultThreadCurrentCulture,

        /// <summary>
        /// 对应 <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>
        /// </summary>
        DefaultThreadCurrentUICulture,

        /// <summary>
        /// 对应 <see cref="CultureInfo.InstalledUICulture"/>
        /// </summary>
        InstalledUICulture,

        /// <summary>
        /// 对应 <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        InvariantCulture
    }
}
