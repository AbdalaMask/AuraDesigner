using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class ProjectTemplate
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Languages { get; set; } = string.Empty;
    
    public override string ToString() => Name;
}

public class NewProjectViewModel : ViewModelBase
{
    private string _projectName = "MyAvaloniaApp";
    private string _projectLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos");
    private string _selectedLanguage = "C#";
    private ProjectTemplate? _selectedTemplate;
    private bool _isBusy;
    private bool _isConfiguring;

    public string ProjectName
    {
        get => _projectName;
        set { _projectName = value; OnPropertyChanged(); }
    }

    public string ProjectLocation
    {
        get => _projectLocation;
        set { _projectLocation = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> AvailableLanguages { get; } = new() { "C#", "F#" };

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ProjectTemplate> Templates { get; } = new();

    public ProjectTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set { _selectedTemplate = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public bool IsConfiguring
    {
        get => _isConfiguring;
        set { _isConfiguring = value; OnPropertyChanged(); }
    }

    public System.Action<string?>? RequestClose;

    public NewProjectViewModel()
    {
        _ = LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync()
    {
        IsBusy = true;
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "new list avalonia",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            ParseTemplates(output);
            
            if (Templates.Count > 0)
                SelectedTemplate = Templates[0];
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to load templates: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ParseTemplates(string output)
    {
        Templates.Clear();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool contentStarted = false;
        
        // Find indices
        int shortNameIdx = -1;
        int languageIdx = -1;

        foreach (var line in lines)
        {
            if (line.StartsWith("Template Name") && line.Contains("Short Name"))
            {
                shortNameIdx = line.IndexOf("Short Name");
                languageIdx = line.IndexOf("Language");
                continue;
            }
            if (line.StartsWith("-------"))
            {
                contentStarted = true;
                continue;
            }

            if (contentStarted && shortNameIdx > 0)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                string name = line.Substring(0, shortNameIdx).Trim();
                string shortName = languageIdx > shortNameIdx 
                    ? line.Substring(shortNameIdx, languageIdx - shortNameIdx).Trim() 
                    : line.Substring(shortNameIdx).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                
                string languages = "";
                if (languageIdx > 0 && line.Length > languageIdx)
                {
                    string rest = line.Substring(languageIdx);
                    languages = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                }

                Templates.Add(new ProjectTemplate
                {
                    Name = name,
                    ShortName = shortName,
                    Languages = languages
                });
            }
        }
    }

    public async Task<string?> GenerateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(ProjectLocation) || SelectedTemplate == null)
            return null;

        IsBusy = true;
        try
        {
            if (!Directory.Exists(ProjectLocation))
                Directory.CreateDirectory(ProjectLocation);

            // Command: dotnet new avalonia.app -n MyAvaloniaApp -o "C:\path" -lang "C#"
            var langArg = SelectedLanguage == "C#" ? "C#" : "F#";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"new {SelectedTemplate.ShortName} -n \"{ProjectName}\" -o \"{Path.Combine(ProjectLocation, ProjectName)}\" -lang \"{langArg}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var fullDir = Path.Combine(ProjectLocation, ProjectName);
                
                // Prioritize finding .sln first, then .csproj / .fsproj
                string? projFile = null;
                var slns = Directory.GetFiles(fullDir, "*.sln");
                if (slns.Length > 0) projFile = slns[0];
                else
                {
                    var projs = Directory.GetFiles(fullDir, "*.*sproj");
                    if (projs.Length > 0) projFile = projs[0];
                }
                
                return projFile ?? fullDir;
            }
            else
            {
                Debug.WriteLine($"Failed to generate project. Output: {output}. Error: {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error generating project: " + ex.Message);
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async void BrowseCommand()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel != null)
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Select Project Location",
                    AllowMultiple = false
                });

                if (folders != null && folders.Count > 0)
                {
                    ProjectLocation = folders[0].Path.LocalPath;
                }
            }
        }
    }

    public void CancelCommand()
    {
        RequestClose?.Invoke(null);
    }

    public void NextCommand()
    {
        if (SelectedTemplate != null)
        {
            IsConfiguring = true;
        }
    }

    public void BackCommand()
    {
        IsConfiguring = false;
    }

    public async void CreateCommand()
    {
        var projectFile = await GenerateProjectAsync();
        RequestClose?.Invoke(projectFile);
    }
}
