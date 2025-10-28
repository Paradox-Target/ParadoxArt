using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services.GameResources;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed class FocusInfoViewModel(FocusNode focusNode) : ObservableObject
{
    public FocusNode FocusNode { get; } = focusNode;

    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();

    public string IconPath => SpriteService.TryGetSpriteFilePath(FocusNode.Icon, out string? path) ? path : string.Empty;
}
