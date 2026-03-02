using Avalonia.Controls;
using AuraDesigner.Core.Models;
using System.Text;
using System.Xml.Linq;

namespace AuraDesigner.Core;

public static class XamlGenerator
{
    private static readonly XNamespace DefaultNs = "https://github.com/avaloniaui";
    private static readonly XNamespace XNs = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static string Generate(IDesignItem rootItem)
    {
        if (rootItem == null || rootItem.Component == null) return string.Empty;
        
        var xmlElement = GenerateElement(rootItem);
        xmlElement.Add(new XAttribute(XNamespace.Xmlns + "x", XNs.NamespaceName));

        return xmlElement.ToString();
    }

    private static XElement GenerateElement(IDesignItem item)
    {
        var type = item.Component.GetType();
        var node = new XElement(DefaultNs + type.Name);

        if (item.Component is Control control)
        {
            // Position
            var left = Canvas.GetLeft(control);
            if (!double.IsNaN(left) && left > 0) node.SetAttributeValue("Canvas.Left", left);

            var top = Canvas.GetTop(control);
            if (!double.IsNaN(top) && top > 0) node.SetAttributeValue("Canvas.Top", top);

            // Bounds
            if (!double.IsNaN(control.Width)) node.SetAttributeValue("Width", control.Width);
            if (!double.IsNaN(control.Height)) node.SetAttributeValue("Height", control.Height);

            // Margin & Padding
            if (control.Margin != Avalonia.Thickness.Parse("0")) node.SetAttributeValue("Margin", control.Margin.ToString());
            
            var paddingProp = control.GetType().GetProperty("Padding");
            if (paddingProp != null && paddingProp.GetValue(control) is Avalonia.Thickness padding && padding != Avalonia.Thickness.Parse("0"))
            {
                node.SetAttributeValue("Padding", padding.ToString());
            }

            // Alignments
            if (control.HorizontalAlignment != Avalonia.Layout.HorizontalAlignment.Stretch)
                node.SetAttributeValue("HorizontalAlignment", control.HorizontalAlignment.ToString());
            
            if (control.VerticalAlignment != Avalonia.Layout.VerticalAlignment.Stretch)
                node.SetAttributeValue("VerticalAlignment", control.VerticalAlignment.ToString());

            // Grid Attributes
            int gridCol = Grid.GetColumn(control);
            if (gridCol > 0) node.SetAttributeValue("Grid.Column", gridCol);

            int gridRow = Grid.GetRow(control);
            if (gridRow > 0) node.SetAttributeValue("Grid.Row", gridRow);

            // Specific Properties
            if (control is ContentControl contentControl && contentControl.Content is string s)
            {
                node.SetAttributeValue("Content", s);
            }
            else if (control is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
            {
                node.SetAttributeValue("Text", textBlock.Text);
            }
            else if (control is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                node.SetAttributeValue("Text", textBox.Text);
            }

            // Nested Properties (like RenderTransform)
            if (control.RenderTransform != null)
            {
                var transformType = control.RenderTransform.GetType();
                var transformNode = new XElement(DefaultNs + type.Name + ".RenderTransform");
                var actualTransformNode = new XElement(DefaultNs + transformType.Name);
                
                // Currently only handling Angle for RotateTransform as an MVP based on user snippet
                var angleProp = transformType.GetProperty("Angle");
                if (angleProp != null)
                {
                    actualTransformNode.SetAttributeValue("Angle", angleProp.GetValue(control.RenderTransform));
                }
                
                transformNode.Add(actualTransformNode);
                node.Add(transformNode);
            }
        }

        foreach (var child in item.Children)
        {
            node.Add(GenerateElement(child));
        }

        return node;
    }
}
