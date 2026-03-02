using System;
using System.Text;
using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class PackageManagerConsoleViewModel : Tool
{
    private string _consoleText = "Package Manager Console Host Version 1.0\r\nType 'help' to get help.\r\n\r\nPM> ";

    public string ConsoleText
    {
        get => _consoleText;
        set => SetProperty(ref _consoleText, value);
    }

    public PackageManagerConsoleViewModel()
    {
        Id = "PackageManagerConsole";
        Title = "Package Manager Console";
    }
}
