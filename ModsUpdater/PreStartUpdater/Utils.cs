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
            WriteLine($"Directory does not exist: {baseDir.FullName}");
            return false;
        }
        if (fake) {
            WriteLine($"We would have rdeleted {baseDir.FullName}");
            return true;

        }

        try
        {
            // Delete all files in the directory
            foreach (FileInfo file in baseDir.GetFiles())
            {
                try
                {
                    if (!file.Exists) WriteLine("We will be deleting a file that does not exists");
                    file.Refresh();
                    file.Delete();
                    // WriteLine($"Deleted file: {file.FullName}");
                    file.Refresh();

                    if (file.Exists)
                    {
                        WriteLine($"(actually we did not dzelete {file.FullName} lol)");
                        for (int i = 0; i < 5; i++)
                        {
                            WriteLine("Tying again... " + i);
                            file.Delete();
                        }
                        if (file.Exists) WriteLine("giving up");
                    }

                }
                catch (System.Exception ex)
                {
                    WriteLine($"Error deleting file {file.FullName}: {ex.Message}");
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
                    WriteLine($"Error deleting subdirectory {subdirectory.FullName}: {ex.Message}");
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
                WriteLine($"Error deleting directory {baseDir.FullName}: {ex.Message}");
                return false;
            }
        }
        catch (System.UnauthorizedAccessException)
        {
            WriteLine($"Access denied: {baseDir.FullName}");
            return false;
        }
        return true;
    }

    public static bool UnzipZipToModsFolder(string zipPath, string forcedName="")
    {

        
        try
        {
            WriteLine(PreStartUpdaterPatcher.MODSFOLDER.FullName);
            if (IsModinfoTopLevel(zipPath) == true) //  extract to mods/ModName/
            {
                string newModDir = Path.Combine(PreStartUpdaterPatcher.MODSFOLDER.Parent.FullName, (forcedName == "") ? zipPath.Split('/').Last().Split('\\').Last().Replace(".zip", "") : forcedName);
                WriteLine($"new mod dit {newModDir}");
                Directory.CreateDirectory(newModDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, newModDir, true);

            }
            else {
                // forcename not supported
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, PreStartUpdaterPatcher.MODSFOLDER.Parent.FullName, true);}
        }
        catch (Exception ex)
        {
            WriteLine($"Error when uncompressing {zipPath}: {ex}");
            return false;
        }
        return true;

    }


    private static bool IsModinfoTopLevel(string zipFilePath)
    {

        using (var archive = ZipFile.OpenRead(zipFilePath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Name.Equals("modinfo.json", System.StringComparison.OrdinalIgnoreCase) && entry.FullName.Split('/').Length == 1) // Check if it's not a directory & not in a dir
                {
                    return true;
                }
            }
        }

        return false;
    }

}