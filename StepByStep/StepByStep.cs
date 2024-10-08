﻿using BepInEx;
using Menu;
using Menu.Remix.MixedUI;
using On.RWCustom;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static StepByStep.PluginInfo;
namespace StepByStep;

using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;


[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class StepByStep : BaseUnityPlugin
{
    static BepInEx.Logging.ManualLogSource LocalLogSource;
    bool localPauseStatus = false;
    bool currentlySteppingStatus = false;
    int stepperCounter = 0;
    int intermediateDelay = 0;
    public static ManualLogSource lls
    {
        get => LocalLogSource;
    }

    bool releasedPauseButton = false;




    #region opts
    public System.Collections.Generic.Dictionary<Configurable<bool>, ModOptions.SomeUpdateFunction> Configurables = new();

    private Configurable<KeyCode> pauseButtonConfigurable; // stored in this.config.configurables[]
    private Configurable<KeyCode> resumeButtonConfigurable; // stored in this.config.configurables[]
    private Configurable<KeyCode> stepButtonConfigurable; // stored in this.config.configurables[]
    private Configurable<float> chainStepSpeedConfigurable;

    private KeyCode[] keys = new KeyCode[2];

    private float chainSpeedStep = 0.5f;

    private ModOptions Options;
    #endregion opts



    #region modecode
    public StepByStep()
    {
        LocalLogSource = base.Logger;
        LocalLogSource.LogInfo("Initialized.");
        Options = new ModOptions(this, LocalLogSource);
        SetUpOptionsBindings();

        setStatingValues(pauseButtonConfigurable.Value, stepButtonConfigurable.Value, chainStepSpeedConfigurable.Value);
    }


#pragma warning disable IDE0051 // this is for the OnEnable and OnDisable unused warnings
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += OnModsInitDetour;
        On.RainWorld.Update += UpdateStopper;
    }


    private void OnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, Options))
        {
            lls.LogInfo("Registered Mod Interface");
        }
        else
        {
            lls.LogError("Could not register Mod Interface");
        }
    }
#pragma warning restore IDE0051

    private void UpdateStopper(On.RainWorld.orig_Update orig, RainWorld self)
    {
            if (intermediateDelay != 0) intermediateDelay--;
        if (Input.GetKey(keys[0]) )
        {
            if (!localPauseStatus && intermediateDelay==0)
            {
                localPauseStatus = true;
                intermediateDelay = 10;
            }
            else
            {

                if (intermediateDelay == 0)
                {
                    localPauseStatus = false;
                    lls.LogInfo("recieved end of press");
                    intermediateDelay = 10;
                }
            }
        }


        else if (localPauseStatus)
        {

            if (Input.GetKey(keys[1]))
            {

                if ((stepperCounter == 0) || stepperCounter > 20)
                { //2s ?
                    if (stepperCounter % ((int)(chainSpeedStep * 40)) == 0)
                    {
                        currentlySteppingStatus = true;
                    }
                }
                stepperCounter++;
                //LocalLogSource.LogDebug(stepperCounter + " " + (stepperCounter % ((int)(chainSpeedStep * 50))));
            }
            else stepperCounter = 0;

        }

        if (!localPauseStatus || currentlySteppingStatus) orig(self);

        if (currentlySteppingStatus == true)
        {
            //LocalLogSource.LogInfo("Stepping");
            currentlySteppingStatus = false;
        }
    }
    #endregion modecode

    internal void SetUpOptionsBindings()
    {
        //Binds the available options
        //Adds them to the global options list so that they can be initiated
        // IsLoggingEnabled = Options.config.Bind<bool>("IsLoggingEnabled", true);
        // Configurables.Add(IsLoggingEnabled, ToggleCustomLogReplacements);

        pauseButtonConfigurable = Options.config.Bind<KeyCode>("pauseButtonConfigurable", KeyCode.P);
        pauseButtonConfigurable.info.description = "Key pressed to enter and exit meta-pause";
        pauseButtonConfigurable.OnChange += () => { keys[0] = pauseButtonConfigurable.Value; };


        stepButtonConfigurable = Options.config.Bind<KeyCode>("stepButtonConfigurable", KeyCode.L);
        stepButtonConfigurable.info.description = "Key pressed to step forwards once";
        stepButtonConfigurable.OnChange += () => { keys[1] = stepButtonConfigurable.Value; };


        chainStepSpeedConfigurable = Options.config.Bind<float>("chainStepSpeedConfigurable", 0.20f, new ConfigAcceptableRange<float>(0f, 1f));
        chainStepSpeedConfigurable.info.description = "Speed at which steps chain when holding the step key. 1 = 1/60 updates = 1 update/sec";
        chainStepSpeedConfigurable.OnChange += () =>
        {
            chainSpeedStep = chainStepSpeedConfigurable.Value;
        };

    }

    internal void SetUpOptionsSettings()
    {
        Options.AddKeyBindOption(pauseButtonConfigurable, 40, "Key for pause");
        Options.AddKeyBindOption(stepButtonConfigurable, 75, "Key for step");
        Options.AddSliderOption(chainStepSpeedConfigurable, 110, "Speed of chain-step");
    }
    internal void setStatingValues(KeyCode pause, KeyCode step, float chain)
    {
        keys[0] = pause;
        keys[1] = step;
        chainSpeedStep = chain;
    }

}


