using BepInEx;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.IO;
using static MoreLogs.PluginInfo;
using System;
using JetBrains.Annotations;
using BepInEx.Logging;
using On.RWCustom;
using System.Linq;


namespace MoreLogs;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]

public class MoreLogger : BaseUnityPlugin
{

    #region opts
    public System.Collections.Generic.Dictionary<Configurable<bool>, ModOptions.SomeUpdateFunction> Configurables = new();

    private Configurable<bool> IsLoggingEnabled;
    private Configurable<bool> LogAllThroughUnity;
    private Configurable<bool> SomeOtherBool;


    #endregion opts

    private ModOptions Options;
    bool done = false;
    int SyncLoadModStateCounter = 0;
    bool UnityLogsOn = true;

    public ManualLogSource MainRestoredLogSource { get; private set; }

    public MoreLogger()
    {
        try
        {
            Logger.LogInfo("ZZZ instanciated More Logger");
            Options = new ModOptions(this, Logger)
            {
                localMoreLogsInstance = this,
                Configurables = Configurables
            };


        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }


    [UsedImplicitly]
    private void Awake()
    {


        if (done) return;



        // basic hooking
        MainRestoredLogSource = BepInEx.Logging.Logger.CreateLogSource("Custom");

        Logger.LogInfo("Hooking setup methods...");


        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        On.PlayerProgression.SyncLoadModState += TriggerRestoreSettings;
        SetUpOptionsBindings();

        done = true;



        // detour
        Hook showLogsHook = new Hook(
     typeof(RainWorld).GetProperty("ShowLogs", BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
     typeof(MoreLogger).GetMethod("RainWorld_ShowLogs_get", BindingFlags.Static | BindingFlags.Public));
        log("Hooked RainWorld.ShowLogs with success");

        // string targetPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "mods","MoreLogs","ononce");
        // if (!File.Exists(targetPath))
        // {
        //     log("First launch ! Enabling BepInEx console. User should restart !");

        //     // drop the presence file in our directory
        //     FileStream fs = new FileStream(targetPath, FileMode.CreateNew);
        //     fs.Close();

        //     changeBepInConfig();
        // } else {
        //     Logger.LogInfo("Not setting to true cuz already exists");
        // }

    }

    internal void SetUpOptionsBindings()
    {
        //Binds the available options
        //Adds them to the global options list so that they can be initiated
        IsLoggingEnabled = Options.config.Bind<bool>("IsLoggingEnabled", true);
        Configurables.Add(IsLoggingEnabled, ToggleCustomLogReplacements);

        LogAllThroughUnity = Options.config.Bind<bool>("LogAllThroughUnity", false);
        Configurables.Add(LogAllThroughUnity, null);

        SomeOtherBool = Options.config.Bind<bool>("SomeOtherBool", true);
        Configurables.Add(SomeOtherBool, null);




    }

    internal void SetUpOptionsSettings()
    {
        Options.AddBoolOption(IsLoggingEnabled, "Hook Custom.Log*", ToggleCustomLogReplacements, "Hooks the Custom.Log* functions. This is immediately reflected.");
        Options.AddBoolOption(LogAllThroughUnity, "Log everything using Unity", ToggleUnityLogs, "This is not necessary");
        Options.AddBoolOption(SomeOtherBool, "useless button");
        Options.SeyupBepinCfg();
    }


    internal void ApplyCurrentOptions()
    {
        foreach (var pair in Configurables)
        {
            pair.Value?.Invoke(pair.Key);
            if (pair.Key.key == "LogAllThroughUnity")
            {
                UnityLogsOn = pair.Key.Value;
            }
        }
        Logger.LogInfo("Applied current configs");
    }


    private void ToggleCustomLogReplacements(Configurable<bool> configurable)
    {
        Logger.LogInfo("Setting Custom hooks to " + ((configurable.Value) ? "enabled" : "disabled"));
        if (configurable.Value)
        {
            On.RWCustom.Custom.Log += LogReplacement;
            //     On.RWCustom.Custom.LogImportant += LogImportantReplacement;
            //     On.RWCustom.Custom.LogWarning += LogWarningReplacement;
            if (Options.WasInitialized)
                Options.Tabs[0].items.First((item) =>
                item is Menu.Remix.MixedUI.OpCheckBox && (item as Menu.Remix.MixedUI.OpCheckBox).cfgEntry.key == "LogAllThroughUnity"
            ).Show();
        }
        else
        {

            On.RWCustom.Custom.Log -= LogReplacement;
            //     On.RWCustom.Custom.LogImportant -= LogImportantReplacement;
            //    On.RWCustom.Custom.LogWarning -= LogWarningReplacement;
            if (Options.WasInitialized) Options.Tabs[0].items.First((item) =>
            item is Menu.Remix.MixedUI.OpCheckBox && (item as Menu.Remix.MixedUI.OpCheckBox).cfgEntry.key == "LogAllThroughUnity"
        ).Hide();
        }
    }


    private void ToggleUnityLogs(Configurable<bool> configurable)
    {
        Logger.LogInfo("Setting unity logs to " + ((configurable.Value) ? "enabled" : "disabled"));
        UnityLogsOn = configurable.Value;

    }


    private void TriggerRestoreSettings(On.PlayerProgression.orig_SyncLoadModState orig, PlayerProgression self)
    {
        SyncLoadModStateCounter++;
        if (SyncLoadModStateCounter == 2)
        {
            ApplyCurrentOptions(); // can't really do it earlier
            Logger.LogInfo("Applied mod settings from Syncthing; unsubscribing");

            On.PlayerProgression.SyncLoadModState -= TriggerRestoreSettings;
        }
        orig(self);
    }


    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {

        orig(self);

        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, Options))
        {
            Logger.LogInfo("Registered Mod Interface");

        }
        else
        {
            Logger.LogError("Could not register Mod Interface");
        }
    }

