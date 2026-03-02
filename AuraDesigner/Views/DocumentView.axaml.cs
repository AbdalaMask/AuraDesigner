using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AuraDesigner.DesignSurface;
using AuraDesigner.Adorners;
using AuraDesigner.Core.Models;
using System.Linq;

namespace AuraDesigner.Views;

public partial class DocumentView : UserControl
{
    private DesignSurfaceControl? _surface;
    private DesignAdornerLayer? _adornerLayer;

    public DocumentView()
    {
        InitializeComponent();

        _surface = this.FindControl<DesignSurfaceControl>("DesignSurface");
        _adornerLayer = this.FindControl<DesignAdornerLayer>("AdornerLayer");

        if (_surface != null && _adornerLayer != null)
        {
            // It's vital we use Tunnel so we catch all clicks before the controls consume them
            _surface.AddHandler(PointerPressedEvent, OnSurfacePointerPressed, RoutingStrategies.Tunnel);
        }

        var designerBorder = this.FindControl<Border>("DesignerBorder");
        if (designerBorder != null)
        {
            DragDrop.SetAllowDrop(designerBorder, true);
            designerBorder.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            designerBorder.AddHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains("AuraDesignerItem") ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Get("AuraDesignerItem") is string typeName && _surface?.RootItem?.Component is Canvas rootCanvas)
        {
            var controlType = System.Type.GetType(typeName);
            if (controlType != null && System.Activator.CreateInstance(controlType) is Control newControl)
            {
                var pos = e.GetPosition(rootCanvas);
                Canvas.SetLeft(newControl, pos.X);
                Canvas.SetTop(newControl, pos.Y);
                
                // Set default styles/content so items like Button or Rectangle have visibility
                if (newControl is Button btn) btn.Content = "New " + controlType.Name;
                if (newControl is TextBox tx) tx.Text = "Text";
                if (double.IsNaN(newControl.Width)) newControl.Width = 100;
                if (double.IsNaN(newControl.Height)) newControl.Height = 30;

                rootCanvas.Children.Add(newControl);
                var item = new DesignItem(newControl) { Name = controlType.Name };
                ((DesignItem)_surface.RootItem).AddChild(item);

                // Run Xaml Sync
                UpdateXamlView();
            }
        }
        e.Handled = true;
    }

    private void UpdateXamlView()
    {
        var editor = this.FindControl<AvaloniaEdit.TextEditor>("XamlEditor");
        if (editor != null && _surface?.RootItem != null)
        {
            editor.Text = AuraDesigner.Core.XamlGenerator.Generate(_surface.RootItem);
        }
    }

    private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_surface?.RootItem?.Component is Canvas rootCanvas && _adornerLayer != null)
        {
            var pointerElement = e.Source as Avalonia.Visual;
            
            // Did we click the canvas background?
            if (pointerElement == rootCanvas)
            {
                _adornerLayer.SelectItem(_surface.RootItem);
                return;
            }

            // Find the clicked child by walking up the visual tree
            var child = pointerElement;
            while (child != null && child.GetVisualParent() != rootCanvas)
            {
                child = child.GetVisualParent();
            }

            if (child != null && child.GetVisualParent() == rootCanvas)
            {
                // Find matching design item
                var designItem = _surface.RootItem.Children.FirstOrDefault(c => c.Component == child);
                if (designItem != null)
                {
                    _adornerLayer.SelectItem(designItem);
                    // Crucial: Handled=true so the child button doesn't swallow the click!
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}
