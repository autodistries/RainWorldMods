using BepInEx;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;






// Access private fields and stuff
using System.Security.Permissions;
using UnityEngine;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace EpicTelemetryBlocker; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

// These properties are properly set when building, so errors are to be expected in your IDE, but building will occur properly.
// If not, please check the .csproj !
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class TelemetryBlocker : BaseUnityPlugin
{
    public static TelemetryBlocker singleInstance;
    bool done = false;
    bool restartRequired = false;
    bool thereWasLareadyOne = false;
    private string originalModDesc;


    

    public TelemetryBlocker()
    {       // ./Managed/com.playeveryware.eos.core.dll
        singleInstance = this;
        Logger.LogInfo($"Using game path: {BepInEx.Paths.GameRootPath}");
        DirectoryInfo dataFolder = new(Path.Combine(BepInEx.Paths.GameRootPath, "RainWorld_Data"));
        FileInfo EpicLibrary = new(Path.Combine(dataFolder.FullName, "Managed", "com.playeveryware.eos.core.dll"));
        Logger.LogInfo($"Trying to find {EpicLibrary.FullName}");
        if (EpicLibrary.Exists)
        {
            Logger.LogWarning("Found soon-to-be active Epic Library. Renaming it so as it does not get loaded next time.");
            string newName = EpicLibrary.FullName.Replace(".dll", "_disabled.dll");
            if (File.Exists(newName))
            {
                Logger.LogWarning("THere was already a file at the target renamed name. Deleting previously disabled. Did the OG dll get re-downloaded somehow ?");
                thereWasLareadyOne = true;
                File.Delete(newName);
            }
            EpicLibrary.MoveTo(newName);
            restartRequired = true;
            On.RainWorld.Start += preventGameFromContinuing;

            Logger.LogInfo("Renamed Epic library. Excpect Epic to whine about it; but no impact should occur on anything. Game restart required");
            // UnityEngine.GUI.Window(0, new Rect(100, 100, 200, 150), PopupWindow, "Please restart the game");
        }
        else
        {
            Logger.LogInfo("Epic library not found. Nothing to do.");
        }

    }

    private void preventGameFromContinuing(On.RainWorld.orig_Start orig, RainWorld self)
    {
        Logger.LogInfo("RWGame constructo called. Making it useless because libraries are currently loaded");
        // orig(self);
    }

   

    private void OnGUI()
    {
        if (restartRequired)
        {
            // Create a window for the popup
            GUI.Window(0, new Rect(100, 100, 300, 150), PopupWindow, "Please restart the game");
        }

    }

    private void PopupWindow(int windowID)
    {
        // Popup content
        GUI.Label(new Rect(10, 20, 280, 115), $"Epic library was renamed and won't be loaded in the future.{(thereWasLareadyOne ? "\nA game update has probably downloaded it again, which is the reason for this to happen." : "")}\nSorry for the inconvenience.\nPress OK to exit.");
        if (GUI.Button(new Rect(5, 125, 290, 20), "OK"))
        {
            Application.Quit(0);
        }
    }

    



    private void OnEnable()
    {
        if (done) return;
        Logger.LogInfo("Hello World (I should already have done my stuff)! (just called shutdown ?)");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        done = true;
    }


 private void Awake()
    {
        try
        {
            var platformInterfaceType = typeof(PlatformInterface);
            var initializeMethod = platformInterfaceType.GetMethod(
                "Initialize",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(InitializeOptions).MakeByRefType() },
                null
            );

            if (initializeMethod != null)
            {
                Logger.LogInfo("Found PlatformInterface.Initialize method");
                
                // Create and apply the harmony patch
                Harmony.CreateAndPatchAll(typeof(TelemetryBlocker));
            }
            else
            {
                Logger.LogWarning("Could not find PlatformInterface.Initialize method");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error initializing EOS disabler: {ex.Message}");
        }
    }

 
    
    

    // private void OnDisable() {
    //     Logger.LogInfo("Disable called - restoring the library (actually this doesn't work. Yet)");
    //     Logger.LogInfo($"Using game path: {BepInEx.Paths.GameRootPath}");
    //     DirectoryInfo dataFolder = new(Path.Combine(BepInEx.Paths.GameRootPath, "RainWorld_Data"));
    //     FileInfo EpicLibraryOG = new(Path.Combine(dataFolder.FullName, "Plugins", "x86_64", "EOSSDK-Win64-Shipping.dll"));
    //     Logger.LogInfo($"Trying to find {EpicLibraryOG.FullName}");
    //     if (EpicLibraryOG.Exists)
    //     {
    //         Logger.LogInfo("Epic library already at its expected place. Nothing to do.");

    //     }
    //     else
    //     {
    //         Logger.LogInfo("OG Epic Library gone. Checking for _disabled.");
    //         FileInfo EpicLibraryDisabled = new(EpicLibraryOG.FullName.Replace(".dll", "_disabled.dll"));
    //         if (EpicLibraryDisabled.Exists)
    //         {
    //             Logger.LogInfo("Found _disabled. restoring original name");
    //             EpicLibraryDisabled.MoveTo(EpicLibraryOG.FullName);
    //         }
    //         Logger.LogError("No _disabled found. Has user messed with the files on their own ?");
    //     }
    //     Logger.LogInfo("Done.");
    // }



    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        On.Menu.Remix.InternalOI_Stats._PreviewMod += tellMeAboutIt;

    }

    private void tellMeAboutIt(On.Menu.Remix.InternalOI_Stats.orig__PreviewMod orig, Menu.Remix.InternalOI_Stats self, Menu.Remix.MenuModList.ModButton button)
    {
        orig(self, button);
        if (self.previewMod?.id == PluginInfo.PLUGIN_GUID && self.lblDescription?.text != null)
        {
            Logger.LogDebug("Previewing our own mod");
            originalModDesc ??= self.lblDescription.text;
            self.lblDescription.text = originalModDesc;
            self.lblDescription.text += restartRequired ? "\nEpic library has been renamed. Game restart is required for changes to apply - sorry !" : "\nEpic library was not found, all is well ! (expect Epic to whine in console)";
        }

    }
}
