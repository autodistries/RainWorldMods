using BepInEx;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static ModsUpdater.PluginInfo;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using UnityEngine;
using Menu.Remix;
using Menu;
using static Menu.Remix.MenuModList;
using MonoMod.Utils;
using BepInEx.Logging;
using System.Security;
using System.Security.Permissions;
using System.Security.Cryptography.X509Certificates;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ModsUpdater;
#nullable enable

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class ModsUpdater : BaseUnityPlugin
{

    public static string THISMODPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName;
    public static string MODSPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.Parent.FullName;

    static readonly List<ModHolder> ModObjects = new();
    static readonly List<ServerMod> serverMods = new();

     OpSimpleImageButton? BtnUpdateAll { get => btnUpdateAll; set => btnUpdateAll = value; }

    private static InternalOI_Stats? localInternalOiStats; // the REMIX STATS page
    private static OpLabel? lblUpdatableMods; // On the REMIX STATS page, the updatable label
    private static OpLabel? lblOrphanedMods;
    private static OpLabel? lblUpToDateMods;
    private static OpSimpleImageButton? btnUpdateAll; // the update all btn
    private static OpLabel? lblModUpdaterStatus; // the info text
    private static OpLabel? lblPreviewUpdateStatus;
    private OpSimpleImageButton lblPreviewUpdateButton;
    private static string modUpdaterStatus = "Welcome !"; // Because text needs to be kept across


    private static ListButton? infoListButton; // the info button on top of the mods list


    static bool done = false; //initialisation

    static bool currentlyReading = false;
    private static bool doneReading = false;
    private Dictionary<string, (DateTime, ServerMod)> UpdateCheckLog; // cached github info. Updates every day
    private string currentlyPreviewedModId;
    readonly ModOptions modOptions;

    public static ManualLogSource logger;


    public ModsUpdater()
    {

        //modOptions = new ModOptions(this, Logger);
    }



    private void OnEnable()
    {
        if (done) return;
        Logger.LogInfo("Hooking setup methods...");
        Logger.LogInfo("todo: add a buton in own remix menu to re-read raindb.js");
        logger = Logger;
        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        On.ModManager.WrapModInitHooks += getLocalMods; // discovers existing local mods (by native Mods manager)
        On.Menu.MainMenu.ctor += ActuallyhookVersionLabelChange; // hooks further hooks & loads servermods

        done = true;
    }

    private async void ActuallyhookVersionLabelChange(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);
        On.Menu.MainMenu.ctor -= ActuallyhookVersionLabelChange; // unhook myself. This only needs to be ran once
        On.Menu.Remix.MenuModList.ModButton.ctor += ModButtonChanger; // catches ModButtons in the list to get the version label
        On.Menu.Remix.MenuModList.ctor += TriggerVersionComp; // after list is built (all ModButtons done), triggers version comparator
        // On.Menu.Remix.MenuModList.Update += ButtonShower;
        On.Menu.Remix.InternalOI_Stats.Initialize += StatsMenuSetup; // adds info about number of updatable mods & button on REMIX if
        On.Menu.Remix.InternalOI_Stats._RefreshStats += StatsMenuLabelUpdate; // upates that info
        On.Menu.Remix.MenuModList.ListButton.ctor += InfoButtonGetter; // lets us make the update button selectable via keyboard
        On.Menu.Remix.InternalOI_Stats._PreviewMod += ModPreviewHooker; // puts stuff inside mod preview
        On.Menu.Remix.MenuModList.ModButton.GrafUpdate += verLabelColorChanger;

        initUpdateCheckLog(); // gather cached data from local file for remote sources
        await ParseAndQueryWorkshopModlist(); // get workshop ServerMods
        await ParseAndQueryForeignModList(); // get other ServerMods
    }

    private void verLabelColorChanger(On.Menu.Remix.MenuModList.ModButton.orig_GrafUpdate orig, ModButton self, float timeStacker)
    {
        orig(self, timeStacker);
        if (self._labelVer is null)return;
        var tm=ModObjects.FirstOrDefault((mh) => mh.VersionLabel == self._labelVer);
        if (!self._IsImageButton && tm is not default(ModHolder)) {
            self._labelVer.color = tm.Color;
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

    private void ModPreviewHooker(On.Menu.Remix.InternalOI_Stats.orig__PreviewMod orig, InternalOI_Stats self, ModButton button)
    {
        orig(self, button);
        OptionInterface oi = button.itf;
        if (oi is null) return;
        ModHolder currentObject = ModObjects.FirstOrDefault((mo) => mo.ModID == oi.mod.id);
        currentlyPreviewedModId = currentObject.ModID;

        OpLabel? lblVersion = self.lblVersion;
        if (currentObject.Status == ModHolder.ModStatusTypes.Updatable&& lblVersion is not null) //TODO: add single update button here
        lblVersion.text+=" -> "+currentObject.ServerMod!.Version;
        if (currentObject is null)  return;
        
            lblPreviewUpdateStatus!.text = "Status: "+currentObject.Status.ToString();
            if (currentObject.Status == ModHolder.ModStatusTypes.Updatable || currentObject.Status == ModHolder.ModStatusTypes.Latest) {
                lblPreviewUpdateStatus!.text+= " (Souce: "+currentObject.ServerMod!.Source+")";
            } else if (currentObject.Status == ModHolder.ModStatusTypes.Updated) {
                lblPreviewUpdateStatus!.text = "Restart the game to apply update";
            }

            if (currentObject.Status == ModHolder.ModStatusTypes.Updatable) {
                lblPreviewUpdateButton.Show();
            } else lblPreviewUpdateButton.Hide();





    }


    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        // if (MachineConnector.SetRegisteredOI(PLUGIN_GUID, modOptions))
        // {
        //     lls.LogInfo("Registered Mod Interface");
        // }
        // else
        // {
        //     lls.LogError("Could not register Mod Interface");
        // }
    }


    /// <summary>
    /// create a ModObject for each local Mod
    /// </summary>
    /// <param name="orig"></param>
    internal void getLocalMods(On.ModManager.orig_WrapModInitHooks orig)
    {
        orig();
        if (ModObjects.Count == 0)
        {
            Logger.LogInfo("Collecting mods after ModManager WrapInit");
            foreach (ModManager.Mod mod in ModManager.InstalledMods)
            {
                ModObjects.Add(new ModHolder(mod));
            }
            Logger.LogInfo("Discovered " + ModObjects.Count + " mods");
        }
        else
        {
            Logger.LogWarning("repeat wrapinit");
        }
    }


    /// <summary>
    /// add labels and buttons to the remix stats interface
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void StatsMenuSetup(On.Menu.Remix.InternalOI_Stats.orig_Initialize orig, InternalOI_Stats self)
    {

        orig(self);
        try
        {

            float num = self._GetDPTexture();
            Logger.LogDebug(num);
            lblUpdatableMods = new OpLabel(new Vector2(20f, 260f - num), new Vector2(560f, 30f), $"Updates found for {ModObjects.Count((mod) => mod.Status == ModHolder.ModStatusTypes.Updatable)} mods", FLabelAlignment.Left);
            lblOrphanedMods = new OpLabel(new Vector2(20f, 240f - num), new Vector2(560f, 30f), "UwU", FLabelAlignment.Left);
            lblUpToDateMods = new OpLabel(new Vector2(20f, 220f - num), new Vector2(560f, 30f), "AwA", FLabelAlignment.Left);
            lblModUpdaterStatus = new OpLabel(new Vector2(20f, 160f - num), new Vector2(560f, 30f), modUpdaterStatus, FLabelAlignment.Center) {
                color = Color.cyan
            };

            BtnUpdateAll = new OpSimpleImageButton(new Vector2(lblUpdatableMods.label.textRect.xMax + 28f, 260f - num), new Vector2(30f, 30f), "keyShiftB")
            {
                description = "Update all mods"
            };
            BtnUpdateAll.OnClick += async (UIfocusable trigger) =>
            {
                Logger.LogDebug("trying to update mods");
                BtnUpdateAll!.greyedOut = true;
                int successCounter = 0;
                int failureCounter = 0;
                foreach (var mod in ModObjects.FindAll((el) => el.Status == ModHolder.ModStatusTypes.Updatable))
                {
                    trigger.description = "Updating " + mod.Mod.name;
                    int res = await mod.triggerUpdate();
                    if (res == 0) successCounter++;
                    else failureCounter++;
                }
                trigger.description = $"success:{successCounter}, failures: {failureCounter}";
                localInternalOiStats._RefreshStats();
                // refreshMethodInfo.Invoke(, new object[] { });
                
                if (successCounter != 0) ModsUpdater.SetInfoLabelText("Please restart your game to apply updates");
            };



            self.Tabs[0].AddItems(lblUpdatableMods, lblOrphanedMods, lblUpToDateMods, lblModUpdaterStatus, BtnUpdateAll);

            lblPreviewUpdateStatus = new OpLabel(new Vector2(120f, 485f), new Vector2(560f, 30f), "hi");
            lblPreviewUpdateButton  = new OpSimpleImageButton(new Vector2(258f, 485f), new Vector2(30f, 30f), "keyShiftB");
            lblPreviewUpdateButton.Hide();
            lblPreviewUpdateButton.OnClick += async (UIfocusable targetBtn) => {
                targetBtn.greyedOut = true;
                targetBtn.description = "Updating...";
                ModHolder mod = ModObjects.FirstOrDefault((mo) => mo.ModID == currentlyPreviewedModId);
                if (mod is not null) {
                    int res = await mod.triggerUpdate();
                    if (res == 0) {
                        mod.Status = ModHolder.ModStatusTypes.Updated;
                    targetBtn.greyedOut = false;
                    targetBtn.description = "OK !";
                    mod.updateLabel();
                    lblPreviewUpdateStatus.text = lblPreviewUpdateStatus.text.Split('>').Length == 2 ? lblPreviewUpdateStatus.text.Split('>')[1].Trim() : lblPreviewUpdateStatus.text;
                    
                    } 
                    else{ targetBtn.description = "Could not download the update";
                    
                    Logger.LogError(Utils.StatusInfo.get(res));}
                } else {
                    targetBtn.description = "Could not find target mod";
                }

            };
            self.Tabs[1].AddItems(lblPreviewUpdateStatus, lblPreviewUpdateButton);


            //Tabs[1].AddItems()

            Logger.LogDebug("successfully added items to stats menu");
        }
        catch (Exception e) { e.LogDetailed(); }
    }
    
    /// <summary>
    /// update labels & buttons
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    private void StatsMenuLabelUpdate(On.Menu.Remix.InternalOI_Stats.orig__RefreshStats orig, InternalOI_Stats self)
    {
        orig(self);
        localInternalOiStats = self;
        bool anuoneUpdatable = false;
        string updateDetails = "(";
        foreach (var mo in ModObjects.FindAll((mod) => mod.Status == ModHolder.ModStatusTypes.Updatable)) {
            anuoneUpdatable = true;
            updateDetails+=mo.Mod.name+", ";
        }
        if (!anuoneUpdatable)
        {
            BtnUpdateAll!.greyedOut = true;

        } else {
            BtnUpdateAll!.greyedOut = false;
            updateDetails= updateDetails.Substring(0, updateDetails.Length -2) + ")";
            BtnUpdateAll!.description +=updateDetails;
        }
        lblUpdatableMods!.text = $"Updates found for {ModObjects.Count((mod) => mod.Status == ModHolder.ModStatusTypes.Updatable)} mods";
        lblOrphanedMods!.text = $"Orphan mods: {ModObjects.Count((mod) => mod.Status == ModHolder.ModStatusTypes.Orphan)}";
        lblUpToDateMods!.text = $"Up-to-date (or better) mods: {ModObjects.Count((mod) => mod.Status == ModHolder.ModStatusTypes.Latest)}";

    }


    /// <summary>
    /// tries to find for each existing mod, a corrseponding serverMod
    /// updates the ModObject's status
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="tab"></param>
    internal void TriggerVersionComp(On.Menu.Remix.MenuModList.orig_ctor orig, MenuModList self, ConfigMenuTab tab)
    {
        orig(self, tab);
        if (!doneReading) return;

        #region versionLabelsComp
        Logger.LogInfo("finished discovering versionlabels. comparing strings.");
        foreach (var mo in ModObjects)
        {
            if (mo.VersionLabel is not null)
            {

               // Logger.LogDebug($"prepare comparing version on {mo.ModID}");

                var currentServerMod = (mo.ServerMod is not null) ? mo.ServerMod : serverMods.FirstOrDefault((mod) => mod.ID == mo.ModID);


                if (currentServerMod != null)
                {
                    mo.ServerMod = currentServerMod;
                    Logger.LogInfo($"{mo.Mod.id}: loc {mo.Mod.version} vs rem {mo.ServerMod.Version} -> {Utils.VersionManager.CompareVersions(mo.Mod.version, mo.ServerMod.Version)}");
                    switch (Utils.VersionManager.CompareVersions(mo.Mod.version, mo.ServerMod.Version)) {
                        case 2:
                            mo.Status = ModHolder.ModStatusTypes.Dev;
                            break;
                        case 1:
                        mo.VersionLabel.text = mo.Mod.version + "->" + mo.ServerMod.Version;
                        mo.Status = ModHolder.ModStatusTypes.Updatable;
                        break;
                        case 0:
                            mo.Status = ModHolder.ModStatusTypes.Latest;
                            break;
                         default :
                            mo.Status = ModHolder.ModStatusTypes.Unknown;
                            break;

                    }

                }
                else
                {
                    mo.Status = ModHolder.ModStatusTypes.Orphan;
                  //  mo.VersionLabel.text = "orphan";
                }
                // mo.VersionLabel.color = UnityEngine.Random.ColorHSV();
                // mo.VersionLabel.text = "wa";
                
            }
            else
            {
                mo.Status = ModHolder.ModStatusTypes.Unknown;
            }

            mo.UpdateColor();

        }
        #endregion versionLabelsComp


    }


    /// <summary>
    /// get the Remix menu info button. Make it select our update button on click
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

    internal static void SetInfoLabelText(string text)
    {
        modUpdaterStatus = text;
        if (lblModUpdaterStatus is null) return;
        lblModUpdaterStatus.text = modUpdaterStatus;
    }



    // also provide update info (orphaned, updatable, updated) inside mod preview window
    // add update button to config & stats view

    #region ModListUpdateLabel

    /// <summary>
    /// Modbuttons are represented by the mods inside the mods menu list in the remix menu
    /// this catches their version label and lets us edit it
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="list"></param>
    /// <param name="index"></param>
    internal void ModButtonChanger(On.Menu.Remix.MenuModList.ModButton.orig_ctor orig, Menu.Remix.MenuModList.ModButton self, Menu.Remix.MenuModList list, int index)
    {

        orig(self, list, index);
       

        var modObject = ModObjects.FirstOrDefault((modObject) => modObject.ModID == self.ModID);
        if (modObject is not null)
        {
            modObject.ModButton = self;
            Logger.LogWarning("added mod button info for " + self.ModID);

        }
        else Logger.LogWarning("could not find mod for " + self.ModID);

        return;

    }


    internal object? instanciatedFieldGetterReflector(System.Type type, object targetInstance, string fieldName)
    {
        FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo is not null)
        {
            object? result = fieldInfo.GetValue(targetInstance);
            return result;
        }
        return null;

    }



        internal object? instanciatedPropertyGetterReflector(System.Type type, object targetInstance, string propertyName)
    {
        PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo is not null)
        {
            object? result = propertyInfo.GetValue(targetInstance);
            return result;
        }
        return null;

    }
    internal OptionInterface? optionInterfaceGetterReflector(MenuModList.ModButton mb)
    {
        PropertyInfo propertyInfo = typeof(Menu.Remix.MenuModList.ModButton).GetProperty("itf", BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null)
        {
            OptionInterface? result = propertyInfo.GetValue(mb) as OptionInterface;
            return result;
        }
        Logger.LogError("could not find optioninterface from mod !");
        return null;
    }


    internal FLabel? versionLabelGetterReflector(MenuModList.ModButton mb)
    {
        var fieldInfo = typeof(Menu.Remix.MenuModList.ModButton).GetField("_labelVer", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo != null)
        {
            FLabel? value = fieldInfo.GetValue(mb) as FLabel;
            return value;

            // Now you can work with 'value', which is of type FLabel
        }
        Logger.LogError("could not find label current button !");

        return null;
    }
    #endregion ModListUpdateLabel


    #region ServerModsGenerators
    /// <summary>
    /// this gets data based on the update_url value inside modinfo.json
    /// "update_url": "https://github.com/autodistries/dummymod/releases/latest"
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
        foreach (var mo in ModObjects)
        {
            if (mo.ServerMod is null && !mo.Mod.hideVersion && WasLastUpdateLongAgo(mo.Mod.id))
            {
                searchedForeModsCount++;
                mo.RemoteModSourceInfo = new RemoteModSourceInfo(mo.Mod);
                if (mo.RemoteModSourceInfo.status == RemoteModSourceInfo.Status.NoUpdateLink) {
                    mo.Status = ModHolder.ModStatusTypes.Latest;
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
            Logger.LogInfo($"From {totalModsCount} mods, {searchedForeModsCount} searched for update_url, {offshortUrlModsCount} hits, {updatableOffshoreModsCount} updatable");
    }


    /// <summary>
    /// this gets from the workshop (github raindb.js)
    /// after DownloadFileIfNewerAsync, read all lines and manually parse through them
    /// </summary>
    /// <returns></returns>
    private async Task ParseAndQueryWorkshopModlist()
    {
        //should be able to concat results with github-provided results
        if (currentlyReading) return;
        currentlyReading = true;

        float startTime = Time.time;
        string url = "https://raw.githubusercontent.com/AndrewFM/RainDB/master/raindb.js";
        string targetPath = Path.Combine(ModsUpdater.THISMODPATH, "raindb.js");

        int result = await Utils.FileManager.DownloadFileIfNewerAsync(url, targetPath);
        if (result <= -10) {
            SetInfoLabelText("You are offline, or the updater had a fatal error.");
            Utils.FileManager.offlineMode = true;
            return;
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
                serverMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink, ServerMod.ServerModType.Workshop) );
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

        Logger.LogInfo($"loaded {serverMods.Count} remote mods in {elapsedTime}s");
        currentlyReading = false;
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
        if (UpdateCheckLog == null) {
            initUpdateCheckLog();
        }
        if (UpdateCheckLog!.ContainsKey(modid)) {
            if ( UpdateCheckLog![modid].Item1 - DateTime.Now>= TimeSpan.FromDays(1)) {
                return true;
            } else return false;
        } else return true;
    }

    private void initUpdateCheckLog()
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
                    Logger.LogError("update time logs file is malformed at line "+line); // the damaged file will be overwritten later
                    return;
                }
                string modid = parts[0].Trim();
                string modversion = parts[2].Trim();
                string modlink = parts[3].Trim();
                DateTime latestupdate = DateTime.ParseExact(parts[1].Trim(), "yyyyMMddHHss", System.Globalization.CultureInfo.InvariantCulture);
                ServerMod serverMod = new(modid,modversion,modlink) {Source = ServerMod.ServerModType.Url};
                serverMods.Add(serverMod);
                UpdateCheckLog.Add(modid, (latestupdate, serverMod));
            }
        }
    }

    private void writeUpdateCheckLog() {
        string updateFilePath = Path.Combine(THISMODPATH, "updatechecktimes.log");
        if (!File.Exists(updateFilePath))
        {
           initUpdateCheckLog();
        }
        List<string> lines = new();
        foreach (var linec in UpdateCheckLog) {
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



