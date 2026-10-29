using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed class ThemeLayoutPanel : Panel
{
    private readonly Dictionary<Control, string> roles = [];
    private ThemeLayout model = ThemeLayout.From(null);
    public bool UsingFallback { get; private set; }
    public ThemeLayout Model { get => model; set { model = value; InvalidateMeasure(); } }
    public ThemeLayoutPanel() { ClipToBounds = true; }
    public void AddSlot(string role, Control control) { if (role != "gamelist") control.IsHitTestVisible = false; roles[control] = role; Children.Add(control); }
    public void RemoveSlot(Control control) { Children.Remove(control); roles.Remove(control); }
    private (Rect Frame, bool Fallback) Frame(Size size)
    {
        var width = Math.Min(size.Width, size.Height * model.AspectRatio); var height = width / model.AspectRatio;
        var frame = new Rect((size.Width - width) / 2, (size.Height - height) / 2, width, height);
        var fallback = !model.Usable || !model.Elements.TryGetValue("gamelist", out var list) || list.Width * width < 180 || list.Height * height < 100;
        return (frame, fallback);
    }
    private ThemePlacement? Placement(Control control, bool fallback)
    {
        if (!roles.TryGetValue(control, out var role)) return null;
        return fallback ? role == "gamelist" ? ThemeLayout.NativeList : null : model.Elements.GetValueOrDefault(role);
    }
    private Rect Rectangle(Control control, ThemePlacement p, Rect frame)
    {
        var width = frame.Width * p.Width; var height = frame.Height * p.Height;
        if (p.ProportionalImage)
        {
            var image = control as Image ?? (control as ContentControl)?.Content as Image;
            var size = image?.Source?.Size;
            // Missing covers keep a stable portrait placeholder; absent logos use a bounded native ratio.
            var ratio = size is { Width: > 0, Height: > 0 } s && double.IsFinite(s.Width / s.Height) ? s.Width / s.Height : roles[control] == "md_image" ? 2d / 3 : 3;
            if (width == 0) width = height * ratio;
            else if (height == 0) height = width / ratio;
            else { width = Math.Min(width, height * ratio); height = width / ratio; }
            // A very wide/tall image with an automatic axis remains within a finite template-sized box.
            var scale = Math.Min(1, Math.Min(frame.Width * 2 / Math.Max(width, 1), frame.Height * 2 / Math.Max(height, 1)));
            width *= scale; height *= scale;
        }
        return new(frame.X + frame.Width * p.X - width * p.OriginX, frame.Y + frame.Height * p.Y - height * p.OriginY, width, height);
    }
    private static void ApplyText(Control control, ThemePlacement p)
    {
        if (control is not TextBlock text) return;
        text.TextAlignment = p.TextAlignment switch { "center" => TextAlignment.Center, "right" => TextAlignment.Right, _ => TextAlignment.Left };
        text.VerticalAlignment = p.VerticalAlignment switch { "center" => VerticalAlignment.Center, "bottom" => VerticalAlignment.Bottom, _ => VerticalAlignment.Top };
        text.ClipToBounds = true;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 960, double.IsFinite(availableSize.Height) ? availableSize.Height : 540);
        var (frame, fallback) = Frame(size);
        foreach (var child in Children)
        {
            var p = Placement(child, fallback); child.IsVisible = p?.Visible == true;
            if (p != null) { ApplyText(child, p); child.Measure(Rectangle(child, p, frame).Size); }
        }
        return size;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (frame, fallback) = Frame(finalSize); UsingFallback = fallback;
        foreach (var child in Children)
        {
            var p = Placement(child, fallback); child.IsVisible = p?.Visible == true;
            if (p == null) continue;
            child.Opacity = p.Opacity; child.ZIndex = p.Layer;
            child.Arrange(Rectangle(child, p, frame));
        }
        return finalSize;
    }
}
