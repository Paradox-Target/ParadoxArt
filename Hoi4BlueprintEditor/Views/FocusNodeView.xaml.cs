using System.Windows.Controls;
using System.Windows.Input;

namespace Hoi4BlueprintEditor.Views;

/// <summary>
/// Interaction logic for FocusNodeView.xaml
/// </summary>
public partial class FocusNodeView : UserControl
{
    public FocusNodeView()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }
}