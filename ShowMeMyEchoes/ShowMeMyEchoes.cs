using BepInEx;
using Menu;
using MoreSlugcats;
using System.Linq;




using System.Text.RegularExpressions;
using UnityEngine;
// Access private fields and stuff
#pragma warning disable CS0618 // SecurityAction.RequestMinimum is obsolete. However, this does not apply to the mod, which still needs it. Suppress the warning indicating that it is obsolete.
using System.Security;
using System.Security.Permissions;
using static GhostWorldPresence;
using System.Collections.Generic;
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ShowMeMyEchoes; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]


public partial class ShowMeMyEchoes : BaseUnityPlugin
{

    bool done = false;
    static bool autoLoadItems = true;
    private object _lock = new object();
    CollectiblesTracker singleCollectiblesTracker;
    internal static bool modSwitchOn = true;
    internal static bool showPrimedGhosts = true;
    internal static bool showNeverMetGhosts = true;
    private ModOptions options;
    static readonly Color echoTokenColor = new(1, 0.86f, 0.54f, 1);

    public ShowMeMyEchoes()
    {
    }

    private void Awake()
    {
        if (done) return;
        Logger.LogInfo("Hello World !");
        options = new ModOptions(Logger);
        try
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
            On.MoreSlugcats.CollectiblesTracker.ctor += storeObjectAndAppendEchoes;

        }
        catch (System.Exception ex)
        {

            Logger.LogError("Owie, something bad happened with this mod, so it has been disabled !\n" + ex);
            On.MoreSlugcats.CollectiblesTracker.ctor -= storeObjectAndAppendEchoes;
            On.RainWorld.OnModsInit -= RainWorldOnOnModsInitDetour;
        }
        done = true;
    }

    private void storeObjectAndAppendEchoes(On.MoreSlugcats.CollectiblesTracker.orig_ctor orig, CollectiblesTracker self, Menu.Menu menu, MenuObject owner, Vector2 pos, FContainer container, SlugcatStats.Name saveSlot)
    {
        orig(self, menu, owner, pos, container, saveSlot);
        if (!modSwitchOn) return;
        singleCollectiblesTracker = self;
        Logger.LogInfo("Stored CollectiblesTracker");
        if (singleCollectiblesTracker != null && menu.manager.rainWorld.progression.currentSaveState != null)
        {
            List<string> regionsAlreadyProcessed = new();

            Logger.LogInfo($"Found a savestate for {saveSlot}");
            foreach (var pair in menu.manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.ghostsTalkedTo)
            {//#FEBA70
                regionsAlreadyProcessed.Add(pair.Key.ToString().ToUpper());
                FSprite s = null;
                bool addIt = false;
                Logger.LogInfo($"adding a token for {pair.Key} (val {pair.Value})");
                GhostID ghostID = GetGhostID(pair.Key.ToString().ToUpper());
                if (ghostID == GhostID.NoGhost) {
                    Logger.LogWarning("Could not find registered ghost ID of region"+pair.Key.ToString().ToUpper()+", discarding it");
                    continue;
                }
                if ((pair.Value != 0) && false == SpawnGhost(ghostID, 9, 9, pair.Value, saveSlot == SlugcatStats.Name.Red))
                {
                    Logger.LogDebug($"This one has already been collected before !");
                    s = new FSprite("ctOn") { color = echoTokenColor };
                    addIt = true;
                }
                else if (showPrimedGhosts && SpawnGhost(ghostID, 9, 9, pair.Value, saveSlot == SlugcatStats.Name.Red))
                {
                    Logger.LogDebug($"This one has been primed !");
                    s = new FSprite("ctOff") { color = echoTokenColor };
                    addIt = true;
                }
                else if (showNeverMetGhosts && pair.Value == 0 && singleCollectiblesTracker.sprites.Keys.Contains(pair.Key.ToString().ToLower()))
                {
                    Logger.LogDebug($"This one is very new !");
                    s = new FSprite("Circle4") { color = echoTokenColor };
                    addIt = true;
                }
                else
                {
                    Logger.LogDebug("Disposing of this ghost.");
                }
                if (addIt)
                {
                    if (!singleCollectiblesTracker.sprites.ContainsKey(pair.Key.ToString().ToLower())) {
                        Logger.LogWarning("Would have added, but could not find ghost ID in tracker:"+pair.Key.ToString().ToLower()+", discarding it");
                        continue;
                    }
                    if (singleCollectiblesTracker.regionIcons.Length == 0) {
                        Logger.LogError("No region icons to add collectible dot to.");
                        continue;
                    }
                    singleCollectiblesTracker.sprites[pair.Key.ToString().ToLower()].Add(s);
                    singleCollectiblesTracker.regionIcons.First().container.AddChild(s);
                }
            }
            if (showNeverMetGhosts)
            {
                var regions = SlugcatStats.SlugcatStoryRegions(saveSlot);
                Logger.LogInfo($"For this slugcat, regions : " + string.Join(", ", regions));
                Logger.LogInfo($"Already processed : " + string.Join(", ", regionsAlreadyProcessed));
                Logger.LogInfo($"Removed {regions.RemoveAll(regionsAlreadyProcessed.Contains)} from regions");
                Logger.LogInfo($"After removing already processed : " + string.Join(", ", regions));
                foreach (string region in regions)
                {
                    if (GetGhostID(region) != GhostID.NoGhost)
                    {
                        Logger.LogInfo($"Region {region} has an undiscovered ghost");
                        if (!singleCollectiblesTracker.sprites.Keys.Contains(region.ToLower()))
                        {
                            Logger.LogInfo("This region has no sprites. Not adding.");
                            continue;
                        }
                        var s = new FSprite("Circle4") { color = echoTokenColor };
                        singleCollectiblesTracker.sprites[region.ToLower()].Add(s);
                        singleCollectiblesTracker.regionIcons.First().container.AddChild(s);
                    }
                    else
                    {
                        Logger.LogInfo($"Region {region} has no ghosts");
                    }
                }

            }

        }
        else
            Logger.LogInfo("No currensSaveState, so not adding echoes !");

    }



    private void setShortcuts(On.Menu.SlugcatSelectMenu.orig_Update orig, Menu.SlugcatSelectMenu self)
    {
        orig(self);
        if (!ModManager.MMF) return;

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
    }


}
