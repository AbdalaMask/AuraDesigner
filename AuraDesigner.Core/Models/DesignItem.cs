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

    public DesignItem(object component, Type? type = null)
    {
        Component = component ?? throw new ArgumentNullException(nameof(component));
        ComponentType = type ?? component.GetType();
    }

    public DesignItem(object component, XElement xmlNode, Type? type = null) : this(component, type)
    {
        XmlNode = xmlNode;
    }

    public void SetProperty(string name, string value)
    {
        // 1. Update the XML node for serialization fidelity
        XmlNode?.SetAttributeValue(name, value);

        // 2. Update the live component via Reflection
        var designProperty = ComponentType.GetProperty(name);
        if (designProperty != null && designProperty.CanWrite)
        {
            try
            {
                var convertedValue = PropertyConverter.ConvertValue(value, designProperty.PropertyType);
                
                // Try to set on the actual visual instance if the property exists there too
                // We MUST use the instance's own property info to avoid TargetException
                var instanceProperty = Component.GetType().GetProperty(name);
                if (instanceProperty != null && instanceProperty.CanWrite)
                {
                    try 
                    { 
                        // Only set if we have a valid converted value
                        if (convertedValue != null)
                        {
                            instanceProperty.SetValue(Component, convertedValue);
                        }
                    }
                    catch (Exception ex) 
                    { 
                        System.Diagnostics.Debug.WriteLine($"Error setting visual property {name} on {Component.GetType().Name}: {ex.Message}"); 
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error preparing property {name}: {ex.Message}");
            }
        }
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
