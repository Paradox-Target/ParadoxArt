using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Controls;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

/// <summary>
/// 画布交互管理器，统一管理画布上的鼠标交互逻辑
/// </summary>
public sealed class CanvasInteractionManager
{
    private readonly EditorCanvasViewModel _viewModel;
    private readonly Control _canvas;
    private readonly ConnectionPreviewOverlayControl _connectionPreview;
    private readonly Func<FocusNode, bool> _openFocusInfoView;
    private readonly Action _closeFocusInfoView;

    // 右键按下判定的移动阈值（像素）
    private const double RightClickMoveThreshold = 5.0;

    // 交互状态
    private CanvasInteractionMode _mode = CanvasInteractionMode.None;
    private Point _lastMousePosition;
    private Point _rightButtonDownPosition;
    private FocusNode? _draggedNode;
    private FocusNode? _rightClickedNode;
    private ConnectionType _connectionType = ConnectionType.None;

    /// <summary>
    /// 获取当前交互模式
    /// </summary>
    public CanvasInteractionMode Mode => _mode;

    /// <summary>
    /// 获取或设置连接类型
    /// </summary>
    public ConnectionType ConnectionType
    {
        get => _connectionType;
        set
        {
            _connectionType = value;
            _connectionPreview.Mode = value;

            if (value != ConnectionType.None)
            {
                _mode = CanvasInteractionMode.Connecting;
            }
            else if (_mode == CanvasInteractionMode.Connecting)
            {
                _mode = CanvasInteractionMode.None;
            }
        }
    }

    /// <summary>
    /// 右键点击的国策节点
    /// </summary>
    public FocusNode? RightClickedNode => _rightClickedNode;

    /// <summary>
    /// 右键点击位置是否在某个国策节点上
    /// </summary>
    public bool CursorOverFocus => _rightClickedNode is not null;

    /// <summary>
    /// 右键点击位置是否不在某个国策节点上
    /// </summary>
    public bool CursorNotOverFocus => !CursorOverFocus;

    /// <summary>
    /// 当选中节点变化时触发
    /// </summary>
    public event Action? SelectionChanged;

    /// <summary>
    /// 当连接建立时触发
    /// </summary>
    public event Action<FocusNode, FocusNode, ConnectionType>? ConnectionRequested;

    public CanvasInteractionManager(
        EditorCanvasViewModel viewModel,
        Control canvas,
        ConnectionPreviewOverlayControl connectionPreview,
        Func<FocusNode, bool> openFocusInfoView,
        Action closeFocusInfoView
    )
    {
        _viewModel = viewModel;
        _canvas = canvas;
        _connectionPreview = connectionPreview;
        _openFocusInfoView = openFocusInfoView;
        _closeFocusInfoView = closeFocusInfoView;
    }

    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    public void HandlePointerPressed(PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(_canvas).Properties;
        var position = e.GetPosition(_canvas);
        var hitViewModel = GetHitFocusNodeViewModel(position);

        if (props.IsRightButtonPressed)
        {
            HandleRightButtonDown(e, position, hitViewModel);
            return;
        }

        if (props.IsMiddleButtonPressed)
        {
            HandleMiddleButtonDown(position);
            return;
        }

        if (props.IsLeftButtonPressed)
        {
            HandleLeftButtonDown(e, position, hitViewModel);
        }
    }

    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    public void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        var props = e.GetCurrentPoint(_canvas).Properties;

