using Avalonia.Controls;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class UnitToolTipView : UserControl
{
    public UnitToolTipView()
        : this("单位名称", "这是一段描述", new TextBlock()) { }

    public UnitToolTipView(string unitName, string unitDescription, TextBlock unitModifiers)
    {
        InitializeComponent();
        NameText.Text = unitName;
        if (string.IsNullOrEmpty(unitDescription))
        {
            DescriptionText.IsVisible = false;
        }
        else
        {
            DescriptionText.Text = unitDescription;
        }
        Modifiers.Content = unitModifiers;
    }
}
