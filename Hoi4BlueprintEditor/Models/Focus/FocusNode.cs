namespace Hoi4BlueprintEditor.Models.Focus;

public sealed class FocusNode
{
    public string Id { get; set; } = string.Empty;
    public List<FocusNode> MutuallyExclusive { get; } = [];
    public FocusNode? RelativePosition { get; set; }

    // TODO: 多选一/多个必选
    public List<FocusNode> Prerequisite { get; } = [];
    public Point Position { get; set; }
    public string Icon { get; set; } = string.Empty;
    public int Cost { get; set; }
}
