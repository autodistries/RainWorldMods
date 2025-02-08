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

namespace SleepySlugcat;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class SleepySlugcatto : BaseUnityPlugin
{






    List<bool> sleeping = new();//false
    List<bool> wakeUp = new();//false

    int debugCounter = 0;//0
    List<bool> forbidGrasps = new();//False
    List<FLabel> threatLabel = new(); //
    List<ThreatDetermination> currentThreat = new();
    public string colorMode = "random";
    private bool singleZs = false;
    private List<int> updatesSinceLastZPopped = new();

    private bool anyoneInVoidSea = false;












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

            On.RainWorldGame.ctor += ResetDebuggingView; // reset variables and other things when re-entering the game

            On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour; // mod options interface

            On.Menu.PauseMenu.Singal += ResetLabels;

            On.Player.ctor += createMeadowCompatData;
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            Logger.LogError("this mod has been disabled.");
            this.OnDisable();
        }
    }

    private void createMeadowCompatData(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (ModManager.ActiveMods.Any((el) => el.id == "henpemaz_rainmeadow"))
        {
            LocalLogSource.LogInfo("Created a Player object with Meadow Compat enabled");
            Type extensionType = Type.GetType("RainMeadow.Extensions, Rain Meadow");

            if (extensionType == null)
            {
                LocalLogSource.LogInfo("Could not find RainMeadow.Extensions type. Skipping.");
                return;
            }
                LocalLogSource.LogInfo("Found RainMeadow.Extensions type");

            MethodInfo getOnlineObjectMethod = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault((mi) => mi.Name == "GetOnlineObject" && mi.GetParameters().Length == 1);

            var playerOpo = getOnlineObjectMethod.Invoke(null, [self.abstractPhysicalObject]); // self.abstractPhysicalObject.GetOnlineObject();

            if (playerOpo is null)
            {
                LocalLogSource.LogWarning("No playerOpo");
                return;
            }

            LocalLogSource.LogWarning("playerOpo found");

            var compatType = Type.GetType("SleepySlugcat.SleepyMeadowCompat, SleepySlugcat");
            if (compatType == null)
            {
                LocalLogSource.LogWarning("could not find SleepySlugcat.SleepyMeadowCompat");
                // Log or silently exit if your own type cannot be resolved.
                return;
            }
                LocalLogSource.LogWarning("Found SleepySlugcat.SleepyMeadowCompat");

            // bool OnlineEntity.TryGetData<T>(out T d)
            // public bool TryGetData(Type T, out EntityData d);

            MethodInfo tryGetData = playerOpo.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(mi =>
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    // Expecting 1 parameter (the out parameter)
                    return mi.Name == "TryGetData" && ps.Length == 2 && (ps[1].IsOut || ps[1].ParameterType.IsByRef);
                });
            object[] args = new object[] { compatType, null }; // will contain the out parameter
            object resultObj = tryGetData.Invoke(playerOpo, args);
            bool hadData = resultObj is bool b && b;
            //  var a = self.abstractPhysicalObject.GetOnlineObject();
            //  a.GetData


            if (hadData)
            {
                LocalLogSource.LogWarning("playerOpo already had data");

                return;
            }
            LocalLogSource.LogWarning("adding Sleeper data to playerOpo");


            // T OnlineEntity.AddData<T>(T toAdd)
            // var compatInstance = Activator.CreateInstance(compatType);
            // MethodInfo addData = playerOpo.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(mi =>
            //     {
            //         ParameterInfo[] ps = mi.GetParameters();
            //         // Expecting 1 parameter (the out parameter)
            //         return mi.Name == "AddData" && ps.Length == 1;
            //     });
            // object[] args2 = new object[] { compatInstance }; // will contain the out parameter
            // addData.Invoke(playerOpo, args2);


            MethodInfo addDataMethodDef = playerOpo.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(mi =>
                mi.Name == "AddData" &&
                mi.IsGenericMethodDefinition &&
                mi.GetGenericArguments().Length == 1 &&
                mi.GetParameters().Length == 1);

            if (addDataMethodDef == null)
            {
                LocalLogSource.LogWarning("Could not locate generic AddData method.");
                return;
            }

            // Get the non-generic method by specifying SleepyMeadowCompat as the type argument.
            MethodInfo addDataMethod = addDataMethodDef.MakeGenericMethod(compatType);

            // Create an instance of your SleepyMeadowCompat.
            object compatInstance = Activator.CreateInstance(compatType);
            // Now call the method with the instance as the parameter.
            addDataMethod.Invoke(playerOpo, new object[] { compatInstance });
        }
    }

    private void ResetLabels(On.Menu.PauseMenu.orig_Singal orig, Menu.PauseMenu self, Menu.MenuObject sender, string message)
    {
        if (message is "RESTART" or "YES_EXIT" or "EXIT" or "EDIT")
        {
            foreach (var label in CWT.fLabels)
            {
                Futile.stage.RemoveChild(label);
            }
        }
        orig(self, sender, message);
    }

    private void OnDisable()
    {
        On.Player.Update -= CheckForSleepySlugcat; //handles main logic
        On.Player.CanIPickThisUp -= CanWeReallyGrabThatRn; // prevent grabbing when sleeping
        On.Player.Collide -= WtfWeGotHit; //stop sleeping if something collides with us
        On.Player.Die -= WhyDidIDie; // stop sleeping right befroe we die obviously
        On.Player.JollyEmoteUpdate -= NoYouDont; // If jolly is enabled, its emote thing will overlap with our sleep thing and uncurl

        On.RainWorldGame.ctor -= ResetDebuggingView; // reset variables and other things when re-entering the game

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
            && (slugSleepData.forceWakeUp
                || (!remoteBeing && (self.input[currentPlayerIndex].y > 0 || self.input[currentPlayerIndex].x != 0 || self.input[currentPlayerIndex].jmp))
                || slugSleepData.threatDetermination?.currentThreat > 0.34f
                || (self.bodyMode.value != "Crawl" && self.forceSleepCounter >= 260)
                || self.grabbedBy.Count != 0
                || self.dead
                || self.Submersion > 0.6f))
            {
                LocalLogSource.LogInfo("waking up rn");
            //     LocalLogSource.LogDebug($@"slugSleepData.forceWakeUp:{slugSleepData.forceWakeUp} self.input[currentPlayerIndex].y > 0:{self.input[currentPlayerIndex].y > 0} self.input[currentPlayerIndex].x != 0:{self.input[currentPlayerIndex].x != 0} self.input[currentPlayerIndex].jmp:{self.input[currentPlayerIndex].jmp}
            //  currentThreat[currentPlayerIndex].currentThreat > 0.30f:{slugSleepData.threatDetermination?.currentThreat > 0.30f}
            //  self.bodyMode.value != Crawl:{self.bodyMode.value != "Crawl"}
            //  self.dead:{self.dead} self.Submersion > 0.6f:{self.Submersion > 0.6f}             ");
                slugSleepData.forceWakeUp = false;
                self.forceSleepCounter = 0;
                slugSleepData.sleeping = false;
                slugSleepData.graspsForbidden = false;
            }


            if (debugCounter % 3 == 0)
            {
                if (!self.dead && slugSleepData.threatDetermination != null) slugSleepData.threatDetermination.Update(self.abstractCreature.world.game);
                slugSleepData.updateDebugString(self.abstractCreature);
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
                self.forceSleepCounter--; // gradually decrease sleepiness if threshsold not reached
            }


        }
        catch (Exception e)
        {
            abnormalState = true;
            Logger.LogError(e);
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
            slugSleepData.forceWakeUp = true;
            self.forceSleepCounter = 0;
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
            slugSleepData.forceWakeUp = true;
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



    }

    /// <summary>
    /// small chance of summonning a Z each time
    /// </summary>
    /// <param name="self"></param>
    /// <returns>true if Z was summonned</returns>
    private bool showZs(Player self)
    {
        CWT.SlugSleepData slugSleepData = self.abstractCreature.GetCWTData();

        if (slugSleepData.timeSinceLastZPopped > 160 || UnityEngine.Random.value < (0.005 + modOptions.ZsQtyVarianceConfigurable.Value * 0.015))// idk what this is && updatesSinceLastZPopped[self.abstractCreature.world.game.Players.IndexOf(self.abstractCreature)] > 25 - modOptions.ZsQtyVarianceConfigurable.Value * 10)
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
