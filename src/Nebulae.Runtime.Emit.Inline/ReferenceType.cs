namespace Nebulae.Runtime.Emit.Inline
{
    internal enum ReferenceType
    {
        Empty,

        Type,
        Generic,

        Constructor,

        Event,
        EventAdd,
        EventRaise,
        EventRemove,

        Field,

        Indexer,
        IndexerGet,
        IndexerSet,

        Method,
        MethodMakeGeneric,

        Property,
        PropertyGet,
        PropertySet,


        Placeholder
    }
}
