using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<DivisionTemplatePickerView>]
public sealed partial class DivisionTemplatePickerView : UserControl, ITabViewItem, ISave
{
    private readonly DivisionTemplatePickerViewModel _viewModel;

    /// <summary>
    /// 设计器使用
    /// </summary>
    public DivisionTemplatePickerView()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public DivisionTemplatePickerView(DivisionTemplatePickerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        Header = Path.GetFileName(viewModel.FilePath);
    }

    public string Header { get; } = string.Empty;

    public string FilePath => _viewModel.FilePath;

    public string ToolTip => _viewModel.FilePath;

    public FAIconSource TabIcon { get; } =
        new FAPathIconSource { Data = (Geometry)App.Current.Resources["DivisionDesignerIconGeometry"]! };

    public void Save()
    {
        _viewModel.Save();
    }
}
