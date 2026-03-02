using System;
using System.Text;
using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class OutputViewModel : Tool
{
    public static OutputViewModel? Instance { get; private set; }

    private StringBuilder _logContent = new StringBuilder();
    private string _displayText = string.Empty;

    public string DisplayText
    {
        get => _displayText;
        set => SetProperty(ref _displayText, value);
    }

    public OutputViewModel()
    {
        Id = "Output";
        Title = "Output";
        Instance = this;
        
        Log("AuraDesigner Built-in Output initialized.");
    }

    public void Log(string message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _logContent.AppendLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
            DisplayText = _logContent.ToString();
        });
    }

    public void Clear()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _logContent.Clear();
            DisplayText = string.Empty;
        });
    }
}
