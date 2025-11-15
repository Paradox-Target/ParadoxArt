using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hoi4BlueprintEditor.Controls;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Point = System.Windows.Point;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private FocusNode? _movedFocusNode;
    private readonly EditorCanvasViewModel _viewModel;

    private const double FocusInfoViewWidthRatio = 0.35;
    private const double FocusInfoViewHeightRatio = 0.9;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasView()
    {
        InitializeComponent();

        // 鼠标事件
        MouseWheel += OnMouseWheel;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        _viewModel = App.Current.Services.GetRequiredService<EditorCanvasViewModel>();
        DataContext = _viewModel;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _movedFocusNode = null;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        var result = VisualTreeHelper.HitTest(this, position);
        if (result.VisualHit is FrameworkElement { DataContext: FocusNodeViewModel viewModel })
        {
            if (e.ClickCount <= 1)
            {
                _movedFocusNode = viewModel.Model;
                FocusInfoView.IsOpen = false;
                return;
            }

            _movedFocusNode = null;
            if (
                FocusInfoView.DataContext is FocusInfoViewModel oldViewModel
                && oldViewModel.FocusNode == viewModel.Model
            )
            {
                FocusInfoView.IsOpen = true;
                Log.Debug("相同国策, 跳过切换");
                return;
            }

            FocusInfoView.Width = ActualWidth * FocusInfoViewWidthRatio;
            FocusInfoView.Height = ActualHeight * FocusInfoViewHeightRatio;

            FocusInfoView.DataContext = new FocusInfoViewModel(viewModel.Model);
            FocusInfoView.IsOpen = true;
            Log.Debug("切换到国策: {Name}", viewModel.Model.Id);
        }
        else
        {
            FocusInfoView.IsOpen = false;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            if (_lastMousePositionOnCanvas.HasValue)
            {
                var currentMousePosition = e.GetPosition(this);
                var delta = currentMousePosition - _lastMousePositionOnCanvas.Value;

                _viewModel.TranslateX += delta.X;
                _viewModel.TranslateY += delta.Y;

                // 鼠标图案改变
                Cursor = Cursors.Hand;
            }

            _lastMousePositionOnCanvas = e.GetPosition(this);
        }
        else
        {
            _lastMousePositionOnCanvas = null;
            Cursor = Cursors.Arrow;
        }

        if (e.LeftButton == MouseButtonState.Pressed && _movedFocusNode is not null)
        {
            var position = e.GetPosition(this);
            double scale = _viewModel.Scale;

            _movedFocusNode.SetRawPosition(
                (int)((position.X - _viewModel.TranslateX) / (GridRulerControl.CellWidth * scale)),
                (int)((position.Y - _viewModel.TranslateY) / (GridRulerControl.CellHeight * scale))
            );
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _lastMousePositionOnCanvas = null;
        Cursor = Cursors.Arrow;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 缩放
        const double scaleRate = 1.1;
        var mousePoint = e.GetPosition(this);

        double newScale;

        if (e.Delta > 0)
        {
            // 放大
            newScale = _viewModel.Scale * scaleRate;
        }
        else
        {
            // 缩小
            newScale = _viewModel.Scale / scaleRate;
        }

        if (newScale < 0.1)
        {
            newScale = 0.1;
        }
        if (newScale > 5.0)
        {
            newScale = 5.0;
        }

        double oldScale = _viewModel.Scale;
        _viewModel.TranslateX = mousePoint.X - (mousePoint.X - _viewModel.TranslateX) * (newScale / oldScale);
        _viewModel.TranslateY = mousePoint.Y - (mousePoint.Y - _viewModel.TranslateY) * (newScale / oldScale);

        _viewModel.Scale = newScale;
    }
}
