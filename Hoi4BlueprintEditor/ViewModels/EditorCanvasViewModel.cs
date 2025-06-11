using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models;

namespace Hoi4BlueprintEditor.ViewModels;

public partial class EditorCanvasViewModel : ObservableObject
{
    public ObservableCollection<FocusNodeViewModel> Nodes { get; } = new();

    public EditorCanvasViewModel()
    {
        // 假数据测试
        LoadTestData();
    }

    private void LoadTestData()
    {
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNodeModel
                {
                    Id = "GER_Test1",
                    X = 0,
                    Y = 0,
                    Icon = "GFX_GER_Test1",
                }
            )
        );
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNodeModel
                {
                    Id = "GER_Test2",
                    X = 1,
                    Y = 0,
                    Icon = "GFX_GER_Test2",
                }
            )
        );
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNodeModel
                {
                    Id = "GER_Test3",
                    X = 2,
                    Y = 1,
                    Icon = "GFX_GER_Test3",
                }
            )
        );
        Nodes.Add(
            new FocusNodeViewModel(
                new FocusNodeModel
                {
                    Id = "GER_Test4",
                    X = 3,
                    Y = 1,
                    Icon = "GFX_GER_Test4",
                }
            )
        );
    }
}
