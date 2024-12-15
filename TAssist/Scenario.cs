using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Rewired.Dev;

namespace TAssist;

public enum ActionType
{
    Up,
    Left,
    Down,
    Right,
    Grab,
    Jump,
    Throw,
    Undefined,
    LoopStart,
    LoopEnd
}

class Scenario
{

    #region actions

    class Action
    {
        int aliveTimeMax = 1;
        int aliveTimeLeft = 1;

        bool herited = false;

        ActionType actionType;

        string ogstring = "";

        public Action(Action action)
        {
            aliveTimeMax = action.AliveTimeLeft;
            aliveTimeLeft = action.AliveTimeLeft;
            actionType = action.ActionType;
            herited = action.Herited;
            ogstring = action.Ogstring;
        }


        public Action(int maxlength = 1, ActionType at = ActionType.Undefined, bool heritage = false, string s = "")
        {
            aliveTimeMax = maxlength;
            aliveTimeLeft = maxlength;
            actionType = at;
            herited = heritage;
            ogstring = s;
        }

        public int AliveTimeLeft { get => aliveTimeLeft; set => aliveTimeLeft = value; }
        public ActionType ActionType { get => actionType; set => actionType = value; }
        public string Ogstring { get => ogstring; set => ogstring = value; }
        public bool Herited { get => herited; set => herited = value; }

        public override string ToString()
        {
            return $"{actionType} {aliveTimeLeft}";
        }
    }


    #endregion actions

    static BepInEx.Logging.ManualLogSource LocalLogSource;

    string targetRoom = "*";

    int currentProgress = 0;

    int endProgress = 0;

    readonly string source = "";

    public bool Done
    {
        get => currentProgress >= endProgress;
        set => currentProgress = (value == false) ? 0 : currentProgress;
    }

    public static ManualLogSource lls
    {
        get => LocalLogSource;
        set => LocalLogSource = value;
    }
    public int EndProgress { get => endProgress; set => endProgress = value; }

    List<List<Action>> actions = new();
    List<List<Action>> loopingActions = new();
    int loopStartIndex = -1;
    int currentLoopsQty = 0;
    int targetLoopsQty = 0;

    public Scenario(string filePath)
    {
        if (LocalLogSource == null)
        {
            UnityEngine.Debug.Log("No logger provided to Scenario");
            return;
        }

        lls.LogDebug($"New Scenario for {filePath}");

        source = filePath;

        Refresh(source);

    }

    public void Refresh(string filePath = null)
    {
        if (filePath == null)
        {
            if (source != "")
            {
                filePath = source;
            }
        }

        targetRoom = "*";
        endProgress = 0;
        currentProgress = 0;
        actions.Clear();


        targetLoopsQty = 0;
        currentLoopsQty = 0;
        loopingActions.Clear();
        loopStartIndex = -1;


        lls.LogDebug($"reloading file {filePath}");

        Queue<string> lines;
        try
        {
            // Read all lines from the file
            lines = new Queue<string>(File.ReadAllLines(filePath));

        }
        catch (Exception ex)
        {
            LocalLogSource.LogError($"Error reading the file: {ex.Message}");
            return;
        }


        targetRoom = lines.Dequeue();
        while (lines.Count > 0)
        {
            actions.Add(stringToActions(lines.Dequeue()));
            endProgress++;
        }
        LocalLogSource.LogDebug($"finished parsing.");
    }

