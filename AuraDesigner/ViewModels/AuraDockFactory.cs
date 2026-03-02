using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class AuraDockFactory : Factory
{
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;

    public SolutionExplorerViewModel? SolutionExplorer { get; private set; }

    public override IRootDock CreateLayout()
    {
        var toolboxView = new ToolDock
        {
            Id = "Toolbox",
            Title = "Toolbox",
            Proportion = 0.2,
            ActiveDockable = new ToolboxViewModel { Id = "ToolboxPanel", Title = "Toolbox" }
        };

        var solutionExplorerVM = new SolutionExplorerViewModel { Id = "SolutionPanel", Title = "Solution Explorer" };
        solutionExplorerVM.FileOpenRequested += OnFileOpenRequested;
        SolutionExplorer = solutionExplorerVM;

        var solutionView = new ToolDock
        {
            Id = "SolutionExplorer",
            Title = "Solution Explorer",
            Proportion = 0.5,
            ActiveDockable = solutionExplorerVM
        };
        
        var propertiesView = new ToolDock
        {
            Id = "Properties",
            Title = "Properties",
            Proportion = 0.5,
            ActiveDockable = new PropertiesViewModel { Id = "PropertiesPanel", Title = "Properties" }
        };

        var rightPane = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            Proportion = 0.2,
            VisibleDockables = CreateList<IDockable>(solutionView, new ProportionalDockSplitter(), propertiesView)
        };

        _documentDock = new DocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>(),
            CanCreateDocument = true
        };

        var mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                toolboxView,
                new ProportionalDockSplitter(),
                _documentDock,
                new ProportionalDockSplitter(),
                rightPane
            )
        };

        var errorListVM = new ErrorListViewModel();
        var outputVM = new OutputViewModel();
        var pmcVM = new PackageManagerConsoleViewModel();

        var bottomPane = new ToolDock
        {
            Id = "BottomPane",
            Title = "BottomPane",
            Proportion = 0.3,
            ActiveDockable = errorListVM,
            VisibleDockables = CreateList<IDockable>(errorListVM, outputVM, pmcVM)
        };

        var rootContainer = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(
                mainLayout,
                new ProportionalDockSplitter(),
                bottomPane
            )
        };

        var rootDock = CreateRootDock();
        rootDock.IsCollapsable = false;
        rootDock.DefaultDockable = rootContainer;
        rootDock.VisibleDockables = CreateList<IDockable>(rootContainer);

        _rootDock = rootDock;
        return rootDock;
    }

    private void OnFileOpenRequested(object? sender, string fullPath)
    {
        if (_documentDock == null) return;

        var fileName = System.IO.Path.GetFileName(fullPath);
        
        // Check if already open
        foreach (var dockable in _documentDock.VisibleDockables!)
        {
            if (dockable.Id == fullPath)
            {
                _documentDock.ActiveDockable = dockable;
                return;
            }
        }

        Document newDoc;
        if (fullPath.EndsWith(".axaml", System.StringComparison.OrdinalIgnoreCase))
        {
            newDoc = new DocumentViewModel
            {
                Id = fullPath,
                Title = fileName
            };
        }
        else
        {
            newDoc = new CodeDocumentViewModel
            {
                Id = fullPath,
                Title = fileName
            };
        }

        _documentDock.VisibleDockables.Add(newDoc);
        _documentDock.ActiveDockable = newDoc;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object>>
        {
            ["ToolboxPanel"] = () => layout,
            ["SolutionPanel"] = () => layout,
            ["PropertiesPanel"] = () => layout
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
    
    public override object? GetContext(string id)
    {
        // Try precise match first
        if (ContextLocator?.TryGetValue(id, out var cLocator) == true)
        {
            return cLocator?.Invoke();
        }
        
        // Fallback for dynamic documents (the Root Dock context)
        return _rootDock;
    }
}
