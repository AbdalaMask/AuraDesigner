using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AuraDesigner.Core.Models;

public class DesignItem : IDesignItem
{
    public string Name { get; set; } = string.Empty;
    public Type ComponentType { get; }
    public object Component { get; }
    public IDesignItem? Parent { get; set; }
    public XElement? XmlNode { get; set; }
    
    private readonly List<IDesignItem> _children = new();
    public IEnumerable<IDesignItem> Children => _children;

    public DesignItem(object component)
    {
        Component = component ?? throw new ArgumentNullException(nameof(component));
        ComponentType = component.GetType();
    }

    public void AddChild(DesignItem child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public void RemoveChild(DesignItem child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
        }
    }
}
