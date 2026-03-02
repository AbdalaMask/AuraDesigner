using Avalonia.Controls;
using AuraDesigner.Core.Models;
using System.Text;
using System.Xml.Linq;

namespace AuraDesigner.Core;

public static class XamlGenerator
{
    private static readonly XNamespace DefaultNs = "https://github.com/avaloniaui";
    private static readonly XNamespace XNs = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static string Generate(string originalXaml, IDesignItem rootItem)
    {
        if (rootItem == null) return string.Empty;

        // If it's a completely newly scaffolded root element, we just return its XML.
        if (rootItem.XmlNode?.Document == null && rootItem.XmlNode != null)
        {
            var nodeToSerialize = new XElement(rootItem.XmlNode);
            nodeToSerialize.SetAttributeValue(XNamespace.Xmlns + "x", XNs.NamespaceName);
            return nodeToSerialize.ToString();
        }

        // Ideally, we just return the containing Document the Node intrinsically lives in!
        return rootItem.XmlNode?.Document?.ToString() ?? rootItem.XmlNode?.ToString() ?? string.Empty;
    }
}
