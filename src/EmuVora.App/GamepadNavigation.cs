namespace EmuVora.App;

// Each window owns a tracker. Newly connected or held inputs must first return to neutral.
public sealed class GamepadNavigation
{
    private readonly Dictionary<int,ushort> previous=[];
    private readonly HashSet<int> armed=[];
    private ushort direction;
    private long repeatAt;
    public int ActiveIndex {get;private set;}=-1;
    public ushort Poll(IReadOnlyList<Gamepad.Snapshot> pads,bool enabled,long now)
    {
        var connected=pads.Select(p=>p.Index).ToHashSet();
        foreach(var index in previous.Keys.Where(i=>!connected.Contains(i)).ToArray()){previous.Remove(index);armed.Remove(index);}
        var edges=new Dictionary<int,ushort>();
        foreach(var pad in pads)
        {
            var buttons=Gamepad.NavigationButtons(pad);
            var known=previous.TryGetValue(pad.Index,out var old);
            if(!enabled)armed.Remove(pad.Index);
            else if(buttons==0)armed.Add(pad.Index);
            edges[pad.Index]=enabled&&known&&armed.Contains(pad.Index)?(ushort)(buttons&~old):(ushort)0;
            previous[pad.Index]=buttons;
        }
        if(!connected.Contains(ActiveIndex)){ActiveIndex=pads.Count>0?pads[0].Index:-1;direction=0;repeatAt=now+400;}
        if(!enabled||ActiveIndex<0){direction=0;repeatAt=now+400;return 0;}
        if(previous[ActiveIndex]==0||!armed.Contains(ActiveIndex))
        {
            var next=pads.FirstOrDefault(p=>edges[p.Index]!=0);
            if(edges.GetValueOrDefault(next.Index)!=0&&ActiveIndex!=next.Index){ActiveIndex=next.Index;direction=0;}
        }
        var pressed=edges[ActiveIndex];var current=armed.Contains(ActiveIndex)?(ushort)(previous[ActiveIndex]&15):(ushort)0;
        if(direction!=current){direction=current;repeatAt=now+400;}
        else if(current!=0&&now>=repeatAt){pressed|=current;repeatAt=now+140;}
        return pressed;
    }
}
