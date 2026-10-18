using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace EmuVora.App;

public static class ControllerUi
{
    private sealed class Back(Action action){public Action Action=action;}
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window,Back> BackActions=new();
    public static void SetBack(Window window,Action action){BackActions.Remove(window);BackActions.Add(window,new(action));}
    public static void Attach(Window window)
    {
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
        var navigation=new GamepadNavigation();
        timer.Tick+=(_,_)=>
        {
            var pressed=navigation.Poll(Gamepad.Read(),window.IsActive,Environment.TickCount64);
            if(pressed!=0)Handle(window,pressed);
        };
        window.Opened+=(_,_)=>timer.Start();window.Closed+=(_,_)=>timer.Stop();
    }
    public static void Handle(Window window,ushort pressed)
    {
        if((pressed&0x2000)!=0){if(BackActions.TryGetValue(window,out var back))back.Action();else window.Close();return;}
        var tabs=window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault(t=>t.IsEffectivelyVisible);
        if(tabs!=null&&(pressed&0x300)!=0){tabs.SelectedIndex=(tabs.SelectedIndex+((pressed&0x200)!=0?1:tabs.ItemCount-1))%Math.Max(1,tabs.ItemCount);window.UpdateLayout();First(window)?.Focus();return;}
        var controls=window.GetVisualDescendants().OfType<Control>().Where(c=>c.IsEffectivelyVisible&&c.IsEffectivelyEnabled&&c.Focusable&&c is Button or CheckBox or ComboBox or TextBox or Slider or ListBox or Expander).ToArray();
        if(controls.Length==0)return;
        var focused=window.FocusManager?.GetFocusedElement() as Control;
        var current=Array.FindIndex(controls,c=>c==focused||focused!=null&&focused.GetVisualAncestors().Contains(c));
        if(current<0){controls[0].Focus();current=0;}
        var control=controls[current];
        if((pressed&0xC)!=0)
        {
            var delta=(pressed&8)!=0?1:-1;
            if(control is ComboBox combo){combo.SelectedIndex=Math.Clamp(combo.SelectedIndex+delta,0,Math.Max(0,combo.ItemCount-1));return;}
            if(control is Slider slider){slider.Value=Math.Clamp(slider.Value+delta*Math.Max(1,slider.TickFrequency),slider.Minimum,slider.Maximum);return;}
        }
        if((pressed&3)!=0&&control is ListBox list){if(list.ItemCount>0){list.SelectedIndex=Math.Clamp(list.SelectedIndex+((pressed&2)!=0?1:-1),0,list.ItemCount-1);list.ScrollIntoView(list.SelectedItem!);}return;}
        if((pressed&15)!=0){var next=(current+((pressed&10)!=0?1:controls.Length-1))%controls.Length;controls[next].Focus();controls[next].BringIntoView();return;}
        if((pressed&0x1000)!=0)
        {
            if(control is CheckBox check)check.IsChecked=check.IsChecked!=true;
            else if(control is Button button)button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            else if(control is Expander expander)expander.IsExpanded=!expander.IsExpanded;
            else if(control is TextBox text)_=KeyboardAsync(window,text);
            else if(control is ComboBox choice)choice.SelectedIndex=(choice.SelectedIndex+1)%Math.Max(1,choice.ItemCount);
            else if(control is ListBox){controls[(current+1)%controls.Length].Focus();}
        }
    }
    private static Control? First(Window window)=>window.GetVisualDescendants().OfType<Control>().FirstOrDefault(c=>c.IsEffectivelyVisible&&c.IsEffectivelyEnabled&&c.Focusable&&c is Button or CheckBox or ComboBox or TextBox);
    private static async Task KeyboardAsync(Window owner,TextBox target)
    {
        var window=new Window{Title="Xbox · Texteingabe",Width=680,Height=500,Background=owner.Background,Foreground=owner.Foreground,RequestedThemeVariant=owner.RequestedThemeVariant,WindowStartupLocation=WindowStartupLocation.CenterOwner};WindowAppearance.Apply(window);Attach(window);
        var panel=new StackPanel{Spacing=12,Margin=new Avalonia.Thickness(20)};var text=new TextBlock{Text=target.Text??"",TextWrapping=Avalonia.Media.TextWrapping.Wrap};panel.Children.Add(text);var keys=new WrapPanel();bool lower=true;
        foreach(var character in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:/\\.-_'()")
        {var button=new Button{Content=character.ToString(),Width=48,Margin=new Avalonia.Thickness(3)};button.Click+=(_,_)=>text.Text+=(lower?char.ToLowerInvariant(character):character);keys.Children.Add(button);}
        panel.Children.Add(keys);
        void Add(string label,Action action){var button=new Button{Content=label};button.Click+=(_,_)=>action();panel.Children.Add(button);}
        Add("Groß / klein",()=>lower=!lower);Add("Leerzeichen",()=>text.Text+=" ");Add("Zeichen löschen",()=>{if(text.Text?.Length>0)text.Text=text.Text[..^1];});Add("Übernehmen",()=>{target.Text=text.Text;window.Close();});
        panel.Children.Add(new TextBlock{Text="D-Pad: wählen · A: eingeben · B: verwerfen"});window.Content=new ScrollViewer{Content=panel};await window.ShowDialog(owner);
    }
}
