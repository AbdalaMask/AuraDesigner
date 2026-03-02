using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AuraDesigner.Views;

public partial class ToolboxView : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;

    public ToolboxView()
    {
        InitializeComponent();

        var listBox = this.FindControl<ListBox>("ControlList");
        if (listBox != null)
        {
            listBox.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            listBox.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            listBox.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStartPoint = e.GetPosition(this);
            _isDragging = true;
        }
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var point = e.GetPosition(this);
        var diff = _dragStartPoint - point;

        if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3)
        {
            _isDragging = false;
            var listBox = this.FindControl<ListBox>("ControlList");
            if (listBox?.SelectedItem is Type controlType)
            {
                var dragData = new DataObject();
                // Instead of putting a Type object, put the AssemblyQualifiedName
                dragData.Set("AuraDesignerItem", controlType.AssemblyQualifiedName ?? controlType.FullName);
                await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }
}
