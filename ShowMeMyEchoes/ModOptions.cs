using System;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ShowMeMyEchoes;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource Logger;

    OpCheckBox cb;
    Configurable<bool> isModEnabledShowCollected;
    OpCheckBox cb2;
    Configurable<bool> showPrimedEchoes;
    OpCheckBox cb3;
    Configurable<bool> showNeverMetEchoes;


    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");
        isModEnabledShowCollected = config.Bind("isModEnabledShowCollected", true);
        showPrimedEchoes = config.Bind("showPrimedEchoes", true);
        showNeverMetEchoes = config.Bind("showNeverMetEchoes", false);
        try
        {
            Logger.LogInfo("Getting and setting main current value to " + isModEnabledShowCollected.Value);

            ShowMeMyEchoes.modSwitchOn = isModEnabledShowCollected.Value;
            ShowMeMyEchoes.showPrimedGhosts = showPrimedEchoes.Value;
            ShowMeMyEchoes.showNeverMetGhosts = showNeverMetEchoes.Value;
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

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "ShowExhoesOptions");
        this.Tabs = new[]
        {
            opTab
        };

        cb = new OpCheckBox(isModEnabledShowCollected, new Vector2(10f, 490f)) { description = "If this is enabled, collected echoes will show up as a yellow-orange collected token.\nIf off, disables everything else" };
        cb.OnValueChanged += applyModEnabledOrNot;

        cb2 = new OpCheckBox(showPrimedEchoes, new Vector2(10f, 464f)) { description = "If this is enabled, primed echoes will show up as a yellow-orange non collected token" };
        cb2.OnValueChanged += applyShowPrimedEchoes;

        cb3 = new OpCheckBox(showNeverMetEchoes, new Vector2(10f, 438f)) { description = "If this is enabled, collected echoes will show up as a yellow-orange dot" };
        cb3.OnValueChanged += applyShowNeverMetEchoes;

        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Show Me My Echoes Options", true),
            new OpLabel(43f, 492f, "Enable mod and show Collected Echoes"),
            cb,
            new OpLabel(43f, 466f, "Show Primed Echoes"),
            cb2,
            new OpLabel(43f, 440f, "Show never-before seen Echoes"),
            cb3,

        };

        opTab.AddItems(UIArrPlayerOptions);
    }

    private void applyShowNeverMetEchoes(UIconfig config, string value, string oldValue)
    {
        Logger.LogInfo($"showNeverMetEchoes value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            ShowMeMyEchoes.showNeverMetGhosts = true;
        }
        else
        {
            ShowMeMyEchoes.showNeverMetGhosts = false;
        }
    }

    private void applyShowPrimedEchoes(UIconfig config, string value, string oldValue)
    {
        Logger.LogInfo($"applyShowPrimedEchoes value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            ShowMeMyEchoes.showPrimedGhosts = true;
        }
        else
        {
            ShowMeMyEchoes.showPrimedGhosts = false;
        }
    }

    private void applyModEnabledOrNot(UIconfig config, string value, string oldValue)
    {
        Logger.LogInfo($"main value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            ShowMeMyEchoes.modSwitchOn = true;
            cb2.greyedOut = false;
            cb3.greyedOut = false;
        }
        else
        {
            ShowMeMyEchoes.modSwitchOn = false;
            cb2.greyedOut = true;
            cb3.greyedOut = true;
        }
    }
}