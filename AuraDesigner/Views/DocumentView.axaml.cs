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
            _adornerLayer.AdornerUpdated += (s, e) => UpdateXamlView();
        }

        var designerBorder = this.FindControl<Border>("DesignerBorder");
        if (designerBorder != null)
        {
            DragDrop.SetAllowDrop(designerBorder, true);
            designerBorder.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            designerBorder.AddHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext != null)
        {
            var editor = this.FindControl<AvaloniaEdit.TextEditor>("XamlEditor");
            var idProp = DataContext.GetType().GetProperty("Id");
            if (editor != null && idProp != null)
            {
                var id = idProp.GetValue(DataContext) as string;
                if (!string.IsNullOrEmpty(id) && System.IO.File.Exists(id))
                {
                    editor.Text = System.IO.File.ReadAllText(id);
                    var highlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("XML");
                    if (highlighting != null)
                    {
                        editor.SyntaxHighlighting = highlighting;
                    }
                    
                    var newRootItem = AuraDesigner.Core.XamlParser.Parse(editor.Text);
                    if (newRootItem != null && _surface != null)
                    {
                        _surface.RootItem = newRootItem;
                    }
                }
            }
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
                
                // Set default styles/content so items like Button or Rectangle have visibility
                if (newControl is Button btn) btn.Content = "New " + controlType.Name;
                if (newControl is TextBox tx) tx.Text = "Text";
                
                // Auto-wrap root controls that aren't Panels
                if (!_surface.RootItem.Children.Any() && !(newControl is Panel))
                {
                    var wrapperGrid = new Grid { Width = 800, Height = 450, Background = Avalonia.Media.Brushes.Transparent };
                    if (double.IsNaN(newControl.Width)) newControl.Width = 100;
                    if (double.IsNaN(newControl.Height)) newControl.Height = 30;
                    newControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                    newControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                    
                    wrapperGrid.Children.Add(newControl);
                    rootCanvas.Children.Add(wrapperGrid);
                    
                    var wrapperItem = new DesignItem(wrapperGrid) { Name = "RootGrid" };
                    var childItem = new DesignItem(newControl) { Name = controlType.Name };
                    wrapperItem.AddChild(childItem);
                    ((DesignItem)_surface.RootItem).AddChild(wrapperItem);
                }
                else
                {
                    Canvas.SetLeft(newControl, pos.X);
                    Canvas.SetTop(newControl, pos.Y);
                    if (double.IsNaN(newControl.Width)) newControl.Width = 100;
                    if (double.IsNaN(newControl.Height)) newControl.Height = 30;

                    rootCanvas.Children.Add(newControl);
                    var item = new DesignItem(newControl) { Name = controlType.Name };
                    ((DesignItem)_surface.RootItem).AddChild(item);
                }

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
