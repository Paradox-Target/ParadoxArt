using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Hoi4BlueprintBuilder.Core.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ScreenshotService>]
public sealed class ScreenshotService(ProjectConfigService projectConfigService)
{
    public async Task SaveFocusTreeScreenshotAsync(
        IReadOnlyCollection<FocusNodeViewModel> nodes,
        IStorageFile file
    )
    {
        double cellWidth = projectConfigService.FocusCellWidth;
        double cellHeight = projectConfigService.FocusCellHeight;

        (double minX, double minY, double maxX, double maxY) = CalculateBounds(nodes);
        const double padding = 1.0;
        double width = (maxX - minX + 1 + 2 * padding) * cellWidth;
        double height = (maxY - minY + 1 + 2 * padding) * cellHeight;

        var size = new PixelSize((int)width, (int)height);
        var renderBitmap = new RenderTargetBitmap(size);
        using (var dc = renderBitmap.CreateDrawingContext())
        {
            // Draw Background
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                null,
                new Rect(0, 0, width, height)
            );

            // Transform to handle padding and offset
            var transform = new TranslateTransform(
                (padding - minX) * cellWidth,
                (padding - minY) * cellHeight
            );
            var pushedState = dc.PushTransform(transform.Value);

            // Draw Connections
            FocusConnectionLinesControl.DrawNodeConnectionsLines(dc, nodes);

            // Draw Nodes
            foreach (
                var viewModel in nodes
                    .AsValueEnumerable()
                    .Where(nodeViewModel => nodeViewModel.Node.IsVisible)
            )
            {
                DrawNode(dc, viewModel);
            }

            pushedState.Dispose();
        }

        await using var writeStream = await file.OpenWriteAsync();
        renderBitmap.Save(writeStream);
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) CalculateBounds(
        IReadOnlyCollection<FocusNodeViewModel> nodes
    )
    {
        double minX,
            minY,
            maxX,
            maxY;
        if (nodes.Count == 0)
        {
            minX = minY = maxX = maxY = 0;
        }
        else
        {
            minX = double.PositiveInfinity;
            minY = double.PositiveInfinity;
            maxX = double.NegativeInfinity;
            maxY = double.NegativeInfinity;
            foreach (var n in nodes)
            {
                int x = n.Node.X;
                int y = n.Node.Y;
                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        return (minX, minY, maxX, maxY);
    }

    private void DrawNode(DrawingContext dc, FocusNodeViewModel viewModel)
    {
        double cellWidth = projectConfigService.FocusCellWidth;
        double cellHeight = projectConfigService.FocusCellHeight;

        double x = viewModel.Node.X * cellWidth;
        double y = viewModel.Node.Y * cellHeight;

        // Draw Image
        if (viewModel.Bitmap is not null)
        {
            double imgWidth = viewModel.Width;
            double imgHeight = viewModel.Height;

            double imgX = x + (cellWidth - imgWidth) / 2;
            double imgY = y + (cellHeight - imgHeight) / 2;

            dc.DrawImage(viewModel.Bitmap, new Rect(imgX, imgY, imgWidth, imgHeight));
        }

        // Draw Text
        if (!string.IsNullOrEmpty(viewModel.Node.LocalizedName))
        {
            var formattedText = new FormattedText(
                viewModel.Node.LocalizedName,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei"),
                13,
                Brushes.White
            )
            {
                MaxTextWidth = cellWidth - 4,
                TextAlignment = TextAlignment.Center,
                Trimming = TextTrimming.CharacterEllipsis,
            };

            double textY = y + cellHeight + projectConfigService.FocusNameUpOffset;
            double textX = x + 2;

            dc.DrawText(formattedText, new Point(textX, textY));
        }
    }
}
