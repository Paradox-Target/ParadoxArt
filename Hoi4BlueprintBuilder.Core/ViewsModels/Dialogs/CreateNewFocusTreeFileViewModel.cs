using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

[RegisterTransient<CreateNewFocusTreeFileViewModel>]
public sealed partial class CreateNewFocusTreeFileViewModel : ObservableObject
{
    // TODO: 有效性检查
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _countryTag = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private bool _isDefaultFocusTree;

    public event Action<bool>? PrimaryEnableChanged;

    private bool CanCreate =>
        !string.IsNullOrWhiteSpace(FileName)
        && (!string.IsNullOrWhiteSpace(CountryTag) || IsDefaultFocusTree)
        && !string.IsNullOrWhiteSpace(Id);

    public CreateNewFocusTreeFileViewModel()
    {
        PropertyChanged += (_, _) => PrimaryEnableChanged?.Invoke(CanCreate);
    }
}
