using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Models.Focus;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class EditorCanvasViewModel : ObservableObject
{
    public ObservableCollection<FocusNodeViewModel> Nodes { get; } = new();
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

                var focusTreeNode = rootNode.Nodes.FirstOrDefault(node =>
                    node.Key.EqualsIgnoreCase("focus_tree")
                );
                Nodes.Clear();
                foreach (
                    var node in focusTreeNode?.Nodes.Where(node => node.Key.EqualsIgnoreCase("focus")) ?? []
                )
                {
                    Nodes.Add(CreateFocusNodeFromAstNode(node));
                }
                Log.Info("已加载国策树文件: {FilePath}", message.FilePath);
                Log.Info("共添加: {Amount}", Nodes.Count);
            }
        );
    }

    private static FocusNodeViewModel CreateFocusNodeFromAstNode(Node focusNode)
    {
        var model = new FocusNode();
        var point = new Point();

        foreach (var child in focusNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.EqualsIgnoreCase("x"))
                {
                    point.X = leaf.Value.TryGetInt(out int x) ? x : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase("y"))
                {
                    point.Y = leaf.Value.TryGetInt(out int y) ? y : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase("id"))
                {
                    model.Id = leaf.ValueText;
                }
                else if (leaf.Key.EqualsIgnoreCase("icon"))
                {
                    model.Icon = leaf.ValueText;
                }
                else if (leaf.Key.EqualsIgnoreCase("cost"))
                {
                    model.Cost = leaf.Value.TryGetInt(out int cost) ? cost : 0;
                }
            }
        }

        model.Position = point;
        return new FocusNodeViewModel(model);
    }

    private void LoadTestData()
    {
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test1",
                    Position = new Point(0, 0),
                    Icon = "GFX_GER_Test1",
                }
            )
        );
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test2",
                    Position = new Point(1, 0),
                    Icon = "GFX_GER_Test2",
                }
            )
        );
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test3",
                    Position = new Point(2, 1),
                    Icon = "GFX_GER_Test3",
                }
            )
        );
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test4",
                    Position = new Point(3, 1),
                    Icon = "GFX_GER_Test4",
                }
            )
        );
    }
}
