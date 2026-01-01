namespace Hoi4BlueprintBuilder.Core.Models;

/// <summary>
/// 画布交互模式枚举
/// </summary>
public enum CanvasInteractionMode
{
    /// <summary>
    /// 空闲状态
    /// </summary>
    None,

    /// <summary>
    /// 拖动画布
    /// </summary>
    Panning,

    /// <summary>
    /// 拖动国策节点
    /// </summary>
    DraggingNode,

    /// <summary>
    /// 框选节点
    /// </summary>
    BoxSelecting,

    /// <summary>
    /// 连接模式（设置前置、互斥、相对位置）
    /// </summary>
    Connecting,

    /// <summary>
    /// 右键按下等待判定（移动则框选，不移动则菜单）
    /// </summary>
    RightButtonPending
}
