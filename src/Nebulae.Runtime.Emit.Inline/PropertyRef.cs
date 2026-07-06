namespace Nebulae.Runtime.Emit.Inline
{
#if NET10_0_OR_GREATER
#pragma warning disable CA1822
#endif

    /// <summary>
    /// 属性引用
    /// </summary>
    [Reference(ReferenceType.Property)]
    public sealed class PropertyRef
    {
        /// <summary>
        /// 获取属性 <see langword="get"/> 方法的引用
        /// </summary>
        public MethodRef Get
        {
            [Reference(ReferenceType.PropertyGet)]
            get => IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取属性 <see langword="set"/> 方法的引用
        /// </summary>
        public MethodRef Set
        {
            [Reference(ReferenceType.PropertySet)]
            get => IL.Throw<MethodRef>();
        }


        private PropertyRef() { }
    }
}
