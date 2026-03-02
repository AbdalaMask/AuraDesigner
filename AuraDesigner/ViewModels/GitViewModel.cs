using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dock.Model.Mvvm.Controls;
using CommunityToolkit.Mvvm.Input;

namespace AuraDesigner.ViewModels;

public class GitFileStatus
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Modified", "Added", "Deleted", "Untracked"
}

public class GitViewModel : Tool
{
    private string _repositoryPath = string.Empty;
    private string _commitMessage = "Update XAML";
    private string _tagName = "v1.0.0";
    private bool _isGitRepository;

    public string CommitMessage
    {
        get => _commitMessage;
        set => SetProperty(ref _commitMessage, value);
    }

    public string TagName
    {
        get => _tagName;
        set => SetProperty(ref _tagName, value);
    }

    public bool IsGitRepository
    {
        get => _isGitRepository;
        set => SetProperty(ref _isGitRepository, value);
    }

    public ObservableCollection<GitFileStatus> ChangedFiles { get; } = new();

    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand CommitCommand { get; }
    public IRelayCommand TagCommand { get; }
    public IRelayCommand StageAllCommand { get; }
    public IRelayCommand PushCommand { get; }
    public IRelayCommand TagLastCommitCommand { get; }
    public IRelayCommand SaveAndSyncCommand { get; }

    public GitViewModel()
    {
        Id = "Git";
        Title = "Git Changes";
        RefreshCommand = new AsyncRelayCommand(RefreshStatusAsync);
        CommitCommand = new AsyncRelayCommand(CommitChangesAsync);
        TagCommand = new AsyncRelayCommand(CreateTagAsync);
        StageAllCommand = new AsyncRelayCommand(StageAllAsync);
        PushCommand = new AsyncRelayCommand(PushAsync);
        TagLastCommitCommand = new AsyncRelayCommand(TagLastCommitAsync);
        SaveAndSyncCommand = new AsyncRelayCommand(SaveAndSyncAsync);
    }

    public void SetRepositoryPath(string path)
    {
        _repositoryPath = path;
        _ = RefreshStatusAsync();
    }

    public async Task RefreshStatusAsync()
    {
        if (string.IsNullOrEmpty(_repositoryPath)) return;

        try
        {
            var output = await RunGitCommandAsync("status --porcelain");
            IsGitRepository = true;
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ChangedFiles.Clear();
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Length < 3) continue;

                    var statusChar = line.Substring(0, 2).Trim();
                    var relativePath = line.Substring(3).Trim();
                    
                    ChangedFiles.Add(new GitFileStatus
                    {
                        FileName = Path.GetFileName(relativePath),
                        FullPath = Path.Combine(_repositoryPath, relativePath),
                        Status = statusChar switch
                        {
                            "M" => "Modified",
                            "A" => "Added",
                            "D" => "Deleted",
                            "??" => "Untracked",
                            _ => statusChar
                        }
                    });
                }
            });
        }
        catch (Exception ex)
        {
            IsGitRepository = false;
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ChangedFiles.Clear());
            Debug.WriteLine($"Git status error: {ex.Message}");
        }
    }

    public async Task CommitChangesAsync()
    {
        if (!IsGitRepository || string.IsNullOrEmpty(_repositoryPath) || string.IsNullOrEmpty(CommitMessage)) return;

        try
        {
            await RunGitCommandAsync("add .");
            await RunGitCommandAsync($"commit -m \"{CommitMessage}\"");
            CommitMessage = "Update";
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Git commit error: {ex.Message}");
        }
    }

    public async Task CreateTagAsync()
    {
        if (!IsGitRepository || string.IsNullOrEmpty(_repositoryPath) || string.IsNullOrEmpty(TagName)) return;

        try
        {
            await RunGitCommandAsync($"tag {TagName}");
            Debug.WriteLine($"Created tag: {TagName}");
            // Optional: push tag
            // await RunGitCommandAsync($"push origin {TagName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Git tag error: {ex.Message}");
        }
    }

    public async Task StageAllAsync()
    {
        if (!IsGitRepository || string.IsNullOrEmpty(_repositoryPath)) return;
        try
        {
            await RunGitCommandAsync("add .");
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Git stage all error: {ex.Message}");
        }
    }

    public async Task PushAsync()
    {
        if (!IsGitRepository || string.IsNullOrEmpty(_repositoryPath)) return;
        try
        {
            await RunGitCommandAsync("push");
            Debug.WriteLine("Git push successful");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Git push error: {ex.Message}");
        }
    }

    public async Task TagLastCommitAsync()
    {
        if (!IsGitRepository || string.IsNullOrEmpty(_repositoryPath) || string.IsNullOrEmpty(TagName)) return;
        try
        {
            // Just use the existing tag logic but explicitly for the last modification logic
            await CreateTagAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Git tag last error: {ex.Message}");
        }
    }

    public async Task SaveAndSyncAsync()
    {
        if (!IsGitRepository || string.IsNullOrEmpty(_repositoryPath)) return;
        try
        {
            await RunGitCommandAsync("add .");
            await RunGitCommandAsync($"commit -m \"Auto-save: {DateTime.Now}\"");
            await RunGitCommandAsync("push");
            await RefreshStatusAsync();
            Debug.WriteLine("Save & Sync successful");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Git save and sync error: {ex.Message}");
        }
    }

    private Task<string> RunGitCommandAsync(string args)
    {
        return Task.Run(() =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = _repositoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new Exception($"Git command failed: {error}");
            }

            return output;
        });
    }
}
