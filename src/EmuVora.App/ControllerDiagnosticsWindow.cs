using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace EmuVora.App;

public sealed class ControllerDiagnosticsWindow : Window
{
    private readonly TextBlock[] slots=Enumerable.Range(0,4).Select(_=>new TextBlock{TextWrapping=TextWrapping.Wrap}).ToArray();
    private readonly TextBlock history=new(){TextWrapping=TextWrapping.Wrap,FontSize=12};
    private readonly TextBlock backendStatus=new(){TextWrapping=TextWrapping.Wrap,FontSize=12};
    private readonly StackPanel mappedPads=new(){Spacing=12};
    private readonly Queue<string> events=[];
    private Dictionary<int,string> connected=[];
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(100)};
    private int chordIndex=-1;
    private long chordSince;
    private readonly Func<bool> inputActive;
    public ControllerDiagnosticsWindow(Func<Gamepad.Snapshot[]>? read=null,Func<bool>? active=null)
    {
        inputActive=active??(()=>IsActive);
        Title="Controller prüfen";Width=740;Height=720;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        WindowAppearance.Apply(this);
        var panel=new StackPanel{Spacing=14,Margin=new Avalonia.Thickness(24)};
        panel.Children.Add(new TextBlock{Text="Controller-Test",FontSize=23,TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(backendStatus);
        panel.Children.Add(new TextBlock{Text="Teste Tasten, beide Sticks und Trigger. Ziehe den Controller ab und verbinde ihn erneut. Eingaben lösen hier keine Spielstarts oder Menüaktionen aus. Zum Schließen View + Menu auf demselben Controller eine Sekunde halten, Escape drücken oder Schließen anklicken. Tasten folgen den Positionen des Xbox-Layouts. SDL meldet den Gerätenamen und, falls bekannt, Kabel/Drahtlos. Drahtlos bedeutet nicht zwingend Bluetooth. XInput liefert weder Modell noch Verbindungstyp. Gerätekennungen sind keine garantierten Spielerplätze im Emulator.",TextWrapping=TextWrapping.Wrap});
        foreach(var slot in slots)panel.Children.Add(new Border{Padding=new Avalonia.Thickness(12),BorderThickness=new Avalonia.Thickness(1),BorderBrush=Brushes.Gray,Child=slot});
        panel.Children.Add(mappedPads);
        panel.Children.Add(new TextBlock{Text="Verbindungen in dieser Sitzung",FontWeight=FontWeight.Bold});panel.Children.Add(history);
        var layout=new DockPanel();var close=new Button{Content="Schließen",Margin=new Avalonia.Thickness(24,10)};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Bottom);layout.Children.Add(close);layout.Children.Add(new ScrollViewer{Content=panel});Content=layout;
        KeyDown+=(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;Close();}};
        void Poll(){var pads=(read??Gamepad.Read)();UpdateSnapshots(pads,Environment.TickCount64);if(read==null){backendStatus.Text=Gamepad.BackendName+(Gamepad.BackendWarning is{} warning?" · XInput-Rückfall: "+warning:" · Offline-Controllerprofile geladen")+(pads.Length==0?" · Kein Controller verbunden":"");if(Gamepad.BackendName=="SDL 3")foreach(var slot in slots)if(slot.Parent is Control frame)frame.IsVisible=false;}}
        timer.Tick+=(_,_)=>Poll();
        Opened+=(_,_)=>{Poll();timer.Start();};Closed+=(_,_)=>timer.Stop();
    }
    public void UpdateSnapshots(IReadOnlyList<Gamepad.Snapshot> pads,long now)
    {
        var current=pads.GroupBy(p=>p.Index).ToDictionary(g=>g.Key,g=>Gamepad.Label(g.First()));
        foreach(var index in connected.Keys.Except(current.Keys).Order())Record(connected[index]+": getrennt");
        foreach(var index in current.Keys.Except(connected.Keys).Order())Record(current[index]+": verbunden");
        connected=current;
        var sdl=pads.Any(p=>p.Backend=="SDL 3");mappedPads.Children.Clear();
        foreach(var pad in pads.Where(p=>p.Backend=="SDL 3").Take(16))mappedPads.Children.Add(new Border{Padding=new Avalonia.Thickness(12),CornerRadius=new(8),BorderThickness=new Avalonia.Thickness(1),BorderBrush=Brushes.Gray,Child=new TextBlock{TextWrapping=TextWrapping.Wrap,Text=$"{Gamepad.Label(pad)} · {pad.Connection??"Unbekannt"}\nTasten: {ButtonNames(pad.Buttons)}\nLinker Stick: X {pad.X}, Y {pad.Y} · Rechter Stick: X {pad.RightX}, Y {pad.RightY}\nTrigger: LT {pad.LeftTrigger}/255 · RT {pad.RightTrigger}/255"}});
        if(sdl)backendStatus.Text="SDL 3 · Automatische Belegung";
        for(var i=0;i<4;i++)
        {
            if(slots[i].Parent is Control frame)frame.IsVisible=!sdl;
            var found=pads.Where(p=>p.Backend=="XInput"&&p.Index==i).ToArray();
            if(found.Length==0){slots[i].Text=$"Slot {i+1} · nicht verbunden";continue;}
            var pad=found[0];slots[i].Text=$"Slot {i+1} · verbunden\nTasten: {ButtonNames(pad.Buttons)}\nLinker Stick: X {pad.X}, Y {pad.Y} · Rechter Stick: X {pad.RightX}, Y {pad.RightY}\nTrigger: LT {pad.LeftTrigger}/255 · RT {pad.RightTrigger}/255";
        }
        // Ignore test inputs in inactive windows; require a fresh full hold after focus returns.
        var chord=inputActive()?pads.Where(p=>(p.Buttons&0x30)==0x30).Select(p=>p.Index).DefaultIfEmpty(-1).First():-1;
        if(chord<0||chord!=chordIndex){chordIndex=chord;chordSince=now;}
        else if(now-chordSince>=1000)Close();
    }
    private void Record(string value){events.Enqueue(value);while(events.Count>12)events.Dequeue();history.Text=string.Join("\n",events);}
    public static string ButtonNames(ushort buttons)
    {
        (ushort Mask,string Name)[] names=[(1,"↑"),(2,"↓"),(4,"←"),(8,"→"),(0x10,"Menu"),(0x20,"View"),(0x40,"LS"),(0x80,"RS"),(0x100,"LB"),(0x200,"RB"),(0x1000,"A"),(0x2000,"B"),(0x4000,"X"),(0x8000,"Y")];
        var pressed=names.Where(n=>(buttons&n.Mask)!=0).Select(n=>n.Name).ToArray();return pressed.Length==0?"keine":string.Join(" · ",pressed);
    }
}
