using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace EmuVora.Core;

public static class EsDeFrontend
{
    public const string ThemeName="alekfull-nx-es-de";
    public static readonly EmulatorPackage Engine=new("esde","3.5.0","https://gitlab.com/es-de/emulationstation-de/-/package_files/357718057/download","0231edd066a36a9d22d280631cda6b6c1b2afa6dca699bef20e4956a624b7801","ES-DE.exe");
    public static readonly EmulatorPackage Alekfull=new("alekfull-nx","645b2bcc2248e93af8f0f4461a6ee7367fb89365","https://codeload.github.com/anthonycaccese/alekfull-nx-es-de/zip/645b2bcc2248e93af8f0f4461a6ee7367fb89365","9b11b906ec9d785c101bcb63086a8dc9cf140a1a33febab226676ecf5cd32e54","capabilities.xml");
    public static string Home(string data)=>Path.Combine(Path.GetFullPath(data),"frontend");
    public static string AppData(string data)=>Path.Combine(Home(data),"ES-DE");
    public static string GameId(Game game)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(game.Path).ToUpperInvariant()))).ToLowerInvariant();
    public static string SystemId(string system)=>system switch
    {
        "Atari Lynx"=>"atarilynx","Atari Jaguar"=>"atarijaguar","WonderSwan"=>"wonderswan","WonderSwan Color"=>"wonderswancolor","32X"=>"sega32x",
        "Arcade (FinalBurn Neo)"=>"fbneo","Arcade (MAME)"=>"mame",_=>EmulationStationTheme.SystemId(system)
    };
    public static string ThemeId(string system)=>system is "Arcade (FinalBurn Neo)" or "Arcade (MAME)"?"arcade":SystemId(system);
    public static string NativeSystem(string system)=>"emuvora-"+SystemId(system);
    private static XElement ReadXml(string file)
    {
        using var reader=XmlReader.Create(file,new(){DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=32*1024*1024});
        return XDocument.Load(reader).Root??throw new IOException("XML-Datei ist leer: "+file);
    }
    public static async Task InstallAsync(string data,AppSettings settings,PackageManager manager,IProgress<DownloadProgress>? progress=null,CancellationToken token=default)
    {
        async Task<string> Install(EmulatorPackage package)
        {
            if(settings.Packages.TryGetValue(package.Id,out var installed)&&installed.Version==package.Version&&File.Exists(installed.ExecutablePath))return installed.ExecutablePath;
            var previous=settings.Packages.GetValueOrDefault(package.Id)?.ExecutablePath;
            var result=await manager.InstallAsync(package,Path.Combine(data,"emulators"),progress,token);
            settings.Packages[package.Id]=new(package.Version,result,previous);new SettingsStore(Path.Combine(data,"settings.json")).Save(settings);return result;
        }
        settings.EsDeExecutable=await Install(Engine);
        var themeSource=Path.GetDirectoryName(await Install(Alekfull))!;
        var themeTarget=Path.Combine(AppData(data),"themes",ThemeName);
        // Leave ES-DE theme customizations intact when the pinned version is unchanged.
        var marker=Path.Combine(themeTarget,".emuvora-version");
        if(!File.Exists(marker)||File.ReadAllText(marker)!=Alekfull.Version)
        {
            await Task.Run(()=>
            {
                foreach(var file in Directory.EnumerateFiles(themeSource,"*",new EnumerationOptions{RecurseSubdirectories=true,AttributesToSkip=FileAttributes.ReparsePoint}))
                {token.ThrowIfCancellationRequested();var target=Path.Combine(themeTarget,Path.GetRelativePath(themeSource,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);}
                AtomicFile.WriteText(marker,Alekfull.Version);
            },token);
        }
    }
    private static void Set(XElement root,string kind,string name,string value)
    {
        var element=root.Elements(kind).FirstOrDefault(e=>(string?)e.Attribute("name")==name);
        if(element==null){element=new XElement(kind,new XAttribute("name",name));root.Add(element);}element.SetAttributeValue("value",value);
    }
    public static void Export(string data,string launcher,IReadOnlyList<Game> games,AppSettings settings)
    {
        if(!Path.IsPathFullyQualified(launcher)||!File.Exists(launcher)||!Path.GetExtension(launcher).Equals(".exe",StringComparison.OrdinalIgnoreCase))throw new IOException("EmuVora-Programmdatei fehlt.");
        var app=AppData(data);Directory.CreateDirectory(app);var romRoot=Path.Combine(Home(data),"ROMs");Directory.CreateDirectory(romRoot);
        var systems=new XElement("systemList");
        var known=games.Where(g=>EmulatorCatalog.Systems.Contains(g.System)).GroupBy(g=>g.System);
        foreach(var group in known)
        {
            var native=NativeSystem(group.Key);var systemFolder=Path.Combine(romRoot,native);Directory.CreateDirectory(systemFolder);
            var listFile=Path.Combine(app,"gamelists",native,"gamelist.xml");
            Dictionary<string,XElement> old=File.Exists(listFile)?ReadXml(listFile).Elements("game").Where(g=>g.Element("path")!=null).GroupBy(g=>(string)g.Element("path")!).ToDictionary(g=>g.Key,g=>g.First()):[];
            var list=new XElement("gameList");
            foreach(var game in group)
            {
                var id=GameId(game);var relative="./"+id+".emuvora";var stub=Path.Combine(systemFolder,id+".emuvora");
                if(!File.Exists(stub))AtomicFile.WriteText(stub,"EmuVora library reference. Original ROM: "+game.Path);
                var element=old.TryGetValue(relative,out var existing)?new XElement(existing):new XElement("game");
                element.SetElementValue("path",relative);element.SetElementValue("name",game.Title);element.SetElementValue("favorite",game.Favorite?"true":"false");element.SetElementValue("playcount",game.PlayCount);
                if(game.LastPlayed is{} last)element.SetElementValue("lastplayed",last.UtcDateTime.ToString("yyyyMMdd'T'HHmmss",CultureInfo.InvariantCulture));
                list.Add(element);
                if(settings.Covers.TryGetValue(game.Path,out var cover)&&File.Exists(cover))
                {
                    var extension=Path.GetExtension(cover).ToLowerInvariant();if(extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))continue;
                    if(extension==".jpeg")extension=".jpg";
                    var target=Path.Combine(app,"downloaded_media",native,"covers",id+extension);Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    if(!Path.GetFullPath(cover).Equals(Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase))File.Copy(cover,target,true);
                }
            }
            AtomicFile.WriteText(listFile,new XDocument(list).ToString());
            var command=$"\"{launcher}\" --esde-launch \"%BASENAME%\" --data-dir \"{Path.GetFullPath(data)}\"";
            systems.Add(new XElement("system",new XElement("name",native),new XElement("fullname",group.Key),new XElement("path","%ROMPATH%/"+native),new XElement("extension",".emuvora"),new XElement("command",new XAttribute("label","EmuVora"),command),new XElement("platform",group.Key.StartsWith("Arcade")?"arcade":SystemId(group.Key)),new XElement("theme",ThemeId(group.Key))));
        }
        AtomicFile.WriteText(Path.Combine(app,"custom_systems","es_systems.xml"),new XDocument(systems).ToString());
        var settingsFile=Path.Combine(app,"settings","es_settings.xml");var nativeSettings=File.Exists(settingsFile)?ReadXml(settingsFile):new XElement("config");
        Set(nativeSettings,"string","ROMDirectory",romRoot);Set(nativeSettings,"string","MediaDirectory",Path.Combine(app,"downloaded_media"));
        if(!nativeSettings.Elements("string").Any(e=>(string?)e.Attribute("name")=="Theme"))Set(nativeSettings,"string","Theme",ThemeName);
        Set(nativeSettings,"string","InputControllerType","xbox");Set(nativeSettings,"bool","ParseGamelistOnly","true");Set(nativeSettings,"string","ApplicationLanguage","de_DE");Set(nativeSettings,"bool","ApplicationUpdaterPrereleases","false");
        Set(nativeSettings,"bool","ScraperSearchFileHash","false");Set(nativeSettings,"bool","ScraperSearchMetadataName","true");
        if(!File.Exists(settingsFile)){Set(nativeSettings,"string","ThemeColorScheme","dark-original");Set(nativeSettings,"string","ThemeVariant","gamelist-metadata-on-boxart");Set(nativeSettings,"string","ThemeFontSize","medium");Set(nativeSettings,"string","ThemeAspectRatio","automatic");}
        AtomicFile.WriteText(settingsFile,new XDocument(nativeSettings).ToString());
    }
    public static void ImportChanges(string data,LibraryStore store,AppSettings settings)
    {
        foreach(var group in store.GetAll().Where(g=>EmulatorCatalog.Systems.Contains(g.System)).GroupBy(g=>g.System))
        {
            var native=NativeSystem(group.Key);var file=Path.Combine(AppData(data),"gamelists",native,"gamelist.xml");if(!File.Exists(file))continue;
            var entries=ReadXml(file).Elements("game").Where(e=>e.Element("path")!=null).GroupBy(e=>(string)e.Element("path")!).ToDictionary(e=>e.Key,e=>e.First());
            foreach(var game in group)
            {
                var id=GameId(game);
                if(entries.TryGetValue("./"+id+".emuvora",out var entry))
                {
                    var favorite=bool.TryParse((string?)entry.Element("favorite"),out var value)?value:game.Favorite;
                    var count=int.TryParse((string?)entry.Element("playcount"),out var parsed)?Math.Max(game.PlayCount,parsed):game.PlayCount;var last=game.LastPlayed;
                    if(DateTimeOffset.TryParseExact((string?)entry.Element("lastplayed"),"yyyyMMdd'T'HHmmss",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var nativeLast)&&(last==null||nativeLast>last))last=nativeLast;
                    store.Update(game with{Favorite=favorite,PlayCount=count,LastPlayed=last});
                }
                var covers=Path.Combine(AppData(data),"downloaded_media",native,"covers");
                if(!settings.ProtectedCovers.Contains(game.Path)&&Directory.Exists(covers)){var cover=new[]{".png",".jpg",".jpeg",".webp"}.Select(ext=>Path.Combine(covers,id+ext)).FirstOrDefault(File.Exists);if(cover!=null)settings.Covers[game.Path]=cover;}
            }
        }
    }
    public static ProcessStartInfo CreateStartInfo(string executable,string data)
    {
        if(!File.Exists(executable))throw new IOException("ES-DE wurde noch nicht installiert.");
        var start=new ProcessStartInfo(executable){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(executable)!};
        start.ArgumentList.Add("--home");start.ArgumentList.Add(Home(data));start.ArgumentList.Add("--gamelist-only");start.ArgumentList.Add("--no-update-check");return start;
    }
}
