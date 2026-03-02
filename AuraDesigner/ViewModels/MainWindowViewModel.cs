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

        // Hook up XAML Parser logging to the new Tool Windows
        AuraDesigner.Core.XamlParser.LogMessage = (msg) =>
        {
            OutputViewModel.Instance?.Log(msg);
        };

        AuraDesigner.Core.XamlParser.LogError = (msg, code, file, line) =>
        {
            ErrorListViewModel.Instance?.AddError(msg, code, file, line);
        };
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
                    Title = "Open Solution or Project File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Solution Files (*.sln)") { Patterns = new[] { "*.sln" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("C# Project Files (*.csproj)") { Patterns = new[] { "*.csproj" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("Avalonia XAML (*.axaml)") { Patterns = new[] { "*.axaml" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("All Files (*.*)") { Patterns = new[] { "*.*" } }
                    }
                });

                if (files != null && files.Count > 0)
                {
                    var filePath = files[0].Path.LocalPath;
                    System.Console.WriteLine($"Opened: {filePath}");

                    // Route this down directly to the SolutionExplorer in the Factory
                    if (Factory is AuraDockFactory docFactory && docFactory.SolutionExplorer != null)
                    {
                        docFactory.SolutionExplorer.LoadProject(filePath);
                    }
                }
            }
        }
    }

    public async void NewProjectCommand()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var window = new AuraDesigner.Views.NewProjectWindow();
            var result = await window.ShowDialog<string?>(desktop.MainWindow);

            if (!string.IsNullOrEmpty(result))
            {
                // Route this down directly to the SolutionExplorer in the Factory
                if (Factory is AuraDockFactory docFactory && docFactory.SolutionExplorer != null)
                {
                    docFactory.SolutionExplorer.LoadProject(result);
                }
            }
        }
    }
}
