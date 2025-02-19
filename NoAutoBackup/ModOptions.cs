using System;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace NoAutoBackup;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource Logger;

    Configurable<bool> shouldCancelAutoBackups;


    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");
        shouldCancelAutoBackups = config.Bind("shouldCancelAutoBackups", true);
        try
        {
            Logger.LogInfo("Getting and setting current value to " + shouldCancelAutoBackups.Value);

            BackupsDisabler.modEnabled = shouldCancelAutoBackups.Value;
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

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "BackupsDisablerOptions");
        this.Tabs = new[]
        {
            opTab
        };

        var cb = new OpCheckBox(shouldCancelAutoBackups, new Vector2(10f, 490f));
        cb.OnValueChanged += applyModEnabledOrNot;

        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Auto-backups disabler", true),
            new OpLabel(43f, 492f, "Disable auto backups"),
            cb
        };

        opTab.AddItems(UIArrPlayerOptions);
    }

    private void applyModEnabledOrNot(UIconfig config, string value, string oldValue)
    {
        Logger.LogInfo($"Value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            BackupsDisabler.modEnabled = true;
        }
        else
        {
            BackupsDisabler.modEnabled = false;
        }
    }

    private void togglePreventSpearsHit()
    {

    }
}