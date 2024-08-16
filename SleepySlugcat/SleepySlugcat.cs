using BepInEx;
using static SleepySlugcat.Utils;
using static SleepySlugcat.PluginInfo;
using UnityEngine;
using System.Collections.Generic;
using System;


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

    private Dictionary<int, int> playerNumberEquivalent = new(); // because they could choose White and Red (0 and 2)



    private int translatedPlayerNumber(int i) {
        return playerNumberEquivalent[i];
    }


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
        if (self.abstractCreature.world.game.Players.Count != sleeping.Count)
        {
            LocalLogSource.LogInfo("filling in all lists to match players count ! (mod)" + self.abstractCreature.world.game.Players.Count);

            

            for (int i = 0; i < self.abstractCreature.world.game.Players.Count; i++)
            {
                playerNumberEquivalent.Add((self.room.game.Players[i].realizedCreature as Player).slugcatStats.name.Index, i);
                LocalLogSource.LogInfo("i:"+i+" index:"+(self.room.game.Players[i].realizedCreature as Player).slugcatStats.name.Index+ "translation:"+translatedPlayerNumber((self.room.game.Players[i].realizedCreature as Player).slugcatStats.name.Index)+" playerstate:"+self.playerState.playerNumber );
                sleeping.Add(false);
                wakeUp.Add(false);
                forbidGrasps.Add(false);
                currentThreat.Add(new ThreatDetermination((self.room.game.Players[i].realizedCreature as Player).slugcatStats.name.Index));
                currentThreat[i].Update(self.abstractCreature.world.game);
                threatLabel.Add(null);
                LocalLogSource.LogInfo("ok for p no "+i);
            }
        }

        //LocalLogSource.LogInfo("We're at step 1");

        // showZs(self);

        if (sleeping[translatedPlayerNumber(self.playerState.playerNumber)])
        {
            if (!singleZs) showZs(self);
        }
        if (sleeping[translatedPlayerNumber(self.playerState.playerNumber)] &&
        (wakeUp[translatedPlayerNumber(self.playerState.playerNumber)] || self.input[translatedPlayerNumber(self.playerState.playerNumber)].y > 0 || self.input[translatedPlayerNumber(self.playerState.playerNumber)].x != 0 || self.input[translatedPlayerNumber(self.playerState.playerNumber)].jmp
        || currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].currentThreat > 0.30f
        || self.bodyMode.value != "Crawl" || self.grabbedBy.Count != 0
        || self.dead || self.Submersion > 0.6f))
        {
             LocalLogSource.LogInfo("waking up rn");
            wakeUp[translatedPlayerNumber(self.playerState.playerNumber)] = false;
            self.forceSleepCounter = 0;
            sleeping[translatedPlayerNumber(self.playerState.playerNumber)] = false;
            forbidGrasps[translatedPlayerNumber(self.playerState.playerNumber)] = false;
        }
        debugCounter++;
        debugCounter %= 40;
        //LocalLogSource.LogDebug("We're at step 2");


        if (debugCounter % 10 == 0 || debugCounter % 10 == 1)
        {
            if (!self.dead) currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].Update(self.abstractCreature.world.game);

            // showOrUpdateTheThreats(self); // debug thingie !
        }




        if (!sleeping[translatedPlayerNumber(self.playerState.playerNumber)] && self.Consious
        && !self.inShortcut // while in shortcuts, no ground, so IsTileSolid nullrefs
        && self.input[translatedPlayerNumber(self.playerState.playerNumber)].y < 0 && !self.input[translatedPlayerNumber(self.playerState.playerNumber)].jmp && !self.input[translatedPlayerNumber(self.playerState.playerNumber)].thrw && !self.input[translatedPlayerNumber(self.playerState.playerNumber)].pckp && Math.Abs(self.input[translatedPlayerNumber(self.playerState.playerNumber)].x) < 0.2f // Check for self.inputs: only down
        && self.IsTileSolid(1, 0, -1) //&& ((!self.IsTileSolid(1, -1, -1) || !self.IsTileSolid(1, 1, -1)) && self.IsTileSolid(1, self.input[0].x, 0)) // check if we have ground to sleep on
        && currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].currentThreat < 0.15f // check if we feel threatened
        && !self.room.abstractRoom.shelter // do not nap while in shelter
        )
        {
            self.forceSleepCounter += 2;
            if (self.forceSleepCounter > 214)
            {
                self.forceSleepCounter = 262;
                self.LoseAllGrasps();


            }
            // LocalLogSource.LogInfo("P" + translatedPlayerNumber(self.playerState.playerNumber) + " " + self.forceSleepCounter + " " + self.sleepCurlUp + " " + self.sleepCounter);
        }

        else if (!sleeping[translatedPlayerNumber(self.playerState.playerNumber)] && self.forceSleepCounter > 0)
        {
            self.forceSleepCounter--; // gradually decrease sleepiness if threshsold not reached
        }

        if (self.forceSleepCounter > 260)
        {
            sleeping[translatedPlayerNumber(self.playerState.playerNumber)] = true;
            forbidGrasps[translatedPlayerNumber(self.playerState.playerNumber)] = true;
        }

        orig(self, eu);

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
        if (forbidGrasps[translatedPlayerNumber(self.playerState.playerNumber)]) return false;
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
        if (sleeping[translatedPlayerNumber(self.playerState.playerNumber)])
        {
            wakeUp[translatedPlayerNumber(self.playerState.playerNumber)] = true;
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
        if (sleeping[translatedPlayerNumber(self.playerState.playerNumber)] || self.sleepCurlUp > 0.5f)
        {
            wakeUp[translatedPlayerNumber(self.playerState.playerNumber)] = true;
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
        if (sleeping.Count != 0)
        {
            LocalLogSource.LogInfo("clearing variables");
            sleeping.Clear();
            wakeUp.Clear();
            forbidGrasps.Clear();
            currentThreat.Clear();
            playerNumberEquivalent.Clear();
            foreach (var thing in threatLabel)
            {
                // LocalLogSource.LogInfo("removing debug view from");

                if (thing != null) Futile.stage.RemoveChild(thing);

            }
            threatLabel.Clear();
        }
    }

    /// <summary>
    /// small chance of summonning a Z each time
    /// </summary>
    /// <param name="self"></param>
    /// <returns>true if Z was summonned</returns>
    private bool showZs(Player self)
    {
        if (UnityEngine.Random.value < (0.005 + modOptions.ZsQtyVarianceConfigurable.Value*0.015))
        {
            //  if (Zs.text != modOptions.Zs)
            if (Zs.decayEnabled != modOptions.ZsColorIsDecayOnConfigurable.Value) Zs.decayEnabled = modOptions.ZsColorIsDecayOnConfigurable.Value;
            if (Zs.text != modOptions.ZsTextContentConfigurable.Value) Zs.text = modOptions.ZsTextContentConfigurable.Value;
            // LocalLogSource.LogInfo("Spawning a bubble P" + translatedPlayerNumber(self.playerState.playerNumber) + " " + " " + self.forceSleepCounter + " " + self.sleepCurlUp + " " + self.sleepCounter + " mode: "+modOptions.ZsColorTypeConfigurable.Value + " rainbow:"+modOptions.ZsColorRainbowTypeConfigurable.Value);
            Zs.baseSizeVar = modOptions.ZsSizeVarianceConfigurable.Value;
            self.room.AddObject(
                new Zs(
                    self.bodyChunks[0].pos + RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * UnityEngine.Random.value * self.bodyChunks[0].rad + new Vector2((float)self.ThrowDirection * 2f, -2f),
                    (RWCustom.Custom.DegToVec(UnityEngine.Random.Range(-25f, 25f)) + new Vector2(self.ThrowDirection, 0f)) * 0.09f,
                    self.ThrowDirection,
                    getZsColor(self)
                )
                {
                    parentPlayerId = translatedPlayerNumber(self.playerState.playerNumber),
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
        if (!self.dead && threatLabel[translatedPlayerNumber(self.playerState.playerNumber)] == null)
        {

            // LocalLogSource.LogInfo("threatd ok");
            string text = $"threat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].threat}\ncurrentThreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].currentThreat}\n musicAgnosticThreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].musicAgnosticThreat}\ncurrentMusicAgnosticThreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].currentMusicAgnosticThreat}\n{debugCounter}";

            threatLabel[translatedPlayerNumber(self.playerState.playerNumber)] = new FLabel(RWCustom.Custom.GetFont(), text)
            {
                alignment = FLabelAlignment.Left,
                x = 100.2f + translatedPlayerNumber(self.playerState.playerNumber) * 200f,
                y = RWCustom.Custom.rainWorld.options.ScreenSize.y - 50.2f
            };

            // LocalLogSource.LogInfo("label creation ok ok");

            Futile.stage.AddChild(threatLabel[translatedPlayerNumber(self.playerState.playerNumber)]);
            // LocalLogSource.LogInfo("addding label ok");
        }
        else if (threatLabel[translatedPlayerNumber(self.playerState.playerNumber)] != null)
        {
            string text = $"P{translatedPlayerNumber(self.playerState.playerNumber)}\nthreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].threat}\ncurrentThreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].currentThreat}\n musicAgnosticThreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].musicAgnosticThreat}\ncurrentMusicAgnosticThreat: {currentThreat[translatedPlayerNumber(self.playerState.playerNumber)].currentMusicAgnosticThreat}\n{debugCounter}";

            threatLabel[translatedPlayerNumber(self.playerState.playerNumber)].text = text;

            if (self.dead)
            {
                // LocalLogSource.LogInfo("removing debug info for P" + translatedPlayerNumber(self.playerState.playerNumber));
                Futile.stage.RemoveChild(threatLabel[translatedPlayerNumber(self.playerState.playerNumber)]);
                threatLabel[translatedPlayerNumber(self.playerState.playerNumber)] = null;
            }
        }

    }

}