    private List<Action> stringToActions(string v)
    {
        List<Action> res = new();
        if (v == "")
        {
            return res;
        }
        foreach (string a in v.ToLower().Split(' '))
        {
            string b = a.ToLower();

            int l = 1;
            string r = System.Text.RegularExpressions.Regex.Replace(b, "[a-z]", string.Empty);
            if (r.Length != 0)
            {
                if (r == "-")
                {
                    l = 0;
                }
                else if (int.TryParse(r, out int reslen))
                {
                    l = reslen;
                }
            }
            else l = 0;

            Action act = new();
            if (b.Contains("jump"))
            {
                act = new(l, ActionType.Jump, s: v);
            }
            else if (b.Contains("throw"))
            {
                act = new(l, ActionType.Throw, s: v);
            }
            else if (b.Contains("up"))
            {
                act = new(l, ActionType.Up, s: v);
            }
            else if (b.Contains("down"))
            {
                act = new(l, ActionType.Down, s: v);
            }
            else if (b.Contains("left"))
            {
                act = new(l, ActionType.Left, s: v);
            }
            else if (b.Contains("right"))
            {
                act = new(l, ActionType.Right, s: v);
            }
            else if (b.Contains("grab"))
            {
                act = new(l, ActionType.Grab, s: v);
            }
            else if (b.Contains("loopstart"))
            {
                act = new(l, ActionType.LoopStart, s: v);
            }
            else if (b.Contains("loopend"))
            {
                act = new(l, ActionType.LoopEnd, s: v);
            }
            else
            {
                lls.LogError($"Could not parse line {v}");
                continue;
            }
            res.Add(act);
        }
        string s = "";
        res.ForEach((a) => s += a.ToString() + "; ");
        lls.LogDebug($"line gave {s}");
        return res;
    }

    internal bool[] Presses()
    {
        lls.LogDebug("Parsing next presses");

        bool[] res = { false, false, false, false, false, false, false };
        List<ActionType> interruptedAT = new();
        if ((targetLoopsQty != 0 && currentLoopsQty == 0) || (actions[currentProgress].Count != 0 && actions[currentProgress].First().Ogstring.Contains("loopstart")))
        {
            var loopActions = actions[currentProgress].Where(action => !action.Herited)
                .Select(action => new Action(action)) // Create a new copy of each action
                .ToList();
            loopingActions.Add(loopActions);
        }
        lls.LogDebug("progess "+currentProgress);
        foreach (Action a in actions[currentProgress])
        {
            lls.LogDebug(a);

            if (a.ActionType == ActionType.LoopEnd && currentLoopsQty < targetLoopsQty)
            {
                lls.LogDebug($"Replacing items from {loopStartIndex} to {currentProgress - loopStartIndex} with {loopingActions.Count} els");
                actions.RemoveRange(loopStartIndex, currentProgress - loopStartIndex);
                actions.InsertRange(loopStartIndex, loopingActions.Take(loopingActions.Count-1).Select(innerList => innerList.Select(action => new Action(action)).ToList()).ToList());
                currentProgress = loopStartIndex;
                currentLoopsQty++;
                lls.LogDebug($"Loop tail, going back to head, {targetLoopsQty - currentLoopsQty} left");
                break;
            }
            else if (a.ActionType == ActionType.LoopEnd && currentLoopsQty == targetLoopsQty)
            {
                lls.LogDebug($"Loop END");
                targetLoopsQty = -1;
                currentLoopsQty = 0;
                loopingActions.Clear();
                loopStartIndex = 0;
                continue;

            }
            else if (a.ActionType == ActionType.LoopStart)
            {
                lls.LogDebug("Loop start::");
                if (loopStartIndex != currentProgress)
                {
                    if (targetLoopsQty > 0)
                    {
                        lls.LogWarning("Conflicting loops were found");
                        targetLoopsQty = 0;
                        currentLoopsQty = 0;
                        loopingActions.Clear();
                    }

                    targetLoopsQty = a.AliveTimeLeft - 1;
                    // currentLoopsQty++;
                    lls.LogDebug($"New loop ! idx{currentProgress} for {targetLoopsQty} times");
                    loopStartIndex = currentProgress;
                }
                else
                {
                    lls.LogDebug($"idx{currentProgress} for {targetLoopsQty - currentLoopsQty} times");
                    currentLoopsQty++;
                }
                continue;
            }


            if (interruptedAT.Contains(a.ActionType)) continue;
            if (a.AliveTimeLeft == 0)
            {
                res[ActionToIndex(a.ActionType)] = false;
                interruptedAT.Add(a.ActionType);
            }
            else if (a.AliveTimeLeft >= 1)
            {
                res[ActionToIndex(a.ActionType)] = true;
                if (a.AliveTimeLeft > 1 && endProgress > currentProgress + 1)
                {
                    a.AliveTimeLeft--;
                    a.Herited = true;
                    actions[currentProgress + 1].Add(a);
                }
            }
        }
        // lls.LogDebug(res);
        currentProgress++;

        return res;
    }

