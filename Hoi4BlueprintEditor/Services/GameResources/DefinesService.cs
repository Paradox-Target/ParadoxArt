using System.Text;
using Hoi4BlueprintEditor.Services.GameResources.Base;
using Microsoft.Extensions.DependencyInjection;
using NLua;

namespace Hoi4BlueprintEditor.Services.GameResources;

// 使用 byte 是因为不需要分开存储各个文件中的变量, 而是集中在一个静态只读字段中, 使用 byte 只是为了最小化浪费内存
// BUG: 当部分定义在 Mod 中被修改后, 如果之后删除了 Mod, 那么定义将无法被恢复到游戏定义值
// 解决方案: 重新读取游戏文件?
[RegisterSingleton<DefinesService>]
public sealed class DefinesService : ResourcesService<DefinesService, byte, byte>, IDisposable
{
    private static readonly Lua GlobalLua = new() { State = { Encoding = Encoding.UTF8 } };

    public DefinesService(IServiceProvider serviceProvider)
        : base(Path.Combine(Keywords.Common, "defines"), WatcherFilter.Lua, serviceProvider, PathType.Folder)
    { }

    protected override void SortFilePath(string[] filePathArray)
    {
        var pathService = App.Current.Services.GetRequiredService<GameResourcesPathService>();

        Array.Sort(
            filePathArray,
            (x, y) =>
            {
                int xPriority = GetFilePathPriority(x, pathService);
                int yPriority = GetFilePathPriority(y, pathService);

                return xPriority.CompareTo(yPriority);
            }
        );
    }

    private static int GetFilePathPriority(string filePath, GameResourcesPathService pathService)
    {
        if (Path.GetFileName(filePath).Equals("00_defines.lua", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var fileType = pathService.GetFileOrigin(filePath);
        if (fileType == FileOrigin.Game)
        {
            return 1;
        }

        return 2;
    }

    public long GetLong(string defineName)
    {
        return GlobalLua.GetLong(defineName);
    }

    protected override byte ParseFileToContent(byte result)
    {
        return result;
    }

    protected override byte GetParseResult(string filePath)
    {
        GlobalLua.DoFile(filePath);

        return 0;
    }

    public void Dispose()
    {
        GlobalLua.Dispose();
    }
}
