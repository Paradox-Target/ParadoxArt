using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Hoi4BlueprintBuilder.Core.Controls;

[TemplatePart(PART_BackgroundBorder, typeof(Border))]
[PseudoClasses(PC_DropDownOpen, PC_Empty)]
public sealed class MultiComboBox : SelectingItemsControl
{
    public const string PART_BackgroundBorder = "PART_BackgroundBorder";
    public const string PC_DropDownOpen = ":dropdownopen";
    public const string PC_Empty = ":empty";

    private Border? _rootBorder;

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        ComboBox.IsDropDownOpenProperty.AddOwner<MultiComboBox>();

    public static readonly StyledProperty<double> MaxDropDownHeightProperty = AvaloniaProperty.Register<
        MultiComboBox,
        double
    >(nameof(MaxDropDownHeight), 400);

    public static new readonly StyledProperty<IList?> SelectedItemsProperty = AvaloniaProperty.Register<
        MultiComboBox,
        IList?
    >(nameof(SelectedItems));

    public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
        AvaloniaProperty.Register<MultiComboBox, IDataTemplate?>(nameof(SelectedItemTemplate));

    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<MultiComboBox>();

    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    public new IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public IDataTemplate? SelectedItemTemplate
    {
        get => GetValue(SelectedItemTemplateProperty);
        set => SetValue(SelectedItemTemplateProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public ICommand RemoveItemCommand { get; }

    static MultiComboBox()
    {
        FocusableProperty.OverrideDefaultValue<MultiComboBox>(true);
        IsDropDownOpenProperty.Changed.AddClassHandler<MultiComboBox>(
            (box, args) => box.PseudoClasses.Set(PC_DropDownOpen, args.GetNewValue<bool>())
        );
        SelectedItemsProperty.Changed.AddClassHandler<MultiComboBox>(
            (box, args) => box.OnSelectedItemsChanged(args)
        );
    }

    public MultiComboBox()
    {
        SelectedItems = new AvaloniaList<object>();
        RemoveItemCommand = new RemoveCommandImpl(this);
        UpdateEmptyPseudoClass();
    }

    private void OnSelectedItemsChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.OldValue is INotifyCollectionChanged oldList)
        {
            oldList.CollectionChanged -= OnSelectedItemsCollectionChanged;
        }

        if (args.NewValue is INotifyCollectionChanged newList)
        {
            newList.CollectionChanged += OnSelectedItemsCollectionChanged;
        }

        UpdateEmptyPseudoClass();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyPseudoClass();
        var containers = Presenter?.Panel?.Children;
        if (containers == null)
        {
            return;
        }

        foreach (var container in containers)
        {
            if (container is MultiComboBoxItem item)
            {
                item.UpdateSelection();
            }
        }
    }

    private void UpdateEmptyPseudoClass()
    {
        PseudoClasses.Set(PC_Empty, SelectedItems == null || SelectedItems.Count == 0);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = item;
        return item is not MultiComboBoxItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new MultiComboBoxItem();
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (item is MultiComboBoxItem containerItem)
        {
            container.DataContext = containerItem.Content;
            return;
        }

        container.DataContext = item;
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is MultiComboBoxItem mcbItem)
        {
            mcbItem.UpdateSelection();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_rootBorder != null)
        {
            _rootBorder.RemoveHandler(PointerPressedEvent, OnBackgroundPointerPressed);
        }

        _rootBorder = e.NameScope.Find<Border>(PART_BackgroundBorder);

        if (_rootBorder != null)
        {
            _rootBorder.AddHandler(
                PointerPressedEvent,
                OnBackgroundPointerPressed,
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel
            );
        }

        UpdateEmptyPseudoClass();
    }

    private void OnBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
        e.Handled = true;
    }

    public void Remove(object? item)
    {
        if (item != null)
        {
            SelectedItems?.Remove(item);
        }
    }

    private sealed class RemoveCommandImpl(MultiComboBox parent) : ICommand
    {
        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            parent.Remove(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
