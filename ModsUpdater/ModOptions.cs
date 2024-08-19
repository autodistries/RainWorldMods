using BepInEx;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;
namespace ModsUpdater;

public class ModOptions : OptionInterface
{
    ModsUpdater parent;
    bool WasInitialized = false;
    ManualLogSource lls;


    

    UIelement[] UIArrPlayerOptions;
    public ModOptions(ModsUpdater parent, BepInEx.Logging.ManualLogSource logSource)
    {
        lls = logSource;
        this.parent = parent;

    }


    public override void Initialize()
    {
        base.Initialize();
        WasInitialized = true;

        lls.LogInfo("TemplateModOptions INITIALIZED");

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "Mod Options");
        this.Tabs = new[]
        {
            opTab
        };

        lls.LogDebug(1);


        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Mods Updater", true),
        };
        lls.LogDebug(2);


        opTab.AddItems(UIArrPlayerOptions);
        lls.LogDebug(3);


        parent.SetUpOptionsSettings();
        lls.LogDebug(4);


    }



    internal void SetUpOptionsBindings()
    {
        //Binds the available options
        //Adds them to the global options list so that they can be initiated
        // IsLoggingEnabled = Options.config.Bind<bool>("IsLoggingEnabled", true);
        // Configurables.Add(IsLoggingEnabled, ToggleCustomLogReplacements);

        // pauseButtonConfigurable = Options.config.Bind<KeyCode>("pauseButtonConfigurable", KeyCode.P);
        // pauseButtonConfigurable.info.description = "Key pressed to enter and exit meta-pause";
        // pauseButtonConfigurable.OnChange += () => { keys[0] = pauseButtonConfigurable.Value; };


        // stepButtonConfigurable = Options.config.Bind<KeyCode>("stepButtonConfigurable", KeyCode.L);
        // stepButtonConfigurable.info.description = "Key pressed to step forwards once";
        // stepButtonConfigurable.OnChange += () => { keys[1] = stepButtonConfigurable.Value; };


        // chainStepSpeedConfigurable = Options.config.Bind<float>("chainStepSpeedConfigurable", 0.20f, new ConfigAcceptableRange<float>(0f, 1f));
        // chainStepSpeedConfigurable.info.description = "Speed at which steps chain when holding the step key. 1 = 1/60 updates = 1 update/sec";
        // chainStepSpeedConfigurable.OnChange += () =>
        // {
        //     chainSpeedStep = chainStepSpeedConfigurable.Value;
        // };

    }




     public void AddButtonOption(int num, string text, Menu.Remix.MixedUI.OnSignalHandler updateFunction)
    {
                lls.LogDebug(6);

        if (this.Tabs != default(OpTab[]))
        {
                    lls.LogDebug(7);

            float num2 = 20f;
            float num3 = 550f;
            OpSimpleButton opSimpleButton = new OpSimpleButton(new Vector2(num2, num3 - num), new Vector2(170f, 36f), text) {
                description = text,
            };
                    lls.LogDebug(8);

            opSimpleButton.OnClick += updateFunction;

            Tabs[0].AddItems(opSimpleButton);
                    lls.LogDebug(9);

        }
        else
        {
            lls.LogError("We tried to add elements to our settings page, but there was no settings page");
        }
    }



}