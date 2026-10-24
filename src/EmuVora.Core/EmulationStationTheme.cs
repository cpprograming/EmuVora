using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public sealed record ThemeElement(string Type, string Name, Dictionary<string,string> Properties);
public sealed class EmulationStationTheme
{
    private static XElement ReadXml(string file)
    {
        Backups.NoLinks(file);
        using var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024});
        return XDocument.Load(reader).Root??throw new IOException("Leere Theme-XML.");
    }
    public List<ThemeElement> Elements { get; } = [];
    public List<string> Warnings { get; } = [];
    public static string SystemId(string system) => system switch
    {
        "PS1" => "psx", "PS2" => "ps2", "PSP" => "psp", "GameCube" => "gc", "Wii" => "wii",
        _ => SystemCatalog.Find(system)?.Aliases.Split('|')[0] ?? "nes"
    };
    public static EmulationStationTheme Load(string directory, string system,bool preferDark=true)
    {
        var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)); var theme=new EmulationStationTheme();
        Backups.NoLinks(root);
        var modern=File.Exists(Path.Combine(root,"capabilities.xml"));string variant="",scheme="",aspect="16:9";
        if(modern)
        {
            var caps=ReadXml(Path.Combine(root,"capabilities.xml"));if(caps.Name.LocalName!="themeCapabilities")throw new IOException("Ungültige ES-DE-capabilities.xml.");
            variant=(string?)caps.Elements("variant").FirstOrDefault(e=>e.Element("selectable")?.Value!="false")?.Attribute("name")??"";
            scheme=(string?)caps.Elements("colorScheme").FirstOrDefault(e=>((string?)e.Attribute("name"))?.Contains(preferDark?"dark":"light",StringComparison.OrdinalIgnoreCase)==true)?.Attribute("name")??(string?)caps.Element("colorScheme")?.Attribute("name")??"";
            aspect=caps.Elements("aspectRatio").Any(e=>e.Value=="16:9")?"16:9":caps.Element("aspectRatio")?.Value??"16:9";
            theme.Warnings.Add($"ES-DE an natives EmuVora-Layout angepasst. Variante: {variant}; Farbschema: {scheme}; Vorlagenformat: {aspect}.");
            theme.Warnings.Add("Videos, Sounds, Animationen, eigene Schriftarten und Speziallayouts werden nicht übernommen. Ressourcen und Lizenzhinweise bleiben erhalten.");
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
                        if(type is not ("image" or "text" or "textlist" or "helpsystem")) {theme.Warnings.Add("Nicht unterstütztes Element: "+type);continue;}
                        var mapped=modern?name switch{"background"=>"canvas","background-art"=>"background","system-logo"=>"logo","game-list"=>"gamelist","game-name"=>"md_name","game-description"=>"md_description",_=>name}:name;
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
        return theme;
    }
    public static string Import(string source,string dataDirectory,bool preferDark=true,CancellationToken token=default)
    {
        var root=Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
        var parent=Path.Combine(Path.GetFullPath(dataDirectory),"themes");
        if(parent.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||root.StartsWith(parent+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||root.Equals(parent,StringComparison.OrdinalIgnoreCase))throw new IOException("Bitte einen Theme-Ordner außerhalb des verwalteten Theme-Verzeichnisses wählen.");
        // Validate before copying; no network references, links or executable theme code.
        var files=new List<string>();long bytes=0;
        void Enumerate(string folder,int depth=0)
        {
            token.ThrowIfCancellationRequested();
            if(depth>32)throw new IOException("Theme-Ordner ist zu tief verschachtelt.");
            if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)throw new IOException("Theme enthält eine Ordnerverknüpfung.");
            foreach(var file in Directory.EnumerateFiles(folder)) {if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Theme enthält eine Dateiverknüpfung.");bytes+=new FileInfo(file).Length;files.Add(file);if(files.Count>10000||bytes>256L*1024*1024)throw new IOException("Theme ist zu groß (max. 256 MiB / 10.000 Dateien).");}
            foreach(var child in Directory.EnumerateDirectories(folder))Enumerate(child,depth+1);
        }
        Enumerate(root);
        var xmls=files.Where(f=>Path.GetFileName(f).Equals("theme.xml",StringComparison.OrdinalIgnoreCase)).ToArray();
        if(xmls.Length==0)throw new IOException("Der Ordner enthält keine theme.xml.");
        // Validate every system, not just the currently selected console.
        var notes=new HashSet<string>();var supported=0;foreach(var system in EmulatorCatalog.Systems){token.ThrowIfCancellationRequested();var loaded=Load(root,system,preferDark);supported+=loaded.Elements.Count(e=>e.Properties.Count>0);foreach(var note in loaded.Warnings)notes.Add(note);}
        if(supported==0)throw new IOException("Keine unterstützten Theme-Elemente gefunden.");
        var destination=Path.Combine(parent,Path.GetFileName(root)+"-"+Guid.NewGuid().ToString("N"));
        try {Directory.CreateDirectory(destination);foreach(var file in files){token.ThrowIfCancellationRequested();var target=Path.Combine(destination,Path.GetRelativePath(root,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target);}File.WriteAllText(Path.Combine(destination,"emuvora-import-report.txt"),"EmuVora · Theme-Anpassung\nLayout und Bedienung bleiben nativ. Nicht jedes ES-DE-Element ist übertragbar.\n"+string.Join("\n",notes));return destination;}
        catch {if(Directory.Exists(destination))Directory.Delete(destination,true);throw;}
    }
}
