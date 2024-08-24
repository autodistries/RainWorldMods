

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IL.Menu;
using IL.MoreSlugcats;

namespace ModsUpdater;

public static class Utils
{


    public static class VersionManager
    {
        // Only supports . as a separator !
        public static bool IsVersionGreater(string a, string b)
        {
            Console.WriteLine("Comparing "+a+" to "+b);
            if (a == null || b==null ||!CheckVersionValidity(a) || !CheckVersionValidity(b))
            {

                return false;
            }

            List<Int32> versionA = VersionToList(a);
            List<Int32> versionB = VersionToList(b);

            for (int i = 0; i < Math.Max(versionA.Count, versionB.Count); i++)
            {
                if (versionA.Count > i && versionB.Count > i)
                {
                    if (versionB[i] > versionA[i]) return true;
                    else if (versionA[i] > versionB[i]) return false;
                }
                else if (versionB.Count > versionA.Count && versionB[i] != 0) return true;
            }
            return false;
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
            if (s.Length==0) return false;
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

        public static async Task<int> DownloadFileAsync(string url, string localPath)
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

                    return 0;

                }
                catch (Exception ex)
                {
                    return -1;
                }
            }
        }

        public static async Task<int> GetUpdateAndUnzip(string url, string modPath)
        {
            if (!url.EndsWith("zip")) return -1;
            if (!(Directory.GetParent(Path.GetFullPath(modPath)).ToString() == ModsUpdater.MODSPATH))
            {
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
}