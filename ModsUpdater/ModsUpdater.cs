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
namespace ModsUpdater;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]


public partial class ModsUpdater : BaseUnityPlugin
{

    public static string THISMODPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.FullName;
    public static string MODSPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent.Parent.FullName;

    static List<LocalMod> localMods = new();
    static List<ServerMod> serverMods = new();
    static BepInEx.Logging.ManualLogSource lls;

    static bool done = false;

    ModOptions modOptions;
    OpTab savedOpTab;



    public ModsUpdater()
    {
        lls = base.Logger;

        modOptions = new ModOptions(this, lls);

    }


    private void Awake()
    {
        if (done) return;

        lls.LogInfo("Hooking setup methods...");


        On.RainWorld.OnModsInit += RainWorldOnOnModsInitDetour;

        done = true;
    }





    private void RainWorldOnOnModsInitDetour(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (MachineConnector.SetRegisteredOI(PLUGIN_GUID, modOptions))
        {
            lls.LogInfo("Registered Mod Interface");
        }
        else
        {
            lls.LogError("Could not register Mod Interface");
        }
    }

    // ConfigMenuTab configMenuTab = new();
    // List<ModButton> modButtons = new();
    public void SetUpOptionsSettings()
    {
        /*menuTab = new ConfigMenuTab();*/
        // modButtons.Add(new ModButton(configMenuTab.modList, 0));
        lls.LogDebug("3.5");
        modOptions.AddButtonOption(40, "set update mods", setUpdMods);
        modOptions.AddButtonOption(80, "set download mods", setDlMods);
        modOptions.AddButtonOption(120, "set text toaa", setTxt);
        modOptions.AddButtonOption(160, "make it dirtyy", setdirty);
        lls.LogDebug("3.6");
        /*if (ConfigContainer.menuTab.modList.GetModButton(ModManager.InstalledMods[i].id) == null)*/
        /*sortedModButtons.AddRange(list);*/
    }

    private void setdirty(UIfocusable trigger)
    {
        modOptions.localModsContainer.listIsDirty = true;
    }

    private void setTxt(UIfocusable trigger)
    {
        ModOptions.SetInfoLabel("zngjjjjjjjjjjjjjjjjjjjjjj");
    }

    private void setDlMods(UIfocusable trigger)
    {
        modOptions.localModsContainer.CurrentContainerStatus = ModOptions.ModsContainer.ContainerStatus.Download;
    }

    private void setUpdMods(UIfocusable trigger)
    {
        modOptions.localModsContainer.CurrentContainerStatus = ModOptions.ModsContainer.ContainerStatus.Update;
    }

    private void trytobbb(UIfocusable trigger)
    {
        OpTab[] tabs = modOptions.Tabs;
        for (int i = 0; i < tabs.Length; i++)
        {
            foreach (UIelement item in tabs[i].items)
            {
                if (item is UIconfig && (item as UIconfig).cosmetic)
                {
                    item.Reset();
                }
            }
        }
    }

    private void trytoaaa(UIfocusable trigger)
    {
        modOptions.ResetUIelements();

    }

    private void tryToCreateTab2(UIfocusable trigger)
    {
        var opTab = new Menu.Remix.MixedUI.OpTab(modOptions, "Test");
        savedOpTab = modOptions.Tabs[0];
        modOptions.Tabs = new[]
        {
            opTab
        };

        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Mods Updater", true),
        };
        lls.LogDebug(2);


        opTab.AddItems(UIArrPlayerOptions);
    }

    private async void tryToUpdateFirstMod(UIfocusable trigger)
    {
        if (serverMods.Count == 0) return;
        int result = await FileManager.GetUpdateAndUnzip(serverMods.First().Link, localMods.First((mod) => mod.ID == serverMods.First().ID).Path);
    }

    private async void downloadRemoteModsList(UIfocusable trigger)
    {
        OpSimpleButton button = trigger as OpSimpleButton;
        button.text = "Please wait...";
        button.description = button.text;


    }

    private void listLocalMods(UIfocusable trigger)
    {
        foreach (var mod in localMods)
        {
            string temp = "-> ";
            try
            {
                foreach (int i in Utils.VersionManager.VersionToList(mod.Version))
                {
                    temp += i.ToString() + "-";
                }
            }
            catch (Exception e) { temp += "error"; lls.LogError(e); }
            lls.LogInfo(mod + temp);
        }
    }

    /// <summary>
    /// WE NEED TO IGNORE STEAMWORKS MODS!!
    /// </summary>
    /// <param name="focusable"></param>
    public void loadLocalMods(UIfocusable focusable)
    {

        foreach (ModManager.Mod mod in ModManager.InstalledMods)
        {
            localMods.Add(new LocalMod(mod.id, mod.version, mod.path));
        }
    }

    /// <summary>
    /// loads mods from raindb.js
    /// will only retain mods that exist inside localMods
    /// as such, needs localMods to be filled first !
    /// </summary>
    /// <param name="uIfocusable"></param>
   





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


public class LocalMod
{
    string id;
    string version;
    string path;

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

    public LocalMod(string id, string version, string path)
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


public class ServerMod : LocalMod
{
    string link;

    public string Link
    {
        get => link;
    }
    public ServerMod(string id, string version, string link) : base(id, version, "")
    {
        this.link = link;
    }
}


