using Hoi4BlueprintBuilder.Core.Extensions;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Base;

public abstract class CommonResourcesService<TType, TContent> : ResourcesService<TType, TContent, Node>
    where TType : CommonResourcesService<TType, TContent>
{
    /// <inheritdoc />
    protected CommonResourcesService(
        string folderOrFileRelativePath,
        WatcherFilter filter,
        IServiceProvider serviceProvider,
        PathType pathType = PathType.Folder,
        SearchOption searchOption = SearchOption.TopDirectoryOnly,
        bool isAsyncLoading = false
    )
        : base(folderOrFileRelativePath, filter, serviceProvider, pathType, searchOption, isAsyncLoading) { }

    ///<inheritdoc />
    protected abstract override TContent? ParseFileToContent(Node rootNode);

    protected override Node? GetParseResult(string filePath)
    {
        if (!TextParser.TryParse(filePath, out var rootNode, out var error))
        {
            Log.LogParseError(error);
            return null;
        }
        return rootNode;
    }

    protected override async Task<Node?> GetParseResultAsync(string filePath)
    {
        if (
            !TextParser.TryParse(
                filePath,
                await File.ReadAllTextAsync(filePath, App.Utf8EncodingWithoutBom).ConfigureAwait(false),
                out var rootNode,
                out var error
            )
        )
        {
            Log.LogParseError(error);
            return null;
        }
        return rootNode;
    }
}
