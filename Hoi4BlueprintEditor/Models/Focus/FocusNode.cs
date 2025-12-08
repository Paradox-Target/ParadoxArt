using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Messages;

namespace Hoi4BlueprintEditor.Models.Focus;

/// <summary>
/// 国策节点
/// </summary>
/// <remarks>不能充当键值, 因为 <see cref="GetHashCode"/> 值会变</remarks>
/// <param name="path">来源文件绝对路径</param>
/// <param name="type">国策类型</param>
public sealed partial class FocusNode(string path, FocusType type)
    : ObservableObject,
        IEquatable<FocusNode>,
        IDisposable
{
    [ObservableProperty]
    private string _id = string.Empty;
    public FocusType Type { get; } = type;

    /// <summary>
    /// 国策来源文件的绝对路径
    /// </summary>
    public string Path { get; } = path;

    public IReadOnlyList<FocusNode> MutuallyExclusive => _mutuallyExclusive;
    private readonly List<FocusNode> _mutuallyExclusive = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(X))]
    [NotifyPropertyChangedFor(nameof(Y))]
    private FocusNode? _relativePosition;

    /// <summary>
    /// 使用此节点作为相对位置源的节点的集合
    /// </summary>
    public IReadOnlyCollection<FocusNode> RelativePositionChildren => _relativePositionChildren;
    private readonly List<FocusNode> _relativePositionChildren = [];

    /// <summary>
    /// 前提条件
    /// </summary>
    /// <remarks>
    /// 每个项目中的集合代表一个 prerequisite 节点内容
    /// 内层 List 集合 OR 前置条件
    /// 外层 List 集合 AND 前置条件
    /// </remarks>
    public IReadOnlyList<IReadOnlyList<FocusNode>> Prerequisite => _prerequisite;

    private readonly List<List<FocusNode>> _prerequisite = [];

    /// <summary>
    /// 将本节点当作前提条件的 <see cref="FocusNode"/> 集合
    /// </summary>
    public IReadOnlyList<FocusNode> Children => _children;
    private readonly List<FocusNode> _children = [];

    /// <summary>
    /// 原始的位置, 对应脚本中的 X 与 Y 值 ,不包含相对位置的偏移, 不能代表显示位置。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(X))]
    [NotifyPropertyChangedFor(nameof(Y))]
    private FocusPoint _rawPosition = new(0, 0);

    public int X => RelativePosition is null ? RawPosition.X : RawPosition.X + RelativePosition.X;
    public int Y => RelativePosition is null ? RawPosition.Y : RawPosition.Y + RelativePosition.Y;

    [ObservableProperty]
    private string _icon = string.Empty;

    public decimal Cost { get; set; }

    public string CompletionReward { get; set; } = string.Empty;

    /// <summary>
    /// 添加互斥节点关系, 双向添加
    /// </summary>
    /// <param name="focusNode">节点</param>
    public void AddMutuallyExclusive(FocusNode focusNode)
    {
        if (!_mutuallyExclusive.Contains(focusNode))
        {
            _mutuallyExclusive.Add(focusNode);
        }
        if (!focusNode._mutuallyExclusive.Contains(this))
        {
            focusNode._mutuallyExclusive.Add(this);
        }
    }

    public void AddPrerequisite(List<FocusNode> prerequisiteNodes)
    {
        _prerequisite.Add(prerequisiteNodes);
        foreach (var node in prerequisiteNodes)
        {
            if (!node._children.Contains(this))
            {
                node._children.Add(this);
            }
        }
    }

    public void RemovePrerequisite(FocusNode focusNode)
    {
        InternalRemovePrerequisite(focusNode, true);
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.InternalRemovePrerequisite(this, false);
        }
        _children.Clear();
    }

    private void InternalRemovePrerequisite(FocusNode focusNode, bool removeFromChildren)
    {
        foreach (var prerequisiteGroup in _prerequisite)
        {
            if (!prerequisiteGroup.Remove(focusNode))
            {
                continue;
            }

            if (removeFromChildren)
            {
                focusNode._children.Remove(this);
            }
            if (prerequisiteGroup.Count == 0)
            {
                _prerequisite.Remove(prerequisiteGroup);
            }
            break;
        }
    }

    public void ClearPrerequisites()
    {
        foreach (var group in _prerequisite)
        {
            foreach (var node in group)
            {
                node._children.Remove(this);
            }
        }
        _prerequisite.Clear();
    }

    /// <summary>
    /// 清除使用此节点作为相对位置的所有节点的相对位置设置
    /// </summary>
    public void ClearRelativePositionChildren()
    {
        foreach (var child in _relativePositionChildren.ToArray())
        {
            child.ConvertToAbsolutePosition();
        }
        _relativePositionChildren.Clear();
    }

    public void ClearMutuallyExclusive()
    {
        foreach (var node in _mutuallyExclusive)
        {
            node._mutuallyExclusive.Remove(this);
        }
        _mutuallyExclusive.Clear();
    }

    /// <summary>
    /// 将 <c>RawPosition.X</c> 设置为指定值，自动扣除 <see cref="RelativePosition"/> 的偏移
    /// </summary>
    /// <param name="x"></param>
    public void SetRawX(int x)
    {
        int offsetX = RelativePosition?.X ?? 0;
        var newPosition = new FocusPoint(x - offsetX, RawPosition.Y);
        if (newPosition != RawPosition)
        {
            RawPosition = newPosition;
            RedrawFocusConnectionLinesIfNeed();
        }
    }

    /// <summary>
    /// 将 <c>RawPosition.Y</c> 设置为指定值，自动扣除 <see cref="RelativePosition"/> 的偏移
    /// </summary>
    /// <param name="y"></param>
    public void SetRawY(int y)
    {
        int offsetY = RelativePosition?.Y ?? 0;
        var newPosition = new FocusPoint(RawPosition.X, y - offsetY);
        if (newPosition != RawPosition)
        {
            RawPosition = newPosition;
            RedrawFocusConnectionLinesIfNeed();
        }
    }

    /// <summary>
    /// 将 <c>RawPosition.X</c> 和 <c>RawPosition.Y</c> 设置为指定值，自动扣除 <see cref="RelativePosition"/> 的偏移
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void SetRawPosition(int x, int y)
    {
        int offsetX = RelativePosition?.X ?? 0;
        int offsetY = RelativePosition?.Y ?? 0;
        var newPosition = new FocusPoint(x - offsetX, y - offsetY);
        if (newPosition != RawPosition)
        {
            RawPosition = newPosition;
            RedrawFocusConnectionLinesIfNeed();
        }
    }

    private void RedrawFocusConnectionLinesIfNeed()
    {
        if (
            RelativePositionChildren.IsNotEmpty
            || MutuallyExclusive.IsNotEmpty
            || Prerequisite.IsNotEmpty
            || Children.IsNotEmpty
        )
        {
            WeakReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);
        }
    }

    /// <summary>
    /// 相对位置转换为绝对位置, 并清除 <see cref="RelativePosition"/> 设置
    /// </summary>
    public void ConvertToAbsolutePosition()
    {
        if (RelativePosition is null)
        {
            return;
        }

        // 注意这里不能直接赋值 RawPosition，因为会发送多余的消息
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        _rawPosition = new FocusPoint(X, Y);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        RelativePosition = null;
    }

    /// <summary>
    /// 检查是否会形成循环引用
    /// </summary>
    /// <param name="targetNode">目标相对位置节点</param>
    /// <returns>如果会形成循环引用则返回 true</returns>
    private bool WouldCreateCircularReference(FocusNode targetNode)
    {
        // 检查目标节点的所有祖先节点，看是否包含当前节点
        var visited = new HashSet<FocusNode>();
        var current = targetNode;

        while (current != null)
        {
            // 如果在目标节点的祖先链中找到了当前节点，则会形成循环
            if (current == this)
            {
                return true;
            }

            // 防止无限循环（理论上不应该发生，但作为安全措施）
            if (!visited.Add(current))
            {
                // 检测到已存在的循环，但不涉及当前节点
                break;
            }

            current = current.RelativePosition;
        }

        return false;
    }

    /// <summary>
    /// 将节点转换为相对定位模式
    /// </summary>
    /// <param name="relativeNode">相对位置参考节点</param>
    /// <returns>如果转换成功返回 true，如果会导致循环引用则返回 false</returns>
    public bool ConvertToRelativePosition(FocusNode relativeNode)
    {
        if (relativeNode == this)
        {
            return false;
        }

        // 检查是否会形成循环引用
        if (WouldCreateCircularReference(relativeNode))
        {
            return false;
        }

        int offsetX = X - relativeNode.X;
        int offsetY = Y - relativeNode.Y;
        // 注意这里不能直接赋值 RawPosition，因为会发送多余的消息
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing
        _rawPosition = new FocusPoint(offsetX, offsetY);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing
        RelativePosition = relativeNode;
        return true;
    }

    partial void OnRelativePositionChanged(FocusNode? oldValue, FocusNode? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnPropertyChanged;
            oldValue._relativePositionChildren.Remove(this);
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnPropertyChanged;
            newValue._relativePositionChildren.Add(this);
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(X))
        {
            OnPropertyChanged(nameof(X));
        }
        else if (e.PropertyName == nameof(Y))
        {
            OnPropertyChanged(nameof(Y));
        }
    }

    public bool Equals(FocusNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id
            && Type == other.Type
            && Path == other.Path
            && RawPosition.Equals(other.RawPosition);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is FocusNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Id.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Type;
            hashCode = (hashCode * 397) ^ Path.GetHashCode();
            hashCode = (hashCode * 397) ^ RawPosition.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(FocusNode? left, FocusNode? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FocusNode? left, FocusNode? right)
    {
        return !Equals(left, right);
    }

    public void Dispose()
    {
        RelativePosition?.PropertyChanged -= OnPropertyChanged;
        // 越过属性，直接置空，避免触发 OnRelativePositionChanged
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        _relativePosition = null;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        ClearChildren();
        ClearPrerequisites();
        ClearRelativePositionChildren();
        ClearMutuallyExclusive();
    }

    public override string ToString()
    {
        return $"FocusNode [{Id} ({Path})]";
    }
}
