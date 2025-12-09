using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hoi4BlueprintEditor.Constants;
using Hoi4BlueprintEditor.Controls;
using Hoi4BlueprintEditor.ViewsModels;
using ZLinq;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<ScreenshotService>]
public sealed class ScreenshotService
{
    public void SaveFocusTreeScreenshot(IReadOnlyCollection<FocusNodeViewModel> nodes, string filePath)
    {
        // 1. Calculate Bounds
        double minX = nodes.AsValueEnumerable().Min(static n => n.Model.X);
        double minY = nodes.AsValueEnumerable().Min(static n => n.Model.Y);
        double maxX = nodes.AsValueEnumerable().Max(static n => n.Model.X);
        double maxY = nodes.AsValueEnumerable().Max(static n => n.Model.Y);

        const double padding = 1.0; // 1 cell padding
        double width = (maxX - minX + 1 + 2 * padding) * FocusMapConstants.CellWidth;
        double height = (maxY - minY + 1 + 2 * padding) * FocusMapConstants.CellHeight;

        var drawingVisual = new DrawingVisual();
        using (var dc = drawingVisual.RenderOpen())
        {
            // Draw Background
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                null,
                new Rect(0, 0, width, height)
            );

            // Transform to handle padding and offset
            var transform = new TranslateTransform(
                (padding - minX) * FocusMapConstants.CellWidth,
                (padding - minY) * FocusMapConstants.CellHeight
            );
            dc.PushTransform(transform);

            // Draw Connections
            FocusMapControl.DrawNodeConnectionsLines(dc, nodes);

            // Draw Nodes
            foreach (var viewModel in nodes)
            {
                DrawNode(dc, viewModel);
            }

            dc.Pop();
        }

        var renderBitmap = new RenderTargetBitmap((int)width, (int)height, 96d, 96d, PixelFormats.Pbgra32);
        renderBitmap.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var fileStream = new FileStream(filePath, FileMode.Create);
        encoder.Save(fileStream);
    }

    private static void DrawNode(DrawingContext dc, FocusNodeViewModel viewModel)
    {
        double x = viewModel.Model.X * FocusMapConstants.CellWidth;
        double y = viewModel.Model.Y * FocusMapConstants.CellHeight;

        // Draw Image
        if (viewModel.BitmapSource is not null)
        {
            double imgWidth = viewModel.Width;
            double imgHeight = viewModel.Height;

            double imgX = x + (FocusMapConstants.CellWidth - imgWidth) / 2;
            double imgY = y + (FocusMapConstants.CellHeight - imgHeight) / 2;

            dc.DrawImage(viewModel.BitmapSource, new Rect(imgX, imgY, imgWidth, imgHeight));
        }

        // Draw Text
        if (!string.IsNullOrEmpty(viewModel.LocalizedName))
        {
            var formattedText = new FormattedText(
                viewModel.LocalizedName,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei"),
                12,
                Brushes.White,
                96
            )
            {
                MaxTextWidth = FocusMapConstants.CellWidth - 4,
                TextAlignment = TextAlignment.Center,
                Trimming = TextTrimming.CharacterEllipsis
            };

            double textY = y + FocusMapConstants.FocusHeight + FocusMapConstants.FocusNameUpOffset;
            double textX = x + 2;

            dc.DrawText(formattedText, new Point(textX, textY));
        }
    }
}
