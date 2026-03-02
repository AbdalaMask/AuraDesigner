using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AuraDesigner.Core.Models;

namespace AuraDesigner.Adorners;

public class SelectionAdorner : Control
{
    public IDesignItem TargetItem { get; }
    private Control? TargetControl => TargetItem.Component as Control;

    public SelectionAdorner(IDesignItem item)
    {
        TargetItem = item ?? throw new ArgumentNullException(nameof(item));
        IsHitTestVisible = false; // Allow clicking through the border. Resize thumbs would be true.
    }

    public void UpdateBounds(Visual adornerLayer)
    {
        if (TargetControl != null && TargetControl.IsVisible)
        {
            var transform = TargetControl.TransformToVisual(adornerLayer);
            if (transform.HasValue)
            {
                var targetBounds = new Rect(TargetControl.Bounds.Size);
                var adornerBounds = targetBounds.TransformToAABB(transform.Value);
                Width = adornerBounds.Width;
                Height = adornerBounds.Height;
                Canvas.SetLeft(this, adornerBounds.Left);
                Canvas.SetTop(this, adornerBounds.Top);
                IsVisible = true;
                return;
            }
        }
        IsVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // Draw a pleasant blue selection border
        var pen = new Pen(new SolidColorBrush(Color.Parse("#007ACC")), 2);
        context.DrawRectangle(null, pen, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }
}
