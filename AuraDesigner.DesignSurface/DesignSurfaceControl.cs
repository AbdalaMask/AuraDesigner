using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using AuraDesigner.Core.Models;

namespace AuraDesigner.DesignSurface;

public class DesignSurfaceControl : Decorator
{
    private IDesignItem? _rootItem;

    public IDesignItem? RootItem
    {
        get => _rootItem;
        set
        {
            if (_rootItem != value)
            {
                _rootItem = value;
                UpdateSurface();
            }
        }
    }

    public DesignSurfaceControl()
    {
        // Setup drop support
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // Supply a default root canvas
        var defaultCanvas = new Canvas { Background = Brushes.White, Width = 800, Height = 600 };
        RootItem = new DesignItem(defaultCanvas) { Name = "RootCanvas" };
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("AuraDesignerItem"))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Get("AuraDesignerItem") is Type controlType && RootItem?.Component is Canvas rootCanvas)
        {
            if (Activator.CreateInstance(controlType) is Control newControl)
            {
                var pos = e.GetPosition(rootCanvas);
                Canvas.SetLeft(newControl, pos.X);
                Canvas.SetTop(newControl, pos.Y);
                
                // Defaults for some controls
                if (newControl is Button btn) btn.Content = "New Button";
                if (newControl is TextBox tx) { tx.Text = "Text"; tx.Width = 100; }

                rootCanvas.Children.Add(newControl);
                var item = new DesignItem(newControl) { Name = controlType.Name };
                ((DesignItem)RootItem).AddChild(item);
            }
        }
        e.Handled = true;
    }

    private void UpdateSurface()
    {
        if (_rootItem?.Component is Window window)
        {
            // Top-level controls like Window cannot be children of other controls.
            // We host the interior of the window instead.
            var content = window.Content as Control;
            if (content != null)
            {
                // Detach from the window so we can host it in our surface
                window.Content = null;
                Child = content;
            }
        }
        else if (_rootItem?.Component is Control control)
        {
            Child = control;
        }
        else
        {
            Child = null;
        }
    }
}
