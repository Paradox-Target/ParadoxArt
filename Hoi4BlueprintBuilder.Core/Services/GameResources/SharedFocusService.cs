using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources;

[RegisterSingleton<SharedFocusService>]
public sealed class SharedFocusService : CommonResourcesService<SharedFocusService, Dictionary<string, Node>>
{
    public SharedFocusService(IServiceProvider serviceProvider)
        : base(
            Path.Combine(Keywords.Common, "national_focus"),
            WatcherFilter.Text,
            serviceProvider,
            PathType.Folder,
            SearchOption.AllDirectories,
            true
        ) { }

    public IDictionary<string, Dictionary<string, Node>> AllSharedFocuses => Resources;

    protected override Dictionary<string, Node> ParseFileToContent(Node rootNode)
    {
        var sharedFocuses = new Dictionary<string, Node>();
        foreach (var child in rootNode.AllArray)
        {
            if (child.TryGetNode(out var node) && node.Key.EqualsIgnoreCase(Keywords.SharedFocus))
            {
                string? id = node
                    .Leaves.AsValueEnumerable()
                    .FirstOrDefault(leaf => leaf.Key.EqualsIgnoreCase("id"))
                    ?.ValueText;

                if (id is not null)
                {
                    sharedFocuses[id] = node;
                }
            }
        }

        return sharedFocuses;
    }
}
