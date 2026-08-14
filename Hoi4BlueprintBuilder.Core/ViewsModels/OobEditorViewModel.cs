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
using Hoi4BlueprintBuilder.Core.Services.GameResources.Modifiers;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Localization.Strings;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<OobEditorViewModel>]
public sealed partial class OobEditorViewModel : ObservableObject, IClosed
{
    public IEnumerable<EquipmentsVo> Equipments =>
        _equipments.Select(x => new EquipmentsVo(
            _localizationFormatService.GetFormatTextInAll(x.Key),
            x.Value
        ));

    [ObservableProperty]
    public partial IReadOnlyList<TemplateAttributeVo> TemplateAttributes { get; private set; } = [];

    public static SupplyPriorities[]? SupplyPriorities { get; private set; }
    public int DivisionBrigadeWidth { get; }

    public int DivisionBrigadeHeight { get; }

    public int DivisionSupportWidth { get; }

    public int DivisionSupportHeight { get; }
    public int RegimentalSupportWidth { get; }
    public int RegimentalSupportHeight { get; }

    public string CanParachutedCountText =>
        CanNotParachutedCount == 0
            ? LangResources.OobEditorView_ParachutingAllowed
            : LangResources.OobEditorView_ParachutingNotAllowed;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTerrainModifiers))]
    public partial IReadOnlyList<TerrainModifierVo> TerrainModifiers { get; private set; } = [];

    public bool HasTerrainModifiers => TerrainModifiers.Count > 0;
    public string TerrainAttackHeader { get; }
    public string TerrainMovementHeader { get; }
    public string TerrainDefenceHeader { get; }

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
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly LocalizationService _localizationService;
    private readonly ClipboardService _clipboardService;
    private readonly NotificationService _notificationService;
    private readonly ModifierDisplayService _modifierService;
    private readonly ModifierService _modifierValueService;
    private readonly TerrainModifierCalculator _terrainModifierCalculator;
    private readonly TerrainService _terrainService;
    private Action<string>? _setText;
    private string? _generatedText;

    private readonly Dictionary<PositionInfo, UnitInfo> _existingUnits = [];

    /// <summary>
    /// Key: 装备名称, Value: 数量
    /// </summary>
    private readonly Dictionary<string, int> _equipments = [];
    private readonly Dictionary<string, Bitmap?> _terrainImages = new(StringComparer.OrdinalIgnoreCase);
    private readonly UnitInfo EmptyUnit =
        new(
            string.Empty,
            string.Empty,
            false,
            false,
            0,
            false,
            0,
            false,
            new HashSet<string>(),
            [],
            new UnitIntrinsicStats(),
            new HashSet<string>(),
            [],
            []
        );

    /// <summary>
    /// 每一点堑壕值可以增加的攻防加成
    /// </summary>
    private readonly double _digInFactor;

    /// <summary>
    /// 默认堑壕值上限
    /// </summary>
    private readonly double _unitDigInCap;

    private static TemplateAttributesLocalizations? _templateAttributesLocalization;

    private const string Support = "support";
    private const string UnitPropertyChange = "UnitPropertyChange";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public OobEditorViewModel(
        DefinesService definesService,
        UnitService unitService,
        ImageService imageService,
        LocalizationFormatService localizationFormatService,
        LocalizationService localizationService,
        ClipboardService clipboardService,
        NotificationService notificationService,
        ModifierDisplayService modifierService,
        ModifierService modifierValueService,
        TerrainModifierCalculator terrainModifierCalculator,
        TerrainService terrainService
    )
    {
        _unitService = unitService;
        _imageService = imageService;
        _localizationFormatService = localizationFormatService;
        _localizationService = localizationService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _modifierService = modifierService;
        _modifierValueService = modifierValueService;
        _terrainModifierCalculator = terrainModifierCalculator;
        _terrainService = terrainService;
        _templateAttributesLocalization ??= new TemplateAttributesLocalizations(_localizationFormatService);

        SupplyPriorities ??=
        [
            new SupplyPriorities(
                _localizationFormatService.GetFormatTextInAll("TEMPLATE_PRIO_0"),
                _localizationFormatService.GetFormatTextInAll("TEMPLATE_PRIO_0_DESC"),
                0
            ),
            new SupplyPriorities(
                _localizationFormatService.GetFormatTextInAll("TEMPLATE_PRIO_1"),
                _localizationFormatService.GetFormatTextInAll("TEMPLATE_PRIO_1_DESC"),
                1
            ),
            new SupplyPriorities(
                _localizationFormatService.GetFormatTextInAll("TEMPLATE_PRIO_2"),
                _localizationFormatService.GetFormatTextInAll("TEMPLATE_PRIO_2_DESC"),
                2
            )
        ];

        DivisionBrigadeWidth = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_BRIGADE_WIDTH");
        DivisionBrigadeHeight = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_BRIGADE_HEIGHT");
        DivisionSupportWidth = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_SUPPORT_WIDTH");
        DivisionSupportHeight = definesService.GetInt("NDefines.NMilitary.MAX_DIVISION_SUPPORT_HEIGHT");
        RegimentalSupportWidth = definesService.GetInt("NDefines.NMilitary.MAX_REGIMENTAL_SUPPORT_WIDTH");
        RegimentalSupportHeight = definesService.GetInt("NDefines.NMilitary.MAX_REGIMENTAL_SUPPORT_HEIGHT");
        _unitDigInCap = definesService.GetDouble("NDefines.NMilitary.UNIT_DIGIN_CAP");
        _digInFactor = definesService.GetDouble("NDefines.NMilitary.DIG_IN_FACTOR");
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

        DivisionalSupportHeader = _localizationFormatService.GetFormatTextInAll("SUPPORT_HEADER");
        RegimentalSupportHeader = _localizationFormatService.GetFormatTextInAll("REGIMENTAL_SUPPORT_HEADER");
        DesignerBlockedByRegimentBattalions = _localizationFormatService.GetFormatTextWithColor(
            "DESIGNER_BLOCKED_BY_REGIMENT_BATTALIONS",
            "NUM_BATTALIONS",
            MinUseRegimentalCount
        );
        DesignerCombatWidth = _localizationFormatService.GetFormatTextInAll("DESIGNER_COMBATWIDTH");
        TotalWidthDesc = _localizationFormatService
            .GetFormatTextWithColor("DESIGNER_COMBATWIDTH_DESC")
            .ToTextBlock();
        TotalManpowerDesc = _localizationFormatService.GetFormatTextInAll("DESIGNER_MANPOWER_DESC");
        DesignerManpower = _localizationFormatService.GetFormatTextInAll("DESIGNER_MANPOWER");
        RegimentalSupportDesc = _localizationFormatService
            .GetFormatTextWithColor("DESIGNER_REGIMENTAL_SUPPORT_COLUMN_TITLE")
            .ToTextBlock();
        DivisionalSupportDesc = _localizationFormatService
            .GetFormatTextWithColor("DESIGNER_SUPPORT_COLUMN_TITLE")
            .ToTextBlock();
        TerrainAttackHeader = _localizationFormatService.GetFormatTextInAll("STAT_ADJUSTER_ATTACK");
        TerrainMovementHeader = _localizationFormatService.GetFormatTextInAll("STAT_ADJUSTER_MOVEMENT");
        TerrainDefenceHeader = _localizationFormatService.GetFormatTextInAll("STAT_ADJUSTER_DEFENCE");
        RefreshTemplateAttributes();
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
            string name = _localizationFormatService.GetFormatTextInAll($"group_{group.Key}_title");
            var subUnits = GetSubUnits(group, position);

            if (existingUnit is not null)
            {
                subUnits.Insert(
                    0,
                    new UnitInfoVo(
                        _localizationFormatService.GetFormatTextInAll("DESIGNER_REMOVE"),
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
            Title = LangResources.OobEditorView_UnitPickerTitle,
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
                ToolTip.SetTip(
                    button,
                    new UnitToolTipView(
                        unit.Name,
                        _localizationFormatService.TryGetFormatTextInAll(
                            $"{unitInfo.Name}_desc",
                            out string? desc
                        )
                            ? desc
                            : string.Empty,
                        _modifierService.GetDescription(unitInfo.Modifiers).ToTextBlock()
                    )
                );

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
            RefreshTemplateAttributes();
            // ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged(UnitPropertyChange);
            OnPropertyChanged(nameof(Equipments));
            RefreshTerrainModifiers();
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

    private void RefreshTemplateAttributes()
    {
        ArgumentNullException.ThrowIfNull(_templateAttributesLocalization);

        int unitCount = _existingUnits.Count;
        if (unitCount == 0)
        {
            TemplateAttributes =
            [
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Hp,
                    _templateAttributesLocalization.HpDesc,
                    FormatValue(TemplateAttributesLocalizations.HpKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Organization,
                    _templateAttributesLocalization.OrganizationDesc,
                    FormatValue(TemplateAttributesLocalizations.OrganizationKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.RecoveryRate,
                    _templateAttributesLocalization.RecoveryRateDesc,
                    FormatValue(TemplateAttributesLocalizations.RecoveryRateKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Recon,
                    _templateAttributesLocalization.ReconDesc,
                    FormatValue(TemplateAttributesLocalizations.ReconKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Suppression,
                    _templateAttributesLocalization.SuppressionDesc,
                    FormatValue(TemplateAttributesLocalizations.SuppressionKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Weight,
                    _templateAttributesLocalization.WeightDesc,
                    FormatValue(TemplateAttributesLocalizations.WeightKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.SupplyConsumption,
                    _templateAttributesLocalization.SupplyConsumptionDesc,
                    FormatValue(TemplateAttributesLocalizations.SupplyConsumptionKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.CasualtyTrickleback,
                    _templateAttributesLocalization.CasualtyTricklebackDesc,
                    FormatValue(TemplateAttributesLocalizations.CasualtyTricklebackKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.ExperienceLoss,
                    _templateAttributesLocalization.ExperienceLossDesc,
                    FormatValue(TemplateAttributesLocalizations.ExperienceLossKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.EquipmentCaptureRatio,
                    _templateAttributesLocalization.EquipmentCaptureRatioDesc,
                    FormatValue(TemplateAttributesLocalizations.EquipmentCaptureRatioKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.TrainingTime,
                    _templateAttributesLocalization.TrainingTimeDesc,
                    FormatValue(TemplateAttributesLocalizations.TrainingTimeKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Initiative,
                    _templateAttributesLocalization.InitiativeDesc,
                    FormatValue(TemplateAttributesLocalizations.InitiativeKey, 0)
                ),
                new TemplateAttributeVo(
                    _templateAttributesLocalization.Entrenchment,
                    _templateAttributesLocalization.EntrenchmentDesc,
                    FormatValue(TemplateAttributesLocalizations.EntrenchmentKey, 0)
                )
            ];
            return;
        }

        double maxStrength = 0;
        double maxOrganisation = 0;
        double defaultMorale = 0;
        double recon = 0;
        double suppression = 0;
        double suppressionFactor = 0;
        double supplyConsumption = 0;
        double casualtyTrickleback = 0;
        double experienceLossFactor = 0;
        double equipmentCaptureFactor = 0;
        double trainingTime = 0;
        double initiative = 0;
        double entrenchment = 0;
        double weight = 0;
        double supplyConsumptionFactor = 0;

        var battalionMultipliers = new Dictionary<string, List<BattalionMultiplier>>();

        foreach (
            var multiplier in _existingUnits
                .AsValueEnumerable()
                .Where(static pair =>
                    pair.Key.SlotType != UnitSlotType.Common && pair.Value.BattalionMultipliers.IsNotEmpty
                )
                .SelectMany(static pair => pair.Value.BattalionMultipliers)
        )
        {
            if (!battalionMultipliers.TryGetValue(multiplier.Category, out var list))
            {
                list = new List<BattalionMultiplier>(4);
                battalionMultipliers[multiplier.Category] = list;
            }
            list.Add(multiplier);
        }

        foreach (var unit in _existingUnits.Values)
        {
            var stats = ApplyBattalionMultipliers(unit);
            maxStrength += stats.MaxStrength;
            maxOrganisation += stats.MaxOrganisation;
            defaultMorale += stats.DefaultMorale;
            recon += stats.Recon;
            suppression += stats.Suppression;
            suppressionFactor += stats.SuppressionFactor;
            supplyConsumption += stats.SupplyConsumption;
            supplyConsumptionFactor += stats.SupplyConsumptionFactor;
            casualtyTrickleback += stats.CasualtyTrickleback;
            experienceLossFactor += stats.ExperienceLossFactor;
            equipmentCaptureFactor += stats.EquipmentCaptureFactor;
            trainingTime = Math.Max(trainingTime, stats.TrainingTime);
            initiative += stats.Initiative;
            entrenchment += stats.Entrenchment;
            weight += stats.Weight;
        }

        supplyConsumption *= 1.0 + supplyConsumptionFactor;
        double organization = maxOrganisation / unitCount;
        double recoveryRate = defaultMorale / unitCount;
        suppression *= 1 + suppressionFactor;

        TemplateAttributes =
        [
            new TemplateAttributeVo(
                _templateAttributesLocalization.Hp,
                _templateAttributesLocalization.HpDesc,
                FormatValue(TemplateAttributesLocalizations.HpKey, maxStrength)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.Organization,
                _templateAttributesLocalization.OrganizationDesc,
                FormatValue(TemplateAttributesLocalizations.OrganizationKey, organization)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.RecoveryRate,
                _templateAttributesLocalization.RecoveryRateDesc,
                FormatValue(TemplateAttributesLocalizations.RecoveryRateKey, recoveryRate)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.Recon,
                _templateAttributesLocalization.ReconDesc,
                FormatValue(TemplateAttributesLocalizations.ReconKey, recon)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.Suppression,
                _templateAttributesLocalization.SuppressionDesc,
                FormatValue(TemplateAttributesLocalizations.SuppressionKey, suppression)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.Weight,
                _templateAttributesLocalization.WeightDesc,
                FormatValue(TemplateAttributesLocalizations.WeightKey, weight)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.SupplyConsumption,
                _templateAttributesLocalization.SupplyConsumptionDesc,
                FormatValue(TemplateAttributesLocalizations.SupplyConsumptionKey, supplyConsumption)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.CasualtyTrickleback,
                _templateAttributesLocalization.CasualtyTricklebackDesc,
                FormatValue(TemplateAttributesLocalizations.CasualtyTricklebackKey, casualtyTrickleback)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.ExperienceLoss,
                _templateAttributesLocalization.ExperienceLossDesc,
                FormatValue(TemplateAttributesLocalizations.ExperienceLossKey, experienceLossFactor)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.EquipmentCaptureRatio,
                _templateAttributesLocalization.EquipmentCaptureRatioDesc,
                FormatValue(TemplateAttributesLocalizations.EquipmentCaptureRatioKey, equipmentCaptureFactor)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.TrainingTime,
                _templateAttributesLocalization.TrainingTimeDesc,
                FormatValue(TemplateAttributesLocalizations.TrainingTimeKey, trainingTime)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.Initiative,
                _templateAttributesLocalization.InitiativeDesc,
                FormatValue(TemplateAttributesLocalizations.InitiativeKey, initiative)
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.Entrenchment,
                $"{_templateAttributesLocalization.EntrenchmentDesc}",
                $"{FormatValue(TemplateAttributesLocalizations.EntrenchmentKey, entrenchment + _unitDigInCap)} ({entrenchment:#.##} + {_unitDigInCap})"
            ),
            new TemplateAttributeVo(
                _templateAttributesLocalization.EntrenchmentModifier,
                $"{_templateAttributesLocalization.EntrenchmentModifier}: {_modifierValueService.GetDisplayValue((entrenchment + _unitDigInCap) * _digInFactor, "+%")}",
                _modifierValueService.GetDisplayValue((entrenchment + _unitDigInCap) * _digInFactor, "+%")
            )
        ];
        return;

        UnitIntrinsicStats ApplyBattalionMultipliers(UnitInfo unit)
        {
            if (battalionMultipliers.Count == 0)
            {
                return unit.Stats;
            }

            var stats = unit.Stats;
            //TODO: 加乘还是叠乘?
            foreach (var pair in battalionMultipliers)
            {
                if (!unit.IsInCategory(pair.Key))
                {
                    continue;
                }

                foreach (var battalionMultiplier in pair.Value)
                {
                    stats = battalionMultiplier.IsAdditive
                        ? stats.Add(battalionMultiplier.Stats)
                        : stats.Multiply(battalionMultiplier.Stats);
                }
            }

            return stats;
        }
    }

    private string FormatValue(string key, double value)
    {
        if (!_localizationService.TryGetValueInAll($"{key}_DIFF", out string? format))
        {
            format = _localizationService.GetValueInAll($"{key}_VALUE");
        }
        return _modifierValueService.GetDisplayValue(value, format, withPlusSign: false);
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
                var info = _existingUnits.Keys.FirstOrDefault(x =>
                    x.Point.X == position.Point.X && x.SlotType == UnitSlotType.Common
                );
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
            list.Add(
                new UnitInfoVo(_localizationFormatService.GetFormatText(unitInfo.Name), image, unitInfo)
            );
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
        var rootNode = new Node(string.Empty);
        rootNode.AddChild(template);
        _generatedText = rootNode.ToScript();
        _setText?.Invoke(_generatedText);

        Debug.Assert(SupplyPriorities is not null, "SupplyPriorities is null");
    }

    private void RefreshTerrainModifiers()
    {
        var lineBattalions = new List<UnitInfo>();
        var divisionalSupport = new List<UnitInfo>();
        var regimentalSupportByColumn = new List<(int Column, UnitInfo Unit)>();
        var lineBattalionsByColumn = new Dictionary<int, List<UnitInfo>>();

        foreach (var (position, unit) in _existingUnits)
        {
            if (position.SlotType == UnitSlotType.Common)
            {
                lineBattalions.Add(unit);
                int column = position.Point.X;
                if (!lineBattalionsByColumn.TryGetValue(column, out var columnBattalions))
                {
                    columnBattalions = [];
                    lineBattalionsByColumn.Add(column, columnBattalions);
                }
                columnBattalions.Add(unit);
            }
            else if (position.SlotType == UnitSlotType.DivisionalSupport)
            {
                divisionalSupport.Add(unit);
            }
            else if (position.SlotType == UnitSlotType.RegimentalSupport)
            {
                regimentalSupportByColumn.Add((position.Point.X, unit));
            }
        }

        var regimentalSupport = regimentalSupportByColumn
            .AsValueEnumerable()
            .Where(item =>
                lineBattalionsByColumn.TryGetValue(item.Column, out var columnBattalions)
                && _terrainModifierCalculator.CanApplyRegimentalSupport(
                    item.Unit,
                    columnBattalions,
                    MinUseRegimentalCount
                )
            )
            .Select(static item => item.Unit)
            .ToArray();
        var modifiers = _terrainModifierCalculator.Calculate(
            lineBattalions,
            divisionalSupport,
            regimentalSupport,
            _terrainService.LandTerrains
        );

        TerrainModifiers = modifiers
            .AsValueEnumerable()
            .Select(result =>
            {
                var modifier = result.Modifier;
                return new TerrainModifierVo(
                    _localizationFormatService.GetFormatTextInAll(result.Terrain),
                    GetTerrainImage(result.Terrain),
                    _modifierValueService.GetTerrainModifierDisplayValue("ATTACK", modifier.Attack),
                    _modifierValueService.GetTerrainModifierBrush("ATTACK", modifier.Attack),
                    _modifierValueService.GetTerrainModifierDisplayValue("MOVEMENT", modifier.Movement),
                    _modifierValueService.GetTerrainModifierBrush("MOVEMENT", modifier.Movement),
                    _modifierValueService.GetTerrainModifierDisplayValue("DEFENCE", modifier.Defence),
                    _modifierValueService.GetTerrainModifierBrush("DEFENCE", modifier.Defence)
                );
            })
            .ToArray();
    }

    private Bitmap? GetTerrainImage(string terrain)
    {
        if (_terrainImages.TryGetValue(terrain, out var image))
        {
            return image;
        }

        image = _imageService.GetIconByName($"GFX_adjuster_{terrain}_bg");
        _terrainImages.Add(terrain, image);
        return image;
    }

    public void Close()
    {
        foreach (var image in _terrainImages.Values)
        {
            image?.Dispose();
        }
        _terrainImages.Clear();
    }

    private sealed record PositionInfo(Point Point, UnitSlotType SlotType);

    private sealed class TemplateAttributesLocalizations
    {
        public TemplateAttributesLocalizations(LocalizationFormatService localizationService)
        {
            Hp = localizationService.GetFormatTextInAll(HpKey);
            HpDesc = localizationService.GetFormatTextInAll($"{HpKey}_DESC");
            Organization = localizationService.GetFormatTextInAll(OrganizationKey);
            OrganizationDesc = localizationService.GetFormatTextInAll($"{OrganizationKey}_DESC");
            RecoveryRate = localizationService.GetFormatTextInAll(RecoveryRateKey);
            RecoveryRateDesc = localizationService.GetFormatTextInAll($"{RecoveryRateKey}_DESC");
            Recon = localizationService.GetFormatTextInAll(ReconKey);
            ReconDesc = localizationService.GetFormatTextInAll($"{ReconKey}_DESC");
            Suppression = localizationService.GetFormatTextInAll(SuppressionKey);
            SuppressionDesc = localizationService.GetFormatTextInAll($"{SuppressionKey}_DESC");
            SupplyConsumption = localizationService.GetFormatTextInAll(SupplyConsumptionKey);
            SupplyConsumptionDesc = localizationService.GetFormatTextInAll($"{SupplyConsumptionKey}_DESC");
            CasualtyTrickleback = localizationService.GetFormatTextInAll(CasualtyTricklebackKey);
            CasualtyTricklebackDesc = localizationService.GetFormatTextInAll(
                $"{CasualtyTricklebackKey}_DESC"
            );
            EquipmentCaptureRatio = localizationService.GetFormatTextInAll(EquipmentCaptureRatioKey);
            EquipmentCaptureRatioDesc = localizationService.GetFormatTextInAll(
                $"{EquipmentCaptureRatioKey}_DESC"
            );
            ExperienceLoss = localizationService.GetFormatTextInAll(ExperienceLossKey);
            ExperienceLossDesc = localizationService.GetFormatTextInAll($"{ExperienceLossKey}_DESC");
            TrainingTime = localizationService.GetFormatTextInAll(TrainingTimeKey);
            TrainingTimeDesc = localizationService.GetFormatTextInAll($"{TrainingTimeKey}_DESC");
            Initiative = localizationService.GetFormatTextInAll(InitiativeKey);
            InitiativeDesc = localizationService.GetFormatTextInAll($"{InitiativeKey}_DESC");
            Entrenchment = localizationService.GetFormatTextInAll(EntrenchmentKey);
            EntrenchmentDesc = localizationService.GetFormatTextInAll($"{EntrenchmentKey}_DESC");
            Weight = localizationService.GetFormatTextInAll(WeightKey);
            WeightDesc = localizationService.GetFormatTextInAll($"{WeightKey}_DESC");
            EntrenchmentModifier = localizationService.GetFormatTextInAll("MODIFIER_COMBAT_ENTRENCHMENT");
        }

        public const string HpKey = "STAT_COMMON_MAX_STRENGTH";
        public string Hp { get; }
        public string HpDesc { get; }
        public const string OrganizationKey = "STAT_COMMON_MAX_ORG";
        public string Organization { get; }
        public string OrganizationDesc { get; }
        public const string RecoveryRateKey = "STAT_ARMY_DEFAULT_MORALE";
        public string RecoveryRate { get; }
        public string RecoveryRateDesc { get; }
        public const string ReconKey = "STAT_ARMY_RECON";
        public string Recon { get; }
        public string ReconDesc { get; }
        public const string SuppressionKey = "STAT_ARMY_SUPRESSION";
        public string Suppression { get; }
        public string SuppressionDesc { get; }
        public const string SupplyConsumptionKey = "STAT_ARMY_SUPPLY_CONSUMPTION";
        public string SupplyConsumption { get; }
        public string SupplyConsumptionDesc { get; }

        public const string CasualtyTricklebackKey = "STAT_CASUALTY_TRICKLEBACK";
        public string CasualtyTrickleback { get; }
        public string CasualtyTricklebackDesc { get; }
        public const string EquipmentCaptureRatioKey = "STAT_ARMY_EQUIPMENT_CAPTURE_FACTOR";
        public string EquipmentCaptureRatio { get; }
        public string EquipmentCaptureRatioDesc { get; }
        public const string ExperienceLossKey = "STAT_ARMY_EXPERIENCE_LOSS_FACTOR";
        public string ExperienceLoss { get; }
        public string ExperienceLossDesc { get; }
        public const string TrainingTimeKey = "DESIGNER_TRAINING_TIME";
        public string TrainingTime { get; }
        public string TrainingTimeDesc { get; }
        public const string InitiativeKey = "STAT_ARMY_INITIATIVE";
        public string Initiative { get; }
        public string InitiativeDesc { get; }
        public const string EntrenchmentKey = "STAT_ARMY_ENTRENCHMENT";
        public string Entrenchment { get; }
        public string EntrenchmentDesc { get; }
        public string EntrenchmentModifier { get; }
        public const string WeightKey = "STAT_COMMON_WEIGHT";
        public string Weight { get; }
        public string WeightDesc { get; }
    }
}

public sealed record UnitGroupVo(string GroupName, Bitmap? Image, IReadOnlyCollection<UnitInfoVo> Units);

public sealed record UnitInfoVo(
    string Name,
    Bitmap? Image,
    UnitInfo UnitInfo,
    bool IsRemoveOperation = false
);

public sealed record EquipmentsVo(string Name, int Quantity);

public sealed record TemplateAttributeVo(string Name, string Description, string Value);

public sealed record TerrainModifierVo(
    string Name,
    Bitmap? Image,
    string Attack,
    IBrush? AttackBrush,
    string Movement,
    IBrush? MovementBrush,
    string Defence,
    IBrush? DefenceBrush
);

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
