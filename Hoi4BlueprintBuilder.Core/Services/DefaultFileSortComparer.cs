using NaturalSort.Extension;

namespace Hoi4BlueprintBuilder.Core.Services;

public sealed class DefaultFileSortComparer : IFileSortComparer
{
    private readonly NaturalSortComparer _comparer = new(StringComparison.OrdinalIgnoreCase);
    public int Compare(string? x, string? y)
    {
        return _comparer.Compare(x, y);
    }
}