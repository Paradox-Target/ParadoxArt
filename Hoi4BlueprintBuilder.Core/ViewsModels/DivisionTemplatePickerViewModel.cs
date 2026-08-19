using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Localization.Strings;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<DivisionTemplatePickerViewModel>]
public sealed partial class DivisionTemplatePickerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial IReadOnlyList<DivisionTemplateCardVo> Templates { get; private set; } = [];

    [ObservableProperty]
    public partial bool IsShowingEditor { get; private set; }

    [ObservableProperty]
    public partial OobEditorView? EditorView { get; private set; }

    [ObservableProperty]
    public partial string SelectedTemplateName { get; private set; } = string.Empty;

    public bool HasTemplates => Templates.Count > 0;
    public string FilePath { get; }

    private readonly List<Node> _templateNodes;
    private readonly UnitService _unitService;
    private readonly NotificationService _notificationService;
    private readonly TelemetryService _telemetryService;
    private readonly SettingsService _settingsService;
    private readonly GameResourcesPathService _pathService;
    private DivisionTemplateCardVo? _selectedCard;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DivisionTemplatePickerViewModel(
        UserStatusService statusService,
        UnitService unitService,
        NotificationService notificationService,
        TelemetryService telemetryService,
        SettingsService settingsService,
        GameResourcesPathService pathService
    )
    {
        _unitService = unitService;
        _notificationService = notificationService;
        _telemetryService = telemetryService;
        _settingsService = settingsService;
        _pathService = pathService;
        FilePath =
            statusService.CurrentSelectedFile?.FullPath
            ?? throw new ArgumentNullException(nameof(statusService.CurrentSelectedFile));

        Dictionary<string, ushort> units = [];
        if (TextParser.TryParse(FilePath, out var rootNode, out var error))
        {
            _templateNodes = [.. DivisionTemplateHelper.GetDivisionTemplates(rootNode)];
            ParseUnitsCount(rootNode, units);
        }
        else
        {
            _templateNodes = [];
            Log.LogParseError(error);
            _notificationService.Show(
                LangResources.DivisionTemplatePickerView_LoadFailed,
                LangResources.Common_Error,
                NotificationType.Error
            );
        }

        Templates = _templateNodes
            .AsValueEnumerable()
            .Select((n, index) => CreateCard(n, index, units))
            .OrderByDescending(x =>
                int.Parse(
                    x.Stats.FirstOrDefault(vo =>
                        vo.Label == LangResources.DivisionTemplatePickerView_Count
                    )?.Value ?? "0"
                )
            )
            .ToArray();
        _telemetryService.TrackEvent(
            "Open_Division_Template_File",
            new Dictionary<string, string> { { "TemplateCount", Templates.Count.ToString() } }
        );
    }

    private static void ParseUnitsCount(Node rootNode, Dictionary<string, ushort> units)
    {
        foreach (var unitsNode in rootNode.NodesValue.Where(n => n.Key.EqualsIgnoreCase("units")))
        {
            foreach (var divisionNode in unitsNode.NodesValue.Where(n => n.Key.EqualsIgnoreCase("division")))
            {
                string? name = divisionNode
                    .LeavesValue.FirstOrDefault(l => l.Key.EqualsIgnoreCase("division_template"))
                    ?.ValueText;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (units.TryGetValue(name, out ushort count))
                    {
                        units[name] = ++count;
                    }
                    else
                    {
                        units[name] = 1;
                    }
                }
            }
        }
    }

    private DivisionTemplateCardVo CreateCard(
        Node templateNode,
        int index,
        IReadOnlyDictionary<string, ushort> units
    )
    {
        string name = LangResources.DivisionTemplatePickerView_Unnamed;
        bool isLocked = false;
        int battalionCount = 0;
        int supportCount = 0;
        int width = 0;
        int manpower = 0;

        foreach (var child in templateNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.EqualsIgnoreCase("name") && !string.IsNullOrWhiteSpace(leaf.ValueText))
                {
                    name = leaf.ValueText;
                }
                else if (leaf.Key.EqualsIgnoreCase("is_locked") && leaf.Value.TryGetBool(out bool locked))
                {
                    isLocked = locked;
                }
            }
            else if (child.TryGetNode(out var node))
            {
                if (node.Key.EqualsIgnoreCase("regiments"))
                {
                    battalionCount = node.AllArray.Length;
                }
                else if (node.Key.EqualsIgnoreCase("support"))
                {
                    supportCount = node.AllArray.Length;
                }
            }
        }

        foreach (var unitName in GetUnitNames(templateNode))
        {
            var unitInfo = FindUnitInfo(unitName);
            if (unitInfo is null)
            {
                continue;
            }
            width += unitInfo.Width;
            manpower += unitInfo.Manpower;
        }

        return new DivisionTemplateCardVo(
            name,
            index,
            isLocked,
            [
                new DivisionTemplateStatVo(
                    LangResources.DivisionTemplatePickerView_Battalions,
                    battalionCount.ToString()
                ),
                new DivisionTemplateStatVo(
                    LangResources.DivisionTemplatePickerView_Support,
                    supportCount.ToString()
                ),
                new DivisionTemplateStatVo(
                    LangResources.DivisionTemplatePickerView_CombatWidth,
                    width.ToString()
                ),
                new DivisionTemplateStatVo(
                    LangResources.DivisionTemplatePickerView_Manpower,
                    manpower.ToString("N0")
                ),
                new DivisionTemplateStatVo(
                    LangResources.DivisionTemplatePickerView_Count,
                    units.GetValueOrDefault(name, (ushort)0).ToString()
                )
            ]
        );
    }

    private static IEnumerable<string> GetUnitNames(Node templateNode)
    {
        var names = new List<string>();
        foreach (var child in templateNode.NodesValue)
        {
            if (
                !child.Key.EqualsIgnoreCase("regiments")
                && !child.Key.EqualsIgnoreCase("support")
                && !child.Key.EqualsIgnoreCase("regimental_support")
            )
            {
                continue;
            }

            foreach (var unitNode in child.NodesValue)
            {
                names.Add(unitNode.Key);
            }
        }

        return names;
    }

    private UnitInfo? FindUnitInfo(string name) =>
        _unitService.AllUnits.FirstOrDefault(unit =>
            unit.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
        );

    [RelayCommand]
    private void PickTemplate(DivisionTemplateCardVo? card)
    {
        if (card is null || card.NodeIndex >= _templateNodes.Count)
        {
            return;
        }

        var editorView = App.Current.Services.GetRequiredService<OobEditorView>();
        editorView.LoadTemplate(_templateNodes[card.NodeIndex]);
        _selectedCard = card;
        SelectedTemplateName = card.Name;
        EditorView = editorView;
        IsShowingEditor = true;
        _telemetryService.TrackEvent(
            "Open_Division_Template",
            new Dictionary<string, string> { { "TemplateName", card.Name } }
        );
    }

    [RelayCommand]
    private void Back()
    {
        IsShowingEditor = false;
        if (EditorView is not null)
        {
            EditorView.ViewModel.Close();
            EditorView = null;
        }
    }

    /// <summary>
    /// 将编辑器中的模板内容保存回文件中对应的 <c>division_template</c> 节点
    /// </summary>
    public void Save()
    {
        if (_selectedCard is null || EditorView is null)
        {
            return;
        }

        if (!TextParser.TryParse(FilePath, out var rootNode, out var error))
        {
            Log.LogParseError(error);
            _notificationService.Show(
                LangResources.SaveFailed,
                LangResources.Common_Error,
                NotificationType.Error
            );
            return;
        }

        var templateNodes = DivisionTemplateHelper.GetDivisionTemplates(rootNode).ToList();
        if (_selectedCard.NodeIndex >= templateNodes.Count)
        {
            Log.Error("保存部队模板失败, 找不到索引为 {Index} 的模板", _selectedCard.NodeIndex);
            _notificationService.Show(
                LangResources.SaveFailed,
                LangResources.Common_Error,
                NotificationType.Error
            );
            return;
        }

        templateNodes[_selectedCard.NodeIndex].AllArray = EditorView.ViewModel.CreateTemplateNode().AllArray;

        var fileOrigin = _pathService.GetFileOrigin(FilePath);
        if (fileOrigin == FileOrigin.Mod)
        {
            File.WriteAllText(FilePath, rootNode.ToScript(), App.Utf8EncodingWithoutBom);
        }
        else if (fileOrigin == FileOrigin.Game)
        {
            string relativePath = Path.GetRelativePath(_settingsService.GameRootFolderPath, FilePath);
            string modFilePath = Path.Combine(_settingsService.ModRootFolderPath, relativePath);
            File.WriteAllText(modFilePath, rootNode.ToScript(), App.Utf8EncodingWithoutBom);
        }
        else
        {
            Log.Error("保存文件中遇到无法识别的文件来源: {FilePath}", FilePath);
            _notificationService.Show(
                LangResources.SaveFailed,
                LangResources.Common_Error,
                NotificationType.Error
            );
            return;
        }

        _notificationService.Show(
            LangResources.SavedSuccessfully,
            LangResources.Success,
            NotificationType.Success
        );
        _telemetryService.TrackEvent("Save_Division_Template");
    }
}

/// <summary>
/// 部队模板统计项
/// </summary>
/// <param name="Label">统计标签</param>
/// <param name="Value">统计值</param>
public sealed record DivisionTemplateStatVo(string Label, string Value);

/// <summary>
/// 部队模板选择卡片
/// </summary>
/// <param name="Name">模板名</param>
/// <param name="NodeIndex">模板在文件中的序号</param>
/// <param name="IsLocked">模板是否锁定</param>
/// <param name="Stats">统计信息列表</param>
public sealed record DivisionTemplateCardVo(
    string Name,
    int NodeIndex,
    bool IsLocked,
    IReadOnlyList<DivisionTemplateStatVo> Stats
);