        switch (props.PointerUpdateKind)
        {
            case PointerUpdateKind.LeftButtonReleased:
                HandleLeftButtonUp();
                break;

            case PointerUpdateKind.MiddleButtonReleased:
                HandleMiddleButtonUp();
                break;
        }
    }

    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    public StandardCursorType HandlePointerMoved(PointerEventArgs e)
    {
        var position = e.GetPosition(_canvas);
        var props = e.GetCurrentPoint(_canvas).Properties;

        // 连接模式下更新预览
        if (_mode == CanvasInteractionMode.Connecting)
        {
            var hitViewModel = GetHitFocusNodeViewModel(position);
            _connectionPreview.To = hitViewModel?.Model;
            return StandardCursorType.Cross;
        }

        // 画布平移
        if (_mode == CanvasInteractionMode.Panning || props.IsMiddleButtonPressed)
        {
            _mode = CanvasInteractionMode.Panning;

            var delta = position - _lastMousePosition;
            _viewModel.TranslateX += delta.X;
            _viewModel.TranslateY += delta.Y;
            _lastMousePosition = position;

            return StandardCursorType.Hand;
        }

        // 拖动节点
        if (_mode == CanvasInteractionMode.DraggingNode && _draggedNode is not null)
        {
            var gridPosition = GetMousePositionOnGrid(position);

            // 检查是否有选中的节点需要一起移动
            var selectedNodes = GetSelectedNodes();
            if (selectedNodes.Count > 1 && selectedNodes.Contains(_draggedNode))
            {
                // 计算拖动增量
                int deltaX = gridPosition.X - _draggedNode.RawPosition.X;
                int deltaY = gridPosition.Y - _draggedNode.RawPosition.Y;

                // 移动所有选中的节点
                foreach (var node in selectedNodes)
                {
                    node.SetRawPosition(node.RawPosition.X + deltaX, node.RawPosition.Y + deltaY);
                }
            }
            else
            {
                _draggedNode.SetRawPosition(gridPosition.X, gridPosition.Y);
            }

            return StandardCursorType.SizeAll;
        }

        _lastMousePosition = position;
        return StandardCursorType.Arrow;
    }

    /// <summary>
    /// 处理鼠标离开事件
    /// </summary>
    public void HandlePointerExited()
    {
        // 鼠标离开时只重置平移状态
        // 框选和右键等待状态保留，等待鼠标释放事件来处理
        if (_mode == CanvasInteractionMode.Panning)
        {
            _mode = CanvasInteractionMode.None;
        }
    }

    /// <summary>
    /// 处理滚轮事件（缩放）
    /// </summary>
    public void HandlePointerWheelChanged(PointerWheelEventArgs e)
    {
        const double scaleRate = 1.1;
        var mousePoint = e.GetPosition(_canvas);

        double newScale = e.Delta.Y > 0 ? _viewModel.Scale * scaleRate : _viewModel.Scale / scaleRate;

        newScale = Math.Clamp(newScale, 0.1, 5.0);

        double oldScale = _viewModel.Scale;
        _viewModel.TranslateX = mousePoint.X - (mousePoint.X - _viewModel.TranslateX) * (newScale / oldScale);
        _viewModel.TranslateY = mousePoint.Y - (mousePoint.Y - _viewModel.TranslateY) * (newScale / oldScale);
        _viewModel.Scale = newScale;
    }

    /// <summary>
    /// 开始设置连接
    /// </summary>
    public void StartConnection(FocusNode from, ConnectionType type)
    {
        _rightClickedNode = from;
        ConnectionType = type;
        _connectionPreview.From = from;
    }

    /// <summary>
    /// 取消连接模式
    /// </summary>
    public void CancelConnection()
    {
        ConnectionType = ConnectionType.None;
        _connectionPreview.ClearPreview();
        _mode = CanvasInteractionMode.None;
    }

    /// <summary>
    /// 清除所有选中状态
    /// </summary>
    public void ClearSelection()
    {
        foreach (var node in _viewModel.Nodes)
        {
            node.IsSelected = false;
        }
        SelectionChanged?.Invoke();
    }

    /// <summary>
    /// 获取所有选中的节点
    /// </summary>
    public List<FocusNode> GetSelectedNodes()
    {
        return _viewModel
            .Nodes.AsValueEnumerable()
            .Where(vm => vm.IsSelected)
            .Select(vm => vm.Model)
            .ToList();
    }

    #region Private Methods

    private void HandleRightButtonDown(
        PointerPressedEventArgs e,
        Point position,
        FocusNodeViewModel? hitViewModel
    )
    {
        _rightButtonDownPosition = position;
        _rightClickedNode = hitViewModel?.Model;

        // 连接模式时右键取消
        if (_mode == CanvasInteractionMode.Connecting)
        {
            CancelConnection();
            return;
        }

        // 进入右键等待状态
        _mode = CanvasInteractionMode.RightButtonPending;
    }

    private void HandleMiddleButtonDown(Point position)
    {
        _mode = CanvasInteractionMode.Panning;
        _lastMousePosition = position;
    }

    private void HandleMiddleButtonUp()
    {
        if (_mode == CanvasInteractionMode.Panning)
        {
            _mode = CanvasInteractionMode.None;
        }
    }

    private void HandleLeftButtonDown(
        PointerPressedEventArgs e,
        Point position,
        FocusNodeViewModel? hitViewModel
    )
    {
        // 连接模式下点击节点建立连接
        if (
            _mode == CanvasInteractionMode.Connecting
            && hitViewModel is not null
            && _rightClickedNode is not null
        )
        {
            if (_rightClickedNode != hitViewModel.Model)
            {
                ConnectionRequested?.Invoke(_rightClickedNode, hitViewModel.Model, _connectionType);
                CancelConnection();
            }
            return;
        }

        if (hitViewModel is not null)
        {
            if (e.ClickCount > 1)
            {
                // 双击打开详情
                _openFocusInfoView(hitViewModel.Model);
            }
            else
            {
                // 单击开始拖动
                _draggedNode = hitViewModel.Model;
                _mode = CanvasInteractionMode.DraggingNode;
                _closeFocusInfoView();

                // 如果点击的节点未选中，清除其他选中并选中此节点
                if (!hitViewModel.IsSelected)
                {
                    ClearSelection();
                    hitViewModel.IsSelected = true;
                    SelectionChanged?.Invoke();
                }
            }
        }
        else
        {
            // 点击空白处
            ClearSelection();
            _closeFocusInfoView();
        }
    }

    private void HandleLeftButtonUp()
    {
        if (_mode == CanvasInteractionMode.DraggingNode)
        {
            _draggedNode = null;
            _mode = CanvasInteractionMode.None;
        }
    }

    private FocusNodeViewModel? GetHitFocusNodeViewModel(Point position)
    {
        var visuals = _canvas.GetVisualsAt(position);
        foreach (var visual in visuals)
        {
            if (visual is Control { DataContext: FocusNodeViewModel viewModel })
            {
                return viewModel;
            }
        }
        return null;
    }

    private (int X, int Y) GetMousePositionOnGrid(Point mousePoint)
    {
        double rX = mousePoint.X - _viewModel.TranslateX;
        double rY = mousePoint.Y - _viewModel.TranslateY;
        double width = FocusMapConstants.CellWidth * _viewModel.Scale;
        double height = FocusMapConstants.CellHeight * _viewModel.Scale;

        int x = (int)Math.Floor(rX / width);
        int y = (int)Math.Floor(rY / height);

        return (x, y);
    }

    /// <summary>
    /// 获取右键点击时的网格坐标
    /// </summary>
    public (int X, int Y) GetRightClickGridPosition()
    {
        return GetMousePositionOnGrid(_rightButtonDownPosition);
    }

    /// <summary>
    /// 判断当前是否应该显示上下文菜单（右键没有移动）
    /// </summary>
    public bool ShouldShowContextMenu()
    {
        return _mode == CanvasInteractionMode.RightButtonPending;
    }

    #endregion
}
