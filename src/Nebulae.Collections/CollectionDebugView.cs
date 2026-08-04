using System.Diagnostics;

namespace Nebulae.Collections
{
    internal interface ICollectionDebugView<T>
    {
        T[] ToArray();
    }


    internal class CollectionDebugView<T>(ICollectionDebugView<T> collection)
    {
        public readonly ICollectionDebugView<T> Collection = collection;


        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => Collection.ToArray();
    }
}
