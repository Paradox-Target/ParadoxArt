using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.ViewsModels;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;

namespace Hoi4BlueprintEditor.Views;

[RegisterTransient<ModInitializeWindowView>]
public sealed partial class ModInitializeWindowView : Window
{
    private readonly ModInitializeWindowViewModel _viewModel;

    public ModInitializeWindowView(ModInitializeWindowViewModel viewModel)
    {
        InitializeComponent();

        viewModel.Window = this;
        _viewModel = viewModel;
        DataContext = viewModel;
    }
}
