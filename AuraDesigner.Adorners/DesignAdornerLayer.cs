using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AuraDesigner.Core.Models;
using Avalonia.Media;

namespace AuraDesigner.Adorners;

public class DesignAdornerLayer : Canvas
{
    private SelectionAdorner? _currentSelection;
    private bool _isDragging;
    private Point _lastMousePosition;

    public DesignAdornerLayer()
    {
        // Must be transparent but hit testable
        Background = Brushes.Transparent;
    }

    public void SelectItem(IDesignItem? item)
    {
        if (_currentSelection != null)
        {
            Children.Remove(_currentSelection);
            _currentSelection = null;
        }

        if (item != null)
        {
            _currentSelection = new SelectionAdorner(item);
            Children.Add(_currentSelection);
            UpdateAdorners();
        }
    }

    public void UpdateAdorners()
    {
        _currentSelection?.UpdateBounds(this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_currentSelection?.TargetItem.Component is Control targetControl)
        {
            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                // Check if we clicked inside the selected item bound
                var bounds = _currentSelection.Bounds;
                if (bounds.Contains(point.Position))
                {
                    _isDragging = true;
                    _lastMousePosition = point.Position;
                    e.Pointer.Capture(this);
                    e.Handled = true;
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging && _currentSelection?.TargetItem.Component is Control targetControl)
        {
            var currentPos = e.GetCurrentPoint(this).Position;
            var delta = currentPos - _lastMousePosition;
            _lastMousePosition = currentPos;

            var left = Canvas.GetLeft(targetControl);
            var top = Canvas.GetTop(targetControl);

            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            Canvas.SetLeft(targetControl, left + delta.X);
            Canvas.SetTop(targetControl, top + delta.Y);

            UpdateAdorners();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}
