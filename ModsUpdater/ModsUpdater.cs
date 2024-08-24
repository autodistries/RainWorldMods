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
        StatusTypes status = StatusTypes.Empty;
        static List<ListButton>? modButtons;
        public StatusTypes Status
        {
            get => status;
            set => status = value;
        }

        public enum StatusTypes
        {
            Empty,
            Latest,
            Updatable,
            Orphan

        
        }

        public static List<ListButton>? ModButtons {
            get => modButtons;
            set => modButtons =value;
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

        public ModAIOHolder(ModManager.Mod modd)
        {
            mod = modd;
            // versionLabel = versionLabell;
        }


    }

    public static string THISMODPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName;
    public static string MODSPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.Parent.FullName;

    List<ModAIOHolder> ModObjects = new();
    static List<ServerMod> serverMods = new();
    static List<ModManager.Mod> orphanedMods = new();
    static List<ModManager.Mod> updatableMods = new();
    static List<ModManager.Mod> updatedMods = new();
    static BepInEx.Logging.ManualLogSource lls;

    static bool done = false;

    static bool currentlyReading = false;
    static bool doneReading = false;

    ModOptions modOptions;




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
      //  On.Menu.Remix.MenuModList.Update += ButtonShower;
        ParseAndQueryServerModlist(); // 
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
                            mo.Status = ModAIOHolder.StatusTypes.Updatable;
                        }
                        else
                        {
                             mo.VersionLabel.text = "not.an.orphan";
                        }
                    }
                    else
                    {
                        mo.Status = ModAIOHolder.StatusTypes.Orphan;
                        mo.VersionLabel.text = "orphan";
                    }
                
            } else {
                mo.Status = ModAIOHolder.StatusTypes.Empty;
            }
        }
        #endregion versionLabelsComp

        #region updateButton
        var baseModBtnArray = modButtonsGetterReflector(self);
        if (baseModBtnArray is not null)
        {
            ModAIOHolder.ModButtons = baseModBtnArray.ToList();
            var constructor = typeof(ListButton).GetConstructor(new[] { typeof(MenuModList), typeof(ListButton.Role) });
            var listButtonInstance = constructor.Invoke(new object[] { self, ListButton.Role.SwapUp }) as ListButton;

            if (listButtonInstance is not null)
            {
                var posProperty = typeof(UIelement).GetProperty("pos", BindingFlags.Public | BindingFlags.Instance);
                if (posProperty != null)
                {
                    // Assuming pos is of type Vector2
                    Vector2 currentPos = (Vector2)posProperty.GetValue(self);
                    Vector2 newPos = new Vector2(currentPos.x + 174f, -200f);

                    // Now set the new position using SetPos
                    var posSetter = typeof(UIelement).GetMethod("SetPos", BindingFlags.Public | BindingFlags.Instance);
                    posSetter?.Invoke(listButtonInstance, new object[] { newPos });
                    ModAIOHolder.ModButtons.Add(listButtonInstance);
                    baseModBtnArray = ModAIOHolder.ModButtons.ToArray();
                }
                else
                {
                    Debug.LogError("Property 'pos' not found. Did not add btn.");
                }

            }
            else lls.LogError("could not create listbtn from recleftor");
        }
        #endregion updateButton
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


    internal void ButtonShower(On.Menu.Remix.MenuModList.orig_Update orig, MenuModList self)
    {
        orig(self);
        var swapIndexField = typeof(ListButton).GetField("_swapIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    
    if (swapIndexField != null)
    {
        // Get the value of _swapIndex
        int swapIndexValue = (int)swapIndexField.GetValue(null); // Use 'self' if it's an instance field

        if (!self._SearchMode && (ConfigContainer.FocusedElement is ModButton || (self.MenuMouseMode && self.MouseOver)) && swapIndexValue > 0)
        {
            self._roleButtons[6].Show();
            lls.LogDebug("showing upd btn");
        }
        else
        {
            self._roleButtons[6].Hide();
        }
    }
    else
    {
        lls.LogError("Field '_swapIndex' not found.");
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
        if (true ) //because those flabels get discared on every exit of remix menu
        {


            FLabel? currVerLabel = versionLabelGetterReflector(self);
            ModManager.Mod? currentLocalMod = optionInterfaceGetterReflector(self)?.mod; // needs reflection

            var modObject = ModObjects.FirstOrDefault((modObject) => modObject.ModID == currentLocalMod?.id);
            if (modObject is not null)
            {
                modObject.VersionLabel = currVerLabel;
            }
            else lls.LogWarning("could not find mod for " + currentLocalMod.id);
            
            return;
        } else  {
            foreach(var aa in ModObjects) {
                if (aa.VersionLabel is not null) aa.VersionLabel.text+="a";
            }
        }
        return;





    }


    internal ListButton[]? modButtonsGetterReflector(MenuModList mml) {
        FieldInfo fieldInfo = typeof(MenuModList).GetField("_roleButtons",BindingFlags.NonPublic | BindingFlags.Instance );
        if (fieldInfo != null)
        {
            ListButton[] value = fieldInfo.GetValue(mml) as ListButton[];
            return value;
        }
        lls.LogError("could not find mod button list !");

        return null;
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
                serverMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink));
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
    string path;

    string link;

    public string Link
    {
        get => link;
    }
    public string Path
    {
        get => path;
    }
    public string Version
    {
        get => version;
    }
    public string ID
    {
        get => id;
    }

    public ServerMod(string id, string version, string path)
    {
        this.id = id;
        this.version = version;
        this.path = path;
    }

    public override string ToString()
    {
        return this.id + ":" + this.version + ":" + path;
    }
}



