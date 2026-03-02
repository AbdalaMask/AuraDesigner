using System;
using System.Collections.ObjectModel;
using System.Linq;
using Dock.Model.Mvvm.Controls;
using AuraDesigner.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuraDesigner.ViewModels;

public class LogicalTreeViewModel : Tool
{
    private IDesignItem? _rootItem;
    private IDesignItem? _selectedItem;
    private bool _isInternalSelectionChange;

    public ObservableCollection<IDesignItem> RootNodes { get; } = new();

    public IDesignItem? RootItem
    {
        get => _rootItem;
        set
        {
            if (SetProperty(ref _rootItem, value))
            {
                RootNodes.Clear();
                if (value != null)
                {
                    RootNodes.Add(value);
                }
            }
        }
    }

    public IDesignItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                if (!_isInternalSelectionChange)
                {
                    SelectionService.SelectedItem = value;
                }
            }
        }
    }

    public LogicalTreeViewModel()
    {
        Id = "LogicalTree";
        Title = "Logical Tree";
        SelectionService.SelectedItemChanged += SelectionService_SelectedItemChanged;
    }

    private void SelectionService_SelectedItemChanged(object? sender, IDesignItem? e)
    {
        _isInternalSelectionChange = true;
        SelectedItem = e;
        _isInternalSelectionChange = false;
    }

    public void Sync(IDesignItem? root)
    {
        RootItem = root;
    }
}
