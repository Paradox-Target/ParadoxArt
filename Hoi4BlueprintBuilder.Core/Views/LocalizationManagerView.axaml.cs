using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Models.Localization;
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
            var column = new DataGridTemplateColumn
            {
                Header = lang.ToString(),
                Width = new DataGridLength(150, DataGridLengthUnitType.Pixel),
                IsReadOnly = false,
                // 单元格非编辑状态时的模板
                CellTemplate = new FuncDataTemplate<object>(
                    (_, _) =>
                    {
                        var textBlock = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(12, 0)
                        };

                        // 绑定显示的文本
                        textBlock.Bind(TextBlock.TextProperty, new Binding($"[{lang}]"));
                        // 绑定 ToolTip，显示 FilePath
                        textBlock.Bind(
                            Avalonia.Controls.ToolTip.TipProperty,
                            new Binding(".")
                            {
                                Converter = new FuncValueConverter<LocalizationRow, string?>(row =>
                                    row?.Languages.Find(l => l.Language == lang)?.FilePath
                                )
                            }
                        );

                        return textBlock;
                    }
                ),
                // 单元格编辑状态时的模板
                CellEditingTemplate = new FuncDataTemplate<object>(
                    (_, _) =>
                    {
                        var textBox = new TextBox();
                        textBox.Bind(
                            TextBox.TextProperty,
                            new Binding($"[{lang}]") { Mode = BindingMode.TwoWay }
                        );
                        return textBox;
                    }
                )
            };
            MainDataGrid.Columns.Add(column);
        }
    }
}
