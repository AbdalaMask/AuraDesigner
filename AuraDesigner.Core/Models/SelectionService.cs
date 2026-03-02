using System;

namespace AuraDesigner.Core.Models;

/// <summary>
/// A global service to track and broadcast the currently selected item in the designer.
/// </summary>
public static class SelectionService
{
    private static IDesignItem? _selectedItem;

    /// <summary>
    /// Gets or sets the currently selected designer item and fires the SelectedItemChanged event.
    /// </summary>
    public static IDesignItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                SelectedItemChanged?.Invoke(null, value);
            }
        }
    }

    /// <summary>
    /// Event fired whenever the selected item changes. 
    /// Listeners (like PropertiesViewModel) subscribe to this to update their UI.
    /// </summary>
    public static event EventHandler<IDesignItem?>? SelectedItemChanged;
}
