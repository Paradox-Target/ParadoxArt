using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using ZLinq;

namespace Hoi4BlueprintEditor.ViewsModels.Dialogs;

public sealed partial class CreateNewFocusViewModel(IReadOnlyCollection<string> filePaths) : ObservableObject
{
    public FocusType SelectedFocusType => FocusTypes[SelectedFocusTypeIndex].Type;
    public string SelectedFocusFilePath => FocusFileNames[SelectedFocusFileNameIndex].FilePath;
    public FocusTypeItem[] FocusTypes { get; } =
        [new(FocusType.Normal, "普通国策"), new(FocusType.Shared, "共享国策")];
    public FocusFileItem[] FocusFileNames { get; } =
        filePaths
            .AsValueEnumerable()
            .Select(static path => new FocusFileItem(Path.GetFileName(path), path))
            .ToArray();

    [ObservableProperty]
    private string _focusId = string.Empty;

    [ObservableProperty]
    private int _selectedFocusTypeIndex;

    [ObservableProperty]
    private int _selectedFocusFileNameIndex = -1;

    public sealed record FocusTypeItem(FocusType Type, string DisplayName);

    public sealed record FocusFileItem(string FileName, string FilePath);

    public event Action<bool>? PrimaryEnableChanged;

    partial void OnFocusIdChanged(string value)
    {
        PrimaryEnableChanged?.Invoke(!string.IsNullOrWhiteSpace(value));
    }

    partial void OnSelectedFocusFileNameIndexChanged(int value)
    {
        if (value >= 0 && value < FocusFileNames.Length)
        {
            string fileName = FocusFileNames[value].FileName;
            PrimaryEnableChanged?.Invoke(!string.IsNullOrWhiteSpace(fileName));
        }
        else
        {
            PrimaryEnableChanged?.Invoke(false);
        }
    }
}
