using Hoi4BlueprintBuilder.Core.Models.Focus;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class FocusNodeHelper
{
    public static Node CreateAstNodeFromEditorModel(FocusNode editorModel)
    {
        var children = new List<Child>(16)
        {
            ChildHelper.LeafString("id", editorModel.Id),
            ChildHelper.Leaf("x", editorModel.RawPosition.X),
            ChildHelper.Leaf("y", editorModel.RawPosition.Y),
            ChildHelper.Leaf(Keywords.Cost, editorModel.Cost),
            ChildHelper.Leaf(Keywords.CancelIfInvalid, editorModel.CancelIfInvalid),
            ChildHelper.Leaf(Keywords.ContinueIfInvalid, editorModel.ContinueIfInvalid)
        };

        if (!string.IsNullOrWhiteSpace(editorModel.Icon))
        {
            children.Add(ChildHelper.LeafString(Keywords.Icon, editorModel.Icon));
        }

        if (editorModel.RelativePosition is not null)
        {
            children.Add(
                ChildHelper.LeafString(Keywords.RelativePositionId, editorModel.RelativePosition.Id)
            );
        }

        NodeHelper.AddCompletionRewardToChildrenIfExist(children, editorModel);
        NodeHelper.AddMutuallyExclusiveToChildrenIfExist(children, editorModel);
        NodeHelper.AddPrerequisiteToChildrenIfExist(children, editorModel);

        string key = editorModel.Type == FocusType.Shared ? Keywords.SharedFocus : Keywords.Focus;
        var focusNode = new Node(key) { AllArray = children.ToArray() };
        return focusNode;
    }
}
