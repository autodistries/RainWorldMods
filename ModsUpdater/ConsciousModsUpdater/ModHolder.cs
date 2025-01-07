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
        #region mod
        private ModManager.Mod mod;
        public ModManager.Mod Mod
        {
            get => mod;
        }
        public string ModID
        {
            get => mod.id;
        }
        #endregion mod

        #region servermod
        static readonly List<ServerMod> serverMods = new();
        ServerMod? serverMod;
        public ServerMod? ServerMod
        {
            get => serverMod;
            set => serverMod = value;
        }
        RemoteModSourceInfo? remoteModSourceInfo;
        public RemoteModSourceInfo? RemoteModSourceInfo { get => remoteModSourceInfo; set => remoteModSourceInfo = value; }
        #endregion servermod

        # region graphics
        private ModButton? modButton;
        public ModButton? ModButton
        {
            get => modButton;
            set
            {
                modButton = value;
                if (value is not null && value._labelVer is not null) labelVers.Add(value._labelVer);
            }
        }

        public FLabel? VersionLabel
        {
            get => modButton?._labelVer;
        }
        private Color color = Color.white;
        public Color Color
        {
            get => color;
        }
        public static List<FLabel> labelVers = new();
        public void UpdateColor()
        {
            color = status switch
            {

                ModStatusTypes.Empty => Color.red,
                ModStatusTypes.Unknown => C2c(System.Drawing.Color.OrangeRed),
                ModStatusTypes.Dev => Color.cyan,
                ModStatusTypes.Latest => Color.green,
                ModStatusTypes.Updated => C2c(System.Drawing.Color.YellowGreen),
                ModStatusTypes.Updatable => Color.yellow,
                ModStatusTypes.Managed_By_Steam => Color.blue,
                ModStatusTypes.Orphan => Color.grey,
                _ => Color.white,
            };
        }
        public bool updateLabel()
        {
            if (VersionLabel is null) return false;
            UpdateColor();
            VersionLabel.color = color;
            if (status == ModStatusTypes.Updatable && serverMod is not null)
            {
                VersionLabel.text = Mod.version + "->" + serverMod.Version;
                return true;
            }
            else if (status == ModStatusTypes.Updated && serverMod is not null)
            {
                VersionLabel.text = "updated to " + serverMod.Version;
                return true;
            }
            return false;
        }
        #endregion graphics





        ModStatusTypes status = ModStatusTypes.Empty;
        public ModStatusTypes Status
        {
            get => status;
            set => status = value;
        }

        public static List<ServerMod> ServerMods => serverMods;





        public ModHolder(ModManager.Mod modd)
        {
            mod = modd;
        }


        /// <summary>
        /// this would let us easily do that only when on remix page, and interactive update thingie.
        /// </summary>
        /// <returns></returns>

        public async Task<StatusCode> triggerUpdate()
        {
            if (serverMod is null || VersionLabel is null) return StatusCode.InvalidParameters;
            Console.WriteLine("Sterting update process for " + Mod.id);
            Utils.StatusCode res = await FileManager.GetUpdateAndUnzip(serverMod.Link, Mod.path);
            if (res == StatusCode.Success)
            {
                status = ModStatusTypes.Latest;
            }
            return res;
        }


        public void updateLabelText(string text) {
            if (VersionLabel is null) return;
            VersionLabel.text = text;
        }


    }



}

        public enum ModStatusTypes
        {
            Empty, // unknown status
            Dev, // mod is ahead with remote
            Latest, // mod is up-to-date with remote
            Updatable, // a remote update was found
            Updated, //mod was updated this session
            Orphan, // no remote sources have picked up this mod
            Unknown, // no version info or bersionning disabled
            Managed_By_Steam
        }