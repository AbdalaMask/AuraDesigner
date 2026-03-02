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
        }

        foreach (var child in item.Children)
        {
            node.Add(GenerateElement(child));
        }

        return node;
    }
}
