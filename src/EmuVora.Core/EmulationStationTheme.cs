using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public sealed record ThemeElement(string Type, string Name, Dictionary<string,string> Properties);
public sealed class EmulationStationTheme
{
    public List<ThemeElement> Elements { get; } = [];
    public List<string> Warnings { get; } = [];
    public static string SystemId(string system) => system switch
    {
        "PS1" => "psx", "PS2" => "ps2", "PSP" => "psp", "GameCube" => "gc", "Wii" => "wii",
        _ => SystemCatalog.Find(system)?.Aliases.Split('|')[0] ?? "nes"
    };
    public static EmulationStationTheme Load(string directory, string system)
    {
        var root=Path.GetFullPath(directory); var theme=new EmulationStationTheme();
        if(File.Exists(Path.Combine(root,"capabilities.xml"))) throw new IOException("Dieses Theme verwendet ES-DE capabilities.xml. ES-DE-Varianten werden noch nicht unterstützt. Bitte ein klassisches EmulationStation-Theme wählen.");
        var id=SystemId(system);
        var aliases=new[]{id}.Concat(SystemCatalog.Find(system)?.Aliases.Split('|')??[]).Distinct(StringComparer.OrdinalIgnoreCase);
        var entry=aliases.Select(a=>Path.Combine(root,a,"theme.xml")).FirstOrDefault(File.Exists) ?? Path.Combine(root,"theme.xml");
        if(!File.Exists(entry)) { theme.Warnings.Add("Kein Theme für "+system+" vorhanden. EmuVora verwendet seine Standarddarstellung."); return theme; }
        if(!Path.GetDirectoryName(entry)!.Equals(root,StringComparison.OrdinalIgnoreCase))id=Path.GetFileName(Path.GetDirectoryName(entry))!;
        var variables=new Dictionary<string,string>(StringComparer.Ordinal){{"system",id},{"system.theme",id},{"system.name",system}};
        var active=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var visited=0;
        string Expand(string value)
        {
            for(var i=0;i<16;i++) { var expanded=Regex.Replace(value,@"\$\{([^}]+)\}",m=>variables.GetValueOrDefault(m.Groups[1].Value)??m.Value);if(expanded==value)break;value=expanded; }
            if(value.Contains("${")) throw new IOException("Unbekannte Theme-Variable: "+value);
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
                using var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024});
                var xml=XDocument.Load(reader).Root;
                if(xml?.Name.LocalName!="theme")throw new IOException("Keine gültige theme.xml.");
                if(xml.Elements().Any(n=>n.Name.LocalName is "variant" or "colorScheme" or "aspectRatio"))throw new IOException("ES-DE-Varianten werden noch nicht unterstützt.");
                foreach(var node in xml.Elements())
                {
                    if(node.Name.LocalName=="variables") { foreach(var v in node.Elements())variables[v.Name.LocalName]=Expand(v.Value.Trim());continue; }
                    if(node.Name.LocalName=="include") { Read(Resolve(file,node.Value.Trim()),depth+1);continue; }
                    if(node.Name.LocalName=="feature") { theme.Warnings.Add("Optionale Theme-Features werden nicht dargestellt.");continue; }
                    if(node.Name.LocalName!="view")continue;
                    var views=Regex.Split((string?)node.Attribute("name")??"",@"[\s,]+");
                    if(!views.Intersect(new[]{"basic","detailed","grid"}).Any())continue;
                    foreach(var element in node.Elements())foreach(var name in Regex.Split((string?)element.Attribute("name")??"",@"[\s,]+").Where(n=>n.Length>0))
                    {
                        var type=element.Name.LocalName;
                        if(type is not ("image" or "text" or "textlist" or "helpsystem")) {theme.Warnings.Add("Nicht unterstütztes Element: "+type);continue;}
                        var existing=theme.Elements.FirstOrDefault(e=>e.Type==type&&e.Name==name);
                        if(existing==null) {existing=new(type,name,[]);theme.Elements.Add(existing);}
                        foreach(var property in element.Elements())
                        {
                            var key=property.Name.LocalName;var value=Expand(property.Value.Trim());
                            existing.Properties[key]=(key is "path" or "fontPath") && value.Length>0?Resolve(file,value):value;
                        }
                    }
                }
            }
            finally {active.Remove(file);}
        }
        Read(entry,0);return theme;
    }
    public static string Import(string source,string dataDirectory)
    {
        var root=Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
        var parent=Path.Combine(Path.GetFullPath(dataDirectory),"themes");
        if(parent.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||root.StartsWith(parent+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||root.Equals(parent,StringComparison.OrdinalIgnoreCase))throw new IOException("Bitte einen Theme-Ordner außerhalb des verwalteten Theme-Verzeichnisses wählen.");
        // Validate before copying; no network references, links or executable theme code.
        if(File.Exists(Path.Combine(root,"capabilities.xml")))throw new IOException("ES-DE-Themes werden noch nicht unterstützt.");
        var files=new List<string>();long bytes=0;
        void Enumerate(string folder,int depth=0)
        {
            if(depth>32)throw new IOException("Theme-Ordner ist zu tief verschachtelt.");
            if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)throw new IOException("Theme enthält eine Ordnerverknüpfung.");
            foreach(var file in Directory.EnumerateFiles(folder)) {if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Theme enthält eine Dateiverknüpfung.");bytes+=new FileInfo(file).Length;files.Add(file);if(files.Count>10000||bytes>256L*1024*1024)throw new IOException("Theme ist zu groß (max. 256 MiB / 10.000 Dateien).");}
            foreach(var child in Directory.EnumerateDirectories(folder))Enumerate(child,depth+1);
        }
        Enumerate(root);
        var xmls=files.Where(f=>Path.GetFileName(f).Equals("theme.xml",StringComparison.OrdinalIgnoreCase)).ToArray();
        if(xmls.Length==0)throw new IOException("Der Ordner enthält keine theme.xml.");
        // Validate every system, not just the currently selected console.
        foreach(var system in EmulatorCatalog.Systems)Load(root,system);
        var destination=Path.Combine(parent,Path.GetFileName(root)+"-"+Guid.NewGuid().ToString("N"));
        try {Directory.CreateDirectory(destination);foreach(var file in files){var target=Path.Combine(destination,Path.GetRelativePath(root,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target);}return destination;}
        catch {if(Directory.Exists(destination))Directory.Delete(destination,true);throw;}
    }
}
