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

    public override IRootDock CreateLayout()
    {
        var toolboxView = new ToolDock
        {
            Id = "Toolbox",
            Title = "Toolbox",
            Proportion = 0.2,
            ActiveDockable = new ToolboxViewModel { Id = "ToolboxPanel", Title = "Toolbox" }
        };

        var solutionView = new ToolDock
        {
            Id = "SolutionExplorer",
            Title = "Solution Explorer",
            Proportion = 0.5,
            ActiveDockable = new SolutionExplorerViewModel { Id = "SolutionPanel", Title = "Solution Explorer" }
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

        var document1 = new DocumentViewModel
        {
            Id = "MainWindow.axaml",
            Title = "MainWindow.axaml"
        };

        _documentDock = new DocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = document1,
            VisibleDockables = CreateList<IDockable>(document1),
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

        var rootDock = CreateRootDock();
        rootDock.IsCollapsable = false;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        _rootDock = rootDock;
        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object>>
        {
            ["ToolboxPanel"] = () => layout,
            ["SolutionPanel"] = () => layout,
            ["PropertiesPanel"] = () => layout,
            ["MainWindow.axaml"] = () => layout
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}
