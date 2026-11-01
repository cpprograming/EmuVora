using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using EmuVora.Core;

namespace EmuVora.App;

internal sealed record SettingsSection(string Title, string Keywords, Control Body, bool Advanced = false);
internal sealed record SettingsCategory(string Title, IReadOnlyList<SettingsSection> Sections);

internal sealed class SettingsWorkspace : Grid
{
    public TabControl Tabs { get; } = new() { TabStripPlacement = Dock.Left };
    public TextBox Search { get; } = new() { Watermark = "Einstellungen suchen …" };
    private readonly TextBlock result = new() { TextWrapping = TextWrapping.Wrap, Opacity = .75 };
    private readonly TextBlock saved = new() { Text = "Noch keine Änderung in dieser Sitzung.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock pending = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel closeWarning = new() { Spacing = 8, IsVisible = false };
    private readonly List<(int Category, SettingsSection Section, Control Host)> groups = [];
    private readonly Dictionary<Expander,bool> expansion = [];
    private readonly SettingsStore store;
    private bool closed;
    public Func<int> PendingCount { get; set; } = () => 0;
    public bool DiscardRequested { get; private set; }
    public SettingsWorkspace(IReadOnlyList<SettingsCategory> categories, SettingsStore store, bool tv, int selected)
    {
        this.store=store;RowDefinitions=new("Auto,*,Auto");Margin=new(20);RowSpacing=12;
        Tabs.ItemsPanel=new FuncTemplate<Panel?>(()=>new StackPanel{Orientation=Orientation.Vertical});
        Styles.Add(new Style(s=>s.OfType<CheckBox>()){Setters={new Setter(CheckBox.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch),new Setter(CheckBox.ContentTemplateProperty,new FuncDataTemplate<string>((text,_)=>new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap}))}});
        Styles.Add(new Style(s=>s.OfType<ComboBox>()){Setters={new Setter(ComboBox.HorizontalAlignmentProperty,HorizontalAlignment.Stretch)}});
        AutomationProperties.SetName(Search,"Einstellungen durchsuchen");
        var header=new StackPanel{Spacing=8};header.Children.Add(new TextBlock{Text="Einstellungen",FontSize=28,FontWeight=FontWeight.Bold});header.Children.Add(Search);header.Children.Add(result);Children.Add(header);
        var tabs=new List<TabItem>();
        for(var i=0;i<categories.Count;i++)
        {
            var category=categories[i];var page=new StackPanel{Spacing=14,Margin=new(16,0,8,16)};
            foreach(var section in category.Sections)
            {
                section.Body.Margin=new(0);Control host;
                if(section.Advanced)host=new Expander{Header=section.Title,Content=section.Body,HorizontalAlignment=HorizontalAlignment.Stretch};
                else { var body=new StackPanel{Spacing=12};var heading=new TextBlock{Text=section.Title,FontSize=tv?22:19,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap};heading.Classes.Add("settings-section-heading");body.Children.Add(heading);body.Children.Add(section.Body);host=body; }
                var frame=new Border{Padding=new(14),CornerRadius=new(12),BorderThickness=new(1),BorderBrush=Brush.Parse(tv?"#52616B":"#687980"),Child=host};frame.Classes.Add("settings-section");page.Children.Add(frame);groups.Add((i,section,frame));
            }
            var empty=new TextBlock{Text="Keine Treffer in dieser Kategorie. Suchtext ändern oder eine andere Kategorie wählen.",TextWrapping=TextWrapping.Wrap,IsVisible=false};empty.Classes.Add("settings-no-results");page.Children.Add(empty);
            var tab=new TabItem{Padding=new(8,6),Header=new TextBlock{Text=category.Title,Width=tv?160:132,FontSize=tv?18:14,TextWrapping=TextWrapping.Wrap},Content=new ScrollViewer{Content=page}};AutomationProperties.SetName(tab,category.Title);tabs.Add(tab);
        }
        Tabs.ItemsSource=tabs;Tabs.SelectedIndex=Math.Clamp(selected,0,tabs.Count-1);Grid.SetRow(Tabs,1);Children.Add(Tabs);
        var footer=new StackPanel{Spacing=8};footer.Children.Add(new TextBlock{Text="Optionen werden sofort gespeichert. Dateipfade erst mit ihrem Speichern-Knopf übernehmen.",TextWrapping=TextWrapping.Wrap,Opacity=.75});saved.Classes.Add("settings-save-feedback");footer.Children.Add(saved);pending.Classes.Add("settings-pending-feedback");footer.Children.Add(pending);
        closeWarning.Children.Add(new TextBlock{Text="Pfadentwürfe sind noch nicht übernommen. Weiter bearbeiten oder nur diese Entwürfe verwerfen? Bereits gespeicherte Optionen bleiben erhalten.",TextWrapping=TextWrapping.Wrap});footer.Children.Add(closeWarning);
        var discard=new Button{Content="Pfadentwürfe verwerfen und schließen"};discard.Click+=(_,_)=>{DiscardRequested=true;(VisualRoot as Window)?.Close();};closeWarning.Children.Add(discard);
        var keep=new Button{Content="Weiter bearbeiten"};keep.Click+=(_,_)=>{closeWarning.IsVisible=false;Search.Focus();};closeWarning.Children.Add(keep);
        var close=new Button{Content="Schließen",HorizontalAlignment=HorizontalAlignment.Right};close.Click+=(_,_)=> (VisualRoot as Window)?.Close();footer.Children.Add(close);Grid.SetRow(footer,2);Children.Add(footer);
        Search.TextChanged+=(_,_)=>Filter(tabs);store.Saved+=OnSaved;
        KeyDown+=(_,e)=>{if(e.Key==Key.F&&e.KeyModifiers.HasFlag(KeyModifiers.Control)){Search.Focus();e.Handled=true;}else if(e.Key==Key.Escape&&!string.IsNullOrEmpty(Search.Text)){Search.Text="";e.Handled=true;}};
        Filter(tabs);
    }
    public void UpdatePending() { var count=PendingCount();pending.Text=count==0?"":count+" Pfadentwurf/-entwürfe noch nicht übernommen.";if(count==0)closeWarning.IsVisible=false; }
    public bool CanClose()
    {
        UpdatePending();if(DiscardRequested||PendingCount()==0)return true;closeWarning.IsVisible=true;return false;
    }
    public void Detach() {closed=true;store.Saved-=OnSaved;}
    public void UpdateScale(bool tv)
    {
        foreach(var tab in Tabs.Items.OfType<TabItem>())if(tab.Header is TextBlock header){header.FontSize=tv?18:14;header.Width=tv?160:132;}
        foreach(var heading in groups.SelectMany(g=>Controls(g.Host)).OfType<TextBlock>().Where(t=>t.Classes.Contains("settings-section-heading")))heading.FontSize=tv?22:19;
    }
    private void OnSaved(object? sender,EventArgs e)
    {
        void Update(){if(!closed){saved.Text="Gespeichert · "+DateTime.Now.ToString("HH:mm:ss");UpdatePending();}}
        if(Dispatcher.UIThread.CheckAccess())Update();else Dispatcher.UIThread.Post(Update);
    }
    private static IEnumerable<Control> Controls(Control body)=>body.GetLogicalDescendants().OfType<Control>().Prepend(body);
    private static string SearchText(SettingsSection section)=>section.Title+" "+section.Keywords+" "+string.Join(" ",Controls(section.Body).Select(c=>c switch{TextBlock t=>t.Text,ContentControl cc when cc.Content is string text=>text,Expander e=>e.Header?.ToString(),ItemsControl items=>string.Join(" ",items.Items.OfType<string>()),_=>null}));
    private void Filter(IReadOnlyList<TabItem> tabs)
    {
        var terms=(Search.Text??"").Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);var searching=terms.Length>0;
        if(searching&&expansion.Count==0)foreach(var expander in groups.SelectMany(g=>Controls(g.Host)).OfType<Expander>())expansion.TryAdd(expander,expander.IsExpanded);
        var counts=new int[tabs.Count];
        foreach(var group in groups)
        {
            var text=SearchText(group.Section);var match=terms.All(t=>text.Contains(t,StringComparison.OrdinalIgnoreCase));group.Host.IsVisible=match;if(match)counts[group.Category]++;
            if(searching&&match)foreach(var expander in Controls(group.Host).OfType<Expander>())expander.IsExpanded=true;
        }
        if(!searching){foreach(var (expander,expanded) in expansion)expander.IsExpanded=expanded;expansion.Clear();}
        for(var i=0;i<tabs.Count;i++)if(((ScrollViewer)tabs[i].Content!).Content is StackPanel page)page.Children.OfType<TextBlock>().Single(t=>t.Classes.Contains("settings-no-results")).IsVisible=counts[i]==0;
        if(searching&&counts[Math.Max(0,Tabs.SelectedIndex)]==0&&counts.Any(c=>c>0))Tabs.SelectedIndex=Array.FindIndex(counts,c=>c>0);
        result.Text=searching?$"{counts.Sum()} Bereiche in {counts.Count(c=>c>0)} Kategorien · Suchtext leeren für alle Einstellungen.":"Sechs Kategorien · Strg+F: suchen · Controller LB/RB: Kategorie wechseln";
    }
}
