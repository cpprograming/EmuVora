using Avalonia;
using Avalonia.Controls;
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
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 960, double.IsFinite(availableSize.Height) ? availableSize.Height : 540);
        var (frame, fallback) = Frame(size);
        foreach (var child in Children)
        {
            var p = Placement(child, fallback); child.IsVisible = p?.Visible == true;
            if (p != null) child.Measure(new(frame.Width * p.Width, frame.Height * p.Height));
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
            child.Arrange(new(frame.X + frame.Width * p.Left, frame.Y + frame.Height * p.Top, frame.Width * p.Width, frame.Height * p.Height));
        }
        return finalSize;
    }
}
