using BepInEx;
using System;
using System.Collections.Generic;
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

        // On.RegionState.RainCycleTick += tickPositions;
        // On.SaveState.RainCycleTick += tickPositions;

            // On.SaveState.LoadGame += tickPositions;
            // On.Menu.SlugcatSelectMenu.ContinueStartedGame += tickPositions;
            // On.Menu.SlugcatSelectMenu.StartGame += tickPositions;
            //try SlugcatSelectMenu.StartGame
            // SlugcatSelectMenu.ContinueStartedGame  !!!!!!!!!!
            // On.ProcessManager.RequestMainProcessSwitch_ProcessID_float += bordelfontionnnebatard;
            On.RainWorldGame.ctor += aledjemeurs;

        SlugcatPath.Logger = Logger;
        options = new ModOptions(Logger);        }
        catch (System.Exception ex)
        {
            
            Console.WriteLine("Ow, crash !" + ex);
        }

    }

    private void aledjemeurs(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
       orig(self, manager);
        Logger.LogInfo($"NEW RainWorldGame AAAAAAAAAAAAAAAAAAAAAAAA IsStorySession:{self.IsStorySession} safari:{self.rainWorld.safariMode} arena:{self.IsArenaSession}");

       if (self.IsStorySession  && !self.rainWorld.safariMode && !self.IsArenaSession) {
        List<SlugcatStats.Name> names = new();
        self.session.Players.ForEach((ac) => {

                if (ac.realizedCreature != null && ac.realizedCreature is Player p) names.Add( p.slugcatStats.name);
                else Logger.LogWarning("no realized player for creature "+ac);
            });
            Logger.LogInfo($"YEEHAWWWW ticking positions :33333333333333333333333333 we have {string.Join(", ",names)} players ");
        SlugcatPath.CycleTick();

       }
       
    }

    // private void bordelfontionnnebatard(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID_float orig, ProcessManager self, ProcessManager.ProcessID ID, float fadeOutSeconds)
    // {
    //     orig(self, ID, fadeOutSeconds);
    // }

    // private void tickPositions(On.Menu.SlugcatSelectMenu.orig_StartGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    // {
    //    orig(self, storyGameCharacter);
    //     Logger.LogWarning("BBBBBBBBB orig_ContinueStartedGame ran so we ticked");

    // }

    // private void tickPositions(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    // {
    //     orig(self, storyGameCharacter);
    //     Logger.LogWarning("AAAAAAAAAAAAAAAAA orig_ContinueStartedGame ran so we ticked");
    // }

    // private void tickPositions(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    // {
    //     orig(self, str, game);
    //     Logger.LogWarning("CCCCCCCCCCCCC loadgame ran so");
    //     // SlugcatPath.CycleTick();
    // }

    // private void tickPositions(On.SaveState.orig_RainCycleTick orig, SaveState self, RainWorldGame game, bool depleteSwarmRoom)
    // {
    //     orig(self, game, depleteSwarmRoom);
    //     Logger.LogInfo(" DDDDDDDDDDDDDDDDDDDD SaveState.orig_RainCycleTick kfapoekpek");    }

    private void notifyDestroyTextures(On.HUD.Map.orig_DestroyTextures orig, HUD.Map self)
    {
       orig(self);
       Logger.LogWarning("DESTROY TEXTURES");
       path.SetNewMap(null);
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
        if (self.isNPC) return;
        if (self.inShortcut || self.room == null || self.room.world == null || self.room.world.region == null) return;
        if (!ModOptions.doRecordData.Value) return;
       

        if (false && path.QueryMode() != SlugcatPath.MapMode.WRITEREAD) return;
        twoPerSecondPlease++;
        twoPerSecondPlease %= ModOptions.minTicksToRecordPoint.Value;
        if (twoPerSecondPlease != 0) return;
        path.CurrentRegion = self.room.world.region.name;
        if (SlugcatPath.slugcatRegionalPositions.TryGetValue(self.slugcatStats.name, out var regionalData) && regionalData.TryGetValue(self.room.world.region.name, out var positions) && positions.Count != 0 && (self.MapOwnerInRoomPosition - positions.LastOrDefault().pos).magnitude < ModOptions.minDistanceToRecordPointTimes100.Value/100.0f ) return;
        path.addNewPosition(self.slugcatStats.name, new(self.MapOwnerRoom, self.MapOwnerInRoomPosition));
    }

    private void createMapPather(On.HUD.Map.orig_Update orig, HUD.Map self)
    {
        orig(self);
      

      
   


    }

 

    private void Awake()
    {
        if (done) return;
        Logger.LogInfo("Awake.");
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