    public static int ActionToIndex(ActionType at)
    {
        ActionType[] ats = { ActionType.Up, ActionType.Left, ActionType.Down, ActionType.Right, ActionType.Grab, ActionType.Jump, ActionType.Throw };
        return Array.IndexOf(ats, at);
    }
}


/* IL code:
.method private hidebysig static 
		valuetype Player/InputPackage PlayerInputLogic (
			int32 categoryID,
			int32 playerNumber
		) cil managed 
	{
		// Method begins at RVA 0x5c3c0
		// Header size: 12
		// Code size: 749 (0x2ed)
		.maxstack 4
		.locals init (
			[0] valuetype Player/InputPackage,
			[1] class [Rewired_Core]Rewired.Controller,
			[2] class Options/ControlSetup
		)

		IL_0000: ldloca.s 0
		IL_0002: initobj Player/InputPackage
		IL_0008: ldarg.1
		IL_0009: call class [Rewired_Core]Rewired.Controller RWInput::PlayerRecentController(int32)
		IL_000e: stloc.1
		IL_000f: ldsfld class RainWorld RWCustom.Custom::rainWorld
		IL_0014: ldfld class Options RainWorld::options
		IL_0019: ldfld class Options/ControlSetup[] Options::controls
		IL_001e: ldarg.1
		IL_001f: ldelem.ref
		IL_0020: ldloc.1
		IL_0021: ldc.i4.0
		IL_0022: callvirt instance void Options/ControlSetup::UpdateActiveController(class [Rewired_Core]Rewired.Controller, bool)
		IL_0027: ldloca.s 0
		IL_0029: ldsfld class RainWorld RWCustom.Custom::rainWorld
		IL_002e: ldfld class Options RainWorld::options
		IL_0033: ldfld class Options/ControlSetup[] Options::controls
		IL_0038: ldarg.1
		IL_0039: ldelem.ref
		IL_003a: callvirt instance class Options/ControlSetup/Preset Options/ControlSetup::GetActivePreset()
		IL_003f: stfld class Options/ControlSetup/Preset Player/InputPackage::controllerType
		IL_0044: ldloca.s 0
		IL_0046: ldloc.0
		IL_0047: ldfld class Options/ControlSetup/Preset Player/InputPackage::controllerType
		IL_004c: ldsfld class Options/ControlSetup/Preset Options/ControlSetup/Preset::KeyboardSinglePlayer
		IL_0051: call bool class ExtEnum`1<class Options/ControlSetup/Preset>::op_Inequality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
		IL_0056: brfalse.s IL_006a

		IL_0058: ldloc.0
		IL_0059: ldfld class Options/ControlSetup/Preset Player/InputPackage::controllerType
		IL_005e: ldsfld class Options/ControlSetup/Preset Options/ControlSetup/Preset::None
		IL_0063: call bool class ExtEnum`1<class Options/ControlSetup/Preset>::op_Inequality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
		IL_0068: br.s IL_006b

		IL_006a: ldc.i4.0

		IL_006b: stfld bool Player/InputPackage::gamePad
		IL_0070: ldsfld class RainWorld RWCustom.Custom::rainWorld
		IL_0075: ldfld class Options RainWorld::options
		IL_007a: ldfld class Options/ControlSetup[] Options::controls
		IL_007f: ldarg.1
		IL_0080: ldelem.ref
		IL_0081: stloc.2
		IL_0082: ldarg.0
		IL_0083: brtrue.s IL_00e6

		IL_0085: ldloc.2
		IL_0086: ldc.i4.0
		IL_0087: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_008c: brfalse.s IL_0096

		IL_008e: ldloca.s 0
		IL_0090: ldc.i4.1
		IL_0091: stfld bool Player/InputPackage::'jmp'

		IL_0096: ldloc.2
		IL_0097: ldc.i4.4
		IL_0098: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_009d: brfalse.s IL_00a7

		IL_009f: ldloca.s 0
		IL_00a1: ldc.i4.1
		IL_00a2: stfld bool Player/InputPackage::thrw

		IL_00a7: ldloc.2
		IL_00a8: ldc.i4.s 11
		IL_00aa: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_00af: brfalse.s IL_00b9

		IL_00b1: ldloca.s 0
		IL_00b3: ldc.i4.1
		IL_00b4: stfld bool Player/InputPackage::mp

		IL_00b9: ldloc.2
		IL_00ba: ldc.i4.3
		IL_00bb: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_00c0: brfalse.s IL_00ca

		IL_00c2: ldloca.s 0
		IL_00c4: ldc.i4.1
		IL_00c5: stfld bool Player/InputPackage::pckp

		IL_00ca: ldloca.s 0
		IL_00cc: ldloc.2
		IL_00cd: ldc.i4.1
		IL_00ce: callvirt instance float32 Options/ControlSetup::GetAxis(int32)
		IL_00d3: ldloc.2
		IL_00d4: ldc.i4.2
		IL_00d5: callvirt instance float32 Options/ControlSetup::GetAxis(int32)
		IL_00da: newobj instance void [UnityEngine.CoreModule]UnityEngine.Vector2::.ctor(float32, float32)
		IL_00df: stfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_00e4: br.s IL_0139

		IL_00e6: ldarg.0
		IL_00e7: ldc.i4.1
		IL_00e8: bne.un.s IL_0139

		IL_00ea: ldloc.2
		IL_00eb: ldc.i4.8
		IL_00ec: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_00f1: brfalse.s IL_00fb

		IL_00f3: ldloca.s 0
		IL_00f5: ldc.i4.1
		IL_00f6: stfld bool Player/InputPackage::'jmp'

		IL_00fb: ldloc.2
		IL_00fc: ldc.i4.s 9
		IL_00fe: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_0103: brfalse.s IL_010d

		IL_0105: ldloca.s 0
		IL_0107: ldc.i4.1
		IL_0108: stfld bool Player/InputPackage::thrw

		IL_010d: ldloc.2
		IL_010e: ldc.i4.s 13
		IL_0110: callvirt instance bool Options/ControlSetup::GetButton(int32)
		IL_0115: brfalse.s IL_011f

		IL_0117: ldloca.s 0
		IL_0119: ldc.i4.1
		IL_011a: stfld bool Player/InputPackage::mp

		IL_011f: ldloca.s 0
		IL_0121: ldloc.2
		IL_0122: ldc.i4.6
		IL_0123: callvirt instance float32 Options/ControlSetup::GetAxis(int32)
		IL_0128: ldloc.2
		IL_0129: ldc.i4.7
		IL_012a: callvirt instance float32 Options/ControlSetup::GetAxis(int32)
		IL_012f: newobj instance void [UnityEngine.CoreModule]UnityEngine.Vector2::.ctor(float32, float32)
		IL_0134: stfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir

		IL_0139: ldloca.s 0
		IL_013b: ldloc.0
		IL_013c: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0141: ldsfld bool ModManager::MMF
		IL_0146: brtrue.s IL_014f

		IL_0148: ldc.r4 1
		IL_014d: br.s IL_015e

		IL_014f: ldsfld class RainWorld RWCustom.Custom::rainWorld
		IL_0154: ldfld class Options RainWorld::options
		IL_0159: ldfld float32 Options::analogSensitivity

		IL_015e: call valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 [UnityEngine.CoreModule]UnityEngine.Vector2::op_Multiply(valuetype [UnityEngine.CoreModule]UnityEngine.Vector2, float32)
		IL_0163: ldc.r4 1
		IL_0168: call valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 [UnityEngine.CoreModule]UnityEngine.Vector2::ClampMagnitude(valuetype [UnityEngine.CoreModule]UnityEngine.Vector2, float32)
		IL_016d: stfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0172: ldsfld class RainWorld RWCustom.Custom::rainWorld
		IL_0177: ldfld class Options RainWorld::options
		IL_017c: ldfld class Options/ControlSetup[] Options::controls
		IL_0181: ldarg.1
		IL_0182: ldelem.ref
		IL_0183: ldfld bool Options/ControlSetup::xInvert
		IL_0188: brfalse.s IL_019f

		IL_018a: ldloca.s 0
		IL_018c: ldflda valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0191: ldflda float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_0196: dup
		IL_0197: ldind.r4
		IL_0198: ldc.r4 -1
		IL_019d: mul
		IL_019e: stind.r4

		IL_019f: ldsfld class RainWorld RWCustom.Custom::rainWorld
		IL_01a4: ldfld class Options RainWorld::options
		IL_01a9: ldfld class Options/ControlSetup[] Options::controls
		IL_01ae: ldarg.1
		IL_01af: ldelem.ref
		IL_01b0: ldfld bool Options/ControlSetup::yInvert
		IL_01b5: brfalse.s IL_01cc

		IL_01b7: ldloca.s 0
		IL_01b9: ldflda valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_01be: ldflda float32 [UnityEngine.CoreModule]UnityEngine.Vector2::y
		IL_01c3: dup
		IL_01c4: ldind.r4
		IL_01c5: ldc.r4 -1
		IL_01ca: mul
		IL_01cb: stind.r4

		IL_01cc: ldloc.0
		IL_01cd: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_01d2: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_01d7: ldc.r4 -0.5
		IL_01dc: bge.un.s IL_01e6

		IL_01de: ldloca.s 0
		IL_01e0: ldc.i4.m1
		IL_01e1: stfld int32 Player/InputPackage::x

		IL_01e6: ldloc.0
		IL_01e7: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_01ec: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_01f1: ldc.r4 0.5
		IL_01f6: ble.un.s IL_0200

		IL_01f8: ldloca.s 0
		IL_01fa: ldc.i4.1
		IL_01fb: stfld int32 Player/InputPackage::x

		IL_0200: ldloc.0
		IL_0201: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0206: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::y
		IL_020b: ldc.r4 -0.5
		IL_0210: bge.un.s IL_021a

		IL_0212: ldloca.s 0
		IL_0214: ldc.i4.m1
		IL_0215: stfld int32 Player/InputPackage::y

		IL_021a: ldloc.0
		IL_021b: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0220: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::y
		IL_0225: ldc.r4 0.5
		IL_022a: ble.un.s IL_0234

		IL_022c: ldloca.s 0
		IL_022e: ldc.i4.1
		IL_022f: stfld int32 Player/InputPackage::y

		IL_0234: ldsfld bool ModManager::MMF
		IL_0239: brfalse.s IL_02a3

		IL_023b: ldloc.0
		IL_023c: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0241: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::y
		IL_0246: ldc.r4 -0.05
		IL_024b: blt.s IL_0259

		IL_024d: ldloc.0
		IL_024e: ldfld int32 Player/InputPackage::y
		IL_0253: ldc.i4.0
		IL_0254: bge IL_02eb

		IL_0259: ldloc.0
		IL_025a: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_025f: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_0264: ldc.r4 -0.05
		IL_0269: blt.s IL_0274

		IL_026b: ldloc.0
		IL_026c: ldfld int32 Player/InputPackage::x
		IL_0271: ldc.i4.0
		IL_0272: bge.s IL_027e

		IL_0274: ldloca.s 0
		IL_0276: ldc.i4.m1
		IL_0277: stfld int32 Player/InputPackage::downDiagonal
		IL_027c: br.s IL_02eb

		IL_027e: ldloc.0
		IL_027f: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_0284: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_0289: ldc.r4 0.05
		IL_028e: bgt.s IL_0299

		IL_0290: ldloc.0
		IL_0291: ldfld int32 Player/InputPackage::x
		IL_0296: ldc.i4.0
		IL_0297: ble.s IL_02eb

		IL_0299: ldloca.s 0
		IL_029b: ldc.i4.1
		IL_029c: stfld int32 Player/InputPackage::downDiagonal
		IL_02a1: br.s IL_02eb

		IL_02a3: ldloc.0
		IL_02a4: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_02a9: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::y
		IL_02ae: ldc.r4 -0.05
		IL_02b3: bge.un.s IL_02eb

		IL_02b5: ldloc.0
		IL_02b6: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_02bb: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_02c0: ldc.r4 -0.05
		IL_02c5: bge.un.s IL_02d1

		IL_02c7: ldloca.s 0
		IL_02c9: ldc.i4.m1
		IL_02ca: stfld int32 Player/InputPackage::downDiagonal
		IL_02cf: br.s IL_02eb

		IL_02d1: ldloc.0
		IL_02d2: ldfld valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Player/InputPackage::analogueDir
		IL_02d7: ldfld float32 [UnityEngine.CoreModule]UnityEngine.Vector2::x
		IL_02dc: ldc.r4 0.05
		IL_02e1: ble.un.s IL_02eb

		IL_02e3: ldloca.s 0
		IL_02e5: ldc.i4.1
		IL_02e6: stfld int32 Player/InputPackage::downDiagonal

		IL_02eb: ldloc.0
		IL_02ec: ret
	} // end of method RWInput::PlayerInputLogic


    
    C# code
	private static Player.InputPackage PlayerInputLogic(int categoryID, int playerNumber)
	{
		Player.InputPackage result = default(Player.InputPackage);
		Controller newController = PlayerRecentController(playerNumber);
		Custom.rainWorld.options.controls[playerNumber].UpdateActiveController(newController);
		result.controllerType = Custom.rainWorld.options.controls[playerNumber].GetActivePreset();
		result.gamePad = result.controllerType != Options.ControlSetup.Preset.KeyboardSinglePlayer && result.controllerType != Options.ControlSetup.Preset.None;
		Options.ControlSetup controlSetup = Custom.rainWorld.options.controls[playerNumber];
		switch (categoryID)
		{
		case 0:
			if (controlSetup.GetButton(0))
			{
				result.jmp = true;
			}
			if (controlSetup.GetButton(4))
			{
				result.thrw = true;
			}
			if (controlSetup.GetButton(11))
			{
				result.mp = true;
			}
			if (controlSetup.GetButton(3))
			{
				result.pckp = true;
			}
			result.analogueDir = new Vector2(controlSetup.GetAxis(1), controlSetup.GetAxis(2));
			break;
		case 1:
			if (controlSetup.GetButton(8))
			{
				result.jmp = true;
			}
			if (controlSetup.GetButton(9))
			{
				result.thrw = true;
			}
			if (controlSetup.GetButton(13))
			{
				result.mp = true;
			}
			result.analogueDir = new Vector2(controlSetup.GetAxis(6), controlSetup.GetAxis(7));
			break;
		}
		result.analogueDir = Vector2.ClampMagnitude(result.analogueDir * (ModManager.MMF ? Custom.rainWorld.options.analogSensitivity : 1f), 1f);
		if (Custom.rainWorld.options.controls[playerNumber].xInvert)
		{
			result.analogueDir.x *= -1f;
		}
		if (Custom.rainWorld.options.controls[playerNumber].yInvert)
		{
			result.analogueDir.y *= -1f;
		}
		if (result.analogueDir.x < -0.5f)
		{
			result.x = -1;
		}
		if (result.analogueDir.x > 0.5f)
		{
			result.x = 1;
		}
		if (result.analogueDir.y < -0.5f)
		{
			result.y = -1;
		}
		if (result.analogueDir.y > 0.5f)
		{
			result.y = 1;
		}
		if (ModManager.MMF)
		{
			if (result.analogueDir.y < -0.05f || result.y < 0)
			{
				if (result.analogueDir.x < -0.05f || result.x < 0)
				{
					result.downDiagonal = -1;
				}
				else if (result.analogueDir.x > 0.05f || result.x > 0)
				{
					result.downDiagonal = 1;
				}
			}
		}
		else if (result.analogueDir.y < -0.05f)
		{
			if (result.analogueDir.x < -0.05f)
			{
				result.downDiagonal = -1;
			}
			else if (result.analogueDir.x > 0.05f)
			{
				result.downDiagonal = 1;
			}
		}
		return result;
	}
*/