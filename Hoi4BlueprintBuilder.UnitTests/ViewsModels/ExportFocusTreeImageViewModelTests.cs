using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

namespace Hoi4BlueprintBuilder.UnitTests.ViewsModels;

[TestFixture]
public sealed class ExportFocusTreeImageViewModelTests
{
    [Test]
    public void CreateOptions_ShouldUseDefaultJpegQuality()
    {
        var viewModel = new ExportFocusTreeImageViewModel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CreateOptions().JpegQuality, Is.EqualTo(95));
            Assert.That(viewModel.CreateOptions().OutputFormat, Is.EqualTo(FocusTreeExportFormat.Png));
            Assert.That(viewModel.IsJpegSelected, Is.False);
        }
    }

    [Test]
    public void CreateOptions_ShouldKeepConfiguredJpegQuality()
    {
        var viewModel = new ExportFocusTreeImageViewModel { JpegQuality = 42 };

        Assert.That(viewModel.CreateOptions().JpegQuality, Is.EqualTo(42));
    }

    [Test]
    public void SelectingJpeg_ShouldShowQualityAndUseJpegFormat()
    {
        var viewModel = new ExportFocusTreeImageViewModel();
        viewModel.SelectedOutputFormat = viewModel.OutputFormatOptions.Single(
            option => option.Format == FocusTreeExportFormat.Jpeg
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.IsJpegSelected, Is.True);
            Assert.That(viewModel.CreateOptions().OutputFormat, Is.EqualTo(FocusTreeExportFormat.Jpeg));
        }
    }
}
