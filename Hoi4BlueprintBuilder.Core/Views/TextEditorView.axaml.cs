using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Localization.Strings;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<TextEditorView>]
public sealed partial class TextEditorView : UserControl, ITabViewItem, ISave
{
    private readonly NotificationService _notificationService;
    public string Header { get; }
    public string FilePath { get; }
    public string ToolTip { get; }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TextEditorView(UserStatusService statusService, NotificationService notificationService)
    {
        _notificationService = notificationService;
        InitializeComponent();

        if (statusService.CurrentSelectedFile is null)
        {
            throw new ArgumentNullException(nameof(statusService.CurrentSelectedFile));
        }

        string filePath = statusService.CurrentSelectedFile.FullPath;
        TextEditor.SetGrammar(Path.GetExtension(filePath));
        TextEditor.Text = File.ReadAllText(filePath);
        Header = Path.GetFileName(filePath);
        ToolTip = filePath;
        FilePath = filePath;
    }

    public void Save()
    {
        try
        {
            if (Path.GetExtension(FilePath.AsSpan()).Equals(".yml", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(FilePath, TextEditor.Text, Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(FilePath, TextEditor.Text, App.Utf8EncodingWithoutBom);
            }

            _notificationService.Show("保存成功", "成功", NotificationType.Success);
        }
        catch (Exception e)
        {
            Log.Error(e, "保存文件失败");
            _notificationService.Show("保存失败", LangResources.Common_Error, NotificationType.Error);
        }
    }
}
