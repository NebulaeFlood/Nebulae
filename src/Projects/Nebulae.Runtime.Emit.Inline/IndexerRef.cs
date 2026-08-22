namespace Nebulae.Runtime.Emit.Inline
{
#if NET10_0_OR_GREATER
#pragma warning disable CA1822
#endif

    /// <summary>
    /// 索引器引用
    /// </summary>
    [Reference(ReferenceType.Indexer)]
    public sealed class IndexerRef
    {
        /// <summary>
        /// 获取索引器 <see langword="get"/> 方法的引用
        /// </summary>
        public MethodRef Get
        {
            [Reference(ReferenceType.IndexerGet)]
            get => IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取索引器 <see langword="set"/> 方法的引用
        /// </summary>
        public MethodRef Set
        {
            [Reference(ReferenceType.IndexerSet)]
            get => IL.Throw<MethodRef>();
        }


        private IndexerRef() { }
    }
}
