using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading.Tasks;
using BepInEx.MultiFolderLoader;
using MonoMod.ModInterop;

namespace ModsUpdater;


/// <summary>
/// this class manages remote sources. well, it is not ready for anything else than github. for now at least
/// </summary>
public class RemoteModSourceInfo
{
    public Service service = Service.None;
    public Mode mode = Mode.None;
    public Status status = Status.None;

    public string author = "";
    public string repo = "";
    public readonly Uri url;
    //url.segments is relative server path parts: /, folder/, file.ext
    //url.host is the host. www.example.com
    internal ServerMod ServerMod;

    (int, string?) remoteUpdateSource;

    ModManager.Mod mod;

    public RemoteModSourceInfo(ModManager.Mod modd)
    { //instead we should get url from here. so givr Mod as parameter
        mod = modd;
        GetUpdateUrl();
        Console.WriteLine("version data said: " + Utils.StatusInfo.get(remoteUpdateSource.Item1));
        if (remoteUpdateSource.Item1 != 0 || remoteUpdateSource.Item2 == "")
        {
            status = Status.NoUpdateLink;
            return; // getting version was not successful
        }
        url = new Uri(remoteUpdateSource.Item2);
        Console.WriteLine("cerated object " + url);
        ExtractDataFromUrl();

    }

    private async void GetUpdateUrl()
    {
        remoteUpdateSource = await Utils.VersionManager.getModVersionUrl(mod);

    }

    private void buildServerMod()
    {
        throw new NotImplementedException();
    }

    private async void ExtractDataFromUrl()
    {
        if (url.Host == "github.com")
        {
            service = Service.Github;
        }
        else if (url.Host == "codeberg.org")
        {
            service = Service.Unsupported;
        }
        else
        {
            service = Service.Unsupported;
        }

        if (url.Segments.Last() == "latest")
        {
            mode = Mode.Release;
        }

        Console.WriteLine("status is " + service + " " + mode);


    }

    internal async Task<bool> fillInServerInfo()
    {
        if (service != Service.Github || mode != Mode.Release) return false;
        else
        {
            string workingVersion = "";
            string workingId = "";
            string workinglink = "";
            string latestReleaseUrl = "https://api.github.com/repos";
            foreach (string s in url.Segments) latestReleaseUrl += s;
            var client = new HttpClient();

            var os = Environment.OSVersion.ToString();
            var clr = Environment.Version.ToString();

            client.DefaultRequestHeaders.Add("user-agent", $"Mozilla/4.0 (compatible; MSIE 6.0; {os}; .NET CLR {clr};)");


            var response = await client.GetAsync(latestReleaseUrl);
            response.EnsureSuccessStatusCode();

             var jsonResponse = await response.Content.ReadAsStringAsync();
            //   Console.WriteLine(jsonResponse);
            //var text = File.ReadAllText(Path.Combine(ModsUpdater.THISMODPATH, "resp.txt"));
            //  Console.WriteLine(text);
            Dictionary<string, object> dictionary = jsonResponse.dictionaryFromJson();
            
            if (dictionary.ContainsKey("tag_name"))
            {
                workingVersion = dictionary["tag_name"].ToString();
            }

            //if (!Utils.VersionManager.IsVersionGreater(mod.version, workingVersion)) return false;
            workingId = mod.id;
            var assets = (dictionary["assets"] as List<object>);


            int listIndex = 0;
            while (workinglink == "" && listIndex<assets.Count) {
                Dictionary<string, object> assetObject = (Dictionary<string, object>) assets[listIndex];
              
                if (assetObject.ContainsKey("browser_download_url") && (assetObject["browser_download_url"] as string).EndsWith(".zip")) {
                    workinglink = assetObject["browser_download_url"] as string;
                }
                listIndex++;
            }

            if (workinglink == "") {
                return false;
            }
            
           



            ServerMod = new ServerMod(workingId, workingVersion, workinglink, ServerMod.ServerModType.Github);
            return true;
        }
        return false;
    }

    public enum Mode
    {
        None,
        Release, // checking releases
        Multi // given path, except path/ModName/modinfo.json and path.ModName.zip. Unsupported for now
    }

    public enum Service
    {
        None,
        Github,
        Codeberg,
        Unsupported
    }

    public enum Status
    {
        None,
        NoUpdateLink,

    }
}



/*
Gathering data on releases, and git websites apis
Codeberg has a tags and releases system. Tags can be created by hand, or using git cli. Releases are made via web interface.
Releases can create tags if tag doesn't exist yet.
It also has actions, which means we could automate the tagging & releases thing
https://codeberg.org/catsoft/dummymod/releases/latest redirects to latest release which is good


*/

