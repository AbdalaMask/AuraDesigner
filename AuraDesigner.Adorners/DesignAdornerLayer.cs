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
        // Removed Background = Brushes.Transparent;
        // Background must be null so it allows pointer events to pass through 
        // to the DesignSurfaceControl behind it. The Thumbs will still be hit-testable.
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
            _currentSelection.ManipulationCompleted += (s, e) => ItemManipulated?.Invoke(this, EventArgs.Empty);
            Children.Add(_currentSelection);
            UpdateAdorners();
            SelectionService.SelectedItem = item;
        }
        else
        {
            SelectionService.SelectedItem = null;
        }
    }

    public event EventHandler? ItemManipulated;

    public void UpdateAdorners()
    {
        _currentSelection?.UpdateBounds(this);
    }

    // Moving and Resizing is now handled safely by the Thumb controls inside the SelectionAdorner.
}
