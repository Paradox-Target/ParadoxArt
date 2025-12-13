using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class ActivateWindowView : Window
{
    public ActivateWindowView(ActivateWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        WeakReferenceMessenger.Default.Register<ActivateSuccessMessage>(
            this,
            (_, _) => App.Current.Dispatcher.Invoke(Close)
        );
    }
}
