using System;
using BepInEx.Logging;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace BackupTools;

public class ModOptions : OptionInterface
{
    private const string TextWarnAgainstDisable = "--- WARNING ---\nDisabling this option means next time the game will automatically back your saves up, if you already have more than 50 auto-generated backups, the oldest one will be deleted. You risk losing data.\nIf you're unsure, please manually ckeck and/or backup your saves !";
    private readonly ManualLogSource Logger;

    internal Configurable<bool> shouldPreventDelAutoBackups;
    internal Configurable<bool> shouldCancelAutoBackups;


    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");
        shouldPreventDelAutoBackups = config.Bind("shouldPreventDelAutoBackups", false);
        shouldCancelAutoBackups = config.Bind("shouldCancelAutoBackups", false);

        // try
        // {
        //     // nah dude this doesnet nowrl, not this eealy
        //     Logger.LogInfo("Force-early loading config file");
        //     this._LoadConfigFile();
        //     Logger.LogInfo("Getting and setting current values to " + shouldCancelAutoBackups.Value+","+shouldPreventDelAutoBackups.Value);

        //     BackupTools.noAutoBackups = shouldCancelAutoBackups.Value;
        //     BackupTools.noAutoDelete = shouldPreventDelAutoBackups.Value;
        // }
        // catch (Exception ex)
        // {
        //     Logger.LogError("Owie !" + ex);
        // }
    }

    // UIelement[] MakeCheckBox
    public override void Initialize()
    {
        WasInitialized = true;
        yOffsetValue = 490f;
        Logger.LogInfo("Mod Options INITIALIZED");

        var opTab = new OpTab(this, "BackupsDisablerOptions");
        Tabs = new[]
        {
            opTab
        };


        UIelement[] disableAutoBackupEls = MakeCheckBox("Prevent the game from creating automatic backups", shouldCancelAutoBackups, applyEnableDisableAUtoBackups);
        UIelement[] disableAutoDeleteBackups = MakeCheckBox("Prevent auto-deleting automatic backups when there are >50 and the game creates a new one", shouldPreventDelAutoBackups, applyEnableDisableNODelBack);

        // var triggerautoBackupButton = new OpSimpleButton(new Vector2(10f, 400f), new Vector2(100f, 30f), "Trigger AutoBackup");
        // triggerautoBackupButton.OnClick += triggerIt;

        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Backup Tools", true),
            // triggerautoBackupButton
        };

        opTab.AddItems(UIArrPlayerOptions);
        opTab.AddItems(disableAutoBackupEls);
        opTab.AddItems(disableAutoDeleteBackups);


        if (BackupTools.ILFailed)
        {
            ConfigConnector.CreateDialogBoxNotify(BackupTools.ILFailedText);

        }

    }


    float yOffsetValue = 490f;


    private UIelement[] MakeCheckBox(string text, Configurable<bool> configurable, Action<UIconfig, string, string> onValueChange, string descText = null)
    {
        UIelement[] res = new UIelement[2];
        var cb = new OpCheckBox(configurable, new Vector2(10f, yOffsetValue));
        cb.OnValueUpdate += (uiconfig, a, b) =>
        {
            onValueChange(uiconfig, a, b);
        };


        var a = new OpLabel(43f, yOffsetValue + 2f, text);

        res[0] = cb;
        res[1] = a;

        yOffsetValue += 36f;

        return res;
    }



    private void triggerIt(UIfocusable trigger)
    {
        try
        {
            Logger.LogInfo("Did trigger the thing");
            RWCustom.Custom.rainWorld.progression.CreateCopyOfSaves(false);

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private void applyEnableDisableAUtoBackups(UIconfig config, string value, string oldValue)
    {
        // Logger.LogInfo($"Value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            BackupTools.noAutoBackups = true;
        }
        else
        {
            BackupTools.noAutoBackups = false;
        }
    }


    private void applyEnableDisableNODelBack(UIconfig config, string value, string oldValue)
    {
        // Logger.LogInfo($"Value Changed : {oldValue}->{value}");
        if (value.ToLower() == "true")
        {
            BackupTools.noAutoDelete = true;
        }
        else
        {
            Action yesAction = new(() =>
            {
                BackupTools.noAutoDelete = false;
                Logger.LogInfo("Ok yep it's off");
            });
            Action noAction = new(() =>
            {
                Logger.LogInfo("cancelled the off");
                config.value = "true";

            });
            ConfigConnector.CreateDialogBoxYesNo(TextWarnAgainstDisable, yesAction, noAction);
        }
    }


}