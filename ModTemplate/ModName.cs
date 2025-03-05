using BepInEx;


// Access private fields and stuff
using System.Security.Permissions;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ModName; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

// These properties are properly set when building, so errors are to be expected in your IDE, but building will occur properly.
// If not, please check the .csproj !
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class ModMainClass : BaseUnityPlugin
{

    bool done = false;

    public ModMainClass()
    {
    }

    private void OnEnabled()
    {
        if (done) return;
        Logger.LogInfo("Hello World !");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        done = true;
    }

  

    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
    }   
}
