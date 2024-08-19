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
        modOptions.AddButtonOption(40, "load local mods", loadLocalMods);
        modOptions.AddButtonOption(80, "list local mods", listLocalMods);
        modOptions.AddButtonOption(120, "download remote", downloadRemoteModsList);
        modOptions.AddButtonOption(160, "load server mods", loadServerMods);
        modOptions.AddButtonOption(200, "dl first mod", tryToUpdateFirstMod);
        modOptions.AddButtonOption(240, "summon new tab", tryToCreateTab2);
        modOptions.AddButtonOption(280, "modOptions.ResetUIelements", trytoaaa);
        modOptions.AddButtonOption(320, "modOptions. manual reset !!", trytobbb);
        modOptions.AddButtonOption(360, "summon OpScrollBox", trytoopscrollbox);
        lls.LogDebug("3.6");
        /*if (ConfigContainer.menuTab.modList.GetModButton(ModManager.InstalledMods[i].id) == null)*/
        /*sortedModButtons.AddRange(list);*/
    }

    private void trytoopscrollbox(UIfocusable trigger)
    {
       OpScrollBox opScrollBox = new OpScrollBox(new Vector2(30f, 80f), new Vector2(250f, 450f), 25f, false, true, true);
       modOptions.Tabs[0].AddItems(opScrollBox);
       /* In the end we need:
       a button to update all available
       a button & text for update and info abt raindb.js*/
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

        string url = "https://raw.githubusercontent.com/AndrewFM/RainDB/master/raindb.js";
        string targetPath = Path.Combine(THISMODPATH, "raindb.js");

        int result = await FileManager.DownloadFileIfNewerAsync(url, targetPath);
        switch (result)
        {
            case 0:
                button.text = "Updated source";
                button.description = "Local file was updated";
                break;
            case 1:
                button.text = "Nothing to do";
                button.description = "Local file is already up-to-date";
                break;
            case -1:
                button.text = "Error";
                button.description = "Could not get etag from headers";
                break;
            case -2:
                button.text = "Error";
                button.description = "You might not be connected to the internet";
                break;
        }
    }

    private void listLocalMods(UIfocusable trigger)
    {
        foreach (var mod in localMods)
        {
            string temp = "-> ";
            try
            {
                foreach (int i in Utils.VersionToList(mod.Version))
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
    public void loadServerMods(UIfocusable uIfocusable)
    {
        if (localMods.Count == 0)
        {
            lls.LogWarning("localMods wasn't loaded before !");
            return;
        }
        string[] lines = File.ReadAllLines(Path.Combine(THISMODPATH, "raindb.js"));
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
                lls.LogDebug($"adding {currentWorkingID} {currentWorkingVersion} {currentWorkingLink}");
                serverMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink));
            }
            else
            {
                var match = Regex.Match(line, detectionPattern);
                if (match.Success)
                {
                    string key = match.Groups["key"].Value;
                    string value = match.Groups["value"].Value;
                    //  lls.LogDebug($"matched {key} {value}");
                    switch (key)
                    {
                        case "id":
                            {
                                if (!localMods.Any((lmod) => lmod.ID == value)) { skippingThisMod = true; break; }
                                currentWorkingID = value; break;
                            }
                        case "version": currentWorkingVersion = value; break;
                        case "url": currentWorkingLink = value; break;
                    }
                }
            }
        }

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


public class LocalMod
{
    string id;
    string version;
    string path;

    public string Path {
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

    public string Link {
        get => link;
    }
    public ServerMod(string id, string version, string link) : base(id, version, "")
    {
        this.link = link;
    }
}


public class FileManager
{
    public static async Task<int> DownloadFileIfNewerAsync(string url, string localPath)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Send a HEAD request to get the ETag header
                var headResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                headResponse.EnsureSuccessStatusCode();

                // Get the ETag header
                if (headResponse.Headers.ETag != null)
                {
                    string remoteETag = headResponse.Headers.ETag.Tag.Trim('"');

                    // Check if the local file exists
                    if (File.Exists(localPath))
                    {
                        // Read the local ETag from a file or a simple text file
                        string localETagPath = localPath + ".etag";
                        string localETag = File.Exists(localETagPath) ? File.ReadAllText(localETagPath) : null;

                        // Compare the ETags
                        if (remoteETag != localETag)
                        {
                            Console.WriteLine("Remote file is newer. Downloading...");
                            await DownloadFileAsync(url, localPath);
                            // Update the local ETag
                            File.WriteAllText(localETagPath, remoteETag);
                            return 0; // File was downloaded
                        }
                        else
                        {
                            Console.WriteLine("Local file is up to date.");
                            return 1; // Local file is up to date
                        }
                    }
                    else
                    {
                        // If the local file does not exist, download it
                        Console.WriteLine("Local file does not exist. Downloading...");
                        await DownloadFileAsync(url, localPath);
                        // Save the ETag
                        File.WriteAllText(localPath + ".etag", remoteETag);
                        return 0; // File was downloaded
                    }
                }
                else
                {
                    Console.WriteLine("Could not retrieve ETag header.");
                    return -1; // Error occurred
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking remote file: {ex.Message}");
                return -2; // Error occurred
            }
        }
    }

    private static async Task DownloadFileAsync(string url, string localPath)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Send a GET request to the specified URL
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Throw if not a success code.

                // Read the response content as a byte array
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Write the byte array to a file asynchronously using FileStream
                using (FileStream fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await fs.WriteAsync(fileBytes, 0, fileBytes.Length);
                }

                Console.WriteLine($"File downloaded successfully to {localPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
            }
        }
    }

    public static async Task<int> GetUpdateAndUnzip(string url, string modPath) {
        if (!url.EndsWith("zip")) return -1;
        if (!(Directory.GetParent(Path.GetFullPath(modPath)).ToString() == ModsUpdater.MODSPATH)) {
            return -2;
        }
        string fileName = url.Split('/').Last();
        string tempFilePath = Path.Combine(ModsUpdater.THISMODPATH, "." + fileName);
        await DownloadFileAsync(url, tempFilePath);
        Console.WriteLine(ModsUpdater.MODSPATH);
        System.IO.Directory.Delete(modPath, true);
        System.IO.Directory.CreateDirectory(modPath);
        System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, modPath);
        return 0;
    }

}