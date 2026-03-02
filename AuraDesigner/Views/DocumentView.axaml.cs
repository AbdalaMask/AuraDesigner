using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AuraDesigner.DesignSurface;
using AuraDesigner.Adorners;
using AuraDesigner.Core.Models;
using AuraDesigner.ViewModels;
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
            _adornerLayer.ItemManipulated += (s, e) => UpdateXamlView();
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
                        if (DataContext is DocumentViewModel docVM)
                        {
                            docVM.RootItem = newRootItem;
                        }
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
        if (e.Data.Get("AuraDesignerItem") is string typeName)
        {
            var controlType = System.Type.GetType(typeName);
            if (controlType != null && System.Activator.CreateInstance(controlType) is Control newControl)
            {
                var pos = e.GetPosition(_surface);

                // Set default styles/content so items like Button or Rectangle have visibility
                if (double.IsNaN(newControl.Width)) newControl.Width = 100;
                if (double.IsNaN(newControl.Height)) newControl.Height = 30;
                if (newControl is Button btn) btn.Content = controlType.Name;
                if (newControl is TextBlock tb) tb.Text = "Text";
                if (newControl is TextBox tx) tx.Text = "Text";

                // Setup raw XElement to back this control
                System.Xml.Linq.XNamespace ns = "https://github.com/avaloniaui";
                var newNode = new System.Xml.Linq.XElement(ns + controlType.Name);
                newNode.SetAttributeValue("Width", 100);
                newNode.SetAttributeValue("Height", 30);
                if (newControl is Button) newNode.SetAttributeValue("Content", controlType.Name);
                if (newControl is TextBlock || newControl is TextBox) newNode.SetAttributeValue("Text", "Text");

                var newItem = new DesignItem(newControl) { Name = controlType.Name, XmlNode = newNode };

                if (_surface?.RootItem == null)
                {
                    // Dropped on an empty document, it becomes the Root!
                    _surface.RootItem = newItem;
                }
                else
                {
                    var parentControl = _surface.RootItem.Component as Control;
                    
                    if (parentControl is Panel rootPanel)
                    {
                        if (rootPanel is Canvas)
                        {
                            Canvas.SetLeft(newControl, pos.X);
                            Canvas.SetTop(newControl, pos.Y);
                            newNode.SetAttributeValue("Canvas.Left", pos.X);
                            newNode.SetAttributeValue("Canvas.Top", pos.Y);
                        }
                        else
                        {
                            newControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                            newControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                            newControl.Margin = new Avalonia.Thickness(pos.X, pos.Y, 0, 0);
                            newNode.SetAttributeValue("HorizontalAlignment", "Left");
                            newNode.SetAttributeValue("VerticalAlignment", "Top");
                            newNode.SetAttributeValue("Margin", $"{pos.X},{pos.Y},0,0");
                        }
                        rootPanel.Children.Add(newControl);
                        ((DesignItem)_surface.RootItem).AddChild(newItem);
                        ((DesignItem)_surface.RootItem).XmlNode?.Add(newNode);
                    }
                    else if (parentControl is ContentControl contentControl)
                    {
                        contentControl.Content = newControl;
                        ((DesignItem)_surface.RootItem).AddChild(newItem);
                        ((DesignItem)_surface.RootItem).XmlNode?.Add(newNode);
                    }
                }

                // Run Xaml Sync
                UpdateXamlView();
                
                if (DataContext is DocumentViewModel docVM)
                {
                    docVM.RootItem = _surface.RootItem;
                }
            }
        }
        e.Handled = true;
    }

    private void UpdateXamlView()
    {
        var editor = this.FindControl<AvaloniaEdit.TextEditor>("XamlEditor");
        if (editor != null && _surface?.RootItem is DesignItem rootItem && rootItem.XmlNode != null)
        {
            SyncVisualToXml(rootItem);
            editor.Text = AuraDesigner.Core.XamlGenerator.Generate(editor.Text, _surface.RootItem);
        }
    }

    private void SyncVisualToXml(DesignItem item)
    {
        if (item.Component is Control control && item.XmlNode != null)
        {
            // Sync positional and dimensional bounds that could have been modified by the Adorner layer.
            if (!double.IsNaN(control.Width)) item.XmlNode.SetAttributeValue("Width", System.Math.Round(control.Width, 1));
            if (!double.IsNaN(control.Height)) item.XmlNode.SetAttributeValue("Height", System.Math.Round(control.Height, 1));
            
            var left = Canvas.GetLeft(control);
            if (!double.IsNaN(left)) item.XmlNode.SetAttributeValue(System.Xml.Linq.XNamespace.None + "Canvas.Left", System.Math.Round(left, 1));
            
            var top = Canvas.GetTop(control);
            if (!double.IsNaN(top)) item.XmlNode.SetAttributeValue(System.Xml.Linq.XNamespace.None + "Canvas.Top", System.Math.Round(top, 1));
            
            // Sync Margin for Grid/StackPanel positioning
            if (control.Margin != Avalonia.Thickness.Parse("0")) item.XmlNode.SetAttributeValue("Margin", control.Margin.ToString());
        }

        foreach (var child in item.Children.OfType<DesignItem>())
        {
            SyncVisualToXml(child);
        }
    }
    private IDesignItem? FindDesignItemForControl(IDesignItem root, object component)
    {
        if (root.Component == component) return root;
        foreach (var child in root.Children)
        {
            var found = FindDesignItemForControl(child, component);
            if (found != null) return found;
        }
        return null;
    }

    private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_surface?.RootItem == null || _adornerLayer == null) return;

        var pointerElement = e.Source as Avalonia.Visual;
        
        IDesignItem? hitItem = null;
        Avalonia.Visual? current = pointerElement;

        // Walk up the visual tree to find an element that corresponds to an IDesignItem
        while (current != null)
        {
            hitItem = FindDesignItemForControl(_surface.RootItem, current);
            if (hitItem != null)
            {
                // To avoid selecting the root canvas unless we explicitly clicked its background
                if (hitItem == _surface.RootItem && pointerElement != _surface.RootItem.Component)
                {
                    // Keep walking up if we haven't found a child control explicitly
                }
                else
                {
                    break;
                }
            }
            current = current.GetVisualParent();
        }

        if (hitItem != null)
        {
            _adornerLayer.SelectItem(hitItem);
            SelectionService.SelectedItem = hitItem;
            e.Handled = true;
        }
    }
}
