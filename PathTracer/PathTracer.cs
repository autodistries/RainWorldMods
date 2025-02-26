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

    internal static SlugcatPath path = new();

    public static bool debug = true;
    private ModOptions options;

    public ModMainClass()
    {
        try
        {
            
        On.Player.Update += traceCurrentSlugcatPosition;
        
        On.HUD.Map.Update += createMapPather; // this is a debug function and can be removed
        On.HUD.Map.InitiateMapView += addLinesToMapper; // this is triggered when the map is hidden and gets shown

        On.HUD.Map.Draw += updateMapPather; // this is triggered all of the time a Map obj exists
        
        On.HUD.Map.ctor += updateMapObj;    

        On.RegionState.RainCycleTick += clearPastDataPlease;

        SlugcatPath.Logger = Logger;
        options = new ModOptions(Logger);        }
        catch (System.Exception ex)
        {
            
            Console.WriteLine("Ow, crash !" + ex);
        }

    }

    private void clearPastDataPlease(On.RegionState.orig_RainCycleTick orig, RegionState self, int ticks, int foodRepBonus)
    {
        orig(self, ticks, foodRepBonus);
        if (ModOptions.doClearDataOnNewCycle.Value) {
            Logger.LogInfo("New Cycle Tick, clearing past data");
            path.clearLines();
            path.clearPositions();
            SlugcatPath.lastNRooms.Clear();
            var slugcat = path.GetSlugcat();
            if (slugcat == null)slugcat = self.world.game.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
            MetaPathStore.StoreSlugcat(new(), self.world.game.rainWorld.options.saveSlot, slugcat);
        }
    }

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
        if (!ModOptions.doRecordData.Value) return;
        if (path.map == null || self.slugcatStats.name != path.GetSlugcat()) {
            Logger.LogWarning($"Not registering position of lsugcat {self.slugcatStats.name} as {((path.map == null) ? "map is null" : $"sc is supposed to be {path.GetSlugcat()}")}");
            return;
        }
        twoPerSecondPlease++;
        twoPerSecondPlease %= ModOptions.minTicksToRecordPoint.Value;
        if (twoPerSecondPlease != 0) return;
        if (path.slugcatPositions.ContainsKey(path.CurrentRegion) && path.slugcatPositions[path.CurrentRegion].Count != 0 && (self.MapOwnerInRoomPosition - path.slugcatPositions[path.CurrentRegion].LastOrDefault().pos).magnitude < ModOptions.minDistanceToRecordPointTimes100.Value/100.0f ) return;
        path.addNewPosition(new(self.MapOwnerRoom, self.MapOwnerInRoomPosition));
        // Logger.LogInfo($"Added {path.slugcatPositions.LastOrDefault()}");
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
            MetaPathStore.WriteColdFiles();

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
