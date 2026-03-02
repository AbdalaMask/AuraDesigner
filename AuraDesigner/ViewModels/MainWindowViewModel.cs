using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private IFactory? _factory;
    private IRootDock? _layout;

    public IFactory? Factory
    {
        get => _factory;
        set
        {
            if (_factory != value)
            {
                _factory = value;
                OnPropertyChanged();
            }
        }
    }

    public IRootDock? Layout
    {
        get => _layout;
        set
        {
            if (_layout != value)
            {
                _layout = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel()
    {
        Factory = new AuraDockFactory();
        Layout = Factory?.CreateLayout();
        if (Layout != null)
        {
            Factory?.InitLayout(Layout);
        }
    }

    public async void OpenProjectCommand()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel != null)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Open Solution, Project, or XAML File",
                    AllowMultiple = false
                });

                if (files != null && files.Count > 0)
                {
                    var filePath = files[0].Path.LocalPath;
                    System.Console.WriteLine($"Opened: {filePath}");

                    // Route this down to the AuraDockFactory or SolutionExplorerViewModel
                    if (Factory is AuraDockFactory docFactory && docFactory.ContextLocator != null)
                    {
                        if (docFactory.ContextLocator.TryGetValue("SolutionPanel", out var loc) && loc() is IRootDock layout && layout.ActiveDockable is ProportionalDock prop)
                        {
                            // Because of the factory's setup, let's just find the ViewModel from the known instance we created.
                            // A cleaner way is to keep a reference to it in the Factory
                        }

                        // Since Dock.Avalonia layout trees can be deep, let's just do a simple recursive search for the ID
                        var sevm = FindDockable<SolutionExplorerViewModel>(Layout, "SolutionExplorerView");
                        if (sevm != null)
                        {
                            sevm.LoadProject(filePath);
                        }
                    }
                }
            }
        }
    }

    private T? FindDockable<T>(IDockable? current, string id) where T : class, IDockable
    {
        if (current == null) return null;
        if (current.Id == id && current is T typed) return typed;

        if (current is IDock dock && dock.VisibleDockables != null)
        {
            foreach (var child in dock.VisibleDockables)
            {
                var found = FindDockable<T>(child, id);
                if (found != null) return found;
            }
        }
        return null;
    }
}
