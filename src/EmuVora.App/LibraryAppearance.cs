using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private int CardWidth=>settings.TvMode?260:Math.Clamp(settings.CoverSize,120,220);
    private void ApplyLibraryAppearance()
    {
        WindowAppearance.SetTvMode(this,settings.TvMode);main.Margin=new(settings.TvMode?32:24);
        summary.FontSize=controllerStatus.FontSize=themeStatus.FontSize=status.FontSize=settings.TvMode?16:13;
        ApplyLayout();
    }
    private int LibraryColumns()
    {
        var containers=gamesList.GetVisualDescendants().OfType<ListBoxItem>().Where(c=>c.Bounds.Width>0&&c.Bounds.Height>0).ToArray();
        if(containers.Length>0){var row=containers.Min(c=>c.Bounds.Y);return Math.Max(1,containers.Count(c=>Math.Abs(c.Bounds.Y-row)<2));}
        return Math.Max(1,(int)(gamesList.Bounds.Width/(CardWidth+20)));
    }
    private CheckBox TvModeOption()
    {
        var option=new CheckBox{Content="TV-Modus · größere Cover und Bedienelemente",IsChecked=settings.TvMode};
        option.IsCheckedChanged+=(_,_)=>Guard(()=>
        {
            settings.TvMode=option.IsChecked==true;settingsStore.Save(settings);ApplyLibraryAppearance();Refresh();
            foreach(var dialog in OwnedWindows)WindowAppearance.SetTvMode(dialog,settings.TvMode);
        });return option;
    }
}
