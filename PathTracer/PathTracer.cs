using BepInEx;
using System;
using System.IO;
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
        // On.HUD.Map.Update += updateMapObjAgain;  
        On.HUD.Map.DestroyTextures += notifyDestroyTextures;

        On.RegionState.RainCycleTick += clearPastDataPlease;

        SlugcatPath.Logger = Logger;
        options = new ModOptions(Logger);        }
        catch (System.Exception ex)
        {
            
            Console.WriteLine("Ow, crash !" + ex);
        }

    }

    private void notifyDestroyTextures(On.HUD.Map.orig_DestroyTextures orig, HUD.Map self)
    {
       orig(self);
       Logger.LogWarning("DESTROY TEXTURES");
    }

    private void clearPastDataPlease(On.RegionState.orig_RainCycleTick orig, RegionState self, int ticks, int foodRepBonus)
    {
        orig(self, ticks, foodRepBonus);
        if (ModOptions.doClearDataOnNewCycle.Value) {
            SlugcatPath.cycleTick = true;
            Logger.LogInfo("Cycle tick set to true");
        }
    }

    private void updateMapObj(On.HUD.Map.orig_ctor orig, HUD.Map self, HUD.HUD hud, HUD.Map.MapData mapData)
    {
        orig(self, hud, mapData);
        if (debug) Logger.LogInfo("Changed map obj!");
        path.SetNewMap(self);

    }

    private void addLinesToMapper(On.HUD.Map.orig_InitiateMapView orig, HUD.Map self)
    {
        orig(self);
        if (debug) Logger.LogInfo("Initiating view!");
        path.appendNewLines();
    }

    private void updateMapPather(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
    {
        orig(self, timeStacker);

        if (self.lastFade != self.fade || self.depth != self.lastDepth || (self.fade != 0 && self.panVel.magnitude >=0.01) || self.fade is not 0 or 1)
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
        if (path.QueryMode() != SlugcatPath.MapMode.WRITEREAD) return;
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
      

      
        if (Input.GetKeyDown("l"))
        {
            Logger.LogInfo($"forcing data load : for ss {self.hud.rainWorld.options.saveSlot} sc {path.GetSlugcat()}");
            path.SetNewPositions(self.hud.rainWorld.options.saveSlot, path.GetSlugcat());
            Logger.LogInfo($"data load end for ss {self.hud.rainWorld.options.saveSlot} sc {path.GetSlugcat()}");


        }
        if (Input.GetKeyDown("j")) {
            Logger.LogInfo("Forcing write cold files");
            MetaPathStore.modifiedSinceLastWrite = true;
            MetaPathStore.WriteColdFiles();
            MetaPathStore.WriteColdFileCustomAsync();
            Logger.LogInfo("End write cold files");

        }

        if (Input.GetKeyDown("k")) {
            Logger.LogInfo($"forcing data store : for ss {self.hud.rainWorld.options.saveSlot} sc {path.GetSlugcat()} regon {path.CurrentRegion}");

                            MetaPathStore.StoreRegion(path.slugcatPositions.regionDataOrNew(path.CurrentRegion), path.map.hud.rainWorld.options.saveSlot, path.GetSlugcat(), path.CurrentRegion);
            Logger.LogInfo($"end force data store : for ss {self.hud.rainWorld.options.saveSlot} sc {path.GetSlugcat()} regon {path.CurrentRegion}");

        }

                  if (Input.GetKeyDown("h")) {
            Logger.LogInfo("Forcing READ files");
            MetaPathStore.TryLoadFromCold();
            Logger.LogInfo("End READ files");
        }

        if (Input.GetKeyDown("n")) {
            Logger.LogInfo("Forcing write CUSTOM files");
            MetaPathStore.modifiedSinceLastWrite = true;
            MetaPathStore.WriteColdFileCustomAsync();
            Logger.LogInfo("End write CUSTOM files");
        }

                if (Input.GetKeyDown("v")) {
            Logger.LogInfo("Forcing READ CUSTOM files");
            MetaPathStore.TryLoadFromColdCustom();
            Logger.LogInfo("End READ CUSTOM files");
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
