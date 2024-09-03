using BepInEx;
using static SleepySlugcat.Utils;
using static SleepySlugcat.PluginInfo;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Drawing.Text;


namespace SleepySlugcat;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class SleepySlugcatto : BaseUnityPlugin
{


    List<bool> sleeping = new();//false
    List<bool> wakeUp = new();//false

    int debugCounter = 0;//0
    List<bool> forbidGrasps = new();//False
    List<FLabel> threatLabel = new();
    List<ThreatDetermination> currentThreat = new();
    public string colorMode = "random";
    private bool singleZs = false;
    private List<int> updatesSinceLastZPopped = new();








    BepInEx.Logging.ManualLogSource LocalLogSource;

    /// <summary>
    /// Registers hooks
    /// </summary>
    private void OnEnable()
    {
        TODO("Make Zs pop on wakeup ? +Configurable");
        LocalLogSource = BepInEx.Logging.Logger.CreateLogSource("SleepySlugcat");

        LocalLogSource.LogInfo("Hooking setup methods...");

        On.Player.Update += CheckForSleepySlugcat; //handles main logic
        On.Player.CanIPickThisUp += CanWeReallyGrabThatRn; // prevent grabbing when sleeping
        On.Player.Collide += WtfWeGotHit; //stop sleeping if something collides with us
        On.Player.Die += WhyDidIDie; // stop sleeping right befroe we die obviously
        On.Player.JollyEmoteUpdate += NoYouDont; // If jolly is enabled, its emote thing will overlap with our sleep thing and uncurl

        On.RainWorldGame.ctor += ResetDebuggingView; // reset variables and other things when re-entering the game

        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour; // mod options interface

    }


    /// <summary>
    /// Handles inputs and slugcat state updates
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="eu"></param>
    private void CheckForSleepySlugcat(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (!self.abstractCreature.world.game.Players.Any((player) => player == self.abstractCreature)) {
            orig(self, eu); 
            return;
        }

        if (self.abstractCreature.world.game.Players.Count != sleeping.Count)
        {
            LocalLogSource.LogInfo("filling in all lists to match players count ! (mod)" + self.abstractCreature.world.game.Players.Count);

            //clearLocalVariables();

            for (int i = 0; i < self.abstractCreature.world.game.Players.Count; i++)
            {
                
                LocalLogSource.LogInfo("adding player no "+i + "" + (self.room.game.Players[i].realizedCreature as Player).slugcatStats.name);
                LocalLogSource.LogInfo("i:"+i+" index according to game:"+self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)+" playerNumber:"+self.playerState.playerNumber );
                sleeping.Add(false);
                wakeUp.Add(false);
                forbidGrasps.Add(false);
                currentThreat.Add(new ThreatDetermination(i));
                currentThreat[i].Update(self.abstractCreature.world.game);
                threatLabel.Add(null);
                updatesSinceLastZPopped.Add(0);
                LocalLogSource.LogInfo("ok for p no "+i);

            }
        }

        //LocalLogSource.LogInfo($"We're at step 1. currentPlayerIndex will be={self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)} sleeping.Count:{sleeping.Count} updatesSinceLastZPopped.Count:{updatesSinceLastZPopped.Count}");

