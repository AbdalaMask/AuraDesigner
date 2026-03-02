using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using AuraDesigner.Core.Models;

namespace AuraDesigner.Core;

public static class XamlParser
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static Action<string>? LogMessage;
    public static Action<string, string, string, int>? LogError;

    public static IDesignItem? Parse(string xaml)
    {
        try
        {
            LogMessage?.Invoke("Starting XAML parse...");

            var doc = XDocument.Parse(xaml);
            var mapping = new Dictionary<XElement, string>();
            var injectedAttributes = new List<XAttribute>();

            // 1. Inject Name logic for mapping mapping
            InjectNames(doc.Root, mapping, injectedAttributes);

            // 2. Load the modified XAML
            var modifiedXaml = doc.ToString();
            var loadedObject = AvaloniaRuntimeXamlLoader.Load(modifiedXaml);
            
            // 3. Clean up the injected names from the XML DOM mapping immediately
            foreach (var attr in injectedAttributes)
            {
                attr.Remove();
            }

            if (loadedObject is Control rootControl)
            {
                // Disable hit testing so controls in the designer don't intercept clicks natively
                DisableHitTesting(rootControl);

                // 4. Build DesignItem Tree mapping Logical Tree to XML Nodes
                LogMessage?.Invoke("XAML parsed successfully into Visual Tree.");
                return BuildDesignTree(doc.Root, rootControl, mapping);
            }
        }
        catch (System.Xml.XmlException xmlEx)
        {
            LogError?.Invoke(xmlEx.Message, "XML001", "DocumentView.axaml", xmlEx.LineNumber);
            LogMessage?.Invoke($"XML Syntax Error: {xmlEx.Message}");
        }
        catch (Exception ex)
        {
            LogError?.Invoke(ex.Message, "XAML001", "DocumentView.axaml", 0);
            LogMessage?.Invoke($"XAML Runtime Loader Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"XAML Parse Error: {ex}");
        }
        return null;
    }

    private static readonly Dictionary<string, Type?> TypeCache = new();

    private static Type? ResolveType(string typeName)
    {
        if (TypeCache.TryGetValue(typeName, out var cachedType)) return cachedType;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (assembly.FullName != null && assembly.FullName.StartsWith("Avalonia"))
            {
                var type = System.Linq.Enumerable.FirstOrDefault(assembly.GetTypes(), t => t.Name == typeName);
                if (type != null)
                {
                    TypeCache[typeName] = type;
                    return type;
                }
            }
        }
        TypeCache[typeName] = null;
        return null;
    }

    private static void InjectNames(XElement? element, Dictionary<XElement, string> mapping, List<XAttribute> injectedAttributes)
    {
        if (element == null) return;

        // Skip property elements (e.g. <Button.Background>)
        if (!element.Name.LocalName.Contains("."))
        {
            var type = ResolveType(element.Name.LocalName);
            bool supportsName = type != null && typeof(Avalonia.StyledElement).IsAssignableFrom(type);

            if (supportsName)
            {
                var xNameAttr = element.Attribute(XamlNamespace + "Name");
                var nameAttr = element.Attribute("Name");
                
                string elementName;

                if (xNameAttr != null)
                {
                    elementName = xNameAttr.Value;
                }
                else if (nameAttr != null)
                {
                    elementName = nameAttr.Value;
                }
                else
                {
                    // Inject our own identifier
                    elementName = "__aura_" + Guid.NewGuid().ToString("N");
                    var newAttr = new XAttribute("Name", elementName);
                    element.Add(newAttr);
                    injectedAttributes.Add(newAttr);
                }

                mapping[element] = elementName;
            }
        }

        foreach (var child in element.Elements())
        {
            InjectNames(child, mapping, injectedAttributes);
        }
    }

    private static IDesignItem? BuildDesignTree(XElement? element, Control rootControl, Dictionary<XElement, string> mapping)
    {
        if (element == null) return null;

        if (mapping.TryGetValue(element, out var name))
        {
            // Find the control in the logical tree of the loaded root
            var control = FindControlByName(rootControl, name);
            
            if (control is Control visualControl)
            {
                var item = new DesignItem(visualControl)
                {
                    Name = visualControl.Name != null && !visualControl.Name.StartsWith("__aura_") ? visualControl.Name : element.Name.LocalName,
                    XmlNode = element // Store the ORIGINAL, clean XElement
                };

                // Recursively process children XML nodes
                foreach (var childXml in element.Elements())
                {
                    var childItem = BuildDesignTree(childXml, rootControl, mapping);
                    if (childItem != null)
                    {
                        item.AddChild((DesignItem)childItem);
                    }
                }

                return item;
            }
        }

        // Even if we couldn't map this specific visual node (maybe it wasn't a Control),
        // we should still traverse its XML children in case they contain valid Controls.
        foreach (var childXml in element.Elements())
        {
            var childItem = BuildDesignTree(childXml, rootControl, mapping);
            if (childItem != null) return childItem; // Returns the first valid mapped child as the passthrough. (Simplification for property nodes)
        }

        return null;
    }

    private static object? FindControlByName(ILogical root, string name)
    {
        if (root is Control c && c.Name == name) return c;
        if (root is Avalonia.StyledElement se && se.Name == name) return se;

        foreach (var child in root.LogicalChildren)
        {
            var found = FindControlByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void DisableHitTesting(ILogical root)
    {
        if (root is Avalonia.Input.InputElement ie)
        {
            ie.IsHitTestVisible = false;
        }

        foreach (var child in root.LogicalChildren)
        {
            DisableHitTesting(child);
        }
    }
}
