

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BepInEx.Logging;
using IL.Menu;
using IL.MoreSlugcats;

namespace ModsUpdater;

public static class Utils
{


    public static class VersionManager
    {
        // Only supports . as a separator !
        /// <summary>
        /// Compares two version strings. Dots are separators. Letters are supported as the last char.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 if any error occurs
        /// 0 if versions are the same
        /// 1 if a > b
        /// 2 if b > a
        /// </returns>
        public static StatusCode CompareVersions(string a, string b)
        {
            //Console.WriteLine("Comparing " + a + " to " + b);
            if (a == null || b == null || !CheckVersionValidity(a) || !CheckVersionValidity(b))
            {

                return StatusCode.InvalidParameters;
            }
            try {
                List<Int32> versionA = VersionToList(a);
                List<Int32> versionB = VersionToList(b);

                for (int i = 0; i < Math.Max(versionA.Count, versionB.Count); i++)
                {
                    if (versionA.Count > i && versionB.Count > i)
                    {
                        if (versionB[i] > versionA[i]) return StatusCode.UpdateAvailable;
                        else if (versionA[i] > versionB[i]) return StatusCode.AheadOfRemote;
                    }
                    else if (versionB.Count > versionA.Count) return StatusCode.UpdateAvailable;
                    else if (versionB.Count < versionA.Count) return StatusCode.AheadOfRemote;
                }
                return StatusCode.Success;
            }
            catch
            {
                return StatusCode.GenericError;
            }
            

            
        }

        private static List<int> VersionToList(string a)
        {
            List<int> res = new();
            string[] splittedA = a.Split('.');
            foreach (string s in splittedA)
            {
                if (Int32.TryParse(s, out int localVersion))
                {
                    res.Add(localVersion);
                }
                else
                {
                    //separate string from rest
                    // in no case should the version letter(s) be before an int!!
                    string fp = "";
                    string dp = "";
                    for (int i = 0; i < s.Length; i++)
                    {
                        char c = s[i];
                        if (c >= 48 /*0*/ && c <= 57 /*9*/)
                        {
                            fp += c.ToString();
                        }
                        else if (c >= 97 /*a*/ && c <= 122 /*z*/)
                        {
                            if (i != s.Length - 1) throw new FormatException("version string was not in correct format: found number after letter");
                            dp = (c - 96).ToString();
                        }
                        else throw new FormatException("version string was not in correct format: found foreign character");
                    }
                    if (fp.Length != 0) res.Add(Int32.Parse(fp));
                    if (dp.Length != 0) res.Add(Int32.Parse(dp));
                }
            }
            return res;
        }

