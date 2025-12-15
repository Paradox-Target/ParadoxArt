namespace Hoi4BlueprintBuilder.Core.Extensions;

public static class CollectionExtensions
{
    extension<T>(IReadOnlyCollection<T> collection)
    {
        public bool IsEmpty => collection.Count == 0;
        public bool IsNotEmpty => collection.Count != 0;
    }
}