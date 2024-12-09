using BepInEx;

// Access private fields and stuff
using System.Security;
using System.Security.Permissions;
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace ModName;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class ModMainClass : BaseUnityPlugin
{
    BepInEx.Logging.ManualLogSource lls;

    bool done = false;


    public ModMainClass()
    {
        lls = BepInEx.Logging.Logger.CreateLogSource("Mod Name");                     
    }

    private void Awake()
    {
        if (done) return;
        lls.LogInfo("Hello World !");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        done = true;
    }

  

    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
    }   
}
