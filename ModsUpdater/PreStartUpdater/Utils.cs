using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HarmonyLib;
using static System.Diagnostics.Trace;


namespace PreStartUpdater;

public static class FileManager

{
    public static bool RecursiveDelete(DirectoryInfo baseDir, bool fake=false)
    {
        


        if (!baseDir.Exists)
        {
            Logs.logInfo($"Directory does not exist: {baseDir.FullName}");
            return false;
        }
        if (fake) {
            Logs.logInfo($"We would have rdeleted {baseDir.FullName}");
            return true;

        }

        try
        {
            // Delete all files in the directory
            foreach (FileInfo file in baseDir.GetFiles())
            {
                try
                {
                    if (!file.Exists) Logs.logInfo("We will be deleting a file that does not exists");
                    file.Refresh();
                    file.Delete();
                    // Logs.logInfo($"Deleted file: {file.FullName}");
                    file.Refresh();

                    if (file.Exists)
                    {
                        Logs.logInfo($"(actually we did not dzelete {file.FullName} lol)");
                        for (int i = 0; i < 5; i++)
                        {
                            Logs.logInfo("Tying again... " + i);
                            file.Delete();
                        }
                        if (file.Exists) Logs.logInfo("giving up");
                    }

                }
                catch (System.Exception ex)
                {
                    Logs.logInfo($"Error deleting file {file.FullName}: {ex.Message}");
                    return false;
                }
            }

            // Delete all subdirectories recursively
            foreach (DirectoryInfo subdirectory in baseDir.GetDirectories())
            {
                try
                {
                    RecursiveDelete(subdirectory);
                }
                catch (System.Exception ex)
                {
                    Logs.logInfo($"Error deleting subdirectory {subdirectory.FullName}: {ex.Message}");
                    return false;
                }
            }

            // Delete the current directory
            try
            {
                baseDir.Delete(true);
            }
            catch (System.Exception ex)
            {
                Logs.logInfo($"Error deleting directory {baseDir.FullName}: {ex.Message}");
                return false;
            }
        }
        catch (System.UnauthorizedAccessException)
        {
            Logs.logInfo($"Access denied: {baseDir.FullName}");
            return false;
        }
        return true;
    }

    public static bool UnzipZipToModsFolder(string zipPath, string forcedName="")
    {

        
        try
        {
            Logs.logInfo(PreStartUpdaterPatcher.MODSFOLDER.FullName);
            if (IsModinfoTopLevel(zipPath) == true) //  extract to mods/ModName/
            {
                string newModDir = Path.Combine(PreStartUpdaterPatcher.MODSFOLDER.Parent.FullName, (forcedName == "") ? zipPath.Split('/').Last().Split('\\').Last().Replace(".zip", "") : forcedName);
                Logs.logInfo($"new mod dit {newModDir}");
                Directory.CreateDirectory(newModDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, newModDir, true);

            }
            else {
                // forcename not supported
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, PreStartUpdaterPatcher.MODSFOLDER.Parent.FullName, true);}
        }
        catch (Exception ex)
        {
            Logs.logInfo($"Error when uncompressing {zipPath}: {ex}");
            return false;
        }
        return true;

    }


    private static bool IsModinfoTopLevel(string zipFilePath)
    {

        using var archive = ZipFile.OpenRead(zipFilePath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.Name.Equals("modinfo.json", System.StringComparison.OrdinalIgnoreCase) && entry.FullName.Split('/').Length == 1) // Check if it's not a directory & not in a dir
            {
                return true;
            }
        }

        return false;
    }

}


public static class Logs {
        public static void logInfo(string message) {
        WriteLine("[ModsUpadter] " + message);
    }

        public static void logInfo(object value) {
        WriteLine("[ModsUpadter] " + value.ToString());
    }
}