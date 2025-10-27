using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services.GameResources;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class FocusInfoViewModel(FocusNode focusNode) : ObservableObject
{
    private readonly FocusNode _focusNode = focusNode;

    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();

    public string Id
    {
        get => _focusNode.Id;
        set
        {
            _focusNode.Id = value;
            OnPropertyChanged();
        }
    }

    public decimal Cost
    {
        get => _focusNode.Cost;
        set
        {
            _focusNode.Cost = value;
            OnPropertyChanged();
        }
    }

    public string Icon
    {
        get => _focusNode.Icon;
        set
        {
            _focusNode.Icon = value;
            OnPropertyChanged();
        }
    }
    public string IconPath => SpriteService.TryGetSpriteFilePath(Icon, out var path) ? path : string.Empty;

}
