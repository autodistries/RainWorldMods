using BepInEx;
using Menu;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;




// Access private fields and stuff
using System.Security.Permissions;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace BackgroundPreview; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

// These properties are properly set when building, so errors are to be expected in your IDE, but building will occur properly.
// If not, please check the .csproj !
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class ModMainClass : BaseUnityPlugin
{


    bool done = false;
    private SimpleButton previewButton;
    public static MenuScene.SceneID lastSelectedBkg = MenuScene.SceneID.MainMenu;

    public static Options options;


    // class MoreSlugcats.BackgroundsButton : SimpleButton event 
    // singals "BACKGROUND"
    // picked up by void OptionsMenu.Singal(MenuObject sender, string message)

    // if need to create a new process for this, hook ProcessManager.PostSwitchMainProcess

    // new BackgroundOptionsMenu(this); becomes main process



    public ModMainClass()
    {
        On.MoreSlugcats.BackgroundOptionsMenu.ctor += addPreviewButton;
        On.MoreSlugcats.BackgroundOptionsMenu.Singal += catchPreviewSingal;
        On.MoreSlugcats.BackgroundOptionsMenu.UpdateInfoText += showHintForUs;
        On.ProcessManager.PostSwitchMainProcess += checkSwitchPreview;

        BackgroundPreviewMenu.Logger = Logger;

        options = new Options();
    }

    private void checkSwitchPreview(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        // Logger.LogInfo($"We're inside PostSwitchMainProcess: {ID}");

        if (ID == BackgroundPreviewMenu.ProcessID.BackgroundPreviewID)
        {
            // Logger.LogInfo("Creating new BackgroundPreviewMenu");
            BackgroundPreviewMenu.previousProcessID = self.oldProcess?.ID ?? MoreSlugcats.MMFEnums.ProcessID.BackgroundOptions;
            self.currentMainLoop = new BackgroundPreviewMenu(self);
        }
        orig(self, ID);
    }

    private string showHintForUs(On.MoreSlugcats.BackgroundOptionsMenu.orig_UpdateInfoText orig, MoreSlugcats.BackgroundOptionsMenu self)
    {
        if (self.selectedObject == previewButton)
        {
            return "Preview selected background";
        }
        return orig(self);
    }

    private void catchPreviewSingal(On.MoreSlugcats.BackgroundOptionsMenu.orig_Singal orig, MoreSlugcats.BackgroundOptionsMenu self, MenuObject sender, string message)
    {
        if (message == "PREVIEWSCENE")
        {
            self.manager.RequestMainProcessSwitch(BackgroundPreviewMenu.ProcessID.BackgroundPreviewID);
            self.PlaySound(SoundID.MENU_Switch_Page_Out);
            // Logger.LogInfo("Stating previewScene, was running any dialogue? "+self.manager.IsRunningAnyDialog);
            // self.manager.rainWorld.options.Save();
            return;
        }
        else if (message.Contains("BACKGROUND"))
        {
            int num = int.Parse(message.Substring("BACKGROUND".Length), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
            lastSelectedBkg = self.IndexToOption(num);
            // Logger.LogInfo("Last selected scene :" + lastSelectedBkg);
        }
        orig(self, sender, message);
    }

    private void addPreviewButton(On.MoreSlugcats.BackgroundOptionsMenu.orig_ctor orig, MoreSlugcats.BackgroundOptionsMenu self, ProcessManager manager)
    {
        orig(self, manager);
        var btnSize = new Vector2(90f, 30f);
        var mvv = new Vector2(20, 0);
        if (self.TotalPages > 1)
        {
            self.prevPageButton.pos.x -= 20;
            self.prevPageButton.SetSize(btnSize);
            self.nextPageButton.pos.x -= 50;
            self.nextPageButton.SetSize(btnSize);
        }
        previewButton = new SimpleButton(self, self.pages[0], "PREVIEW", "PREVIEWSCENE", new Vector2(580f, 50f), btnSize);
        self.pages[0].subObjects.Add(previewButton);


        lastSelectedBkg = manager.rainWorld.options.TitleBackground;
        Logger.LogInfo("Initialized with bkg " + lastSelectedBkg);
    }

    private void OnEnable()
    {
        if (done) return;
        Logger.LogInfo("Enabled !");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        done = true;
    }

    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

            BackgroundPreviewMenu.ProcessID.RegisterValues();


        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, options))
        {
            Logger.LogInfo("Registered Mod Interface");
        }
        else
        {
            Logger.LogError("Could not register Mod Interface");
        }
    }
}
