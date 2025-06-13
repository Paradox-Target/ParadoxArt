using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Core.Helpers;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models.Focus;
using NLog;
using ObservableCollections;
using ParadoxPower.CSharpExtensions;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class EditorCanvasViewModel : ObservableObject
{
    public  NotifyCollectionChangedSynchronizedViewList<FocusNodeViewModel> Nodes => _nodes.ToNotifyCollectionChanged();
    private readonly ObservableList<FocusNodeViewModel> _nodes = [];
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasViewModel()
    {
        // 假数据测试
        LoadTestData();

        WeakReferenceMessenger.Default.Register<OpenFileMessage>(
            this,
            (_, message) =>
            {
                if (!TextParser.TryParse(message.FilePath, out var rootNode, out var _))
                {
                    return;
                }

                _nodes.Clear();
                var focusNodes = FocusNodeHelper.GetAllNodesFromAst(rootNode);
                _nodes.AddRange(focusNodes.Select(focusNode => new FocusNodeViewModel(focusNode)));
                Log.Info("已加载国策树文件: {FilePath}", message.FilePath);
                Log.Info("共添加: {Amount}", _nodes.Count);
            }
        );
    }

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _translateX = 0;

    [ObservableProperty]
    private double _translateY = 0;

    private void LoadTestData()
    {
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test1",
                    RawPosition = new Point(0, 0),
                    Icon = "GFX_GER_Test1",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test2",
                    RawPosition = new Point(1, 0),
                    Icon = "GFX_GER_Test2",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test3",
                    RawPosition = new Point(2, 1),
                    Icon = "GFX_GER_Test3",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test4",
                    RawPosition = new Point(3, 1),
                    Icon = "GFX_GER_Test4",
                }
            )
        );
    }
}
