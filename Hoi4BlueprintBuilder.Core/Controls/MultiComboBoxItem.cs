using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class MultiComboBoxItem : ContentControl
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<
        MultiComboBoxItem,
        bool
    >(nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private MultiComboBox? _parent;
    private bool _updateInternal;

    static MultiComboBoxItem()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBoxItem>(true);
        IsSelectedProperty.Changed.AddClassHandler<MultiComboBoxItem>(
            (item, args) => item.OnSelectionChanged(args)
        );
    }

    public MultiComboBoxItem()
    {
        UpdatePseudoClasses(IsSelected);
    }

    private void OnSelectionChanged(AvaloniaPropertyChangedEventArgs args)
    {
        var isSelected = args.GetNewValue<bool>();
        UpdatePseudoClasses(isSelected);

        if (_updateInternal)
        {
            return;
        }

        var parent = this.FindLogicalAncestorOfType<MultiComboBox>();
        var dataContext = DataContext;

        if (parent?.SelectedItems is not null && dataContext is not null)
        {
            if (isSelected)
            {
                if (!parent.SelectedItems.Contains(dataContext))
                {
                    parent.SelectedItems.Add(dataContext);
                }
            }
            else
            {
                parent.SelectedItems.Remove(dataContext);
            }
        }
    }

    private void UpdatePseudoClasses(bool isSelected)
    {
        PseudoClasses.Set(":selected", isSelected);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _parent = this.FindLogicalAncestorOfType<MultiComboBox>();

        if (
            IsSelected
            && _parent?.SelectedItems is not null
            && DataContext is not null
            && !_parent.SelectedItems.Contains(DataContext)
        )
        {
            _parent.SelectedItems.Add(this.DataContext);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            SetCurrentValue(IsSelectedProperty, !IsSelected);
            e.Handled = true;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateSelection();
    }

    internal void UpdateSelection()
    {
        _updateInternal = true;
        if (_parent is not null && DataContext is not null)
        {
            SetCurrentValue(IsSelectedProperty, _parent.SelectedItems?.Contains(DataContext) ?? false);
        }
        _updateInternal = false;
    }
}
