using Avalonia.Controls;
using AuraDesigner.Core.Models;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;

namespace AuraDesigner.Core;

public static class XamlParser
{
    private static readonly Assembly _avaloniaControlsAssembly = typeof(Control).Assembly;

    public static IDesignItem? Parse(string xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml)) return null;

        try
        {
            var doc = XDocument.Parse(xaml);
            if (doc.Root != null)
            {
                return BuildDesignTree(doc.Root);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"XAML Parse Error: {ex.Message}");
        }

        return null;
    }

    private static IDesignItem? BuildDesignTree(XElement element)
    {
        // Ignore Window/UserControl root tags for the visual designer for now, 
        // or map them to a root Canvas/Panel if necessary.
        string typeName = element.Name.LocalName;

        if (typeName == "Window" || typeName == "UserControl")
        {
             // Designers usually show the *content* of the window
             var firstChild = element.Elements().FirstOrDefault();
             if (firstChild != null)
             {
                 return BuildDesignTree(firstChild);
             }
             return null;
        }

        var controlType = _avaloniaControlsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == typeName && typeof(Control).IsAssignableFrom(t));

        if (controlType == null) return null;

        var control = Activator.CreateInstance(controlType) as Control;
        if (control == null) return null;

        var item = new DesignItem(control) { Name = element.Attribute("Name")?.Value ?? typeName };

        // Parse Standard Attributes
        if (double.TryParse(element.Attribute(XName.Get("Width", ""))?.Value, out double w)) control.Width = w;
        if (double.TryParse(element.Attribute(XName.Get("Height", ""))?.Value, out double h)) control.Height = h;
        
        var canvasLeft = element.Attribute(XName.Get("Left", "Canvas"));
        if (canvasLeft == null) canvasLeft = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Canvas.Left");
        if (canvasLeft != null && double.TryParse(canvasLeft.Value, out double left)) Canvas.SetLeft(control, left);

        var canvasTop = element.Attribute(XName.Get("Top", "Canvas"));
        if (canvasTop == null) canvasTop = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Canvas.Top");
        if (canvasTop != null && double.TryParse(canvasTop.Value, out double top)) Canvas.SetTop(control, top);

        // Properties like Text and Content
        if (control is TextBlock tb) tb.Text = element.Attribute("Text")?.Value ?? "";
        if (control is TextBox tx) tx.Text = element.Attribute("Text")?.Value ?? "";
        
        if (control is ContentControl cc)
        {
            var contentAttr = element.Attribute("Content")?.Value;
            if (contentAttr != null) cc.Content = contentAttr;
        }

        // Build Children
        foreach (var childNode in element.Elements())
        {
            var childItem = BuildDesignTree(childNode);
            if (childItem != null)
            {
                item.AddChild((DesignItem)childItem);
                
                // Physically add to Avalonia visual tree
                if (control is Panel panel && childItem.Component is Control childControl)
                {
                    panel.Children.Add(childControl);
                }
                else if (control is ContentControl contentControl && childItem.Component is Control contentChildControl)
                {
                   // Do not override text content if elements exist
                   contentControl.Content = contentChildControl;
                }
                 else if (control is Decorator decorator && childItem.Component is Control decoratorChildControl)
                {
                    decorator.Child = decoratorChildControl;
                }
            }
        }

        return item;
    }
}
