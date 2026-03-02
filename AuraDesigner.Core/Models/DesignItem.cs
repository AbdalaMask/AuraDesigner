using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AuraDesigner.Core.Models;

public class DesignItem : IDesignItem
{
    public string Name { get; set; } = string.Empty;
    public Type ComponentType { get; }
    public object Component { get; }
    public object Content => Component;
    public IDesignItem? Parent { get; set; }
    public XElement? XmlNode { get; set; }
    
    private readonly List<IDesignItem> _children = new();
    public IEnumerable<IDesignItem> Children => _children;

    public DesignItem(object component)
    {
        Component = component ?? throw new ArgumentNullException(nameof(component));
        ComponentType = component.GetType();
    }

    public DesignItem(object component, XElement xmlNode) : this(component)
    {
        XmlNode = xmlNode;
    }

    public void SetProperty(string name, string value)
    {
        // 1. Update the XML node for serialization fidelity
        XmlNode?.SetAttributeValue(name, value);

        // 2. Update the live component via Reflection
        var property = ComponentType.GetProperty(name);
        if (property != null && property.CanWrite)
        {
            try
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(Component, convertedValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting property {name}: {ex.Message}");
            }
        }
    }

    private object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(double)) return double.Parse(value);
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType.IsEnum) return Enum.Parse(targetType, value);
        
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        if (converter != null && converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromInvariantString(value);
        }

        return value;
    }

    public void AddChild(IDesignItem child)
    {
        ((DesignItem)child).Parent = this;
        _children.Add(child);
    }

    public void RemoveChild(IDesignItem child)
    {
        if (_children.Remove(child))
        {
            ((DesignItem)child).Parent = null;
        }
    }
}
