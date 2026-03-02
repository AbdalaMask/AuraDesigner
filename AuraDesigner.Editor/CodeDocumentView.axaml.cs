using Avalonia.Controls;
using AvaloniaEdit;
using System;
using System.IO;

namespace AuraDesigner.Editor;

public partial class CodeDocumentView : UserControl
{
    public CodeDocumentView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        // We use dynamic (or just checking Document Id) to avoid cyclical dependency to Main app ViewModels
        if (DataContext != null)
        {
            var editor = this.FindControl<TextEditor>("CodeEditor");
            var idProp = DataContext.GetType().GetProperty("Id");
            if (editor != null && idProp != null)
            {
                var id = idProp.GetValue(DataContext) as string;
                if (!string.IsNullOrEmpty(id) && File.Exists(id))
                {
                    editor.Text = File.ReadAllText(id);
                    
                    var extension = Path.GetExtension(id);
                    var highlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(extension);
                    if (highlighting != null)
                    {
                        editor.SyntaxHighlighting = highlighting;
                    }
                }
            }
        }
    }
}
