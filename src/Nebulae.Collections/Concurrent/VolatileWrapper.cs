namespace Nebulae.Collections.Concurrent
{
    internal struct VolatileWrapper<T> where T : class
    {
        public volatile T? Value;
    }
}