        // showZs(self);
        int currentPlayerIndex = self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature);
       
        if (abnormalStateFreshness > 40)
        {
            LocalLogSource.LogWarning("trying to exit abnormal state !");
            abnormalStateFreshness = 0;
            abnormalState = false;
        }
        if (abnormalState)
        {
            abnormalStateFreshness++; 
            orig(self, eu); 
            return;
        }

        try
        {

            if (sleeping[currentPlayerIndex])
            {
            if (!singleZs) {
                if (showZs(self)) {
                    updatesSinceLastZPopped[currentPlayerIndex] = 0;
                } else {
                    updatesSinceLastZPopped[currentPlayerIndex]++;
                }
            }
        }
        //LocalLogSource.LogDebug("We're at step 2");

        if (sleeping[currentPlayerIndex] &&
        (wakeUp[currentPlayerIndex] || self.input[currentPlayerIndex].y > 0 || self.input[currentPlayerIndex].x != 0 || self.input[currentPlayerIndex].jmp
        || currentThreat[currentPlayerIndex].currentThreat > 0.30f
        || self.bodyMode.value != "Crawl" || self.grabbedBy.Count != 0
        || self.dead || self.Submersion > 0.6f))
        {
             LocalLogSource.LogInfo("waking up rn");
            wakeUp[currentPlayerIndex] = false;
            self.forceSleepCounter = 0;
            sleeping[currentPlayerIndex] = false;
            forbidGrasps[currentPlayerIndex] = false;
        }
        debugCounter++;
        debugCounter %= 40;
       // LocalLogSource.LogDebug("We're at step 3");


        if (debugCounter % 10 == 0 || debugCounter % 10 == 1)
        {
            if (!self.dead) currentThreat[currentPlayerIndex].Update(self.abstractCreature.world.game);

            // showOrUpdateTheThreats(self); // debug thingie !
        }

       // LocalLogSource.LogDebug("We're at step 4");



        if (!sleeping[currentPlayerIndex] && self.Consious
        && !self.inShortcut // while in shortcuts, no ground, so IsTileSolid nullrefs
        && self.input[currentPlayerIndex].y < 0 && !self.input[currentPlayerIndex].jmp && !self.input[currentPlayerIndex].thrw && !self.input[currentPlayerIndex].pckp && Math.Abs(self.input[currentPlayerIndex].x) < 0.2f // Check for self.inputs: only down
        && self.IsTileSolid(1, 0, -1) //&& ((!self.IsTileSolid(1, -1, -1) || !self.IsTileSolid(1, 1, -1)) && self.IsTileSolid(1, self.input[0].x, 0)) // check if we have ground to sleep on
        && currentThreat[currentPlayerIndex].currentThreat < 0.15f // check if we feel threatened
        && !self.room.abstractRoom.shelter // do not nap while in shelter
        )
        {
            self.forceSleepCounter += 2;
            if (self.forceSleepCounter > 214)
            {
                self.forceSleepCounter = 262;
                self.LoseAllGrasps();


            }
            // LocalLogSource.LogInfo("P" + currenPlayerIndex + " " + self.forceSleepCounter + " " + self.sleepCurlUp + " " + self.sleepCounter);
        }


        else if (!sleeping[currentPlayerIndex] && self.forceSleepCounter > 0 && !self.room.abstractRoom.shelter)
        {
            self.forceSleepCounter--; // gradually decrease sleepiness if threshsold not reached
        }
        //LocalLogSource.LogDebug("We're at step 5");

        if (self.forceSleepCounter > 260)
        {
            sleeping[currentPlayerIndex] = true;
            forbidGrasps[currentPlayerIndex] = true;
        }
        //LocalLogSource.LogDebug("We're at step 6");
        } catch (Exception e) {
            abnormalState = true;
            Logger.LogError(e);
        }

        orig(self, eu);

    }

    private void clearLocalVariables()
    {
        LocalLogSource.LogInfo("clearing variables");
            sleeping.Clear();
            wakeUp.Clear();
            forbidGrasps.Clear();
            currentThreat.Clear();
            forbidGrasps.Clear();
            updatesSinceLastZPopped.Clear();
            foreach (var thing in threatLabel)
            {
                // LocalLogSource.LogInfo("removing debug view from");

                if (thing != null) Futile.stage.RemoveChild(thing);

            }
            threatLabel.Clear();
    }

    /// <summary>
    /// Prevent grabbing stuff if we are sleeping
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    private bool CanWeReallyGrabThatRn(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature) != -1 && forbidGrasps[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)]) return false;
        return orig(self, obj);
    }

    /// <summary>
    /// Wake up on Collision with anything
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="otherObject"></param>
    /// <param name="myChunk"></param>
    /// <param name="otherChunk"></param>
    private void WtfWeGotHit(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        if (sleeping[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)])
        {
            wakeUp[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] = true;
            self.forceSleepCounter = 0;
            // LocalLogSource.LogInfo("collided");
        }
        orig(self, otherObject, myChunk, otherChunk);
    }

    /// <summary>
    /// Uncurls if we were sleeping before dying, else we never stop sleeping
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void WhyDidIDie(On.Player.orig_Die orig, Player self)
    {
        if (sleeping[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] || self.sleepCurlUp > 0.5f)
        {
            wakeUp[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] = true;
            self.forceSleepCounter = 0;
            self.sleepCurlUp = 0f;
            // LocalLogSource.LogInfo("died");
        }
        orig(self);
    }

    /// <summary>
    /// Destructive way to prevent jolly's sleep emote from interacting with OUR curl state
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void NoYouDont(On.Player.orig_JollyEmoteUpdate orig, Player self)
    {
        return;
    }

    /// <summary>
    /// Clears variables when new game starts
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="manager"></param>
    private void ResetDebuggingView(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);
        Zs.decayEnabled = modOptions.ZsColorIsDecayOnConfigurable.Value;
        Zs.musician = modOptions.ZsIsSlugcatMusicianOnConfigurable.Value;
        Zs.text = modOptions.ZsTextContentConfigurable.Value;
        Zs.onlyZs = Zs.text.ToLower().All((el) => el == 'z');
        if (Zs.musician) Zs.text = "s";
        Zs.baseSizeVar = modOptions.ZsSizeVarianceConfigurable.Value;

         clearLocalVariables();
            
        
    }

    /// <summary>
    /// small chance of summonning a Z each time
    /// </summary>
    /// <param name="self"></param>
    /// <returns>true if Z was summonned</returns>
    private bool showZs(Player self)
    {
        if (updatesSinceLastZPopped[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] > 160 || UnityEngine.Random.value < (0.005 + modOptions.ZsQtyVarianceConfigurable.Value*0.015) && updatesSinceLastZPopped[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] > 25 - modOptions.ZsQtyVarianceConfigurable.Value * 10)
        {
          
            // LocalLogSource.LogInfo("Spawning a bubble P" + self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature) + " " + " " + self.forceSleepCounter + " " + self.sleepCurlUp + " " + self.sleepCounter + " mode: "+modOptions.ZsColorTypeConfigurable.Value + " rainbow:"+modOptions.ZsColorRainbowTypeConfigurable.Value);
            self.room.AddObject(
                new Zs(
                    self.bodyChunks[0].pos + RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * UnityEngine.Random.value * self.bodyChunks[0].rad + new Vector2((float)self.ThrowDirection * 2f, -2f),
                    (RWCustom.Custom.DegToVec(UnityEngine.Random.Range(-25f, 25f)) + new Vector2(self.ThrowDirection, 0f)) * 0.09f,
                    self.ThrowDirection,
                    getZsColor(self)
                )
                {
                    parentPlayerId = self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature),
                    rainbow = ((modOptions.ZsColorIsRainbowConfigurable.Value) ? modOptions.ZsColorRainbowTypeConfigurable.Value : ""),

                }
            );
            // singleZs=true;
            return true;

        }
        return false;
    }

    /// <summary>
    /// Gives the target Zs color in accordance to the config option
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public Color getZsColor(Player self)
    {
        switch (colorMode)
        {
            case "random":
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);

            case "slugcat":
                return slightColorVariation(self.ShortCutColor());

            case "custom":
                return slightColorVariation(modOptions.ZsColorPickConfigurable.Value);

            default:
                return new Color();
        }
    }

    /// <summary>
    /// gives room for a color to variate slightly based on wheight config
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public Color slightColorVariation(Color c)
    {
        float weight = 1f - modOptions.ZsColorVarianceConfigurable.Value;
        Color d = c;

        d.r = (float)Math.Sqrt((1 - weight) * Math.Pow(UnityEngine.Random.value, 2) + weight * Math.Pow(d.r, 2));
        d.g = (float)Math.Sqrt((1 - weight) * Math.Pow(UnityEngine.Random.value, 2) + weight * Math.Pow(d.g, 2));
        d.b = (float)Math.Sqrt((1 - weight) * Math.Pow(UnityEngine.Random.value, 2) + weight * Math.Pow(d.b, 2));
        return d;

    }

    #region RemixInterface
    private ModOptions modOptions;
    private bool abnormalState = false;
    private int abnormalStateFreshness;

    public SleepySlugcatto()
    {
        modOptions = new ModOptions(this);
    }
    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {

        orig(self);

        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, modOptions))
        {
            LocalLogSource.LogInfo("Registered Mod Interface");
        }
        else
        {
            LocalLogSource.LogError("Could not register Mod Interface");
        }
    }
    #endregion RemixInterface

    private void showOrUpdateTheThreats(Player self) //this is a debug function btw
    {
        if (!self.dead && threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] == null)
        {

            // LocalLogSource.LogInfo("threatd ok");
            string text = $"threat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].threat}\ncurrentThreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].currentThreat}\n musicAgnosticThreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].musicAgnosticThreat}\ncurrentMusicAgnosticThreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].currentMusicAgnosticThreat}\n{debugCounter}";

            threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] = new FLabel(RWCustom.Custom.GetFont(), text)
            {
                alignment = FLabelAlignment.Left,
                x = 100.2f + self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature) * 200f,
                y = RWCustom.Custom.rainWorld.options.ScreenSize.y - 50.2f
            };

            // LocalLogSource.LogInfo("label creation ok ok");

            Futile.stage.AddChild(threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)]);
            // LocalLogSource.LogInfo("addding label ok");
        }
        else if (threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] != null)
        {
            string text = $"P{self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)}\nthreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].threat}\ncurrentThreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].currentThreat}\n musicAgnosticThreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].musicAgnosticThreat}\ncurrentMusicAgnosticThreat: {currentThreat[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].currentMusicAgnosticThreat}\n{debugCounter}";

            threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)].text = text;

            if (self.dead)
            {
                // LocalLogSource.LogInfo("removing debug info for P" + self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature));
                Futile.stage.RemoveChild(threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)]);
                threatLabel[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] = null;
            }
        }

    }

}
