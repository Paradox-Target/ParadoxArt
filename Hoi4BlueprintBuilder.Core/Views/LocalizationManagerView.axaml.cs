using Avalonia.Controls;
using Avalonia.Data;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<LocalizationManagerView>]
public sealed partial class LocalizationManagerView : UserControl, ITabViewItem
{
    public string Header => "本地化编辑器";
    public string ToolTip => "Manage all localizations in the current MOD";
    public string FilePath => "internal://localization_manager";
    public IconSource? TabIcon { get; } = new SymbolIconSource { Symbol = Symbol.Character };

    private readonly LocalizationManagerViewModel _viewModel;

    /// <summary>
    /// 设计器使用
    /// </summary>
    public LocalizationManagerView()
    {
        _viewModel = null!;
        InitializeComponent();
    }

    public LocalizationManagerView(LocalizationManagerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        BuildLanguageColumns();
    }

    private void BuildLanguageColumns()
    {
        foreach (var lang in _viewModel.SupportedLanguages)
        {
            var column = new DataGridTextColumn
            {
                Header = lang.ToString(),
                Binding = new Binding($"[{lang}]") { Mode = BindingMode.TwoWay },
                Width = new DataGridLength(150, DataGridLengthUnitType.Pixel),
                IsReadOnly = false
            };
            MainDataGrid.Columns.Add(column);
        }
    }
}
