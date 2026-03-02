using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AuraDesigner.Core.Models;

public interface IDesignItem
{
    string Name { get; set; }
    Type ComponentType { get; }
    object Component { get; }
    object Content { get; }
    IDesignItem? Parent { get; }
    IEnumerable<IDesignItem> Children { get; }
    XElement? XmlNode { get; set; }
    void SetProperty(string name, string value);
}
