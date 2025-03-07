using System;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace ShowCollectiblesOnCharacterSelect;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource Logger;

    public Configurable<bool> shouldLoadCollectiblesOnTheMenu;
    public Configurable<bool> enableDynamicToggleLoading;
    public Configurable<KeyCode> loadTrackerKeybind;




    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");

        shouldLoadCollectiblesOnTheMenu = config.Bind("shouldLoadCollectiblesOnTheMenu", true);
        enableDynamicToggleLoading = config.Bind("enableDynamicToggleLoading", true);
        loadTrackerKeybind = config.Bind("loadTrackerKeybind", KeyCode.T);


    }
    public override void Initialize()
    {
        WasInitialized = true;
        Logger.LogInfo("Mod Options INITIALIZED");

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "CollectibleonmenuOptions");
        this.Tabs = new[]
        {
            opTab
        };


        // 
        var lastLoadingTImeLabel = new OpLabel(10, 400, $"{((CollectiblesOnCharacterSelect.lastLoadingTime != null) ? $"Last time, for {SlugcatStats.getSlugcatName(CollectiblesOnCharacterSelect.lastSlugcatLoaded)}, this threaded operation took {CollectiblesOnCharacterSelect.lastLoadingTime}ms." : "")}");

        var cb = new OpCheckBox(shouldLoadCollectiblesOnTheMenu, new Vector2(10f, 490f))
        {
            description = @$"When this is enabled, viewing any character on the selection screen will also load its collectibles in the top-right."
        };
        var cb2 = new OpCheckBox(enableDynamicToggleLoading, new Vector2(10f, 460f))
        {
            description = @$"This enables or disables listening to the following keybind.\nPressing that key on the Slugcat Selection screen will toggle the upper checkbox."
        };

        // cb2.OnValueChanged += (e,newVal,oldVal) =>
        // {
        //     if (newVal == "true" && cb.GetValueBool() == true)
        //     {
        //         cb.SetValueBool(false);
        //     }
        // };
        // cb.OnValueChanged += (e,newVal,oldVal) =>
        // {
        //     if (newVal == "true" && cb2.GetValueBool() == true)
        //     {
        //         cb2.SetValueBool(false);
        //     }
        // };


        OpKeyBinder opKeyBinder = new OpKeyBinder(loadTrackerKeybind, new Vector2(140, 456), new Vector2(20, 20))
        {
            description = "The keybind which will toggle auto-loading the Tracker when pressed."
        };

        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Show collectibles character select menu options", true),
            new OpLabel(43f, 492f, "Enable auto-loading collectibles"),
            cb,
            cb2,
            new OpLabel(43f, 462f, "Enable keybind"),
            opKeyBinder,

            lastLoadingTImeLabel

        };




        opTab.AddItems(UIArrPlayerOptions);


        /*
            float num2 = 20f;
            float num3 = 550f;
            OpKeyBinder opKeyBinder = new OpKeyBinder(configurable, new Vector2(num2, num3 - num), new Vector2(20, 20))
            {
                description = configurable.info.description
            };
            UIfocusable.MutualVerticalFocusableBind(opKeyBinder, opKeyBinder);
            OpLabel opLabel2 = new OpLabel(new Vector2(40 + num2, num3 - num), new Vector2(170f, 36f), quickDesc, FLabelAlignment.Left, bigText: false)
            {
                description = opKeyBinder.description
            };
        */
    }


}