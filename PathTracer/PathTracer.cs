using BepInEx;
using System;
using System.Linq;



// Access private fields and stuff
using System.Security;
using System.Security.Permissions;
using UnityEngine;
#pragma warning disable CS0618 // SecurityAction.RequestMinimum is obsolete. However, this does not apply to the mod, which still needs it. Suppress the warning indicating that it is obsolete.
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace PathTracer; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class ModMainClass : BaseUnityPlugin
{
    BepInEx.Logging.ManualLogSource lls;

    bool done = false;
    private bool modEnabled = true;
    int twoPerSecondPlease = 0;

    SlugcatPath path = new();

    public static bool debug = true;
    private ModOptions options;

    public ModMainClass()
    {
        On.Player.Update += traceCurrentSlugcatPosition;
        
        On.HUD.Map.Update += createMapPather; // this is a debug function and can be removed
        On.HUD.Map.InitiateMapView += addLinesToMapper; // this is triggered when the map is hidden and gets shown

        On.HUD.Map.Draw += updateMapPather; // this is triggered all of the time a Map obj exists
        
        On.HUD.Map.ctor += updateMapObj;    


        // On.HUD.HUD.ResetMap += storeCurrentRegion;

        // On.PlayerProgression.LoadMapTexture += clearPaths;
        // On.HUD.Map.ResetReveal += resetMapPather;
        SlugcatPath.Logger = Logger;
        options = new ModOptions(Logger);
    }

    // private void storeCurrentRegion(On.HUD.HUD.orig_ResetMap orig, HUD.HUD self, HUD.Map.MapData mapData)
    // {
    //     Logger.LogInfo("ResetMap!!");
    //     MetaPathStore.SyncColdFiles();
    //     orig(self, mapData);
    // }

    // private void clearPaths(On.PlayerProgression.orig_LoadMapTexture orig, PlayerProgression self, string regionName)
    // {
    //     path.checkDirtyPositions(regionName);

    //     orig(self ,regionName);
    // }

    private void updateMapObj(On.HUD.Map.orig_ctor orig, HUD.Map self, HUD.HUD hud, HUD.Map.MapData mapData)
    {
        orig(self, hud, mapData);
        path.appendNewLines(newMap: self);
        if (debug) Logger.LogInfo("Changed map obj!");

    }

    private void addLinesToMapper(On.HUD.Map.orig_InitiateMapView orig, HUD.Map self)
    {
        orig(self);
        if (debug) Logger.LogInfo("Initiating view!");
        path.appendNewLines(self);
    }

    // private void resetMapPather(On.HUD.Map.orig_ResetReveal orig, HUD.Map self)
    // {
    //     orig(self);
    //     path.processLines(self);
    //     Logger.LogInfo("REsetReveal !");
    // }

    private void updateMapPather(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
    {
        orig(self, timeStacker);
        // if (self.lastFade == self.fade && self.fade == 0f ) return;
        if (self.lastFade == self.fade) {
            if ((self.fade != 1 || self.panVel.magnitude <= 0.1) && self.depth == self.lastDepth) return;
        } 
        path.UpdateLines(timeStacker);
    }

    private void traceCurrentSlugcatPosition(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        twoPerSecondPlease++;
        twoPerSecondPlease %= 60;
        if (twoPerSecondPlease != 0) return;
        if (path.slugcatPositions.ContainsKey(path.CurrentRegion) && path.slugcatPositions[path.CurrentRegion].Count != 0 && (self.MapOwnerInRoomPosition - path.slugcatPositions[path.CurrentRegion].LastOrDefault().pos).magnitude < 0.4 ) return;
        path.addNewPosition(new(self.MapOwnerRoom, self.MapOwnerInRoomPosition));
        Logger.LogInfo($"Added {path.slugcatPositions.LastOrDefault()}");
    }

    private void createMapPather(On.HUD.Map.orig_Update orig, HUD.Map self)
    {
        orig(self);
      

        if (Input.GetKeyDown("m"))
        {
            path.appendNewLines(self);
        }

        if (Input.GetKeyDown("l"))
        {
            debug = !debug;
            Logger.LogInfo($"debug : {debug}");
        }
        if (Input.GetKeyDown("j")) {
            MetaPathStore.SyncColdFiles();

        }

        if (Input.GetKeyDown("k")) {
            MetaPathStore.TryLoadFromCold();
        }

        if (Input.GetKeyDown("h")) {
            Logger.LogInfo($"Current data obj :\n{MetaPathStore.DescribeData()}\n");
        }

    }

 

    private void Awake()
    {
        if (done) return;
        Logger.LogInfo("Awake.");
        MetaPathStore.TryLoadFromCold(); // load previous data from disk
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        done = true;
        Logger.LogInfo("Done with init !");
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
