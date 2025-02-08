using BepInEx;
using UnityEngine;
using static StepByStep.PluginInfo;
namespace StepByStep;

using System;
using BepInEx.Logging;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class StepByStep : BaseUnityPlugin
{
    static BepInEx.Logging.ManualLogSource LocalLogSource;
    bool localPauseStatus = false;
    bool currentlySteppingStatus = false;
    int stepperCounter = 0;
    public static ManualLogSource lls
    {
        get => LocalLogSource;
    }





    #region opts
    public System.Collections.Generic.Dictionary<Configurable<bool>, ModOptions.SomeUpdateFunction> Configurables = new();

    private Configurable<KeyCode> pauseButtonConfigurable; // stored in this.config.configurables[]
    private Configurable<KeyCode> stepButtonConfigurable; // stored in this.config.configurables[]
    private Configurable<float> chainStepSpeedConfigurable;

    private readonly KeyCode[] keys = new KeyCode[2];

    private float chainSpeedStep = 0.5f;

    private ModOptions Options;

    private bool diffKeypresses = true;

    // line 280854 SubTrack.source(AudioSource).Stop()
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
        On.Music.Song.Update += MusicUpdateStopper; // prevents musics from being skipped if stepping
    }

    private void MusicUpdateStopper(On.Music.Song.orig_Update orig, Music.Song self)
    {
        if (!currentlySteppingStatus) orig(self);
    }

    private void PauseMusic(RainWorld self) {
        Music.MusicPlayer player = self.processManager.musicPlayer;
        if (player.song == null) return;
        foreach (var track in player.song.subTracks) {
            track.source.Pause();
        }
    }



        private void UnpauseMusic(RainWorld self) {
        Music.MusicPlayer player = self.processManager.musicPlayer;
        if (player.song == null) return;
        foreach (var track in player.song.subTracks) {
            track.source.UnPause();
        }
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
        // lls.LogDebug("input says: " + Input.inputString);
        if (Input.GetKeyDown(keys[0]) || Input.GetKeyUp(keys[0]) )
        {
                            // lls.LogInfo("KEY IS DONWNWNN!");


            if (!localPauseStatus && Input.GetKeyDown(keys[0]))
            {
                localPauseStatus = true;
                lls.LogInfo("pausing game !");
                diffKeypresses = false;
                PauseMusic(self);
            }
            else if (localPauseStatus && Input.GetKeyUp(keys[0]))
            {
                if (diffKeypresses)
                {
                    localPauseStatus = false; 
                    diffKeypresses = false;
                    lls.LogInfo("resume game !");
                UnpauseMusic(self);

                }
                else diffKeypresses = true;
            }

        }
        else
        {
            if (localPauseStatus)
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


