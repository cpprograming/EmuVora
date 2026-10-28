using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public sealed record ThemeElement(string Type, string Name, Dictionary<string,string> Properties);
public sealed partial class EmulationStationTheme
{
    private static XElement ReadXml(string file)
    {
        Backups.NoLinks(file);
        using var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024});
        return XDocument.Load(reader).Root??throw new IOException("Leere Theme-XML.");
    }
    public List<ThemeElement> Elements { get; } = [];
    public List<string> Warnings { get; } = [];
    public string AspectRatio { get; private set; } = "16:9";
    public static string SystemId(string system) => system switch
    {
        "PS1" => "psx", "PS2" => "ps2", "PSP" => "psp", "GameCube" => "gc", "Wii" => "wii",
        _ => SystemCatalog.Find(system)?.Aliases.Split('|')[0] ?? "nes"
    };
    public static EmulationStationTheme Load(string directory, string system,bool preferDark=true,ThemeSelection? selection=null)
    {
        var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)); var theme=new EmulationStationTheme();
        Backups.NoLinks(root);
        var selected=ResolveSelection(root,preferDark,selection);
        theme.AspectRatio=selected.AspectRatio.Length>0?selected.AspectRatio:"16:9";
        var modern=File.Exists(Path.Combine(root,"capabilities.xml"));string variant="",scheme="",aspect="16:9";
        if(modern)
        {
            var caps=ReadXml(Path.Combine(root,"capabilities.xml"));if(caps.Name.LocalName!="themeCapabilities")throw new IOException("Ungültige ES-DE-capabilities.xml.");
            variant=selected.Variant;scheme=selected.ColorScheme;aspect=selected.AspectRatio.Length>0?selected.AspectRatio:"16:9";
            theme.Warnings.Add($"ES-DE an natives EmuVora-Layout angepasst. Variante: {variant}; Farbschema: {scheme}; Vorlagenformat: {aspect}.");
            theme.Warnings.Add("Teilweise Layoutanpassung für Liste, Hintergrund, Logo, Cover und Texte. Videos werden höchstens als statisches Cover angenähert; Sounds, Animationen und eigene Schriftarten bleiben offen.");
        }
        var id=SystemId(system);
        var aliases=new[]{id}.Concat(SystemCatalog.Find(system)?.Aliases.Split('|')??[]).Distinct(StringComparer.OrdinalIgnoreCase);
        var entry=aliases.Select(a=>Path.Combine(root,a,"theme.xml")).FirstOrDefault(File.Exists) ?? Path.Combine(root,"theme.xml");
        if(!File.Exists(entry)) { theme.Warnings.Add("Kein Theme für "+system+" vorhanden. EmuVora verwendet seine Standarddarstellung."); return theme; }
        if(!Path.GetDirectoryName(entry)!.Equals(root,StringComparison.OrdinalIgnoreCase))id=Path.GetFileName(Path.GetDirectoryName(entry))!;
        var variables=new Dictionary<string,string>(StringComparer.Ordinal){{"system",id},{"system.theme",id},{"system.name",system},{"theme.variant",variant},{"theme.colorScheme",scheme},{"theme.aspectRatio",aspect},{"theme.fontSize","medium"}};
        var active=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var visited=0;
        string Expand(string value)
        {
            for(var i=0;i<16;i++) { var expanded=Regex.Replace(value,@"\$\{([^}]+)\}",m=>variables.GetValueOrDefault(m.Groups[1].Value)??m.Value);if(expanded==value)break;value=expanded; }
            if(value.Contains("${")&&!modern) throw new IOException("Unbekannte Theme-Variable: "+value);
            return value;
        }
        string Resolve(string file,string value)
        {
            var expanded=Expand(value).Replace('/',Path.DirectorySeparatorChar);
            if(Path.IsPathRooted(expanded)||expanded.Contains(':')) throw new IOException("Theme-Ressourcen müssen im Theme-Ordner liegen.");
            var path=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!,expanded));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new IOException("Theme-Pfad verlässt den Theme-Ordner.");
            for(var p=path;!p.Equals(root,StringComparison.OrdinalIgnoreCase);p=Path.GetDirectoryName(p)!)
                if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("Verknüpfte Theme-Ressourcen werden nicht geladen.");
            return path;
        }
        void Read(string file,int depth)
        {
            if(depth>24||++visited>128||!active.Add(file))throw new IOException("Zu viele oder zyklische Theme-Includes.");
            try
            {
                var xml=ReadXml(file);
                if(xml?.Name.LocalName!="theme")throw new IOException("Keine gültige theme.xml.");
                if(!modern&&xml.Elements().Any(n=>n.Name.LocalName is "variant" or "colorScheme" or "aspectRatio"))throw new IOException("Theme-Varianten benötigen capabilities.xml.");
                IEnumerable<XElement> Selected(IEnumerable<XElement> nodes)
                {
                    foreach(var node in nodes)
                    {
                        var key=node.Name.LocalName;
                        if(modern&&key is "variant" or "colorScheme" or "aspectRatio" or "fontSize")
                        {
                            var choice=key switch{"variant"=>variant,"colorScheme"=>scheme,"aspectRatio"=>aspect,_=>"medium"};
                            if(Regex.Split((string?)node.Attribute("name")??"",@"[\s,]+").Contains(choice))foreach(var child in Selected(node.Elements()))yield return child;
                        }
                        else yield return node;
                    }
                }
                foreach(var node in Selected(xml.Elements()))
                {
                    if(node.Name.LocalName=="variables") { foreach(var v in node.Elements())variables[v.Name.LocalName]=Expand(v.Value.Trim());continue; }
                    if(node.Name.LocalName=="include") { if(modern&&Expand(node.Value.Trim()).Contains("${")){theme.Warnings.Add("Dynamisches Include ausgelassen.");continue;}var include=Resolve(file,node.Value.Trim());if(modern&&!File.Exists(include)){theme.Warnings.Add("Fehlendes Include ausgelassen: "+Path.GetFileName(include));continue;}Read(include,depth+1);continue; }
                    if(node.Name.LocalName=="feature") { theme.Warnings.Add("Optionale Theme-Features werden nicht dargestellt.");continue; }
                    if(node.Name.LocalName!="view")continue;
                    var views=Regex.Split((string?)node.Attribute("name")??"",@"[\s,]+");
                    if(!views.Intersect(modern?new[]{"gamelist","all"}:new[]{"basic","detailed","grid"}).Any())continue;
                    foreach(var element in node.Elements())foreach(var name in Regex.Split((string?)element.Attribute("name")??"",@"[\s,]+").Where(n=>n.Length>0))
                    {
                        var type=element.Name.LocalName;
                        if(type=="video"&&name is "game-media" or "md_video"){type="image";theme.Warnings.Add("Videoelement als statisches Spielcover angenähert: "+name);}
                        if(type is not ("image" or "text" or "textlist" or "helpsystem")) {theme.Warnings.Add("Nicht unterstütztes Element: "+type);continue;}
                        var mapped=name is "game-media" or "game-image" or "game-cover" or "md_video"?"md_image":modern?name switch{"background"=>"canvas","background-art"=>"background","system-logo"=>"logo","game-list"=>"gamelist","game-name"=>"md_name","game-description"=>"md_description",_=>name}:name;
                        var existing=theme.Elements.FirstOrDefault(e=>e.Type==type&&e.Name==mapped);
                        if(existing==null) {existing=new(type,mapped,[]);theme.Elements.Add(existing);}
                        foreach(var property in element.Elements())
                        {
                            var key=property.Name.LocalName;var value=Expand(property.Value.Trim());
                            if(modern&&value.Contains("${")){theme.Warnings.Add("Dynamische Eigenschaft ausgelassen: "+key);continue;}
                            existing.Properties[key]=(key is "path" or "fontPath" or "default") && value.Length>0?Resolve(file,value):value;
                        }
                    }
                }
            }
            finally {active.Remove(file);}
        }
        Read(entry,0);
        if(modern)foreach(var image in theme.Elements.Where(e=>e.Type=="image"))if(image.Properties.TryGetValue("path",out var path)&&!File.Exists(path)&&image.Properties.TryGetValue("default",out var fallback)&&File.Exists(fallback))image.Properties["path"]=fallback;
        theme.Warnings.AddRange(ThemeLayout.From(theme).Notes);
        return theme;
    }
    public static string Import(string source,string dataDirectory,bool preferDark=true,CancellationToken token=default)
        => ApplyImport(PreviewImport(source,"NES",preferDark,token:token),dataDirectory,token);
}
