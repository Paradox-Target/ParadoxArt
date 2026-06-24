using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using MessagePipe;

namespace Hoi4BlueprintBuilder.Core.Messages;

public sealed class CreateNewFocusMessageHandler(TabViewService tabViewService)
    : IAsyncRequestHandler<CreateNewFocusMessage, FocusNode>
{
    public ValueTask<FocusNode> InvokeAsync(
        CreateNewFocusMessage request,
        CancellationToken cancellationToken = default
    )
    {
        if (tabViewService.CurrentItem is not FocusTreeEditorView editorView)
        {
            throw new InvalidOperationException("当前没有活动的国策树编辑器");
        }

        return new ValueTask<FocusNode>(editorView.ViewModel.CreateNewFocusAsync(request));
    }
}
