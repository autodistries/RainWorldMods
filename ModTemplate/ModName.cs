using BepInEx;

// Access private fields and stuff
using System.Security;
using System.Security.Permissions;
#pragma warning disable CS0618 // SecurityAction.RequestMinimum is obsolete. However, this does not apply to the mod, which still needs it. Suppress the warning indicating that it is obsolete.
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ModName; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

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
