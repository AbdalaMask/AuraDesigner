using System;
using System.Collections.Generic;

namespace AuraDesigner.Core.Models;

public interface IDesignItem
{
    string Name { get; set; }
    Type ComponentType { get; }
    object Component { get; }
    IDesignItem? Parent { get; }
    IEnumerable<IDesignItem> Children { get; }
}
