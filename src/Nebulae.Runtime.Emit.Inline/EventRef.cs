namespace Nebulae.Runtime.Emit.Inline
{
#if NET10_0_OR_GREATER
#pragma warning disable CA1822
#endif

    /// <summary>
    /// 事件引用
    /// </summary>
    [Reference(ReferenceType.Event)]
    public sealed class EventRef
    {
        /// <summary>
        /// 获取事件的 <see langword="add"/> 方法的引用
        /// </summary>
        public MethodRef Add
        {
            [Reference(ReferenceType.EventAdd)]
            get => IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取引发事件的方法的引用
        /// </summary>
        public MethodRef Raise
        {
            [Reference(ReferenceType.EventRaise)]
            get => IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取事件的 <see langword="remove"/> 方法的引用
        /// </summary>
        public MethodRef Remove
        {
            [Reference(ReferenceType.EventRemove)]
            get => IL.Throw<MethodRef>();
        }


        private EventRef() { }
    }
}
