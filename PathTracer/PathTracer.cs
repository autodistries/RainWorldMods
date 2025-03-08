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
    int manualSwitchDelay = 0;


    internal static SlugcatPath path = new();

    public static bool debug = false;
    private ModOptions options;

    public ModMainClass()
    {
        try
        {
            
        On.Player.Update += traceCurrentSlugcatPosition;
        
        On.HUD.Map.InitiateMapView += addLinesToMapper; // this is triggered when the map is hidden and gets shown

        On.HUD.Map.Draw += updateLinesWhenNeeded; // this is triggered all of the time a Map obj exists
        
        On.HUD.Map.ctor += updateMapObj;  
        On.HUD.Map.Update += dynamicShowUpdate;  
        On.HUD.Map.DestroyTextures += notifyDestroyTextures;

            On.RainWorldGame.ctor += aledjemeurs;

        SlugcatPath.Logger = Logger;
        options = new ModOptions(Logger);        }
        catch (System.Exception ex)
        {
            
            Console.WriteLine("Ow, crash !" + ex);
        }

    }

    private void dynamicShowUpdate(On.HUD.Map.orig_Update orig, HUD.Map self)
    {
        orig(self);
        if (manualSwitchDelay == 0)
        {
            if (Input.GetKey(ModOptions.toggleShowDataKeybind.Value))
            {
                if ((path.map?.visible) != true || ModOptions.doShowData.Value != false)
                {
                    ModOptions.doShowData.Value = false;
                    path.clearLines();
                }
                else
                {
                    ModOptions.doShowData.Value = true;
                    Logger.LogInfo("We're switching it on");
                    path.appendNewLines();
                    path.UpdateLines(1);
                }
                manualSwitchDelay = 20;
                Logger.LogInfo("SWITCHED SHOWDATA TO " + ModOptions.doShowData.Value);
            }
        }
        else if (manualSwitchDelay != 0) manualSwitchDelay--;


        if (Input.GetKeyDown(KeyCode.M)) {
            path.debuginfo();
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            path.OldPositionsCuller(SlugcatStats.Name.White, true);
        }
    }

    private void aledjemeurs(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
       orig(self, manager);
        if (ModMainClass.debug) Logger.LogInfo($"NEW RainWorldGame AAAAAAAAAAAAAAAAAAAAAAAA IsStorySession:{self.IsStorySession} safari:{self.rainWorld.safariMode} arena:{self.IsArenaSession}");

       if (self.IsStorySession  && !self.rainWorld.safariMode && !self.IsArenaSession) {
        List<SlugcatStats.Name> names = new();
        self.session.Players.ForEach((ac) => {

                if (ac.state is PlayerState { slugcatCharacter: SlugcatStats.Name nam }) names.Add( nam);
                else Logger.LogWarning("no realized player for creature "+ac);
                
            });
          if (ModMainClass.debug)  Logger.LogInfo($"YEEHAWWWW ticking positions :3333333333333333333333333");
        SlugcatPath.CycleTick(names);

       }
       
    }

    private void notifyDestroyTextures(On.HUD.Map.orig_DestroyTextures orig, HUD.Map self)
    {
       orig(self);
       if (ModMainClass.debug) Logger.LogWarning("DESTROY MAP TEXTURES");
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
        if (debug) Logger.LogInfo("Initiating map view!");
        path.appendNewLines();
    }

    private void updateLinesWhenNeeded(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
    {
        orig(self, timeStacker);

        if (self.lastFade != self.fade || (self.depth != self.lastDepth && self.visible) || (self.fade != 0 && self.panVel.magnitude >=0.01) || self.fade > 0f && self.fade < 1f)
            path.UpdateLines(timeStacker);
    }

    private void traceCurrentSlugcatPosition(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.isNPC) return;
        if (self.inShortcut || self.room == null || self.room.world == null || self.room.world.region == null) return;
        if (!ModOptions.doRecordData.Value) return;
       

        // if (false && path.QueryMode() != SlugcatPath.MapMode.WRITEREAD) return;
        twoPerSecondPlease++;
        twoPerSecondPlease %= ModOptions.minTicksToRecordPoint.Value;
        if (twoPerSecondPlease != 0) return;
        path.CurrentRegion = self.room.world.region.name;
        if (SlugcatPath.slugcatRegionalPositions.TryGetValue(self.slugcatStats.name, out var regionalData) && regionalData.TryGetValue(self.room.world.region.name, out var positions) && positions.Count != 0 && (self.MapOwnerInRoomPosition - positions.LastOrDefault().pos).magnitude < ModOptions.minDistanceToRecordPointTimes100.Value/100.0f ) return;
        path.addNewPosition(self.slugcatStats.name, new(self.MapOwnerRoom, self.MapOwnerInRoomPosition, self.bodyChunks[1].vel.magnitude));
        
    }


    private void Awake()
    {
        if (done) return;
        Logger.LogInfo("Awake");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        done = true;
        // Logger.LogInfo("Done with init !");
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
