using Avalonia.Headless.NUnit;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

namespace Hoi4BlueprintBuilder.UnitTests.Avalonia;

[TestFixture]
public sealed class SmokeTests
{
    [AvaloniaTest]
    public void View_ShouldInitializeCorrectly()
    {
        var file = new SystemFileItem(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), true, null);

        Assert.DoesNotThrow(() =>
        {
            _ = new GameSettingsPageView();
            _ = new ProjectSettingsView();
            _ = new MainSettingsView();
            _ = new AppSettingsPageView();
            _ = new RenameFileView(new ContentDialog(), file);
            _ = new CreateNewProjectView();
            _ = new CreateNewFocusTreeFileView();
            _ = new CreateNewFocusView();
            _ = new NotSupportInfoControlView(new UserStatusService { CurrentSelectedFile = file });
            _ = new EulaView();
            _ = new LocalizationManagerView();
        });
    }
}
