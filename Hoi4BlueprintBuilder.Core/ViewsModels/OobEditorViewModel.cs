using System.Diagnostics;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Localization.Strings;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<OobEditorViewModel>]
public sealed partial class OobEditorViewModel : ObservableObject
{
    public IEnumerable<EquipmentsVo> Equipments =>
        _equipments.Select(x => new EquipmentsVo(_localizationService.GetFormatText(x.Key), x.Value));
    public static SupplyPriorities[]? SupplyPriorities { get; private set; }
    public int DivisionBrigadeWidth { get; }

    public int DivisionBrigadeHeight { get; }

    public int DivisionSupportWidth { get; }

    public int DivisionSupportHeight { get; }
    public int RegimentalSupportWidth { get; }
    public int RegimentalSupportHeight { get; }

    public string CanParachutedCountText => CanNotParachutedCount == 0 ? "√允许伞降" : "×不允许伞降";
    public string TotalManpowerText => $"{DesignerManpower}{TotalManpower}";
    private string DesignerManpower { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalManpowerText))]
    private partial int TotalManpower { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanParachutedCountText))]
    private partial short CanNotParachutedCount { get; set; }

    [ObservableProperty]
    public partial string TemplateName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLocked { get; set; }

    [ObservableProperty]
    public partial int SelectedSupplyPriorityIndex { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalWidthText))]
    private partial int TotalWidth { get; set; }
    public string TotalWidthText => $"{DesignerCombatWidth}{TotalWidth}";

    [ObservableProperty]
    public partial string DivisionNamesGroup { get; set; } = string.Empty;

    public TextBlock DivisionalSupportDesc { get; }

    /// <summary>
    /// 每列最少几个才能使用团级支援
    /// </summary>
    public int MinUseRegimentalCount { get; }

    public string RegimentalSupportHeader { get; }
    public string DivisionalSupportHeader { get; }
    public string DesignerCombatWidth { get; }
    public TextBlock TotalWidthDesc { get; }
    public string TotalManpowerDesc { get; }
    public TextBlock RegimentalSupportDesc { get; }
    public IEnumerable<Inline> DesignerBlockedByRegimentBattalions { get; }

    private readonly UnitService _unitService;
    private readonly ImageService _imageService;
    private readonly LocalizationFormatService _localizationService;
    private readonly ClipboardService _clipboardService;
    private readonly NotificationService _notificationService;
    private Action<string>? _setText;
    private string? _generatedText;

    private readonly Dictionary<PositionInfo, UnitInfo> _existingUnits = [];
    private readonly Dictionary<string, int> _equipments = [];
    private readonly UnitInfo EmptyUnit =
        new(string.Empty, string.Empty, false, false, 0, false, 0, false, new HashSet<string>(), []);

    private const string Support = "support";
    private const string UnitPropertyChange = "UnitPropertyChange";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public OobEditorViewModel(
        DefinesService definesService,
        UnitService unitService,
        ImageService imageService,
        LocalizationFormatService localizationService,
        ClipboardService clipboardService,
        NotificationService notificationService
    )
    {
        _unitService = unitService;
        _imageService = imageService;
        _localizationService = localizationService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        SupplyPriorities ??=
        [
            new SupplyPriorities(
                _localizationService.GetFormatText("TEMPLATE_PRIO_0"),
                _localizationService.GetFormatText("TEMPLATE_PRIO_0_DESC"),
                0
            ),
            new SupplyPriorities(
                _localizationService.GetFormatText("TEMPLATE_PRIO_1"),
                _localizationService.GetFormatText("TEMPLATE_PRIO_1_DESC"),
                1
            ),
            new SupplyPriorities(
                _localizationService.GetFormatText("TEMPLATE_PRIO_2"),
                _localizationService.GetFormatText("TEMPLATE_PRIO_2_DESC"),
                2
            )
        ];

        DivisionBrigadeWidth = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_BRIGADE_WIDTH");
        DivisionBrigadeHeight = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_BRIGADE_HEIGHT");
        DivisionSupportWidth = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_SUPPORT_WIDTH");
        DivisionSupportHeight = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_SUPPORT_HEIGHT");
        RegimentalSupportWidth = definesService.GetInt("NDefines.NMilitary.MAX_REGIMENTAL_SUPPORT_WIDTH");
        RegimentalSupportHeight = definesService.GetInt("NDefines.NMilitary.MAX_REGIMENTAL_SUPPORT_HEIGHT");
        long[] array = definesService.GetLongs("NDefines.NMilitary.REGIMENTAL_SUPPORT_REQUIRED_BATTALIONS");
        MinUseRegimentalCount = (int)(array.Length > 0 ? array[0] : 0);
        PropertyChanged += (_, e) =>
        {
            if (
                e.PropertyName
                is nameof(IsLocked)
                    or nameof(TemplateName)
                    or nameof(SelectedSupplyPriorityIndex)
                    or UnitPropertyChange
                    or nameof(DivisionNamesGroup)
            )
            {
                GenerateText();
            }
        };

        DivisionalSupportHeader = _localizationService.GetFormatText("SUPPORT_HEADER");
        RegimentalSupportHeader = _localizationService.GetFormatText("REGIMENTAL_SUPPORT_HEADER");
        DesignerBlockedByRegimentBattalions = _localizationService.GetFormatTextWithColor(
            "DESIGNER_BLOCKED_BY_REGIMENT_BATTALIONS",
            "NUM_BATTALIONS",
            MinUseRegimentalCount
        );
        DesignerCombatWidth = _localizationService.GetFormatText("DESIGNER_COMBATWIDTH");
        TotalWidthDesc = _localizationService
            .GetFormatTextWithColor("DESIGNER_COMBATWIDTH_DESC")
            .ToTextBlock();
        TotalManpowerDesc = _localizationService.GetFormatText("DESIGNER_MANPOWER_DESC");
        DesignerManpower = _localizationService.GetFormatText("DESIGNER_MANPOWER");
        RegimentalSupportDesc = _localizationService
            .GetFormatTextWithColor("DESIGNER_REGIMENTAL_SUPPORT_COLUMN_TITLE")
            .ToTextBlock();
        DivisionalSupportDesc = _localizationService
            .GetFormatTextWithColor("DESIGNER_SUPPORT_COLUMN_TITLE")
            .ToTextBlock();
    }

    public void SetTextAction(Action<string> setText)
    {
        _setText = setText;
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        if (string.IsNullOrEmpty(_generatedText))
        {
            return;
        }

        await _clipboardService.SetTextAsync(_generatedText);
        _notificationService.Show(
            LangResources.CopiedToClipboard,
            LangResources.Success,
            NotificationType.Success
        );
    }

    [RelayCommand]
    private async Task PickUnit(Button button)
    {
        var slotType = button.Tag as UnitSlotType? ?? UnitSlotType.Common;
        int x = Grid.GetColumn(button);
        int y = Grid.GetRow(button);

        var list = new List<UnitGroupVo>();
        var position = new PositionInfo(new Point(x, y), slotType);
        var groups = _unitService.AllUnits.GroupBy(item => item.Group);
        if (slotType is UnitSlotType.DivisionalSupport or UnitSlotType.RegimentalSupport)
        {
            groups = groups.Where(g => g.Key == Support);
        }
        else if (TryGetUnitByX(x, slotType, out string currentGroup))
        {
            groups = groups.Where(g => g.Key == currentGroup);
        }
        else if (slotType == UnitSlotType.Common)
        {
            groups = groups.Where(g => g.Key != Support);
        }

        var existingUnit = _existingUnits.GetValueOrDefault(position);

        foreach (var group in groups)
        {
            string imageKey = $"GFX_group_{group.Key}_icon";
            var image = _imageService.GetIconByName(imageKey);
            string name = _localizationService.GetFormatText($"group_{group.Key}_title");
            var subUnits = GetSubUnits(group, position);

            if (existingUnit is not null)
            {
                subUnits.Insert(
                    0,
                    new UnitInfoVo(
                        _localizationService.GetFormatText("DESIGNER_REMOVE"),
                        _imageService.GetIconByName("GFX_remove_icon"),
                        EmptyUnit,
                        true
                    )
                );
            }

            list.Add(new UnitGroupVo(name, image, subUnits));
            if (image is null && group.Key != Support)
            {
                Log.Warn("找不到 Icon '{Name}'", imageKey);
            }
        }

        var viewModel = new UnitPickerViewModel(list);
        var dialog = new FAContentDialog
        {
            Title = "单位选择器",
            Content = new UnitPickerView(viewModel),
            CloseButtonText = LangResources.Common_Cancel
        };
        viewModel.Close += dialog.Hide;
        await dialog.ShowAsync();
        viewModel.Close -= dialog.Hide;
        var unit = viewModel.SelectedUnit;

        if (unit is not null)
        {
            if (existingUnit is not null)
            {
                CleanupUnitCountInfo(existingUnit);
            }
            if (unit.IsRemoveOperation)
            {
                _existingUnits.Remove(position);
                button.Content = null;
            }
            else
            {
                var unitInfo = unit.UnitInfo;
                button.Content = new Image { Source = unit.Image, Stretch = Stretch.Uniform };
                ToolTip.SetTip(button, $"{unit.Name}\n\n{_localizationService.GetFormatText($"{unitInfo.Name}_desc")}");

                if (!unitInfo.CanBeParachuted)
                {
                    ++CanNotParachutedCount;
                }
                TotalManpower += unitInfo.Manpower;
                TotalWidth += unitInfo.Width;
                _existingUnits[position] = unitInfo;
                foreach ((string name, int quantity) in unitInfo.Equipments)
                {
                    _equipments[name] = _equipments.GetValueOrDefault(name) + quantity;
                }
            }
            // ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged(UnitPropertyChange);
            OnPropertyChanged(nameof(Equipments));
        }

        Cleanup(list, unit);
    }

    private void CleanupUnitCountInfo(UnitInfo unitInfo)
    {
        TotalWidth -= unitInfo.Width;
        TotalManpower -= unitInfo.Manpower;
        if (!unitInfo.CanBeParachuted)
        {
            --CanNotParachutedCount;
        }

        foreach ((string name, int quantity) in unitInfo.Equipments)
        {
            int current = _equipments.GetValueOrDefault(name) - quantity;
            if (current == 0)
            {
                _equipments.Remove(name);
            }
            else
            {
                _equipments[name] = current;
            }
        }
    }

    private bool TryGetUnitByX(int x, UnitSlotType slotType, out string group)
    {
        var key = _existingUnits
            .Keys.AsValueEnumerable()
            .FirstOrDefault(info => info.Point.X == x && info.SlotType == slotType);
        if (key is not null)
        {
            group = _existingUnits[key].Group;
            return true;
        }

        group = string.Empty;
        return false;
    }

    private static void Cleanup(List<UnitGroupVo> list, UnitInfoVo? selectedUnit)
    {
        list.ForEach(info =>
        {
            info.Image?.Dispose();
            foreach (var unitInfo in info.Units)
            {
                if (selectedUnit is null || selectedUnit.Name != unitInfo.Name)
                {
                    unitInfo.Image?.Dispose();
                }
            }
        });
    }

    private List<UnitInfoVo> GetSubUnits(IEnumerable<UnitInfo> units, PositionInfo position)
    {
        var list = new List<UnitInfoVo>();
        var slotType = position.SlotType;

        if (slotType == UnitSlotType.DivisionalSupport)
        {
            units = units.Where(item => item is { IsDivisional: true, IsRegimental: false });
        }
        else if (slotType == UnitSlotType.RegimentalSupport)
        {
            int count = _existingUnits.Keys.Count(info =>
                info.Point.X == position.Point.X && info.SlotType == UnitSlotType.Common
            );
            if (count < MinUseRegimentalCount)
            {
                units = [];
            }
            else
            {
                var info = _existingUnits.Keys.FirstOrDefault(x => x.Point.X == position.Point.X);
                string currentGroup = info is null ? string.Empty : _existingUnits[info].Group;
                units = units.Where(item =>
                    item is { IsRegimental: true, IsDivisional: false }
                    && item.IsAllowedBattalionGroup(currentGroup)
                );
            }
        }
        foreach (var unitInfo in units.Where(static unit => unit.AllowInNonArmyHq))
        {
            var image = _imageService.GetIconByName(unitInfo.IconKey);
            list.Add(new UnitInfoVo(_localizationService.GetFormatText(unitInfo.Name), image, unitInfo));
            if (image is null)
            {
                Log.Warn("'{Name}' 找不到图片", unitInfo.IconKey);
            }
        }
        return list;
    }

    private void GenerateText()
    {
        var regiments = new Node("regiments");
        var regimentsList = new List<Child>(8);
        var support = new Node("support");
        var supportList = new List<Child>(4);
        var regimentalSupport = new Node("regimental_support");
        var regimentalSupportList = new List<Child>();

        foreach (var unitInfo in _existingUnits)
        {
            if (unitInfo.Key.SlotType == UnitSlotType.Common)
            {
                var unitNode = new Node(unitInfo.Value.Name)
                {
                    AllArray =
                    [
                        ChildHelper.Leaf("x", unitInfo.Key.Point.X),
                        ChildHelper.Leaf("y", unitInfo.Key.Point.Y)
                    ]
                };
                regimentsList.Add(unitNode);
            }
            else if (unitInfo.Key.SlotType == UnitSlotType.DivisionalSupport)
            {
                var supportNode = new Node(unitInfo.Value.Name)
                {
                    AllArray =
                    [
                        ChildHelper.Leaf("x", unitInfo.Key.Point.X),
                        ChildHelper.Leaf("y", unitInfo.Key.Point.Y)
                    ]
                };
                supportList.Add(supportNode);
            }
            else if (unitInfo.Key.SlotType == UnitSlotType.RegimentalSupport)
            {
                var regimentNode = new Node(unitInfo.Value.Name)
                {
                    AllArray =
                    [
                        // 不知道为什么, 团级支援以 1 开始
                        ChildHelper.Leaf("x", unitInfo.Key.Point.X + 1),
                        ChildHelper.Leaf("y", unitInfo.Key.Point.Y)
                    ]
                };
                regimentalSupportList.Add(regimentNode);
            }
        }
        regiments.AllArray = [.. regimentsList];
        support.AllArray = [.. supportList];
        regimentalSupport.AllArray = [.. regimentalSupportList];

        var template = new Node("division_template");
        var list = new List<Child>(8)
        {
            ChildHelper.LeafQString("name", TemplateName),
            ChildHelper.Leaf("priority", SupplyPriorities?[SelectedSupplyPriorityIndex].Priority ?? 1)
        };
        if (IsLocked)
        {
            list.Add(ChildHelper.Leaf("is_locked", IsLocked));
        }

        if (!string.IsNullOrWhiteSpace(DivisionNamesGroup))
        {
            list.Add(ChildHelper.LeafString("division_names_group", DivisionNamesGroup));
        }

        if (regiments.AllArray.Length > 0)
        {
            list.Add(regiments);
        }
        if (support.AllArray.Length > 0)
        {
            list.Add(support);
        }
        if (regimentalSupport.AllArray.Length > 0)
        {
            list.Add(regimentalSupport);
        }
        template.AllArray = [.. list];
        var rootNode = new Node("");
        rootNode.AddChild(template);
        _generatedText = rootNode.ToScript();
        _setText?.Invoke(_generatedText);

        Debug.Assert(SupplyPriorities is not null, "SupplyPriorities is null");
    }

    private sealed record PositionInfo(Point Point, UnitSlotType SlotType);
}

public sealed record UnitGroupVo(string GroupName, Bitmap? Image, IReadOnlyCollection<UnitInfoVo> Units);

public sealed record UnitInfoVo(
    string Name,
    Bitmap? Image,
    UnitInfo UnitInfo,
    bool IsRemoveOperation = false
);

public sealed record EquipmentsVo(string Name, int Quantity);

public enum UnitSlotType : byte
{
    Common,

    /// <summary>
    /// 支援
    /// </summary>
    DivisionalSupport,

    /// <summary>
    /// 团级支援
    /// </summary>
    RegimentalSupport
}
