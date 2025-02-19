using BepInEx;



namespace NoAutoBackup; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class BackupsDisabler : BaseUnityPlugin
{
    BepInEx.Logging.ManualLogSource lls;

    bool done = false;


    public BackupsDisabler()
    {
        lls = BepInEx.Logging.Logger.CreateLogSource(GetType().ToString());                     
    }

    private void Awake()
    {
        if (done) return;
        lls.LogInfo("Hello World !");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        On.PlayerProgression.CreateCopyOfSaves_bool += PreventSavingIfNotUserInitiated;
        done = true;
    }

    private void PreventSavingIfNotUserInitiated(On.PlayerProgression.orig_CreateCopyOfSaves_bool orig, PlayerProgression self, bool userCreated)
    {
        if (!userCreated) return;
        orig(self, userCreated);
    }

    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
    }   
}
