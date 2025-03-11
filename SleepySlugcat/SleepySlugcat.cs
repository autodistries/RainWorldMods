using BepInEx;
using static SleepySlugcat.Utils;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Drawing.Text;
using System.Drawing.Imaging;
using static SleepySlugcat.PluginInfo;
using RainMeadow;
using System.Reflection;
using System.Security.Permissions;
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SleepySlugcat;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class SleepySlugcatto : BaseUnityPlugin
{
    static bool alreadyCheckedMeadow = false;
    int debugCounter = 0;//0
    List<FLabel> threatLabel = new(); //
    public string colorMode = "random";
    private bool singleZs = false;

    private bool anyoneInVoidSea = false;

    #if DEBUGON
    public static bool showDebug = true;
    public static int inputBlock = 0;
    #endif

    public static BepInEx.Logging.ManualLogSource LocalLogSource;

    /// <summary>
    /// Registers hooks
    /// </summary>
    private void OnEnable()
    {
        TODO("Make Zs pop on wakeup ? +Configurable");
        LocalLogSource = BepInEx.Logging.Logger.CreateLogSource("SleepySlugcat");

        LocalLogSource.LogInfo("Hooking setup methods...");

        try
        {
            On.Player.Update += CheckForSleepySlugcat; //handles main logic
            On.Player.CanIPickThisUp += CanWeReallyGrabThatRn; // prevent grabbing when sleeping
            On.Player.Collide += WtfWeGotHit; //stop sleeping if something collides with us
            On.Player.Die += WhyDidIDie; // stop sleeping right befroe we die obviously
            On.Player.JollyEmoteUpdate += NoYouDont; // If jolly is enabled, its emote thing will overlap with our sleep thing and uncurl
            On.Player.Stun += WakeUpBeforeStun;

            // On.RainWorldGame.ctor += ResetDebuggingView; // reset variables and other things when re-entering the game

            On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour; // mod options interface

            On.Menu.SimpleButton.Clicked += ResetLabels;

            On.Menu.MainMenu.ctor += playerCtorCreator; // hooks Player to add Data to OnlineObject if Meadow is on

        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Logger.LogError("this mod has been disabled.");
            this.OnDisable();
        }
    }


    private void ResetLabels(On.Menu.SimpleButton.orig_Clicked orig, Menu.SimpleButton self)
    {
        if (self.signalText is "RESTART" or "YES_EXIT" or "EXIT" or "EDIT")
        {
            foreach (var label in CWT.fLabels)
            {
                Futile.stage.RemoveChild(label);
            }
        }
        orig(self);

    }

    private void playerCtorCreator(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);
        if (!alreadyCheckedMeadow && ModManager.ActiveMods.Any((el) => el.id == "henpemaz_rainmeadow"))
            On.Player.ctor += createMeadowCompatData;
        alreadyCheckedMeadow = true;

    }

    private void createMeadowCompatData(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        LocalLogSource.LogInfo("Created a Player object with Meadow Compat enabled");
        var playerOpo = self.abstractPhysicalObject.GetOnlineObject();
        if (playerOpo is null)
        {
            return;
        }
        if (playerOpo.TryGetData<SleepyMeadowCompat>(out _))
        {
            return;
        }
        playerOpo.AddData(new SleepyMeadowCompat());
    }

    private void ResetLabels(On.Menu.PauseMenu.orig_Singal orig, Menu.PauseMenu self, Menu.MenuObject sender, string message)
    {
    }

    private void OnDisable()
    {
        On.Player.Update -= CheckForSleepySlugcat; //handles main logic
        On.Player.CanIPickThisUp -= CanWeReallyGrabThatRn; // prevent grabbing when sleeping
        On.Player.Collide -= WtfWeGotHit; //stop sleeping if something collides with us
        On.Player.Die -= WhyDidIDie; // stop sleeping right befroe we die obviously
        On.Player.JollyEmoteUpdate -= NoYouDont; // If jolly is enabled, its emote thing will overlap with our sleep thing and uncurl

        // On.RainWorldGame.ctor -= ResetDebuggingView; // reset variables and other things when re-entering the game

        On.RainWorld.OnModsInit -= RainWorldOnOnModsInitDetour;


    }


    /// <summary>
    /// Handles inputs and slugcat state updates
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="eu"></param>
    private void CheckForSleepySlugcat(On.Player.orig_Update orig, Player self, bool eu)
    {
        debugCounter++;
        debugCounter %= 40;
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();
        if (!anyoneInVoidSea && self.inVoidSea) anyoneInVoidSea = true;
        if (self.isNPC || anyoneInVoidSea)
        {
            orig(self, eu);
            return;
        }
        #if DEBUGON
        if (inputBlock>0) inputBlock--;
        if (slugSleepData.sleeping && inputBlock == 0 && UnityEngine.Input.GetKey("w")) {
            LocalLogSource.LogDebug("w!");
            WakeUp(self, slugSleepData);
            inputBlock = 20;
        }
        if (slugSleepData.sleeping && inputBlock == 0 && UnityEngine.Input.GetKey("x")) {
            LocalLogSource.LogDebug("x!");
            WakeUp(self, slugSleepData, true);
            inputBlock = 20;
        }
                if (slugSleepData.sleeping && inputBlock == 0 && UnityEngine.Input.GetKey("v")) {
            LocalLogSource.LogDebug("v!");
            WakeUp(self, slugSleepData, immediate:true);
            inputBlock = 20;
        }

        if (inputBlock == 0 && UnityEngine.Input.GetKey("c")) {
            LocalLogSource.LogDebug("c!");
            showDebug = !showDebug;
            inputBlock = 20;
        }
        #endif

        //LocalLogSource.LogInfo($"We're at step 1. currentPlayerIndex will be={self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)} sleeping.Count:{sleeping.Count} updatesSinceLastZPopped.Count:{updatesSinceLastZPopped.Count}");

        // showZs(self);

        if (abnormalStateFreshness > 80)
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

            if (slugSleepData.sleeping)
            {
                if (!singleZs)
                {
                    if (showZs(self))
                    {
                        slugSleepData.timeSinceLastZPopped = 0;
                    }
                    else
                    {
                        slugSleepData.timeSinceLastZPopped++;
                    }
                }
            }
            //LocalLogSource.LogDebug("We're at step 2");

            // there needs to be a difference in getting input from Local and Remote. Locals will have an entry inside self.input
            int currentPlayerIndex = self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature);
            bool remoteBeing = currentPlayerIndex == -1;
            // if (remoteBeing)
            // {
            //     // LocalLogSource.LogInfo("Found a foreign being. \n" );
            //     orig(self, eu);
            //     return;
            // }

            if (slugSleepData.sleeping
            && (slugSleepData.forceWakeUp // on collision or death
                || (!remoteBeing && (self.input[currentPlayerIndex].y > 0 || self.input[currentPlayerIndex].x != 0 || self.input[currentPlayerIndex].jmp)) // if local player is moving
                || (remoteBeing && self.forceSleepCounter <= 200) // meadow sync fix
                || slugSleepData.threatDetermination?.currentThreat > 0.34f // if the ambiant threat becomes too high
                || (self.bodyMode.value != "Crawl" && self.forceSleepCounter >= 260) // idk what that was for
                || self.grabbedBy.Count != 0 // when grabbed, by a spider or whatever
                || self.dead // when we're dead by some mysterious way
                || self.Submersion > 0.6f //if the rain floods our place

                ))
            {
                LocalLogSource.LogInfo("waking up rn");
                //     LocalLogSource.LogDebug($@"slugSleepData.forceWakeUp:{slugSleepData.forceWakeUp} self.input[currentPlayerIndex].y > 0:{self.input[currentPlayerIndex].y > 0} self.input[currentPlayerIndex].x != 0:{self.input[currentPlayerIndex].x != 0} self.input[currentPlayerIndex].jmp:{self.input[currentPlayerIndex].jmp}
                //  currentThreat[currentPlayerIndex].currentThreat > 0.30f:{slugSleepData.threatDetermination?.currentThreat > 0.30f}
                //  self.bodyMode.value != Crawl:{self.bodyMode.value != "Crawl"}
                //  self.dead:{self.dead} self.Submersion > 0.6f:{self.Submersion > 0.6f}             ");
                WakeUp(self, slugSleepData);
            }

            #if DEBUGON
            slugSleepData.updateDebugString(self.abstractCreature);
            #endif

            if (debugCounter % 3 == 0)
            {
                if (!self.dead && slugSleepData.threatDetermination != null) slugSleepData.threatDetermination.Update(self.abstractCreature.world.game);
            }




            if (!slugSleepData.sleeping && self.Consious
            && !self.inShortcut // while in shortcuts, no ground, so IsTileSolid nullrefs
            && ((remoteBeing && self.forceSleepCounter > 215) ||
                    (!remoteBeing
                     && self.input[currentPlayerIndex].y < 0
                     && !self.input[currentPlayerIndex].jmp
                     && !self.input[currentPlayerIndex].thrw
                     && !self.input[currentPlayerIndex].pckp
                     && Math.Abs(self.input[currentPlayerIndex].x) < 0.2f))// Check for self.inputs: only down, OR remote being that is already sleeping
            && self.IsTileSolid(1, 0, -1) //&& ((!self.IsTileSolid(1, -1, -1) || !self.IsTileSolid(1, 1, -1)) && self.IsTileSolid(1, self.input[0].x, 0)) // check if we have ground to sleep on
            && (remoteBeing || slugSleepData.threatDetermination?.currentThreat < 0.21f) // check if we feel threatened
            && !self.room.abstractRoom.shelter // do not nap while in shelter
            )
            {
                self.forceSleepCounter += 2;
                if (self.forceSleepCounter > 215)
                {
                    self.LoseAllGrasps();
                    slugSleepData.sleeping = true;
                    slugSleepData.graspsForbidden = true;

                }
            }
            else if (slugSleepData.sleeping && self.forceSleepCounter < 260)
            {
                self.forceSleepCounter += 1;
            }


            else if (!slugSleepData.sleeping && self.forceSleepCounter > 0 && !self.room.abstractRoom.shelter)
            {
                self.forceSleepCounter-=2; // gradually decrease sleepiness if threshsold not reached
            }


        }
        catch (Exception e)
        {
            abnormalState = true;
            Logger.LogError(e);
        }

        orig(self, eu);

    }

    public void WakeUp(Player p, CWT.SlugSleepData? slugSleepData = null, bool quick = false, bool immediate = false)
    {
        if (slugSleepData == null) {
            slugSleepData = p.abstractCreature.GetCWTData();
        }
        slugSleepData.sleeping = false;
        slugSleepData.forceWakeUp = false;
        slugSleepData.graspsForbidden = false;
        p.forceSleepCounter = 215;
        if (quick || immediate) p.forceSleepCounter = 0;
        if (immediate) p.sleepCurlUp = 0;
        LocalLogSource.LogInfo($"Woke up {slugSleepData.testSyncedValue}, force {quick}");
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
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();

        if (!(self.isNPC || anyoneInVoidSea) && slugSleepData.graspsForbidden) return false;
        return orig(self, obj);
    }



    /// <summary>
    /// Wake up on Collision with anything
    /// comparison with self.forceSleepCounter lets the objects we lose grasps on get (hopefully) far enough away
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="otherObject"></param>
    /// <param name="myChunk"></param>
    /// <param name="otherChunk"></param>
    private void WtfWeGotHit(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();

        if (!(self.isNPC || anyoneInVoidSea) && slugSleepData.sleeping && self.forceSleepCounter >= 260) // why the additional fsc check ?
        {
            WakeUp(self, slugSleepData, true);
            LocalLogSource.LogInfo("collided: " + (self).slugcatStats.name + " and " + otherObject.GetType());
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
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();

        if (!(self.isNPC || anyoneInVoidSea) && (slugSleepData.sleeping || self.sleepCurlUp > 0.5f))
        {
            WakeUp(self, slugSleepData, immediate: true);
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
        // at emoteSleepCounter:1.596, no curve at all
        // at 2.408002, we are in full curve
        // self.forceSleepCounter (between 0 and 215) could be mapped to self.emoteSleepCounter (between 1.596 and  2.408)
        self.emoteSleepCounter = Mathf.Lerp(1.596f, 2.408f, self.forceSleepCounter / 215f);
        return;
    }


    private void WakeUpBeforeStun(On.Player.orig_Stun orig, Player self, int st)
    {
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();
        if (slugSleepData.sleeping) {
            WakeUp(self, slugSleepData, immediate:true);
            LocalLogSource.LogInfo("Stun! Wakeup quick");
        }
        orig(self, st);
    }



    /// <summary>
    /// small chance of summonning a Z each time
    /// </summary>
    /// <param name="self"></param>
    /// <returns>true if Z was summonned</returns>
    private bool showZs(Player self)
    {
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();

        if (slugSleepData.timeSinceLastZPopped > 160 || UnityEngine.Random.value < (0.005 + slugSleepData.sleepOptions.qtyVariance * 0.015))// idk what this is && updatesSinceLastZPopped[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] > 25 - modOptions.ZsQtyVarianceConfigurable.Value * 10)
        {

            // LocalLogSource.LogInfo("Spawning a bubble P" + self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature) + " " + " " + self.forceSleepCounter + " " + self.sleepCurlUp + " " + self.sleepCounter + " mode: "+modOptions.ZsColorTypeConfigurable.Value + " rainbow:"+modOptions.ZsColorRainbowTypeConfigurable.Value);
            self.room.AddObject(
                new Zs(
                    self.bodyChunks[0].pos + RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * UnityEngine.Random.value * self.bodyChunks[0].rad + new Vector2((float)self.ThrowDirection * 2f, -2f),
                    (RWCustom.Custom.DegToVec(UnityEngine.Random.Range(-25f, 25f)) + new Vector2(self.ThrowDirection, 0f)) * 0.09f,
                    self.ThrowDirection,
                    slugSleepData.sleepOptions.getZsColor()
                )
                {
                    rainbow = ((slugSleepData.sleepOptions.rainbowEnabled) ? slugSleepData.sleepOptions.rainbowType : null),
                    musician = slugSleepData.sleepOptions.isMusician,
                    text = slugSleepData.sleepOptions.textContent,
                    decayEnabled = slugSleepData.sleepOptions.isDecayOpacityFadeOn,

                }
            );
            // singleZs=true;
            return true;

        }
        return false;
    }




    #region RemixInterface
    internal static ModOptions modOptions;
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


}
