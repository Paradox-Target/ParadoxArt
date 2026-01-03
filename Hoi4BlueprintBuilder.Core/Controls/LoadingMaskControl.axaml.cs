using Avalonia;
using Avalonia.Controls;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed partial class LoadingMaskControl : UserControl
{
    public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<
        LoadingMaskControl,
        string
    >(nameof(Message), string.Empty);

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public LoadingMaskControl()
    {
        InitializeComponent();
    }
}
