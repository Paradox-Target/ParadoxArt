using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Constants;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.ViewsModels;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private FocusNode? _movedFocusNode;
    private readonly EditorCanvasViewModel _viewModel;
    private FocusNode? _lastRightClickFocus;
    private Point _rightClickPoint;
    private SelectState _selectState = SelectState.None;
    private bool CursorOverFocus => _lastRightClickFocus is not null;
    private bool CursorNotOverFocus => !CursorOverFocus;

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
        MouseRightButtonDown += OnMouseRightButtonDown;
        _viewModel = App.Current.Services.GetRequiredService<EditorCanvasViewModel>();
        DataContext = _viewModel;

        // 方便右键菜单在前端绑定 Command
        ContextMenu.DataContext = this;
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _rightClickPoint = e.GetPosition(this);
        var result = VisualTreeHelper.HitTest(this, _rightClickPoint);
        if (result.VisualHit is FrameworkElement { DataContext: FocusNodeViewModel viewModel })
        {
            _lastRightClickFocus = viewModel.Model;
        }
        else
        {
            _lastRightClickFocus = null;
        }
        ContextMenu.IsOpen = true;
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetMutuallyExclusiveFocus()
    {
        if (_lastRightClickFocus is null)
        {
            return;
        }

        _selectState = SelectState.MutuallyExclusive;
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetPrerequisiteFocus()
    {
        if (_lastRightClickFocus is null)
        {
            return;
        }

        _selectState = SelectState.Prerequisite;
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
            var focus = viewModel.Model;
            if (_lastRightClickFocus is not null && _lastRightClickFocus != viewModel.Model)
            {
                if (_selectState == SelectState.MutuallyExclusive)
                {
                    if (!focus.MutuallyExclusive.Contains(_lastRightClickFocus))
                    {
                        focus.MutuallyExclusive.Add(_lastRightClickFocus);
                        _lastRightClickFocus.MutuallyExclusive.Add(viewModel.Model);
                        _lastRightClickFocus = null;
                        WeakReferenceMessenger.Default.Send(new RedrawFocusConnectionLinesMessage());
                    }
                    _selectState = SelectState.None;
                    _lastRightClickFocus = null;
                    return;
                }

                if (_selectState == SelectState.Prerequisite)
                {
                    if (!focus.Prerequisite.Any(prerequisite => prerequisite.Contains(_lastRightClickFocus)))
                    {
                        _lastRightClickFocus.Prerequisite.Add([viewModel.Model]);
                        WeakReferenceMessenger.Default.Send(new RedrawFocusConnectionLinesMessage());
                    }
                    _selectState = SelectState.None;
                    _lastRightClickFocus = null;
                    return;
                }
            }

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

            OpenFocusInfoView(viewModel.Model);
            Log.Debug("信息卡切换到国策: {Name}", viewModel.Model.Id);
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
            var position = GetMousePositionOnGrid(e.GetPosition(this));
            _movedFocusNode.SetRawPosition(position.X, position.Y);
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

    [RelayCommand(CanExecute = nameof(CursorNotOverFocus))]
    private async Task CreateNewFocus()
    {
        var viewModel = new CreateNewFocusViewModel(_viewModel.GetAllFocusFiles())
        {
            FocusId = _viewModel.GetNextFocusId()
        };
        var dialog = new ContentDialog
        {
            Title = "新建国策",
            Content = new CreateNewFocusView { DataContext = viewModel },
            CloseButtonText = "取消",
            PrimaryButtonText = "创建",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };

        Action<bool> onPrimaryEnableChanged = enable => dialog.IsPrimaryButtonEnabled = enable;
        viewModel.PrimaryEnableChanged += onPrimaryEnableChanged;
        var result = await dialog.ShowAsync(App.Current.MainWindow);

        if (result == ContentDialogResult.Primary)
        {
            var position = GetMousePositionOnGrid(_rightClickPoint);
            var newFocusNode = await WeakReferenceMessenger.Default.Send(
                new CreateNewFocusMessage(
                    new FocusPoint(position.X, position.Y),
                    viewModel.FocusId,
                    viewModel.SelectedFocusType,
                    viewModel.SelectedFocusFilePath
                )
            );

            OpenFocusInfoView(newFocusNode);
            Log.Debug("创建新国策: {Name}", newFocusNode.Id);
        }
        viewModel.PrimaryEnableChanged -= onPrimaryEnableChanged;
    }

    private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        CreateNewFocusCommand.NotifyCanExecuteChanged();
        SetPrerequisiteFocusCommand.NotifyCanExecuteChanged();
        SetMutuallyExclusiveFocusCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 获取鼠标在网格中的实际坐标
    /// </summary>
    /// <param name="mousePoint">原始坐标</param>
    /// <returns></returns>
    private (int X, int Y) GetMousePositionOnGrid(Point mousePoint)
    {
        double scale = _viewModel.Scale;
        int x = (int)((mousePoint.X - _viewModel.TranslateX) / (FocusMapConstants.CellWidth * scale));
        int y = (int)((mousePoint.Y - _viewModel.TranslateY) / (FocusMapConstants.CellHeight * scale));
        return (x, y);
    }

    private void OpenFocusInfoView(FocusNode focusNode)
    {
        FocusInfoView.DataContext = new FocusInfoViewModel(focusNode);
        FocusInfoView.Width = ActualWidth * FocusInfoViewWidthRatio;
        FocusInfoView.Height = ActualHeight * FocusInfoViewHeightRatio;
        FocusInfoView.IsOpen = true;
    }

    private enum SelectState : byte
    {
        None,
        MutuallyExclusive,
        Prerequisite
    }
}
