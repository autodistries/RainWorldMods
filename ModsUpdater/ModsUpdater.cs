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
using static ModsUpdater.Utils;
using static Menu.Remix.MenuModList;
using MonoMod.Utils;

namespace ModsUpdater;
#nullable enable

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class ModsUpdater : BaseUnityPlugin
{

    public class ModAIOHolder
    {
        // This allows distributing thinking time across longer
        ModManager.Mod mod;

        ServerMod serverMod;
        FLabel? versionLabel;
        ModStatusTypes status = ModStatusTypes.Empty;

        private static OpLabel? lblUpdatableMods;
        private static OpLabel? lblOrphanedMods;
        private static OpLabel? lblUpToDateMods;
        private static OpSimpleImageButton? btnUpdateAll;

        public ModStatusTypes Status
        {
            get => status;
            set => status = value;
        }

        public enum ModStatusTypes
        {
            Empty,
            Latest,
            Updatable,
            Orphan


        }

        public string ModID
        {
            get => mod.id;
        }
        public ModManager.Mod Mod
        {
            get => mod;
        }

        public ServerMod ServerMod {
            get => serverMod;
            set => serverMod=value;
        }

        public FLabel? VersionLabel
        {
            get => versionLabel;
            set  {versionLabel = value;}
        }

        public static OpLabel? LblUpToDateMods { get => lblUpToDateMods; set => lblUpToDateMods = value; }
        public static OpLabel? LblOrphanedMods { get => lblOrphanedMods; set => lblOrphanedMods = value; }
        public static OpLabel? LblUpdatableMods { get => lblUpdatableMods; set => lblUpdatableMods = value; }
        public static OpSimpleImageButton? BtnUpdateAll { get => btnUpdateAll; set => btnUpdateAll = value; }

        public ModAIOHolder(ModManager.Mod modd)
        {
            mod = modd;
            // versionLabel = versionLabell;
        }

        public async Task<int> triggerUpdate() {
            lls.LogDebug("triggring update on "+serverMod.ID+":"+serverMod.Link+":"+mod.path);
            int res = await FileManager.GetUpdateAndUnzip(serverMod.Link, mod.path);
            if (res == 0) status = ModStatusTypes.Latest;
            lls.LogDebug("result:"+res);
            return res;
        }


    }

    public static string THISMODPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName;
    public static string MODSPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.Parent.FullName;

    List<ModAIOHolder> ModObjects = new();
    static List<ServerMod> serverMods = new();
    static BepInEx.Logging.ManualLogSource lls;


    static bool done = false;

    static bool currentlyReading = false;
    static bool doneReading = false;

    ModOptions modOptions;


    static ListButton? infoListButton;
    private InternalOI_Stats localInternalOiStats;

    public ModsUpdater()
    {
        lls = base.Logger;

        modOptions = new ModOptions(this, lls);
    }


    private void OnEnable()
    {
        if (done) return;

        lls.LogInfo("Hooking setup methods...");
        lls.LogInfo("todo: add a buton in own remix menu to re-read raindb.js");


        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;
        //  On.Menu.MainMenu.ModListButtonPressed += ParseRemoteMods;
        //    On.ModManager.RefreshModsLists += updateNotifier;


        //  On.Menu.Remix.MenuModList.ModButton.cctor += cctorcalml;
        On.ModManager.WrapModInitHooks += getMods; // discovers existing local mods
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
        On.Menu.Remix.InternalOI_Stats.Initialize += StatsMenuLabelsSetup; // adds info about number of updatable mods & button on REMIX if
        On.Menu.Remix.InternalOI_Stats._RefreshStats += StatsMenuLabelUpdate; // upates that info
        On.Menu.Remix.MenuModList.ListButton.ctor += InfoButtonGetter; // lets us make the update button selectable via keyboard
        ParseAndQueryServerModlist(); // 
    }

    private void InfoButtonGetter(On.Menu.Remix.MenuModList.ListButton.orig_ctor orig, ListButton self, MenuModList list, ListButton.Role role)
    {
        orig(self, list, role);
        if (role == ListButton.Role.Stat) {
            infoListButton = self;
            UIfocusable.MutualHorizontalFocusableBind(infoListButton!, ModAIOHolder.BtnUpdateAll!);
            infoListButton.OnClick+= (_) => {
                if (!ModAIOHolder.BtnUpdateAll!.greyedOut) Menu.Remix.ConfigConnector.FocusNewElement(ModAIOHolder.BtnUpdateAll!);
            };
        }
    }

    private void StatsMenuLabelsSetup(On.Menu.Remix.InternalOI_Stats.orig_Initialize orig, InternalOI_Stats self)
    {
            lls.LogDebug("hi from setupstats");

        orig(self);
        try {
            var methodInfo = typeof(InternalOI_Stats).GetMethod("_GetDPTexture", BindingFlags.NonPublic | BindingFlags.Instance);

            float num = (float)(methodInfo?.Invoke(self, new object[]{}))!;
            lls.LogDebug(num);
            ModAIOHolder.LblUpdatableMods = new OpLabel(new Vector2(20f, 260f - num), new Vector2(560f, 30f), $"Updates found for {ModObjects.Count((mod) => mod.Status == ModAIOHolder.ModStatusTypes.Updatable)} mods", FLabelAlignment.Left);
            ModAIOHolder.LblOrphanedMods = new OpLabel(new Vector2(20f, 240f - num), new Vector2(560f, 30f), "UwU", FLabelAlignment.Left);
            ModAIOHolder.LblUpToDateMods = new OpLabel(new Vector2(20f, 220f - num), new Vector2(560f, 30f), "AwA", FLabelAlignment.Left) ;
            
            ModAIOHolder.BtnUpdateAll = new OpSimpleImageButton(new Vector2(ModAIOHolder.LblUpdatableMods.label.textRect.xMax +28f, 260f - num), new Vector2(30f, 30f), "keyShiftB") {
				description = "Update al mods"
			};
            ModAIOHolder.BtnUpdateAll.OnClick += async (UIfocusable trigger) => {
                lls.LogDebug("trying to update mods");
                ModAIOHolder.BtnUpdateAll!.greyedOut = true;
                int successCounter = 0;
                int failureCounter = 0;
                foreach (var mod in ModObjects.FindAll((el) => el.Status == ModAIOHolder.ModStatusTypes.Updatable)) {
                    trigger.description = "Updating " + mod.Mod.name;
                    int res = await mod.triggerUpdate();
                    if (res==0)successCounter++;
                    else failureCounter++;
                }
                trigger.description = $"success:{successCounter}, failures: {failureCounter}";
                var refreshMethodInfo = typeof(InternalOI_Stats).GetMethod("_RefreshStats",BindingFlags.NonPublic | BindingFlags.Instance );
                refreshMethodInfo.Invoke(localInternalOiStats, new object[]{});
            };

           

            self.Tabs[0].AddItems(ModAIOHolder.LblUpdatableMods, ModAIOHolder.LblOrphanedMods, ModAIOHolder.LblUpToDateMods);
            lls.LogDebug("ok for labels");
            self.Tabs[0].AddItems(  ModAIOHolder.BtnUpdateAll);
            // UIfocusable[] array = new UIfocusable[1] { ModAIOHolder.BtnUpdateAll }; // grab info btn here + special case for first arriving. separate setup needed
			// 	foreach (UIfocusable uIfocusable in array)
			// 	{
			// 		uIfocusable.SetNextFocusable(UIfocusable.NextDirection.Left, FocusMenuPointer.GetPointer(FocusMenuPointer.MenuUI._ApplyButton));
			// 		uIfocusable.SetNextFocusable(UIfocusable.NextDirection.Right, ModAIOHolder.BtnUpdateAll);
			// 		uIfocusable.SetNextFocusable(UIfocusable.NextDirection.Up, FocusMenuPointer.GetPointer(FocusMenuPointer.MenuUI._BackButtonUpPointer));
			// 		uIfocusable.SetNextFocusable(UIfocusable.NextDirection.Back, uIfocusable);
			// 	}
            // var focusElementSetter = typeof(ConfigContainer).GetMethod("_FocusNewElement", BindingFlags.NonPublic | BindingFlags.Instance);
            // focusElementSetter.Invoke(ConfigContainer.instance, new object[] {ModAIOHolder.BtnUpdateAll});
            lls.LogDebug("ok for updall + ");

            lls.LogDebug("successfully added labels to stats menu");
        } catch (Exception e) {e.LogDetailed();}
    }
    private void StatsMenuLabelUpdate(On.Menu.Remix.InternalOI_Stats.orig__RefreshStats orig, InternalOI_Stats self)
    {
        orig(self);
        localInternalOiStats = self;
         if (ModObjects.Count((mod) => mod.Status == ModAIOHolder.ModStatusTypes.Updatable) == 0) {
                ModAIOHolder.BtnUpdateAll.greyedOut = true;
            }
        ModAIOHolder.LblUpdatableMods!.text = $"Updates found for {ModObjects.Count((mod) => mod.Status == ModAIOHolder.ModStatusTypes.Updatable)} mods";
        ModAIOHolder.LblOrphanedMods!.text = $"Orphan mods: {ModObjects.Count((mod) => mod.Status == ModAIOHolder.ModStatusTypes.Orphan)}";
        ModAIOHolder.LblUpToDateMods!.text = $"Up-to-date (or better) mods: {ModObjects.Count((mod) => mod.Status == ModAIOHolder.ModStatusTypes.Latest)}";

    }

    internal void TriggerVersionComp(On.Menu.Remix.MenuModList.orig_ctor orig, MenuModList self, ConfigMenuTab tab)
    {
        orig(self, tab);
        
        #region versionLabelsComp
        lls.LogInfo("finished discovering versionlabels. comparing strings.");
        foreach (var mo in ModObjects)
        {
            if (mo.VersionLabel is not null)
            {

                    lls.LogInfo($"prepare comparing version on {mo.ModID}");

                    var currentServerMod = (mo.ServerMod is not null) ? mo.ServerMod : serverMods.FirstOrDefault((mod) => mod.ID == mo.ModID);


                    if (currentServerMod != null)
                    {
                        mo.ServerMod = currentServerMod;
                        if (Utils.VersionManager.IsVersionGreater(mo.Mod.version, mo.ServerMod.Version))
                        {
                            mo.VersionLabel.text = mo.Mod.version + "->" + mo.ServerMod.Version;
                            mo.Status = ModAIOHolder.ModStatusTypes.Updatable;
                        }
                        else
                        {
                            mo.VersionLabel.text = "not.an.orphan";
                            mo.Status = ModAIOHolder.ModStatusTypes.Latest;

                        }
                    }
                    else
                    {
                        mo.Status = ModAIOHolder.ModStatusTypes.Orphan;
                        mo.VersionLabel.text = "orphan";
                    }
                
            } else {
                mo.Status = ModAIOHolder.ModStatusTypes.Empty;
            }
        }
        #endregion versionLabelsComp

      
    }

    internal void getMods(On.ModManager.orig_WrapModInitHooks orig)
    {
        orig();
        if (ModObjects.Count == 0)
        {
            lls.LogInfo("Collecting mods after ModManager WrapInit");
            foreach (ModManager.Mod mod in ModManager.InstalledMods)
            {
                ModObjects.Add(new(mod));
            }
            lls.LogInfo("Discovered " + ModObjects.Count + " mods");
        }
        else
        {
            lls.LogWarning("repeat wrapinit");
        }
    }




    // mouseover update button, then link to update function
    /*
    see line 414886: istButton._swapIndex = ((!_list._SearchMode && selectEnabled) ? index : (-1));
    readonly ListButton[] MenuModList._roleButtons are set up inside public MenuModList.MenuModList(ConfigMenuTab tab)
    Menu.Remix.MenuModList.ListButton public ListButton(MenuModList list, Role role)
    */
    // also provide update info (orphaned, updatable, updated) inside mod preview window
    // a function to revert changes to a mod maybe ? would need to store labels etc
    // add update button to config & stats view

    #region ModListUpdateLabel
    internal void ModButtonChanger(On.Menu.Remix.MenuModList.ModButton.orig_ctor orig, Menu.Remix.MenuModList.ModButton self, Menu.Remix.MenuModList list, int index)
    {

        orig(self, list, index);


            FLabel? currVerLabel = versionLabelGetterReflector(self);
            ModManager.Mod? currentLocalMod = optionInterfaceGetterReflector(self)?.mod; // needs reflection

            var modObject = ModObjects.FirstOrDefault((modObject) => modObject.ModID == currentLocalMod?.id);
            if (modObject is not null)
            {
                modObject.VersionLabel = currVerLabel;
            }
            else lls.LogWarning("could not find mod for " + currentLocalMod.id);
            
            return;






    }



    internal OptionInterface? optionInterfaceGetterReflector(MenuModList.ModButton mb)
    {
        PropertyInfo propertyInfo = typeof(Menu.Remix.MenuModList.ModButton).GetProperty("itf", BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null)
        {
            OptionInterface result = propertyInfo.GetValue(mb) as OptionInterface;
            return result;
        }
        lls.LogError("could not find optioninterface from mod !");
        return null;
    }


    internal FLabel? versionLabelGetterReflector(MenuModList.ModButton mb)
    {
        var fieldInfo = typeof(Menu.Remix.MenuModList.ModButton).GetField("_labelVer", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo != null)
        {
            FLabel value = fieldInfo.GetValue(mb) as FLabel;
            return value;

            // Now you can work with 'value', which is of type FLabel
        }
        lls.LogError("could not find label current button !");

        return null;
    }
    #endregion ModListUpdateLabel







    private async Task ParseAndQueryServerModlist()
    {
        //should be able to concat results with github-provided results
        if (currentlyReading) return;
        currentlyReading = true;

        float startTime = Time.time;
        string url = "https://raw.githubusercontent.com/AndrewFM/RainDB/master/raindb.js";
        string targetPath = Path.Combine(ModsUpdater.THISMODPATH, "raindb.js");

        int result = await Utils.FileManager.DownloadFileIfNewerAsync(url, targetPath);
        switch (result)
        {
            case 0:
                // ModOptions.SetInfoLabel("Updated source", Color.gray);
                break;
            case 1:
                //ModOptions.SetInfoLabel("Local file up-to-date", Color.gray);
                break;
            case -1:
                //ModOptions.SetInfoLabel("Could not get etag from headers", Color.red);

                break;
            case -2:
                //ModOptions.SetInfoLabel("Currently offline", Color.red);


                break;
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
                serverMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink) { Source=ServerMod.ServerModType.Workshop});
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

        lls.LogInfo($"loaded {serverMods.Count} remote mods in {elapsedTime}s");
        currentlyReading = false;
        doneReading = true;

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




    public static string GetLogFor(object target)
    {
        var properties =
            from property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            select new
            {
                Name = property.Name,
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
    string id;
    string version;
    string link;


    public string Link
    {
        get => link;
    }

    public string Version
    {
        get => version;
    }
    public string ID
    {
        get => id;
    }
    public ServerModType Source { get => source; set => source = value; }

    private ServerModType source ;

    public ServerMod(string id, string version, string link)
    {
        this.id = id;
        this.version = version;
        this.link = link;
    }

    public override string ToString()
    {
        return this.id + ":" + this.version + ":" + link;
    }


    public enum ServerModType {
        Workshop,
        Github
    }
}



