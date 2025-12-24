using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.ModInterop;
using static System.Diagnostics.Trace;
using static PreStartUpdater.FileManager;
using static PreStartUpdater.Logs;


namespace PreStartUpdater;




public static class PreStartUpdaterPatcher
{
    #region reqs
    // req n°1
    public static IEnumerable<string> TargetDLLs { get; } = new[] {"Assembly-CSharp.dll"};
    // req n°2
    public static void Patch(AssemblyDefinition assembly)
    {
    }
    #endregion reqs

    private static List<PendingUpdate> PendingUpdates = new();

    public static DirectoryInfo MODSFOLDER = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent;





    public static void Finish() {


        DirectoryInfo BEPINEXPATH = Directory.GetParent(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)).Parent;
        DirectoryInfo MODSUPDATERPATH = new DirectoryInfo(Path.Combine(BEPINEXPATH.Parent.FullName,"ModsUpdater"));
        FileInfo[] pendingUpdatesFile = MODSUPDATERPATH.GetFiles("pendingUpdates.txt");
        if (pendingUpdatesFile.Length == 0) {
            logInfo("No updates pending. Exiting.");
            return;
        } else {
            logInfo("Updates file found.");
            using StreamReader r = new(pendingUpdatesFile[0].FullName);
            string contents = r.ReadToEnd();
            r.Close();
            List<string> lines = contents.Split('\n').ToList().FindAll((line) => line.Split('|').Length == 3);
            logInfo($"Valid lines: {lines.Count}");
            foreach (string line in lines) {
                string[] decinfo = line.Split('|');
                PendingUpdates.Add(new(decinfo[0], decinfo[1], decinfo[2]));
                logInfo(PendingUpdates[PendingUpdates.Count - 1]);
            }

            logInfo($"Applying updates for {PendingUpdates.Count} mods...");

            foreach (var pendingUpdate in PendingUpdates) {
                pendingUpdate.UpdateProcess();
            }
            pendingUpdatesFile[0].Delete();

        }
        // [Info   :     Trace] executing assembly path is Z:\home\guigui\.local\share\Steam\steamapps\common\Rain World\BepInEx

        // Step 1. Check for a .pendingUpdates inside my mod folder
        // Step 2. Read from it, couples of PathsToBeDeleted and ZipToBeUnzipped
        // Step3 done


        // From the other side;
        // on game start, if pendingupdate is here:
        //      delete it, delete the zips, delete the patcher
        // when clicking th eupdate btn, we need to :
        // Download the remote update
        // Create/update .pendingUpdates (json) with PathToBeDeleted and ZipToBeUnzipped of that mod
        // (opt) put the preloader inside the patchers folder
        // disable upd btn; set upd text to pending restart
    }
}

internal class PendingUpdate
{
    string ModName;
    string PreviousModFolderPath;
    string ZipToUnzip;

    public PendingUpdate(string modName, string previousModFolderPath, string zipToUnzip) {
        ModName = modName;
        PreviousModFolderPath = previousModFolderPath;
        ZipToUnzip = zipToUnzip;
    }

    public override string ToString()
    {
        return $"{ModName} | {PreviousModFolderPath} | {ZipToUnzip}";
    }


    public bool UpdateProcess() {

        if (PreviousModFolderPath != "")
        {
            DirectoryInfo previousDirInfo = new(PreviousModFolderPath);

            if (PreStartUpdaterPatcher.MODSFOLDER.Parent.FullName != previousDirInfo.Parent.FullName && PreviousModFolderPath != "")
            {
                logInfo($"TARGET MOD FOLDER IS NOT INSIDE mods FOLDER !!! ABORTING {PreStartUpdaterPatcher.MODSFOLDER.Parent.FullName} vs {previousDirInfo.Parent.FullName}");
                return false;
            }
            string forcedName = previousDirInfo.Name;
            if (previousDirInfo.Exists)
            {
                if (!RecursiveDelete(previousDirInfo))
                {
                    logInfo($"Could not update {ModName}: could not delete previous folder fully. Are some dlls still being used ?");
                    return false;
                }
            }
            if (!UnzipZipToModsFolder(ZipToUnzip, forcedName))
            {
                logInfo($"Failed to unzip {ZipToUnzip}");
                return false;
            }
        } else {
            if (!UnzipZipToModsFolder(ZipToUnzip))
            {
                logInfo($"Failed to unzip {ZipToUnzip}");
                return false;
            }
        }
        new FileInfo(ZipToUnzip).Delete();
        logInfo($"Successfully updated {ModName}");
        return true;
    }
}