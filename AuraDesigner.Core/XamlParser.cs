using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
            LogMessage?.Invoke("Starting Granular XAML parse...");
            var doc = XDocument.Parse(xaml);
            return ParseElement(doc.Root, null);
        }
        catch (System.Xml.XmlException xmlEx)
        {
            LogError?.Invoke(xmlEx.Message, "XML001", "DocumentView.axaml", xmlEx.LineNumber);
            LogMessage?.Invoke($"XML Syntax Error: {xmlEx.Message}");
        }
        catch (Exception ex)
        {
            LogError?.Invoke(ex.Message, "XAML001", "DocumentView.axaml", 0);
            LogMessage?.Invoke($"XAML Parser Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"XAML Parse Error: {ex}");
        }
        return null;
    }

    private static IDesignItem? ParseElement(XElement? element, IDesignItem? parent)
    {
        if (element == null) return null;

        var typeName = element.Name.LocalName;
        if (typeName.Contains(".")) return null;

        var type = ResolveType(typeName);
        if (type == null)
        {
            LogMessage?.Invoke($"Warning: Type '{typeName}' not found.");
            return null;
        }

        // --- PHASE 16: Skip non-visual types ---
        if (!typeof(Control).IsAssignableFrom(type) && !typeof(Window).IsAssignableFrom(type))
        {
            // We still return null so it's not added to the visual tree, 
            // but we might want to store it in Resources later.
            return null;
        }

        try
        {
            object? instance = null;
            Type designType = type;

            // Check if this is a top-level control that shouldn't be instantiated/added as child
            if (typeof(Window).IsAssignableFrom(type))
            {
                instance = new ContentControl();
                LogMessage?.Invoke($"Info: Substituting top-level control '{typeName}' with ContentControl for design surface.");
            }
            else
            {
                instance = Activator.CreateInstance(type);
            }

            if (instance is not Control control) return null;

            var designItem = new DesignItem(control, element, designType);
            control.IsHitTestVisible = false;

            ApplyProperties(designItem, element);

            foreach (var childElement in element.Elements())
            {
                // Skip property elements (e.g. <Button.Background>) as they are handled in ApplyProperties
                if (childElement.Name.LocalName.Contains(".")) continue;

                var childItem = ParseElement(childElement, designItem);
                if (childItem != null)
                {
                    designItem.AddChild(childItem);

                    // Basic Visual Parenting
                    if (control is Panel panel && childItem.Component is Control childControl)
                    {
                        panel.Children.Add(childControl);
                    }
                    else if (control is ContentControl cc)
                    {
                        cc.Content = childItem.Component;
                    }
                }
            }

            return designItem;
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"Error creating '{typeName}': {ex.Message}");
            return null;
        }
    }

    private static void ApplyProperties(IDesignItem item, XElement element)
    {
        var control = (Control)item.Content;

        foreach (var attr in element.Attributes())
        {
            if (attr.Name.NamespaceName == XamlNamespace.NamespaceName || attr.IsNamespaceDeclaration) continue;

            var name = attr.Name.LocalName;
            var value = attr.Value;

            // --- PHASE 16: Binding Detection ---
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                // For now, we update the XML node but don't try to resolve the binding on the live visual
                // to avoid corruption/exceptions in the designer.
                item.XmlNode?.SetAttributeValue(name, value);
                continue; 
            }

            if (name.Contains("."))
                HandleAttachedProperty(control, name, value);
            else
                item.SetProperty(name, value);
        }

        foreach (var propEl in element.Elements())
        {
            if (propEl.Name.LocalName.StartsWith(element.Name.LocalName + "."))
            {
                HandlePropertyElement(item, propEl);
            }
        }
    }

    private static void HandlePropertyElement(IDesignItem item, XElement propertyElement)
    {
        var propertyName = propertyElement.Name.LocalName.Split('.').Last();
        
        if (!propertyElement.HasElements)
        {
            var value = propertyElement.Value;
            // --- PHASE 16: Binding Detection in Property Elements ---
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                item.XmlNode?.Element(propertyElement.Name)?.SetValue(value);
                return;
            }
            item.SetProperty(propertyName, value);
        }
        else
        {
            foreach (var childNode in propertyElement.Elements())
            {
                var childDesignItem = ParseElement(childNode, item);
                if (childDesignItem != null)
                {
                    var property = item.ComponentType.GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            property.SetValue(item.Component, childDesignItem.Component);
                        }
                        catch (Exception ex)
                        {
                            LogMessage?.Invoke($"Warning: Failed to assign property element content: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    private static void HandleAttachedProperty(Control control, string fullName, string value)
    {
        var parts = fullName.Split('.');
        if (parts.Length != 2) return;

        var ownerType = ResolveType(parts[0]);
        if (ownerType == null) return;

        var propField = ownerType.GetField(parts[1] + "Property", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
        if (propField?.GetValue(null) is AvaloniaProperty ap)
        {
            try { control.SetValue(ap, ConvertValue(value, ap.PropertyType)); }
            catch { /* Ignore conversion errors */ }
        }
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(double)) return double.Parse(value);
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType == typeof(Thickness)) return Thickness.Parse(value);
        if (targetType == typeof(Color)) return Color.Parse(value);

        // --- PHASE 16: Brush/Media Handling ---
        if (typeof(IBrush).IsAssignableFrom(targetType))
        {
            try { return Brush.Parse(value); } catch { return Brushes.Transparent; }
        }

        var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        return (converter != null && converter.CanConvertFrom(typeof(string))) ? converter.ConvertFromInvariantString(value) : value;
    }

    private static readonly Dictionary<string, Type?> TypeCache = new();

    private static Type? ResolveType(string typeName)
    {
        if (TypeCache.TryGetValue(typeName, out var cachedType)) return cachedType;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name;
            if (name != null && (name.StartsWith("Avalonia") || name == "AuraDesigner"))
            {
                // Try common namespaces
                var type = assembly.GetType("Avalonia.Controls." + typeName) 
                        ?? assembly.GetType("Avalonia." + typeName)
                        ?? assembly.GetType("Avalonia.Media." + typeName)
                        ?? assembly.GetType("Avalonia.Styling." + typeName)
                        ?? assembly.GetType("Avalonia.Animation." + typeName);

                if (type == null) type = System.Linq.Enumerable.FirstOrDefault(assembly.GetTypes(), t => t.Name == typeName);
                
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
}
