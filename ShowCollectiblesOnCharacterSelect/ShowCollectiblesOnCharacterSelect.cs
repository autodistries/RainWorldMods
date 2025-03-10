﻿using BepInEx;
using MoreSlugcats;
using System;

using System.Threading.Tasks;
using UnityEngine;

using System.Diagnostics;
// Access private fields and stuff
using System.Security;
using System.Security.Permissions;
#pragma warning disable CS0618 // SecurityAction.RequestMinimum is obsolete. However, this does not apply to the mod, which still needs it. Suppress the warning indicating that it is obsolete.
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace ShowCollectiblesOnCharacterSelect; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class CollectiblesOnCharacterSelect : BaseUnityPlugin
{

    bool doneWithInit = false;

    string originalModDesc = null;
    internal static long? lastLoadingTime = null;
    internal static SlugcatStats.Name lastSlugcatLoaded = null;

    private object _I_lock_save_reading = new object();
    
    private ModOptions options;

    int manualLoadDelay = 0;

    CollectiblesTracker singleCollectiblesTracker;


    public CollectiblesOnCharacterSelect()
    {
    }

    private void Awake()
    {
        if (doneWithInit) return;
        Logger.LogInfo("Mod is Awake !");
        options = new ModOptions(Logger);
        try
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
            On.Menu.SlugcatSelectMenu.ctor += addUnlocksToMenu;
            On.Menu.SlugcatSelectMenu.Update += manualKeybindPopper;
            On.Menu.SlugcatSelectMenu.UpdateSelectedSlugcatInMiscProg += autoUpdateTracker;
            Logger.LogInfo("Hooks registered");
        }
        catch (Exception ex)
        {

            Logger.LogError("Owie ! something bad happened with this mod, so it has been disabled !\n" + ex);
            On.RainWorld.OnModsInit -= RainWorldOnOnModsInitDetour;
            On.Menu.SlugcatSelectMenu.ctor -= addUnlocksToMenu;
            On.Menu.SlugcatSelectMenu.UpdateSelectedSlugcatInMiscProg -= autoUpdateTracker;
        }
        doneWithInit = true;
    }

    private void manualKeybindPopper(On.Menu.SlugcatSelectMenu.orig_Update orig, Menu.SlugcatSelectMenu self)
    {
        orig(self);
        if (options.enableDynamicToggleLoading.Value)
        {

            if (manualLoadDelay == 0 && Input.GetKey(options.loadTrackerKeybind.Value))
            {

                if (!options.shouldLoadCollectiblesOnTheMenu.Value && singleCollectiblesTracker == null)
                {
                    addTrackerToMenu(self);
                    options.shouldLoadCollectiblesOnTheMenu.Value = true;
                }
                else
                {
                    options.shouldLoadCollectiblesOnTheMenu.Value = false;
                    if (singleCollectiblesTracker != null)
                    {
                        self.pages[0].RemoveSubObject(singleCollectiblesTracker);
                        singleCollectiblesTracker.RemoveSprites();
                        singleCollectiblesTracker = null;
                    }
                }

                manualLoadDelay = 35;
            }

            if (manualLoadDelay != 0) manualLoadDelay--;



        }
    }

    private void shoMeRandomDesc(On.Menu.Remix.InternalOI_Stats.orig__PreviewMod orig, Menu.Remix.InternalOI_Stats self, Menu.Remix.MenuModList.ModButton button)
    {
        orig(self, button);
        if (self.previewMod?.id == PluginInfo.PLUGIN_GUID && self.lblDescription?.text != null)
        {
            // Logger.LogDebug("Previewing our own mod");
            originalModDesc ??= self.lblDescription.text;
            self.lblDescription.text = originalModDesc;
            self.lblDescription.text += "\nAutomatic Tracker loading is currently " + ((options.shouldLoadCollectiblesOnTheMenu.Value == true) ? $"EN" : "DIS") + "abled. ";
            self.lblDescription.text += (lastLoadingTime != null) ? $"Last time, loading collectibles for {SlugcatStats.getSlugcatName(lastSlugcatLoaded)} took {lastLoadingTime}ms." : "";

        }
    }

    private void addUnlocksToMenu(On.Menu.SlugcatSelectMenu.orig_ctor orig, Menu.SlugcatSelectMenu self, ProcessManager manager)
    {
        orig(self, manager);
        if (options.shouldLoadCollectiblesOnTheMenu.Value) addTrackerToMenu(self);
    }

    private void autoUpdateTracker(On.Menu.SlugcatSelectMenu.orig_UpdateSelectedSlugcatInMiscProg orig, Menu.SlugcatSelectMenu self)
    {
        orig(self);
        if (options.shouldLoadCollectiblesOnTheMenu.Value && singleCollectiblesTracker != null && self.pages[0].subObjects.Contains(singleCollectiblesTracker))
        {
            addTrackerToMenu(self);
        }
    }

    private async void addTrackerToMenu(Menu.SlugcatSelectMenu self)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
                {
                    lock (_I_lock_save_reading)
                    {
                        if (singleCollectiblesTracker != null)
                        {
                            self.pages[0].RemoveSubObject(singleCollectiblesTracker);
                            singleCollectiblesTracker.RemoveSprites();
                            singleCollectiblesTracker = null;
                        }
                        lastSlugcatLoaded = self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
                        self.manager.rainWorld.progression.GetOrInitiateSaveState(lastSlugcatLoaded, null, self.manager.menuSetup, false);
                        singleCollectiblesTracker = new CollectiblesTracker(self, self.pages[0], new Vector2(self.manager.rainWorld.options.ScreenSize.x - 50f + (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, self.manager.rainWorld.options.ScreenSize.y - 15f), self.container, self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat);
                        self.pages[0].subObjects.Add(singleCollectiblesTracker);
                    }
                });
        stopwatch.Stop();
        Logger.LogDebug($"Loaded collectibles for {self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat} in {stopwatch.ElapsedMilliseconds}ms");
        lastLoadingTime = stopwatch.ElapsedMilliseconds;

    }


    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (MachineConnector.SetRegisteredOI(PluginInfo.PLUGIN_GUID, options))
        {
            Logger.LogInfo("Registered Mod Interface");

        }
        else
        {
            Logger.LogError("Could not register Mod Interface");
        }
        On.Menu.Remix.InternalOI_Stats._PreviewMod += shoMeRandomDesc;

    }
}
