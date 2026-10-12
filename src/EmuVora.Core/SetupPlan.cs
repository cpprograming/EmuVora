namespace EmuVora.Core;
public static class SetupPlan
{
    public static EmulatorDefinition[] RequiredEmulators(IEnumerable<string> systems,AppSettings settings)
    {
        var names=systems.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if(names.Length==0)return [EmulatorCatalog.Get("retroarch")];
        return names.Select(system=>EmulatorCatalog.Known.FirstOrDefault(e=>e.Id==settings.PreferredEmulators.GetValueOrDefault(system)&&e.Systems.Contains(system))??EmulatorCatalog.Known.FirstOrDefault(e=>e.Systems.Contains(system))).Where(e=>e!=null).Cast<EmulatorDefinition>().DistinctBy(e=>e.Id).ToArray();
    }
    public static string? FirmwareHint(string system,string data,AppSettings settings)
    {
        var definition=RequiredEmulators([system],settings).FirstOrDefault();
        var exe=definition==null?null:settings.EmulatorPaths.GetValueOrDefault(definition.Id);
        return EmulatorSetup.FirmwareHint(system,data,definition!=null&&exe!=null&&File.Exists(exe)?EmulatorSetup.ConfiguredFirmwareDirectory(definition.Id,exe,data):null);
    }
}
