using BepInEx;
using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
// Access private fields and stuff
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using System.Collections.Generic;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace TAssist;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
// [BepInDependency("nope.stepbystep", BepInDependency.DependencyFlags.SoftDependency)]

public partial class TAssist : BaseUnityPlugin
{
    BepInEx.Logging.ManualLogSource lls;

    bool done = false;

    int cooldown = 0;

    Scenario currentScenario = null;

    // dir keys, stating from up, counter-clockwise. Then pckp, jmp, thrw
    bool[] currentPresses = {false, false, false, false, false, false, false};


public Inputs CurrentInput
            {
                get
                {
                    if(cam.game.Players.Count > 0)
                        if (cam.game.Players[0].realizedCreature is Player ply) return ply.input[0];
                    return new Inputs();
                }
            }
    public TAssist()
    {
        lls = BepInEx.Logging.Logger.CreateLogSource("TAssist");
    }

    public bool isPlayingScenario { get; private set; } = false;

    private void OnEnable()
    {
        if (done) return;
        lls.LogInfo("Hello World ! ");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        On.RainWorld.Update += InputDispatcher;
        Scenario.lls = lls;
        try
        {
            IL.RWInput.PlayerInputLogic_int_int += ILInputForcer;
        }
        catch (System.Exception ex)
        {
            lls.LogError($"Failed IL hook. {ex}");
        }
        currentScenario = new Scenario(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../test.tas");
        done = true;
    }

    private void ILInputForcer(ILContext context)
    {
        ILCursor c = new ILCursor(context);
        c.GotoNext(
            MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(Options.ControlSetup).GetMethod("GetAxis")),
            x => x.MatchNewobj(typeof(UnityEngine.Vector2)),
            x => x.MatchStfld(typeof(Player.InputPackage).GetField("analogueDir"))
        // IL_0134: stfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
        );
        c.MoveAfterLabels();

        // Load the instance of Player.InputPackage (assuming it's in a local variable)
        c.Emit(OpCodes.Ldloc_0); // Load the instance of Player.InputPackage onto the stack

        // Call the C# method to modify the input package
        // c.Emit(OpCodes.Call, typeof(TAssist).GetMethod("InputForcer")); 
        c.EmitDelegate(InputForcer);
        // Store the InputPackage back into local variables
        c.Emit(OpCodes.Stloc_0);

    }

    Player.InputPackage InputForcer(Player.InputPackage result)
    {
        
        string pre = $"{result.pckp} {result.jmp} {result.thrw} {result.analogueDir}";
        if (isPlayingScenario){
        if (currentPresses[0]) result.analogueDir.y=1f;
        if (currentPresses[1]) result.analogueDir.x=-1f;
        if (currentPresses[2]) result.analogueDir.y=-1f;
        if (currentPresses[3]) result.analogueDir.x=1f;
        if (currentPresses[4]) result.pckp=true;
        if (currentPresses[5]) result.jmp=true;
        if (currentPresses[6]) result.thrw=true;}
        string post = $"{result.pckp} {result.jmp} {result.thrw} {result.analogueDir}";
        if (pre != post) {UnityEngine.Debug.Log($"{pre} -> {result.pckp} {result.jmp} {result.thrw} {result.analogueDir}");
             //           if (cam.game.Players[0].realizedCreature is Player ply) return ply.input[0];
             //sometimes the packages does not match
        }
        return result;
    }

    private void InputDispatcher(On.RainWorld.orig_Update orig, RainWorld self)
    {
        if (cooldown != 0) cooldown--;
        if (Input.GetKeyDown(KeyCode.M) && cooldown == 0) {
            isPlayingScenario = !isPlayingScenario;
            cooldown=40;
            lls.LogDebug("switching TAssist");
        }
        if (Input.GetKeyDown(KeyCode.R)) {
            currentScenario.Refresh();
        }
        if (isPlayingScenario && currentScenario.EndProgress == 0)
        {
            isPlayingScenario = false;
            lls.LogDebug("Cancelled playing scenarionas it seems to be invalid");
        }


        if (isPlayingScenario)
        {
            currentPresses = currentScenario.Presses();
            if (currentScenario.Done) {
                isPlayingScenario = false;
                lls.LogDebug("Secnario is done playing");
                currentScenario.Done= false;
                currentScenario.Refresh();
            }
        }
        orig(self);
    }

    // private bool dispatchNextInput()
    // {

    // }

    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
    }
}
