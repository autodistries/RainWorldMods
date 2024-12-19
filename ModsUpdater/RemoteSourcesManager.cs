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
    public Status status = Status.None;

    public readonly Uri url;
    //url.segments is relative server path parts: /, folder/, file.ext
    //url.host is the host. www.example.com
    internal ServerMod ServerMod;

    (Utils.StatusCode, string?) remoteUpdateSource;

    ModManager.Mod mod;

    public RemoteModSourceInfo(ModManager.Mod modd)
    { //instead we should get url from here. so givr Mod as parameter
        mod = modd;
        GetUpdateUrl();
        Console.WriteLine(mod.id+": version data said: " + Utils.GetErrorMessage(remoteUpdateSource.Item1));
        if (remoteUpdateSource.Item1 != 0 || remoteUpdateSource.Item2 == "")
        {
            status = Status.NoUpdateLink;
            return; // getting version was not successful
        }
        url = new Uri(remoteUpdateSource.Item2);
        Console.WriteLine("cerated object " + url);
        FillInServerInfo();

    }

    private async void GetUpdateUrl()
    {
        remoteUpdateSource = await Utils.VersionManager.getModVersionUrl(mod);

    }

    private void buildServerMod()
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns>true if upd found</returns>
    internal bool FillInServerInfo()
    {
        if (remoteUpdateSource.Item1 == 0 || remoteUpdateSource.Item2 != ""){

            string workinglink = remoteUpdateSource.Item2;

            string workingVersion = "1.0"; // we need to extract that from url



            ServerMod = new ServerMod(mod.id, workingVersion, workinglink, ServerMod.ServerModType.Url);
            return true;
} else return false;
    }



    public enum Status
    {
        None,
        NoUpdateLink,
        Ok

    }
}



/*
Gathering data on releases, and git websites apis
Codeberg has a tags and releases system. Tags can be created by hand, or using git cli. Releases are made via web interface.
Releases can create tags if tag doesn't exist yet.
It also has actions, which means we could automate the tagging & releases thing
https://codeberg.org/catsoft/dummymod/releases/latest redirects to latest release which is good


*/