        /// <summary>
        /// checks version validity. Numbers only; one letter allowed at the end. Separator is .
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool CheckVersionValidity(string s)
        {
            if (s.Length == 0) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= 48 /*0*/ && c <= 57 /*9*/ || c >= 97 /*a*/ && c <= 122 /*z*/ || c == '.')
                {
                    if (c >= 97 /*a*/ && c <= 122 /*z*/)
                    {
                        if (i != s.Length - 1) return false;
                    }
                }
                else return false;
            }
            return true;
        }


        public static async Task<(StatusCode, string?)> getModVersionUrl(ModManager.Mod mod)
        {
            string modinfoPath = Path.Combine(mod.path, "modinfo.json");
            Dictionary<string, object> dictionary = File.ReadAllText(modinfoPath).dictionaryFromJson();
            if (dictionary == null)
            {
                return (StatusCode.ModInfoNotReadable, null);
            }
            if (dictionary.ContainsKey("update_url"))
            {
                Debug.Log("found an update url !");
                return (StatusCode.Success, dictionary["update_url"].ToString());
            }
            else
            {
                return (StatusCode.NoUpdateUrlKey, null);
            }
        }

    }


    public class FileManager
    {

        public static bool offlineMode;



public static void RecursiveDelete(DirectoryInfo baseDir)
{
    if (!baseDir.Exists)
    {
        Console.WriteLine($"Directory does not exist: {baseDir.FullName}");
        return;
    }

    try
    {
        // Delete all files in the directory
        foreach (FileInfo file in baseDir.GetFiles())
        {
            try
            {
                file.Delete();
                Console.WriteLine($"Deleted file: {file.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file {file.FullName}: {ex.Message}");
            }
        }

        // Delete all subdirectories recursively
        foreach (DirectoryInfo subdirectory in baseDir.GetDirectories())
        {
            try
            {
                RecursiveDelete(subdirectory);
                Console.WriteLine($"Deleted subdirectory: {subdirectory.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting subdirectory {subdirectory.FullName}: {ex.Message}");
            }
        }

        // Delete the current directory
        try
        {
            baseDir.Delete(true);
            Console.WriteLine($"Deleted directory: {baseDir.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting directory {baseDir.FullName}: {ex.Message}");
        }
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine($"Access denied: {baseDir.FullName}");
    }
}


        public static async Task<StatusCode> IsRemoteFileNewer(string url)
        {
            string tempFilePath = Path.Combine(ModsUpdater.THISMODPATH, url.Split('/').Last()); // something.zip
            Console.WriteLine($"is rm file newer ? {url} compared to {tempFilePath}");
            if (!File.Exists(tempFilePath)) return StatusCode.LocalFileNotFound;


            try
            {
                using HttpClient client = new();

                // Check if the file exists and get its last modified time
                DateTime lastModified = File.GetLastWriteTime(tempFilePath);

                // Send a HEAD request to get the last modified date of the remote file
                client.DefaultRequestHeaders.Add("If-Modified-Since", lastModified.ToString("R"));
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    return StatusCode.LocalFileUpToDate; // File is not modified, no download needed
                }
                else return StatusCode.UpdateAvailable;
            }
            catch (System.Net.Sockets.SocketException nex)
            {
                offlineMode = true;
                UnityEngine.Debug.LogError(nex);
                return StatusCode.Offline; // Offline
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
                return StatusCode.GenericError;
            }
        }



        public static async Task<StatusCode> DownloadFileAsync(string url, string localPath)
        {
            Console.WriteLine($"dwasync {url} {localPath}");
            if (offlineMode) return StatusCode.GenericError;
            using HttpClient client = new HttpClient();
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

                return StatusCode.Success;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode.GenericError;
            }
        }




    public static async Task<long> GetRemoteFileSize(string url)
        {
            if (offlineMode) return 0;
            using HttpClient client = new HttpClient();
            try
            {
                // Send a GET request to the specified URL
                HttpResponseMessage headResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                headResponse.EnsureSuccessStatusCode(); // Throw if not a success code.

                // Read the response content as a byte array
                long totalSize = headResponse.Content.Headers.ContentLength ?? -1;
                return totalSize;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }
        
        /// <summary>
        /// updates a mod. takes url of the zip of the mod
        /// and the path where the current mod is stored
        /// deletes mod direcotory and recreates one
        /// </summary>
        /// <param name="url"></param>
        /// <param name="modPath"></param>
        /// <returns></returns>
        public static async Task<StatusCode> GetUpdateAndUnzip(string url, string modPath)
        {
            Console.WriteLine("Trying to update " + url +" into "+modPath);
            if (offlineMode) return StatusCode.Offline;
            if (url == null || modPath == null) return StatusCode.InvalidParameters;
            if (!url.EndsWith("zip")) return StatusCode.UpdateUrlNotZip;
            var targetModPath = new DirectoryInfo(modPath);

            if (!(targetModPath.Parent.FullName == ModsUpdater.MODSPATH))
            {
                return StatusCode.InvalidTargetPath;
            }

            string fileName = url.Split('/').Last();

            string tempFilePath = Path.Combine(ModsUpdater.THISMODPATH,fileName);

            StatusCode dlres = await DownloadFileAsync(url, tempFilePath);
            if (dlres != StatusCode.Success) return dlres; // dl failed

            RecursiveDelete(targetModPath);
            // maybe more structure checks should be done. Meh.
            if (IsModinfoTopLevel(tempFilePath) == true) //  extract to mods/ModName/
            {
                string newModDir = Path.Combine(ModsUpdater.MODSPATH, fileName.Replace(".zip", ""));
                Directory.CreateDirectory(newModDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, newModDir);

            }
            else System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, ModsUpdater.MODSPATH);

            return StatusCode.Success;
        }

        public static bool IsModinfoTopLevel(string zipFilePath)
        {

            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.Equals("modinfo.json", StringComparison.OrdinalIgnoreCase) && entry.FullName.Split('/').Length == 1) // Check if it's not a directory & not in a dir
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

    public static UnityEngine.Color C2c(System.Drawing.Color c) {
        return new UnityEngine.Color(c.A, c.R, c.G, c.B);
    }

  public static string GetErrorMessage(StatusCode code)
    {
        return ErrorMessages[code];
    }

 private static readonly Dictionary<StatusCode, string> ErrorMessages = new Dictionary<StatusCode, string>
    {
        // { StatusCode.Success, "Success" },
        { StatusCode.LocalFileUpToDate, "Local file is up-to-date" },
        { StatusCode.UpdateAvailable, "A remote update is available" },
        { StatusCode.GenericError, "Generic error. Check console logs." },
        { StatusCode.InvalidParameters, "Supplied parameters are not valid" },
        { StatusCode.NetworkRequestFailed, "Network request failed for an unknown reason." },
        { StatusCode.LocalFileNotFound, "Local file does not exist" },
        { StatusCode.Offline, "Offline" },
        { StatusCode.UpdateUrlNotZip, "Update URL is not a zip" },
        { StatusCode.ModInfoNotFound, "Could not find modinfo" },
        { StatusCode.ModInfoNotReadable, "Modinfo was not readable or JSON-parsable" },
        { StatusCode.NoUpdateUrlKey, "No updateUrl key found" },
        { StatusCode.InvalidTargetPath, "Target path to delete and re-create seems wrong" },
        { StatusCode.AheadOfRemote, "Local file version is ahead of remote" },
    };

    public enum StatusCode
    {
        Success = 0,
        GenericError = -1,

        LocalFileNotFound,
        LocalFileUpToDate,
        UpdateAvailable,
        
        InvalidParameters,
        UpdateUrlNotZip,
        InvalidTargetPath,
        
        NetworkRequestFailed,
        Offline,
        
        ModInfoNotFound,
        ModInfoNotReadable,
        
        NoUpdateUrlKey,
        AheadOfRemote,
    }
}

