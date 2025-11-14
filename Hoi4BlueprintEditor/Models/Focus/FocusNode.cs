using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintEditor.Models.Focus;

public sealed partial class FocusNode(string path, FocusType type) : ObservableObject, IEquatable<FocusNode>
{
    [ObservableProperty]
    private string _id = string.Empty;
    public FocusType Type { get; } = type;

    /// <summary>
    /// 国策来源文件的绝对路径
    /// </summary>
    public string Path { get; } = path;
    public List<FocusNode> MutuallyExclusive { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(X))]
    [NotifyPropertyChangedFor(nameof(Y))]
    private FocusNode? _relativePosition;

    /// <summary>
    /// 前提条件
    /// </summary>
    /// <remarks>
    /// 每个项目中的集合代表一个 prerequisite 节点内容
    /// </remarks>
    public List<List<FocusNode>> Prerequisite { get; } = [];

    /// <summary>
    /// 原始的位置，不包含相对位置的偏移, 不能代表显示位置。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(X))]
    [NotifyPropertyChangedFor(nameof(Y))]
    private Point _rawPosition = new(0, 0);

    public int X => RelativePosition is null ? RawPosition.X : RawPosition.X + RelativePosition.X;
    public int Y => RelativePosition is null ? RawPosition.Y : RawPosition.Y + RelativePosition.Y;

    [ObservableProperty]
    private string _icon = string.Empty;
    public decimal Cost { get; set; }

    /// <summary>
    /// 将 <c>RawPosition.X</c> 设置为指定值，自动扣除 <see cref="RelativePosition"/> 的偏移
    /// </summary>
    /// <param name="x"></param>
    public void SetRawX(int x)
    {
        int offsetX = RelativePosition?.X ?? 0;
        RawPosition = new Point(x - offsetX, RawPosition.Y);
    }

    /// <summary>
    /// 将 <c>RawPosition.Y</c> 设置为指定值，自动扣除 <see cref="RelativePosition"/> 的偏移
    /// </summary>
    /// <param name="y"></param>
    public void SetRawY(int y)
    {
        int offsetY = RelativePosition?.Y ?? 0;
        RawPosition = new Point(RawPosition.X, y - offsetY);
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
        RawPosition = new Point(x - offsetX, y - offsetY);
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
}
