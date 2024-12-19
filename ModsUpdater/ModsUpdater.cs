using BepInEx;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static ModsUpdater.PluginInfo;
using System.Threading.Tasks;
using UnityEngine;
using Menu.Remix;
using static Menu.Remix.MenuModList;
using MonoMod.Utils;
using BepInEx.Logging;
using System.Security;
using System.Security.Permissions;

using static ModsUpdater.Graphics;
using HarmonyLib;
using static ModsUpdater.Utils;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ModsUpdater;
#nullable enable

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class ModsUpdater : BaseUnityPlugin
{

    // The path of THIS mod
    public static string THISMODPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName;
    // The general mods path
    public static string MODSPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.Parent.FullName;

    // static readonly List<ModHolder> ModObjects = new();
    static readonly Dictionary<string, ModHolder> ModObjects = new();

    private List<ServerMod> ServerMods => ModHolder.ServerMods;






    private static ListButton? infoListButton; // the info button on top of the mods list


    static bool done = false; //initialisation

    static bool currentlyLoadingRainDB = false;
    private static bool doneReading = false;
    private Dictionary<string, (DateTime, ServerMod)> UpdateCheckLog; // cached foreign updates info
    private string currentlyPreviewedModId;
    // readonly ModOptions modOptions;



    public ModsUpdater()
    {

        //modOptions = new ModOptions(this, Logger);
    }





    private void OnEnable()
    {
        if (done) return;
        base.Logger.LogInfo("Hooking setup methods...");
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        On.ModManager.WrapModInitHooks += LoadLocalMods; // discovers existing local mods (by native Mods manager)

        On.Menu.MainMenu.ctor += ActuallyhookVersionLabelChange; // hooks further hooks & loads servermods. prevents cctors not being inited and stuff

        done = true;
    }

    private async void ActuallyhookVersionLabelChange(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);
        On.Menu.MainMenu.ctor -= ActuallyhookVersionLabelChange; // unhook myself. This only needs to be ran once
        On.Menu.Remix.MenuModList.ModButton.GrafUpdate += verLabelColorChanger;
        On.Menu.Remix.MenuModList.ModButton.ctor += ModButtonStorer; // catches ModButtons in the list to get the version label, and modifies the labelver in accordance to statuses
        // On.Menu.Remix.MenuModList.Update += ButtonShower;
        On.Menu.Remix.InternalOI_Stats.Initialize += StatsMenuSetup; // adds info about number of updatable mods & button on REMIX if
        On.Menu.Remix.InternalOI_Stats._RefreshStats += StatsMenuLabelsUpdates; // upates that info
        On.Menu.Remix.MenuModList.ListButton.ctor += InfoButtonGetter; // lets us make the update button selectable via keyboard
        On.Menu.Remix.InternalOI_Stats._PreviewMod += ModPreviewHooker;
        On.Menu.ModdingMenu._SwitchToMainMenu += DestroyGraphics; 

        initForeignUpdateCheckLog(); // gather cached data from local file for remote sources
        await Task.Run(ParseAndQueryWorkshopModlist); // get workshop ServerMods
        if (false) await ParseAndQueryForeignModList(); // get other ServerMods
        matchLocalAndServerMods();

    }

    private void DestroyGraphics(On.Menu.ModdingMenu.orig__SwitchToMainMenu orig, Menu.ModdingMenu self)
    {
        orig(self);
        Logger.LogDebug("DESTRUCTIOBNNNN");
        foreach (var mo in ModObjects.Values) {
            if (mo.ModButton is not null) {
                mo.ModButton = null;
            }
        }

        Graphics.BtnUpdateAll = null;
        Graphics.LblModUpdaterStatus = null;
        Graphics.LblOrphanedMods = null;
        Graphics.BtnUpdateAll = null;
        Graphics.LblPreviewUpdateButton = null;
        Graphics.LblPreviewUpdateStatus = null;
        Graphics.LblUpdatableMods = null;
        Graphics.LblUpToDateMods = null;
        Graphics.LocalInternalOiStats = null;


        ModHolder.labelVers.Clear();
    }

    /// <summary>
    /// puts ServerMod inside ModObject with matching modId
    /// Mods managed by Steam are exempt of tha treatment
    /// </summary>
    private void matchLocalAndServerMods()
    {
        if (!doneReading)
        {
            Logger.LogError("Trying to match localAndServerMods, but we're not done reading");
            return;
        }
        foreach (var kvp in ModObjects)
        {
            var mo = kvp.Value;
            if (mo.Status == ModStatusTypes.Managed_By_Steam) continue;

            var currentServerMod = ServerMods.FirstOrDefault((mod) => mod.ID == mo.ModID);

            if (currentServerMod != null)
            {
                mo.ServerMod = currentServerMod;
                base.Logger.LogInfo($"{mo.Mod.id}: loc {mo.Mod.version} vs rem {mo.ServerMod.Version} -> {Utils.VersionManager.CompareVersions(mo.Mod.version, mo.ServerMod.Version)}");
                mo.Status = Utils.VersionManager.CompareVersions(mo.Mod.version, mo.ServerMod.Version) switch
                {
                    Utils.StatusCode.AheadOfRemote => ModStatusTypes.Dev,
                    Utils.StatusCode.UpdateAvailable => ModStatusTypes.Updatable,
                    Utils.StatusCode.LocalFileUpToDate => ModStatusTypes.Latest,
                    _ => ModStatusTypes.Unknown,
                };
            }
            else
            {
                mo.Status = ModStatusTypes.Orphan;
            }
            mo.UpdateColor();
        }
    }



    /// <summary>
    /// When GrafUpdate-ing ModButtons, check wether or not the current ModButton is registered inside ModObjects
    /// If so, change its color to the target color
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="timeStacker"></param>
    private void verLabelColorChanger(On.Menu.Remix.MenuModList.ModButton.orig_GrafUpdate orig, ModButton self, float timeStacker)
    {
        orig(self, timeStacker);
        if (self._labelVer is null || self._IsImageButton || !ModObjects.ContainsKey(self.ModID)) return;
        if (ModObjects[self.ModID].VersionLabel == self._labelVer)
        {
            self._labelVer.color = ModObjects[self.ModID].Color;
        }
    }


    /*
    see line 425222
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        _rect.GrafUpdate(timeStacker);
        _rectH.GrafUpdate(timeStacker);
        _rect.addSize = new Vector2(6f, 6f) * base.bumpBehav.AddSize;
        if (!_IsImageButton)
        {
            _label.color = base.bumpBehav.GetColor(colorEdge);
        }
        */
    


    /// <summary>
    /// Called when switching mod previews
    /// updates current preview's labels (version, source, )
    /// Allo
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="button"></param>
    private void ModPreviewHooker(On.Menu.Remix.InternalOI_Stats.orig__PreviewMod orig, InternalOI_Stats self, ModButton button)
    {
        orig(self, button);
        OptionInterface oi = button.itf;
        if (oi is null) return;
        if (!ModObjects.ContainsKey(oi.mod.id)) return;
        ModHolder currentObject = ModObjects[oi.mod.id];
        currentlyPreviewedModId = currentObject.ModID;

        OpLabel? lblVersion = self.lblVersion;
        if (currentObject.Status == ModStatusTypes.Updatable && lblVersion is not null) //TODO: add single update button here
            lblVersion.text += " -> " + currentObject.ServerMod!.Version;

        Graphics.LblPreviewUpdateStatus!.text = "Status: " + currentObject.Status.ToString().Replace("_"," ");
        if (currentObject.Status == ModStatusTypes.Updatable || currentObject.Status == ModStatusTypes.Latest)
        {
            Graphics.LblPreviewUpdateStatus!.text += " (Souce: " + currentObject.ServerMod!.Source + ")";
        }
        else if (currentObject.Status == ModStatusTypes.Updated)
        {
            Graphics.LblPreviewUpdateStatus!.text = "Restart the game to apply update";
        }

        if (currentObject.Status == ModStatusTypes.Updatable)
        {
            LblPreviewUpdateButton!.Show();
        }
        else LblPreviewUpdateButton!.Hide();





    }


    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
    }


    /// <summary>
    /// create a ModObject for each local Mod
    /// </summary>
    /// <param name="orig"></param>
    internal void LoadLocalMods(On.ModManager.orig_WrapModInitHooks orig)
    {
        orig();
        if (ModObjects.Count == 0)
        {
            base.Logger.LogInfo("Collecting mods after ModManager WrapInit");
            foreach (ModManager.Mod mod in ModManager.InstalledMods)
            {
                var mo = new ModHolder(mod);
                if (mod.workshopMod)
                {
                    mo.Status = ModStatusTypes.Managed_By_Steam;
                    Logger.LogDebug(mod.basePath);
                }
                Logger.LogDebug($"Adding {mod.id}");
                ModObjects.Add(mod.id, mo);
            }
            base.Logger.LogInfo("Discovered " + ModObjects.Count + " mods");
        }
        else
        {
            base.Logger.LogWarning("repeat wrapinit");
        }
    }


    /// <summary>
    /// Create Labels and Buttons, both for the Remix Stats interface and the floating update btns on Mod Previews
    /// Also add click listeners
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void StatsMenuSetup(On.Menu.Remix.InternalOI_Stats.orig_Initialize orig, InternalOI_Stats self)
    {

        orig(self);
        try
        {

            float num = self._GetDPTexture();
            LblUpdatableMods = new OpLabel(new Vector2(20f, 260f - num), new Vector2(560f, 30f), $"I sure hope this does not meet the user's eye", FLabelAlignment.Left);
            LblOrphanedMods = new OpLabel(new Vector2(20f, 240f - num), new Vector2(560f, 30f), "UwU", FLabelAlignment.Left);
            LblUpToDateMods = new OpLabel(new Vector2(20f, 220f - num), new Vector2(560f, 30f), "AwA", FLabelAlignment.Left);
            LblModUpdaterStatus = new OpLabel(new Vector2(20f, 160f - num), new Vector2(560f, 30f), ModUpdaterStatus, FLabelAlignment.Center)
            {
                color = Color.cyan
            };

            BtnUpdateAll = new OpSimpleImageButton(new Vector2(LblUpdatableMods.label.textRect.xMax + 28f, 260f - num), new Vector2(30f, 30f), "keyShiftB")
            {
                description = "Update all mods"
            };
            BtnUpdateAll.OnClick += async (UIfocusable trigger) =>
            {
                base.Logger.LogDebug("trying to update mods");
                BtnUpdateAll!.greyedOut = true;
                int successCounter = 0;
                int failureCounter = 0;
                foreach (var mod in ModObjects.Values.Where((el) => el.Status == ModStatusTypes.Updatable))
                {
                    trigger.description = "Updating " + mod.Mod.name;
                    Utils.StatusCode res = await mod.triggerUpdate();
                    if (res == Utils.StatusCode.Success) successCounter++;
                    else
                    {
                        failureCounter++;
                        Logger.LogError("Err : " + Utils.GetErrorMessage(res));
                    }
                }
                trigger.description = $"success:{successCounter}, failures: {failureCounter}";
                LocalInternalOiStats._RefreshStats();
                // refreshMethodInfo.Invoke(, new object[] { });

                if (successCounter != 0) ModUpdaterStatus = "Please restart your game to apply updates";
            };



            self.Tabs[0].AddItems(LblUpdatableMods, LblOrphanedMods, LblUpToDateMods, LblModUpdaterStatus, BtnUpdateAll);
            Graphics.LblPreviewUpdateStatus = new OpLabel(new Vector2(120f, 485f), new Vector2(560f, 30f), "hi");
            LblPreviewUpdateButton = new OpSimpleImageButton(new Vector2(258f, 485f), new Vector2(30f, 30f), "keyShiftB")
            {
                description = "Update X mod, probably"
            };
            LblPreviewUpdateButton.Hide();
            LblPreviewUpdateButton.OnClick += async (UIfocusable targetBtn) =>
            {
                targetBtn.greyedOut = true;
                targetBtn.description = "Updating...";
                
                if (ModObjects.ContainsKey(currentlyPreviewedModId))
                {
                    ModHolder mod = ModObjects[currentlyPreviewedModId];
                    Utils.StatusCode res = await mod.triggerUpdate();
                    if (res == Utils.StatusCode.Success)
                    {
                        mod.Status = ModStatusTypes.Updated;
                        targetBtn.greyedOut = false;
                        targetBtn.description = "OK !";
                        mod.updateLabel();
                        Graphics.LblPreviewUpdateStatus.text = Graphics.LblPreviewUpdateStatus.text.Split('>').Length == 2 ? Graphics.LblPreviewUpdateStatus.text.Split('>')[1].Trim() : Graphics.LblPreviewUpdateStatus.text;

                    }
                    else
                    {
                        targetBtn.description = "Could not download the update :(\n" ;
                        base.Logger.LogError(Utils.GetErrorMessage(res));
                    }
                }
                else
                {
                    targetBtn.description = "Could not find target mod to update. Where are you ??";
                }

            };
            self.Tabs[1].AddItems(Graphics.LblPreviewUpdateStatus, LblPreviewUpdateButton);



            base.Logger.LogDebug("successfully added items to stats menu");
        }
        catch (Exception e) { e.LogDetailed(); }
    }

    /// <summary>
    /// update labels & buttons on the Remix Stats screen
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void StatsMenuLabelsUpdates(On.Menu.Remix.InternalOI_Stats.orig__RefreshStats orig, InternalOI_Stats self)
    {
        orig(self);
        LocalInternalOiStats = self;
        bool anuoneUpdatable = false;
        string updateDetails = "(";
        foreach (var mo in ModObjects.Values.Where((mod) => mod.Status == ModStatusTypes.Updatable))
        {
            anuoneUpdatable = true;
            updateDetails += mo.Mod.name + ", ";
        }
        if (!anuoneUpdatable)
        {
            BtnUpdateAll!.greyedOut = true;

        }
        else
        {
            BtnUpdateAll!.greyedOut = false;
            updateDetails = updateDetails.Substring(0, updateDetails.Length - 2) + ")";
            BtnUpdateAll!.description += updateDetails;
        }
        
        LblUpdatableMods!.text = $"Updates found for {ModObjects.Values.Count((mod) => mod.Status == ModStatusTypes.Updatable)} mods";
        LblOrphanedMods!.text = $"Orphan mods: {ModObjects.Values.Count((mod) => mod.Status == ModStatusTypes.Orphan)}";
        LblUpToDateMods!.text = $"Up-to-date (or better) mods: {ModObjects.Values.Count((mod) => mod.Status == ModStatusTypes.Latest)}";

    }


    /// <summary>
    /// tries to find for each existing mod, a corrseponding serverMod
    /// updates the ModObject's status
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="tab"></param>
    internal void UpdateLabels(On.Menu.Remix.MenuModList.orig_ctor orig, MenuModList self, ConfigMenuTab tab)
    {
        orig(self, tab);

        #region versionLabelsComp
        base.Logger.LogInfo("Assigning data to versionlabels.");
        foreach (var mo in ModObjects.Values)
        {
            mo.updateLabel();
        }
        #endregion versionLabelsComp


    }


    /// <summary>
    /// Make the Remix menu info button select our update button on click, cuz idk how else to make it selectable
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="list"></param>
    /// <param name="role"></param>
    private void InfoButtonGetter(On.Menu.Remix.MenuModList.ListButton.orig_ctor orig, ListButton self, MenuModList list, ListButton.Role role)
    {
        orig(self, list, role);
        if (role == ListButton.Role.Stat)
        {
            infoListButton = self;
            //UIfocusable.MutualHorizontalFocusableBind(infoListButton!, BtnUpdateAll!);
            infoListButton.OnClick += (_) =>
            {
                if (!BtnUpdateAll!.greyedOut) Menu.Remix.ConfigConnector.FocusNewElement(BtnUpdateAll!);
            };
        }
    }



    // also provide update info (orphaned, updatable, updated) inside mod preview window
    // add update button to config & stats view

    #region ModListUpdateLabel

    /// <summary>
    /// On a ModButton creation, save the current MB into ModObjects to the current matchin Mod (based on ID)
    /// Also updates the label of the button w/ text & color using the dedicated fctn
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="list"></param>
    /// <param name="index"></param>
    internal void ModButtonStorer(On.Menu.Remix.MenuModList.ModButton.orig_ctor orig, Menu.Remix.MenuModList.ModButton self, Menu.Remix.MenuModList list, int index)
    {

        orig(self, list, index);


        if (ModObjects.ContainsKey(self.ModID))
        {
            var modObject = ModObjects[self.ModID];
            modObject.ModButton = self;
            base.Logger.LogWarning("added mod button info for " + self.ModID);
            modObject.updateLabel();

        }
        else base.Logger.LogWarning("could not find mod for " + self.ModID);

        return;

    }


    #endregion ModListUpdateLabel


    #region ServerModsGenerators
    /// <summary>
    /// this gets data based on the update_url value inside modinfo.json
    /// "update_url": "https://github.com/autodistries/RainWorldMods/raw/refs/heads/main/SleepySlugcat/SleepySlugcat.zip"
    /// the check is only done if the mod is not already associated to a Servermod and if update was last checked more than a day ago
    /// </summary>
    /// <returns></returns>
    private async Task ParseAndQueryForeignModList()
    {
        if (Utils.FileManager.offlineMode) return;
        int totalModsCount = ModObjects.Count;
        int searchedForeModsCount = 0;
        int offshortUrlModsCount = 0;
        int updatableOffshoreModsCount = 0;
        foreach (var mo in ModObjects.Values)
        {
            // we'll change the logic later; we'll need to manage which one (workshop/custom) goes first
            if (mo.ServerMod is null && !mo.Mod.hideVersion && WasLastUpdateLongAgo(mo.Mod.id))
            {
                searchedForeModsCount++;
                mo.RemoteModSourceInfo = new RemoteModSourceInfo(mo.Mod);
                if (mo.RemoteModSourceInfo.status == RemoteModSourceInfo.Status.NoUpdateLink)
                {
                    mo.Status = ModStatusTypes.Latest;
                    continue; //skipping because not updatable this way
                }
                offshortUrlModsCount++;

                bool updateFound = mo.RemoteModSourceInfo.FillInServerInfo();
                UpdateCheckLog.Add(mo.Mod.id, (DateTime.Now, mo.RemoteModSourceInfo.ServerMod));
                if (!updateFound) continue;
                else mo.ServerMod = mo.RemoteModSourceInfo.ServerMod;
                updatableOffshoreModsCount++;

            }
        }
        writeUpdateCheckLog();
        base.Logger.LogInfo($"From {totalModsCount} mods, {searchedForeModsCount} searched for update_url, {offshortUrlModsCount} hits, {updatableOffshoreModsCount} updatable");
    }


    /// <summary>
    /// this gets from the workshop (github raindb.js)
    /// after DownloadFileIfNewerAsync, read all lines and manually parse through them
    /// Puts all of its data inside ServerMods[]
    /// </summary>
    /// <returns></returns>
    private async Task ParseAndQueryWorkshopModlist()
    {
        //should be able to concat results with github-provided results
        if (currentlyLoadingRainDB) return;
        currentlyLoadingRainDB = true;

        float startTime = Time.time;
        string url = "https://raw.githubusercontent.com/AndrewFM/RainDB/master/raindb.js";
        string targetPath = Path.Combine(ModsUpdater.THISMODPATH, "raindb.js");

        Utils.StatusCode result = await Utils.FileManager.IsRemoteFileNewer(url);
        Logger.LogDebug(result);

        if (result == StatusCode.LocalFileNotFound || result == StatusCode.UpdateAvailable) {
            await FileManager.DownloadFileAsync(url, targetPath);
        } else if (result == StatusCode.LocalFileUpToDate ) {
            Logger.LogDebug("Not updating raindb.js up to date : " + Utils.GetErrorMessage(result));

        }
        else {
            Logger.LogDebug("Not updating raindb.js cuz error : " + Utils.GetErrorMessage(result));

        }
       
           
     

        string[] lines = File.ReadAllLines(Path.Combine(ModsUpdater.THISMODPATH, "raindb.js"));

        string currentWorkingID = "";
        string currentWorkingVersion = "";
        string currentWorkingLink = "";

        string detectionPattern = @"""(?<key>id|version|url)""\s*:\s*""(?<value>[^""]*)""\s*,?";

        bool skippingThisMod = false;
        foreach (string line in lines.Skip(2))
        {
            if (line == "")
            {
                if (skippingThisMod) skippingThisMod = false;
                continue;
            }
            if (skippingThisMod) continue;
            else if (line == "Mods.push({")
            {
                currentWorkingID = "undefined";
                currentWorkingLink = "undefined";
                currentWorkingVersion = "undefined";
            }
            else if (line == "") continue;
            else if (line == "});")
            {
                ServerMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink, ServerMod.ServerModType.Workshop));
            }
            else
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, detectionPattern);
                if (match.Success)
                {
                    string key = match.Groups["key"].Value;
                    string value = match.Groups["value"].Value;
                    //  lls.LogDebug($"matched {key} {value}");
                    switch (key)
                    {
                        case "id":
                            {
                                if (!ModObjects.ContainsKey(value)) {
                                    skippingThisMod = true;
                                    continue;
                                }
                                currentWorkingID = value; break;

                            }
                        case "version": currentWorkingVersion = value; break;
                        case "url": currentWorkingLink = value; break;
                    }
                }
            }
        }
        float endTime = Time.time;
        float elapsedTime = endTime - startTime;

        base.Logger.LogInfo($"loaded {ServerMods.Count} remote mods in {elapsedTime}s");
        currentlyLoadingRainDB = false;
        doneReading = true;

    }


    #endregion ServerModsGenerators


    #region CachedForeignServerMods
    /// <summary>
    /// checks currently off-raindb cached data latest update
    /// </summary>
    /// <param name="modid"></param>
    /// <returns></returns>
    private bool WasLastUpdateLongAgo(string modid)
    {
        if (UpdateCheckLog == null)
        {
            initForeignUpdateCheckLog();
        }
        if (UpdateCheckLog!.ContainsKey(modid))
        {
            if (UpdateCheckLog![modid].Item1 - DateTime.Now >= TimeSpan.FromDays(1))
            {
                return true;
            }
            else return false;
        }
        else return true;
    }



    /// <summary>
    /// Gets the status of previous ramote foreign mod checks
    /// </summary>
    private void initForeignUpdateCheckLog()
    {
        UpdateCheckLog = new();

        string updateFilePath = Path.Combine(THISMODPATH, "updatechecktimes.log");
        if (!File.Exists(updateFilePath))
        {
            File.Create(updateFilePath);
            return;
        }
        else
        {
            string[] textContent = File.ReadAllLines(updateFilePath);
            foreach (string line in textContent)
            {
                // line should be formatted as such:
                // mod.id@updateDateTime@serverVersion@serverUrl
                string[] parts = line.Split('@');
                if (parts.Length != 4)
                {
                    base.Logger.LogError("update time logs file is malformed at line " + line); // the damaged file will be overwritten later, we'll skip this line
                    continue;
                }
                string modid = parts[0].Trim();
                string modversion = parts[2].Trim();
                string modlink = parts[3].Trim();
                DateTime latestupdate = DateTime.ParseExact(parts[1].Trim(), "yyyyMMddHHss", System.Globalization.CultureInfo.InvariantCulture);
                ServerMod serverMod = new(modid, modversion, modlink) { Source = ServerMod.ServerModType.Url };
                ServerMods.Add(serverMod);
                UpdateCheckLog.Add(modid, (latestupdate, serverMod));
            }
        }
    }

    private void writeUpdateCheckLog()
    {
        string updateFilePath = Path.Combine(THISMODPATH, "updatechecktimes.log");
        if (!File.Exists(updateFilePath))
        {
            initForeignUpdateCheckLog();
        }
        List<string> lines = new();
        foreach (var linec in UpdateCheckLog)
        {
            lines.Add($"{linec.Key}@{linec.Value.Item1.ToString("yyyyMMddHHmm")}@{linec.Value.Item2.Version}@{linec.Value.Item2.Link}");
        }
        File.WriteAllLines(updateFilePath, lines);
    }
    #endregion CachedForeignServerMods


    public static string GetLogFor(object target)
    {
        var properties =
            from property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            select new
            {
                property.Name,
                Value = property.GetValue(target, null)
            };

        var builder = new System.Text.StringBuilder();

        foreach (var property in properties)
        {
            builder
                .Append(property.Name)
                .Append(" = ")
                .Append(property.Value)
                .AppendLine();
        }

        return builder.ToString();
    }



}


public class ServerMod
{
    public string Link { get; }

    public string Version { get; }
    public string ID { get; }
    public ServerModType Source { get => source; set => source = value; }

    private ServerModType source;

    public ServerMod(string id, string version, string link, ServerModType source = ServerMod.ServerModType.Unknown)
    {
        this.ID = id;
        this.Version = version;
        this.Link = link;
        this.Source = source;
    }

    public override string ToString()
    {
        return this.ID + ":" + this.Version + ":" + Link;
    }


    public enum ServerModType
    {
        Workshop,
        Url,
        Unknown
    }
}



