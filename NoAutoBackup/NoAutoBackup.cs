using BepInEx;



namespace NoAutoBackup; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class BackupsDisabler : BaseUnityPlugin
{

    bool done = false;
    private ModOptions options;

    public BackupsDisabler()
    {
    }

    public static bool modEnabled = true;

    private void Awake()
    {
        if (done) return;
        Logger.LogInfo("Awake. Hooked methods.");
        options = new ModOptions(Logger);

        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        On.PlayerProgression.CreateCopyOfSaves_bool += PreventSavingIfNotUserInitiated;
        done = true;
    }

    private void PreventSavingIfNotUserInitiated(On.PlayerProgression.orig_CreateCopyOfSaves_bool orig, PlayerProgression self, bool userCreated)
    {
        if (modEnabled && !userCreated) return;
        orig(self, userCreated);
    }

    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, options))
        {
            Logger.LogInfo("Registered Mod Interface");

        }
        else
        {
            Logger.LogError("Could not register Mod Interface");
        }
    }
}
