using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using AuraDesigner.Core.Models;

namespace AuraDesigner.Adorners;

public class SelectionAdorner : Canvas
{
    public IDesignItem TargetItem { get; }
    private Control? TargetControl => TargetItem.Component as Control;

    private readonly Thumb _moveThumb;
    private readonly Thumb _topLeft, _topRight, _bottomLeft, _bottomRight;
    private readonly Thumb _top, _bottom, _left, _right;

    public SelectionAdorner(IDesignItem item)
    {
        TargetItem = item ?? throw new ArgumentNullException(nameof(item));
        
        // Setup border
        var border = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.Parse("#007ACC")),
            StrokeThickness = 2,
            IsHitTestVisible = false
        };
        Children.Add(border);

        // Center Move Thumb. Must have a brush to be hit-testable in Avalonia
        _moveThumb = new Thumb 
        { 
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), // Practically transparent
            Cursor = new Cursor(StandardCursorType.SizeAll) 
        };
        _moveThumb.DragDelta += OnMoveDragDelta;
        Children.Add(_moveThumb);

        // Resize Thumbs with high Z-Index to ensure they rest above the Move Thumb
        _topLeft = CreateThumb(StandardCursorType.TopLeftCorner);
        _topRight = CreateThumb(StandardCursorType.TopRightCorner);
        _bottomLeft = CreateThumb(StandardCursorType.BottomLeftCorner);
        _bottomRight = CreateThumb(StandardCursorType.BottomRightCorner);
        _top = CreateThumb(StandardCursorType.TopSide);
        _bottom = CreateThumb(StandardCursorType.BottomSide);
        _left = CreateThumb(StandardCursorType.LeftSide);
        _right = CreateThumb(StandardCursorType.RightSide);

        _topLeft.DragDelta += (s, e) => Resize(e, -1, -1, true, true);
        _topRight.DragDelta += (s, e) => Resize(e, 1, -1, false, true);
        _bottomLeft.DragDelta += (s, e) => Resize(e, -1, 1, true, false);
        _bottomRight.DragDelta += (s, e) => Resize(e, 1, 1, false, false);
        _top.DragDelta += (s, e) => Resize(e, 0, -1, false, true);
        _bottom.DragDelta += (s, e) => Resize(e, 0, 1, false, false);
        _left.DragDelta += (s, e) => Resize(e, -1, 0, true, false);
        _right.DragDelta += (s, e) => Resize(e, 1, 0, false, false);
    }

    private Thumb CreateThumb(StandardCursorType cursor)
    {
        var thumb = new Thumb
        {
            Width = 10, Height = 10,
            Background = Brushes.White,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(cursor),
            ZIndex = 100
        };
        Children.Add(thumb);
        return thumb;
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
                
                // Inflate bounds so Thumbs (which hang over the edge by 4px) aren't clipped
                var inflatedBounds = adornerBounds.Inflate(10);
                
                Width = inflatedBounds.Width;
                Height = inflatedBounds.Height;
                Canvas.SetLeft(this, inflatedBounds.Left);
                Canvas.SetTop(this, inflatedBounds.Top);

                // Update internal components (offset by the inflation amount 10px)
                var border = (Rectangle)Children[0];
                border.Width = adornerBounds.Width;
                border.Height = adornerBounds.Height;
                Canvas.SetLeft(border, 10);
                Canvas.SetTop(border, 10);

                _moveThumb.Width = adornerBounds.Width;
                _moveThumb.Height = adornerBounds.Height;
                Canvas.SetLeft(_moveThumb, 10);
                Canvas.SetTop(_moveThumb, 10);

                // Position Thumbs (relative to the inflated space)
                double bw = adornerBounds.Width;
                double bh = adornerBounds.Height;
                SetThumbPosition(_topLeft, 10, 10);
                SetThumbPosition(_topRight, 10 + bw, 10);
                SetThumbPosition(_bottomLeft, 10, 10 + bh);
                SetThumbPosition(_bottomRight, 10 + bw, 10 + bh);
                SetThumbPosition(_top, 10 + bw / 2, 10);
                SetThumbPosition(_bottom, 10 + bw / 2, 10 + bh);
                SetThumbPosition(_left, 10, 10 + bh / 2);
                SetThumbPosition(_right, 10 + bw, 10 + bh / 2);

                IsVisible = true;
                return;
            }
        }
        IsVisible = false;
    }

    private void SetThumbPosition(Thumb thumb, double x, double y)
    {
        Canvas.SetLeft(thumb, x - (thumb.Width / 2));
        Canvas.SetTop(thumb, y - (thumb.Height / 2));
    }

    private void OnMoveDragDelta(object? sender, VectorEventArgs e)
    {
        if (TargetControl == null) return;
        
        var left = Canvas.GetLeft(TargetControl);
        var top = Canvas.GetTop(TargetControl);
        
        if (double.IsNaN(left)) left = 0;
        if (double.IsNaN(top)) top = 0;

        double newLeft = left + e.Vector.X;
        double newTop = top + e.Vector.Y;

        Canvas.SetLeft(TargetControl, newLeft);
        Canvas.SetTop(TargetControl, newTop);
        
        // Notify Parent Layer
        if (Parent is DesignAdornerLayer layer) layer.UpdateAdorners();
    }

    private void Resize(VectorEventArgs e, int xDir, int yDir, bool isLeft, bool isTop)
    {
        if (TargetControl == null) return;

        double curWidth = double.IsNaN(TargetControl.Width) ? TargetControl.Bounds.Width : TargetControl.Width;
        double curHeight = double.IsNaN(TargetControl.Height) ? TargetControl.Bounds.Height : TargetControl.Height;
        
        double newWidth = Math.Max(10, curWidth + (e.Vector.X * xDir));
        double newHeight = Math.Max(10, curHeight + (e.Vector.Y * yDir));

        TargetControl.Width = newWidth;
        TargetControl.Height = newHeight;

        if (isLeft)
        {
            var left = Canvas.GetLeft(TargetControl);
            if (double.IsNaN(left)) left = 0;
            double newLeft = left - (newWidth - curWidth);
            Canvas.SetLeft(TargetControl, newLeft);
        }

        if (isTop)
        {
            var top = Canvas.GetTop(TargetControl);
            if (double.IsNaN(top)) top = 0;
            double newTop = top - (newHeight - curHeight);
            Canvas.SetTop(TargetControl, newTop);
        }

        if (Parent is DesignAdornerLayer layer) layer.UpdateAdorners();
    }
}
