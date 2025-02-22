using System;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ShowCollectiblesOnCharacterSelect;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource Logger;

    Configurable<bool> shouldLoadCollectiblesOnTheMenu;


    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");

        shouldLoadCollectiblesOnTheMenu = config.Bind("shouldLoadCollectiblesOnTheMenu", true);
        try
        {
            Logger.LogInfo("Getting and setting current value to " + shouldLoadCollectiblesOnTheMenu.Value);

            CollectiblesOnCharacterSelect.autoLoadItems = shouldLoadCollectiblesOnTheMenu.Value;
        }
        catch (Exception ex)
        {
            Logger.LogError("Owie !" + ex);
        }
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

        var cb = new OpCheckBox(shouldLoadCollectiblesOnTheMenu, new Vector2(10f, 490f))  {
                description = @$"If this is enabled, viewing any character on the selection screen will also load its collectibles in the top-right.{((CollectiblesOnCharacterSelect.lastLoadingTime != null) ? $"\nLast time, this threaded operation took {CollectiblesOnCharacterSelect.lastLoadingTime}ms." : "")}"
            };
        cb.OnValueChanged += applyModEnabledOrNot;

        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Show collectibles character select menu options", true),
            new OpLabel(43f, 492f, "Enable auto-loading collectibles"),
            cb
        };

        opTab.AddItems(UIArrPlayerOptions);
    }

    private void applyModEnabledOrNot(UIconfig config, string value, string oldValue)
    {
        Logger.LogInfo($"Value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            CollectiblesOnCharacterSelect.autoLoadItems = true;
        }
        else
        {
            CollectiblesOnCharacterSelect.autoLoadItems = false;
        }
    }

    private void togglePreventSpearsHit()
    {

    }
}