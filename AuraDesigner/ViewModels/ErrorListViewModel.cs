using System;
using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class ErrorListItem
{
    public string Type { get; set; } = "Error"; // "Error", "Warning", "Message"
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string State { get; set; } = "Active";
}

public class ErrorListViewModel : Tool
{
    public static ErrorListViewModel? Instance { get; private set; }

    public ObservableCollection<ErrorListItem> Errors { get; } = new ObservableCollection<ErrorListItem>();

    public ErrorListViewModel()
    {
        Id = "ErrorList";
        Title = "Error List";
        Instance = this;
    }

    public void AddError(string description, string code = "", string file = "", int line = 0, string type = "Error")
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Errors.Add(new ErrorListItem
            {
                Type = type,
                Code = code,
                Description = description,
                Project = "AuraDesigner Instance",
                File = file,
                Line = line
            });
        });
    }
    
    public void Clear()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Errors.Clear();
        });
    }
}
