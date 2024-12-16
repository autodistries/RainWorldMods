

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        public static int CompareVersions(string a, string b)
        {
            //Console.WriteLine("Comparing " + a + " to " + b);
            if (a == null || b == null || !CheckVersionValidity(a) || !CheckVersionValidity(b))
            {

                return -1;
            }
            try {
                List<Int32> versionA = VersionToList(a);
                List<Int32> versionB = VersionToList(b);

                for (int i = 0; i < Math.Max(versionA.Count, versionB.Count); i++)
                {
                    if (versionA.Count > i && versionB.Count > i)
                    {
                        if (versionB[i] > versionA[i]) return 1;
                        else if (versionA[i] > versionB[i]) return 2;
                    }
                    else if (versionB.Count > versionA.Count) return 1;
                    else if (versionB.Count < versionA.Count) return 1;
                }
                return 0;
            }
            catch
            {
                return -1;
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


        public static async Task<(int, string?)> getModVersionUrl(ModManager.Mod mod)
        {
            string modinfoPath = Path.Combine(mod.path, "modinfo.json");
            if (!File.Exists(modinfoPath)) return (-20, null);
            Dictionary<string, object> dictionary = File.ReadAllText(modinfoPath).dictionaryFromJson();
            if (dictionary == null)
            {
                return (-21, null);
            }
            if (dictionary.ContainsKey("update_url"))
            {
                Debug.Log("found an update url !");
                return (0, dictionary["update_url"].ToString());
            }
            else
            {
                return (-22, null);
            }
        }

    }


    public class FileManager
    {

        public static bool offlineMode;



        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            
                foreach (var file in baseDir.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (var item in baseDir.EnumerateDirectories())
                {

                    RecursiveDelete(item);


                }
                baseDir.Delete(true);

            

    }
    public static async Task<int> DownloadFileIfNewerAsync(string url, string localPath)
    {
        if (offlineMode) return -10;
        using HttpClient client = new HttpClient();
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
                            return 1; // Local file is up to date
                        }
                    }
                    else
                    {
                        // If the local file does not exist, download it
                        int dlres = await DownloadFileAsync(url, localPath);
                        if (dlres != 0) return dlres;
                        // Save the ETag
                        File.WriteAllText(localPath + ".etag", remoteETag);
                        return 0; // File was downloaded
                    }
                }
                else
                {
                    return -12; // Error occurred
                }
            }
            catch (Exception ex)
            {
                return -11; // Error occurred
            }
        }

        public static async Task<int> DownloadFileAsync(string url, string localPath)
        {
            if (offlineMode) return -10;
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

                    return 0;

                }
                catch (Exception ex)
                {
                    ModsUpdater.logger.LogError(ex);
                    return -3;
                }
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
        public static async Task<int> GetUpdateAndUnzip(string url, string modPath)
        {
            Console.WriteLine("Trying to update " + url +" "+modPath);
            if (offlineMode) return -10;
            if (url == null || modPath == null) return -2;
            if (!url.EndsWith("zip")) return -13;
            var targetModPath = new DirectoryInfo(modPath);

            if (!(targetModPath.Parent.FullName == ModsUpdater.MODSPATH))
            {

                return -23;
            }

            string fileName = url.Split('/').Last();

            string tempFilePath = Path.Combine(ModsUpdater.THISMODPATH, "." + fileName);

            int dlres = await DownloadFileAsync(url, tempFilePath);
            if (dlres != 0) return dlres; // dl failed

            Console.WriteLine(ModsUpdater.MODSPATH);

            RecursiveDelete(targetModPath);
            if (CountTopLevelFilesInZip(tempFilePath) != 0) // this means that at least modinfo.json is there. So extract to mods/ModName/
            {
                string newModDir = Path.Combine(ModsUpdater.MODSPATH, fileName.Replace(".zip", ""));
                Directory.CreateDirectory(newModDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, newModDir);

            }
            else // there is only a dir insize top level of zip. extract to mods/


                System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, ModsUpdater.MODSPATH);

            return 0;
        }

        public static int CountTopLevelFilesInZip(string zipFilePath)
        {
            int fileCount = 0;

            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.Name) && entry.FullName.Split('/').Length == 1) // Check if it's not a directory & not in a dir
                    {
                        fileCount++;
                    }
                }
            }

            return fileCount;
        }

    }
    public static class StatusInfo
    {
        public static string get(int StatusID)
        {
            string statusClass = "";
            string statusBody = "";

            if (StatusID >= 0)
            {
                if (StatusID < 10) statusClass = "General";

            }
            else
            {
                if (StatusID > -10) statusClass = "general";
                else if (StatusID > -20) statusClass = "Networking";
                else if (StatusID > -30) statusClass = "File handling";
            }

            switch (StatusID)
            {
                case 1: statusBody = "local file is up-to-date"; break;
                case 0: statusBody = "Success"; break;

                case -1: statusBody = "generic error"; break;
                case -2: statusBody = "supplied parameters are not valid"; break;
                case -3: statusBody = "network request failed, or probelms writing file"; break;

                case -10: statusBody = "Offline"; break;
                case -13: statusBody = "update url is not a zip"; break;
                case -11: statusBody = "could not request etag. offline ?"; break;
                case -12: statusBody = "no etag found in response"; break;



                case -20: statusBody = "Could not find modinfo"; break;
                case -21: statusBody = "modinfo was not readable or json-parsable"; break;
                case -22: statusBody = "no updateUrl key found"; break;
                case -23: statusBody = "target path to delete and re-create seems wrong"; break;
            }




            return statusClass + " : " + statusBody;
        }
    }


}