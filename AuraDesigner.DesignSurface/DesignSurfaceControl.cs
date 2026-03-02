using Avalonia.Controls;
using AuraDesigner.Core.Models;

namespace AuraDesigner.DesignSurface;

public class DesignSurfaceControl : Decorator
{
    private IDesignItem? _rootItem;

    /// <summary>
    /// Gets or sets the root design item to display.
    /// </summary>
    public IDesignItem? RootItem
    {
        get => _rootItem;
        set
        {
            if (_rootItem != value)
            {
                _rootItem = value;
                UpdateSurface();
            }
        }
    }

    private void UpdateSurface()
    {
        if (_rootItem?.Component is Control control)
        {
            Child = control;
        }
        else
        {
            Child = null;
        }
    }
}
