using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Hoi4BlueprintBuilder.Localization.Strings;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<OobEditorView>]
public sealed partial class OobEditorView : UserControl, ITabViewItem
{
    private const int UnitImageWidth = 85;
    private const int UnitImageHeight = 45;

    public OobEditorView()
    {
        InitializeComponent();
    }

    public OobEditorView(OobEditorViewModel vm, TelemetryService telemetryService)
    {
        InitializeComponent();
        DataContext = vm;
        TextEditor.SetGrammar(".txt");
        vm.SetTextAction(s => TextEditor.Text = s);
        DivisionGrid.RowDefinitions = new RowDefinitions(
            string.Join(',', Enumerable.Repeat('*', vm.DivisionBrigadeHeight))
        );
        DivisionGrid.ColumnDefinitions = new ColumnDefinitions(
            string.Join(',', Enumerable.Repeat('*', vm.DivisionBrigadeWidth))
        );
        DivisionalSupportPanel.RowDefinitions = new RowDefinitions(
            string.Join(',', Enumerable.Repeat('*', vm.DivisionSupportHeight))
        );
        DivisionalSupportPanel.ColumnDefinitions = new ColumnDefinitions(
            string.Join(',', Enumerable.Repeat('*', vm.DivisionSupportWidth))
        );
        RegimentalSupportPanel.RowDefinitions = new RowDefinitions(
            string.Join(',', Enumerable.Repeat('*', vm.RegimentalSupportHeight))
        );
        RegimentalSupportPanel.ColumnDefinitions = new ColumnDefinitions(
            string.Join(',', Enumerable.Repeat('*', vm.RegimentalSupportWidth))
        );

        for (int column = 0; column < vm.DivisionBrigadeWidth; column++)
        {
            for (int row = 0; row < vm.DivisionBrigadeHeight; row++)
            {
                var item = new Button
                {
                    Command = vm.PickUnitCommand,
                    Width = UnitImageWidth,
                    Height = UnitImageHeight
                };

                item.CommandParameter = item;
                Grid.SetRow(item, column);
                Grid.SetColumn(item, row);
                DivisionGrid.Children.Add(item);
            }
        }

        for (int column = 0; column < vm.DivisionSupportWidth; column++)
        {
            for (int row = 0; row < vm.DivisionSupportHeight; row++)
            {
                var button = new Button
                {
                    Width = UnitImageWidth,
                    Height = UnitImageHeight,
                    Command = vm.PickUnitCommand,
                    Tag = UnitSlotType.DivisionalSupport
                };
                button.CommandParameter = button;
                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);
                DivisionalSupportPanel.Children.Add(button);
            }
        }

        for (int column = 0; column < vm.RegimentalSupportWidth; column++)
        {
            for (int row = 0; row < vm.RegimentalSupportHeight; row++)
            {
                var button = new Button
                {
                    Width = UnitImageWidth,
                    Height = UnitImageHeight,
                    Command = vm.PickUnitCommand,
                    Tag = UnitSlotType.RegimentalSupport
                };
                button.CommandParameter = button;
                // 不能共用同一个 TextBlock, 会崩溃.
                Avalonia.Controls.ToolTip.SetTip(
                    button,
                    vm.DesignerBlockedByRegimentBattalions.ToTextBlock()
                );
                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);
                RegimentalSupportPanel.Children.Add(button);
            }
        }

        telemetryService.TrackEvent("Open_Template_Editor_View");
    }

    public string Header => LangResources.OobEditorView_Title;
    public string FilePath => "internal://template_editor";
    public string ToolTip => Header;
    public FAIconSource TabIcon { get; } =
        new FAPathIconSource { Data = (Geometry)App.Current.Resources["DivisionDesignerIconGeometry"]! };

    public OobEditorViewModel ViewModel => (OobEditorViewModel)DataContext!;

    /// <summary>
    /// 载入现有的 <c>division_template</c> 节点, 并把单位渲染到编辑器网格中
    /// </summary>
    /// <param name="templateNode">文件中的部队模板节点</param>
    public void LoadTemplate(Node templateNode)
    {
        ViewModel.LoadTemplate(templateNode);
        RenderPlacedUnits();
    }

    private void RenderPlacedUnits()
    {
        var placedUnits = ViewModel.GetPlacedUnits();
        RenderGrid(DivisionGrid, UnitSlotType.Common, placedUnits);
        RenderGrid(DivisionalSupportPanel, UnitSlotType.DivisionalSupport, placedUnits);
        RenderGrid(RegimentalSupportPanel, UnitSlotType.RegimentalSupport, placedUnits);
    }

    private void RenderGrid(Grid grid, UnitSlotType slotType, IReadOnlyList<PlacedUnitVo> placedUnits)
    {
        foreach (var child in grid.Children.OfType<Button>())
        {
            int x = Grid.GetColumn(child);
            int y = Grid.GetRow(child);
            var unit = placedUnits
                .AsValueEnumerable()
                .FirstOrDefault(item => item.SlotType == slotType && item.X == x && item.Y == y);

            child.Content = unit?.Image;
            if (unit?.ToolTip is not null)
            {
                Avalonia.Controls.ToolTip.SetTip(child, unit.ToolTip);
            }
            else if (slotType == UnitSlotType.RegimentalSupport)
            {
                Avalonia.Controls.ToolTip.SetTip(
                    child,
                    ViewModel.DesignerBlockedByRegimentBattalions.ToTextBlock()
                );
            }
            else
            {
                Avalonia.Controls.ToolTip.SetTip(child, null);
            }
        }
    }
}
