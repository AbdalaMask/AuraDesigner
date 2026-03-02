using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Data;
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

        // --- PHASE 16: Allow non-visual types for property elements/resources ---
        // We will filter during parenting instead.

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

            if (instance == null) return null;

            var designItem = new DesignItem(instance, element, designType);
            if (instance is Control control)
            {
                control.IsHitTestVisible = false;
            }

            ApplyProperties(designItem, element);

            foreach (var childElement in element.Elements())
            {
                var localName = childElement.Name.LocalName;

                // Handle Resources collection
                if (localName.EndsWith(".Resources"))
                {
                    HandleResources(instance, childElement);
                    continue;
                }

                // Skip property elements (e.g. <Button.Background>) as they are handled in ApplyProperties
                if (localName.Contains(".")) continue;

                var childItem = ParseElement(childElement, designItem);
                if (childItem != null)
                {
                    designItem.AddChild(childItem);

                    // Basic Visual Parenting - ONLY for Controls
                    if (designItem.Component is Panel panel && childItem.Component is Control childControl)
                    {
                        panel.Children.Add(childControl);
                    }
                    else if (designItem.Component is ContentControl cc && childItem.Component is Control ccChild)
                    {
                        cc.Content = ccChild;
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
        var instance = item.Component;

        foreach (var attr in element.Attributes())
        {
            if (attr.Name.NamespaceName == XamlNamespace.NamespaceName || attr.IsNamespaceDeclaration) continue;

            var name = attr.Name.LocalName;
            var value = attr.Value;

            // --- PHASE 16: Binding Detection ---
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                if (HandleMarkupExtension(instance, name, value))
                    continue;
            }

            if (name.Contains("."))
            {
                if (instance is Control control)
                    HandleAttachedProperty(control, name, value);
            }
            else
            {
                item.SetProperty(name, value);
            }
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
                if (HandleMarkupExtension(item.Component, propertyName, value))
                {
                    item.XmlNode?.Element(propertyElement.Name)?.SetValue(value);
                    return;
                }
            }
            item.SetProperty(propertyName, value);
        }
        else
        {
            var property = item.ComponentType.GetProperty(propertyName);
            if (property == null) return;

            foreach (var childNode in propertyElement.Elements())
            {
                var childDesignItem = ParseElement(childNode, item);
                if (childDesignItem != null)
                {
                    try
                    {
                        var propValue = property.GetValue(item.Component);
                        
                        // Handle Collections (e.g. GradientStops, Children, etc.)
                        if (propValue is System.Collections.IList list)
                        {
                            list.Add(childDesignItem.Component);
                        }
                        else if (property.CanWrite)
                        {
                            property.SetValue(item.Component, childDesignItem.Component);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"Warning: Failed to assign property element content to '{propertyName}': {ex.Message}");
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
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                HandleMarkupExtension(control, ap, value);
                return;
            }

            try { control.SetValue(ap, ConvertValue(value, ap.PropertyType)); }
            catch { /* Ignore conversion errors */ }
        }
    }

    private static bool HandleMarkupExtension(object instance, string propertyName, string markup)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName);
        if (property == null) return false;

        // Try to find the AvaloniaProperty equivalent for Bind()
        var apField = type.GetField(propertyName + "Property", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
        var ap = apField?.GetValue(null) as AvaloniaProperty;

        if (ap != null && instance is AvaloniaObject ao)
        {
            return HandleMarkupExtension(ao, ap, markup);
        }

        return false;
    }

    private static bool HandleMarkupExtension(AvaloniaObject instance, AvaloniaProperty property, string markup)
    {
        markup = markup.Trim('{', '}');
        var parts = markup.Split(' ', 2);
        var extensionType = parts[0];
        var args = parts.Length > 1 ? parts[1] : "";

        if (extensionType == "Binding" || extensionType == "CompiledBinding")
        {
            var binding = new Binding();
            // Simple path parsing
            if (!string.IsNullOrEmpty(args) && !args.Contains("="))
            {
                binding.Path = args;
            }
            else if (!string.IsNullOrEmpty(args))
            {
                // Handle properties like ElementName=input
                var argParts = args.Split(',');
                foreach (var argPart in argParts)
                {
                    var kv = argPart.Trim().Split('=');
                    if (kv.Length == 2)
                    {
                        var key = kv[0].Trim();
                        var val = kv[1].Trim();
                        if (key == "ElementName") binding.ElementName = val;
                        else if (key == "Path") binding.Path = val;
                    }
                }
            }
            instance.Bind(property, binding);
            return true;
        }
        else if (extensionType == "StaticResource" || extensionType == "DynamicResource")
        {
            var resourceKey = args.Trim();
            if (extensionType == "StaticResource")
            {
                // Only IResourceHost supports Resource lookup
                if (instance is IResourceHost host && host.TryFindResource(resourceKey, out var res))
                {
                    instance.SetValue(property, res);
                }
            }
            else
            {
                if (instance is IResourceHost host)
                {
                    instance.Bind(property, host.GetResourceObservable(resourceKey).ToBinding());
                }
            }
            return true;
        }

        return false;
    }

    private static void HandleResources(object instance, XElement resourcesElement)
    {
        // Many Avalonia objects (Control, Style, App) have a Resources property
        // but no common interface exposes it. Use reflection.
        var prop = instance.GetType().GetProperty("Resources", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var resources = prop?.GetValue(instance) as System.Collections.IDictionary;
        if (resources == null) return;

        foreach (var resourceNode in resourcesElement.Elements())
        {
            var key = resourceNode.Attribute(XamlNamespace + "Key")?.Value ?? resourceNode.Attribute("Key")?.Value;
            if (string.IsNullOrEmpty(key)) continue;

            var type = ResolveType(resourceNode.Name.LocalName);
            if (type != null)
            {
                try
                {
                    var value = PropertyConverter.ConvertValue(resourceNode.Value, type);
                    if (value == null && resourceNode.HasElements)
                    {
                        // Handle more complex resources later
                    }
                    else
                    {
                        resources[key] = value;
                    }
                }
                catch { }
            }
        }
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        return PropertyConverter.ConvertValue(value, targetType);
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
                        ?? assembly.GetType("Avalonia.Controls.Shapes." + typeName)
                        ?? assembly.GetType("Avalonia.Controls.Primitives." + typeName)
                        ?? assembly.GetType("Avalonia." + typeName)
                        ?? assembly.GetType("Avalonia.Media." + typeName)
                        ?? assembly.GetType("Avalonia.Media.Imaging." + typeName)
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