    private void LogImportantReplacement(Custom.orig_LogImportant orig, string[] values)
    {
        string r = "";
        foreach (string value in values) r += value + " ";

        if (!UnityLogsOn) MainRestoredLogSource.LogMessage(r); else UnityEngine.Debug.LogWarning(r);
    }

    private void LogWarningReplacement(Custom.orig_LogWarning orig, string[] values)
    {
        string r = "";
        foreach (string value in values) r += value + " ";
        if (!UnityLogsOn) MainRestoredLogSource.LogWarning(r); else UnityEngine.Debug.LogWarning(r);

    }

    private void LogReplacement(Custom.orig_Log orig, string[] values)
    {
        string r = "";
        foreach (string value in values) r += value + " ";
        if (!UnityLogsOn) MainRestoredLogSource.LogInfo(r); else UnityEngine.Debug.Log(r);

    }




    internal string changeBepInConfig(string category, string option, string switchTo)
    {
        int targetIndex = -1;
        string[] array = null;
        try
        {
            array = File.ReadAllLines(Paths.BepInExConfigPath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Problems with bepincfg: \n{ex}");
        }
        string section = string.Empty;
        string cvalue = "";
        targetIndex = -2;
        for (int i = 0; i < array.Length; i++)
        {

            string text = array[i].Trim();
            if (text.StartsWith("#") || text == "")
            {
                continue;
            }

            if (text.StartsWith("[") && text.EndsWith("]"))
            {
                section = text.Substring(1, text.Length - 2);
                continue;
            }

            if (section != category)
            {
                continue;
            }
            targetIndex = -3;

            string[] array2 = text.Split(new char[1] { '=' }, 2);
            if (array2.Length == 2)
            {
                string key = array2[0].Trim();
                string text2 = array2[1].Trim();
                Logger.LogInfo($"Ok opt ? '{key}' '{text2}'");
                if (key == option)
                {
                    cvalue = text2;
                Logger.LogInfo($"ok!");

                    targetIndex = i;
                    break;

                }
            }
        }

        string outMsg = "";


        switch (targetIndex)
        {
            case -1:
                {
                    outMsg = $"Probably could not find, or is not accessible {Paths.BepInExConfigPath}";
                    break;
                }
            case -2:
                {
                    outMsg=($"Can't find section {category}");
                    break;
                }
            case -3:
                {
                    outMsg=($"can't find option {option} in section {category}");
                    break;
                }
            default:
                {

                    outMsg=($"OK, switching  {category}/{option} from {cvalue} to {switchTo} at line " + targetIndex);
                    array[targetIndex] = $"{option} = {switchTo}";
                    File.WriteAllLines(Paths.BepInExConfigPath, array);
                    // notifyRestartNeeded();
                    break;
                }
        }
        if (Options.statusInfoLabel != null) Options.statusInfoLabel.text = outMsg;
        log(outMsg);
        return outMsg;
    }



    public delegate bool orig_ShowLogs();

    public static bool RainWorld_ShowLogs_get(orig_ShowLogs orig)
    {
        return true;
    }

    private void log(object msg)
    {
        Logger.LogInfo(msg);
    }

}
