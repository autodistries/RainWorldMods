using System;
using System.Threading.Tasks;
using static ModsUpdater.Utils;
using BepInEx.MultiFolderLoader;
using static Menu.Remix.MenuModList;
using Menu.Remix;
using IL.Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;


namespace ModsUpdater;
#nullable enable



public partial class ModsUpdater
{
    /// <summary>
    /// This class holds every info related to a single mod :
    /// its local representation, its corresponding servermod
    /// its version label
    /// </summary>
    public class ModHolder
    {

        public static List<FLabel> labelVers = new();
        private ModManager.Mod mod;

        private ModButton modButton;
        public ModManager.Mod Mod { 
            get => mod;
        }
        public string ModID {
            get => mod.id;
        }

        ServerMod? serverMod;
        public ServerMod? ServerMod
        {
            get => serverMod;
            set => serverMod = value;
        }
        public FLabel VersionLabel {
            get => modButton._labelVer;
        }

        RemoteModSourceInfo? remoteModSourceInfo;

        ModStatusTypes status = ModStatusTypes.Empty;
        public ModStatusTypes Status
        {
            get => status;
            set => status = value;
        }
        public enum ModStatusTypes
        {
            Empty, // unknown status
            Dev, // mod is ahead with remote
            Latest, // mod is up-to-date with remote
            Updatable, // a remote update was found
            Updated, //mod was updated this session
            Orphan, // no remote sources have picked up this mod
            Unknown // no version info or bersionning disabled
        }

        private Color color = Color.white;

        public Color Color {
            get => color;
        }



        
        public RemoteModSourceInfo? RemoteModSourceInfo { get => remoteModSourceInfo; set => remoteModSourceInfo = value; }
        public ModButton ModButton { 
            get => modButton; 
            set  {
                modButton = value; 
                if (value._labelVer is not null) labelVers.Add(value._labelVer);
            }}

        public ModHolder(ModManager.Mod modd)
        {
            mod = modd;
        }

        public void UpdateColor() {
            color = status switch
                {
                    
                    ModStatusTypes.Empty => Color.red,
                    ModStatusTypes.Dev => Color.white,
                    ModStatusTypes.Latest => Color.green,
                    ModStatusTypes.Updatable => Color.yellow,
                    ModStatusTypes.Updated => new Color(System.Drawing.Color.YellowGreen.A, System.Drawing.Color.YellowGreen.R, System.Drawing.Color.YellowGreen.G, System.Drawing.Color.YellowGreen.B),
                    ModStatusTypes.Orphan => Color.grey,
                    ModStatusTypes.Unknown =>  new Color(System.Drawing.Color.OrangeRed.A, System.Drawing.Color.OrangeRed.R, System.Drawing.Color.OrangeRed.G, System.Drawing.Color.OrangeRed.B),
                    _ => Color.white,
                };
        }

        /// <summary>
        /// this would let us easily do that only when on remix page, and interactive update thingie.
        /// </summary>
        /// <returns></returns>
        public bool updateLabel() {
            if (VersionLabel is null) return false;
            VersionLabel.color = MenuModList.ModButton.cOutdated;
            if (status == ModStatusTypes.Updatable &&  serverMod is not null) {
                VersionLabel.text = Mod.version + "->"+serverMod.Version;
                return true;
            } else if (status == ModStatusTypes.Updated && serverMod is not null) {
                VersionLabel.text = "updated to "+serverMod.Version;
                return true;
            } 
            UpdateColor(); 
            return false;
        }

        public async Task<int> triggerUpdate()
        {
            if (serverMod is null || VersionLabel is null) return -10;
            Console.WriteLine("Sterting update process for "+Mod.id);
            int res = await FileManager.GetUpdateAndUnzip(serverMod.Link, Mod.path);
            if (res == 0) {
                status = ModStatusTypes.Latest;
                }
             UpdateColor();
            return res;
        }


    }



}



