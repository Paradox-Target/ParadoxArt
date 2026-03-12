using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.Services;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<NotSupportInfoControlView>]
public sealed partial class NotSupportInfoControlView : UserControl, ITabViewItem
{
    public string Header { get; }
    public string FilePath { get; }
    public string ToolTip { get; }

    public NotSupportInfoControlView(UserStatusService userStatusService)
    {
        InitializeComponent();
        var item =
            userStatusService.CurrentSelectedFile
            ?? throw new InvalidOperationException("CurrentSelectedFile is null");
        Header = item.Name;
        FilePath = item.FullPath;
        ToolTip = item.FullPath;
        FilePathBlock.Text = item.FullPath;
    }
}
