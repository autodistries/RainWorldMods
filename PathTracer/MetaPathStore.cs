using System;
using System.Collections.Generic;
using System.Linq;
using static PathTracer.SlugcatPath;

using Newtonsoft.Json;
using UnityEngine;
using System.IO;



using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;
namespace PathTracer;

public static class MetaPathStore
{
    static readonly JsonSerializerSettings serializerSettings;

    static Dictionary<int,
               Dictionary<SlugcatStats.Name,
                   Dictionary<string,
                       List<PositionEntry>>>> data = new();
    private static object _lock = new object();
    internal static bool modifiedSinceLastWrite = true;


    static MetaPathStore()
    {
    }

    /// <summary>
    /// Get the data for a saveslot, slugcat
    /// </summary>
    /// <param name="saveSlot"></param>
    /// <param name="slugcat"></param>
    /// <returns></returns>
    public static Dictionary<string, List<PositionEntry>> LoadDataFor(int saveSlot, SlugcatStats.Name slugcat)
    {
        Console.WriteLine($"LOADER {saveSlot} for {slugcat}");
        // saveSlot : manager.rainWorld.options.saveSlot
        if (!data.ContainsKey(saveSlot))
        { // no data has been loaded for this save slot yet
            Console.WriteLine("LOADER new saveslot " + saveSlot);
            data.Add(saveSlot, []);
        }
        if (data[saveSlot].TryGetValue(slugcat, out var slugcatGlobalData))
        { // data for this slugcat exists
          // if (slugcatGlobalData.TryGetValue(regionName, out var regionData)) return regionData; // region for this slugcat exists
          // else return []; // no hot data, return nothing
            Console.WriteLine("LOADER loaded " + slugcat + " length " + slugcatGlobalData.Count);

            return slugcatGlobalData;
        }

        data[saveSlot].Add(slugcat, new());

        Console.WriteLine($"LOADER new {slugcat}");

        return new(); // no cold data for this slugcat on this save slllllllllot
    }

    /// <summary>
    /// Stores the positions for a specific saveslot, slugcat and region
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="saveSlot"></param>
    /// <param name="slugcat"></param>
    /// <param name="region"></param>
    public static void StoreRegion(List<PositionEntry> positions, int saveSlot, SlugcatStats.Name slugcat, string region)
    {
        modifiedSinceLastWrite = true;
        Console.WriteLine($"SYNC attempt for saveSlot {saveSlot} slugcat {slugcat} region {region}");
        if (!data.ContainsKey(saveSlot)) data.Add(saveSlot, []);
        if (!data[saveSlot].ContainsKey(slugcat)) data[saveSlot].Add(slugcat, []);
        if (!data[saveSlot][slugcat].ContainsKey(region)) { data[saveSlot][slugcat].Add(region, positions); }
        else
        {
            if (data[saveSlot][slugcat][region].Count() == positions.Count() && positions.Count() != 0 && positions.FirstOrDefault().Equals(data[saveSlot][slugcat][region].FirstOrDefault()) && positions.LastOrDefault().Equals(data[saveSlot][slugcat][region].LastOrDefault()))
            {
                // No modifications
                modifiedSinceLastWrite = false;
                return;
            }
            data[saveSlot][slugcat][region] = positions;
        }

        Console.WriteLine($"SYNC synced {saveSlot}, {slugcat}, {region}, {positions.Count()} records to data blob");
    }



    public static void StoreSlugcat(Dictionary<string, List<PositionEntry>> slugcatData, int saveSlot, SlugcatStats.Name slugcat)
    {
        Console.WriteLine($"SYNC attempt for saveSlot {saveSlot} slugcat {slugcat}");
        modifiedSinceLastWrite = true;
        if (!data.ContainsKey(saveSlot)) data.Add(saveSlot, []);
        if (!data[saveSlot].ContainsKey(slugcat)) data[saveSlot].Add(slugcat, slugcatData);
        else
        {
            if (data[saveSlot][slugcat].GetHashCode() == slugcatData.GetHashCode())
            {
                // No modifications
                modifiedSinceLastWrite = false;
                return;
            }
            data[saveSlot][slugcat] = slugcatData;
        }

        Console.WriteLine($"SYNC synced {saveSlot}, {slugcat}, {slugcatData.Count()} regions to data blob");
    }


    /// <summary>
    /// write the data object to disk, if the option is on
    /// </summary>
    // public async static void WriteColdFiles()
    // {
    //     if (!modifiedSinceLastWrite) return;
    //     if (!ModOptions.doWriteData.Value) return;
    //     else modifiedSinceLastWrite = false;
    //     Console.WriteLine($"WRITER going to write");

    //     var stopwatch = new Stopwatch();
    //     stopwatch.Start();
    //     await Task.Run(() =>
    //             {
    //                 lock (_lock)
    //                 {
    //                     Console.WriteLine($"WRITER description : {DescribeData()}");
    //                     string json = JsonConvert.SerializeObject(data, serializerSettings);
    //                     Console.WriteLine($"WRITER Serialized data gives: \n{json}");
    //                     File.WriteAllText(targetStorageFile, json);
    //                 }
    //             });
    //     stopwatch.Stop();
    //     Console.WriteLine($"WRITER took {stopwatch.ElapsedMilliseconds}ms");
    // }


    /// <summary>
    /// Loads Tracker data into memory. COmpletely overwrites previous data. SHuold be ran only once, early
    /// </summary>
    // public async static Task<Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>>> TryLoadFromCold(bool inplace = true)
    // {
    //     modifiedSinceLastWrite = false;
    //     Console.WriteLine("READER going to read");
    //     Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> outData = [];
    //     var stopwatch = new Stopwatch();
    //     stopwatch.Start();
    //     await Task.Run(() =>
    //             {
    //                 lock (_lock)
    //                 {

    //                     if (File.Exists(targetStorageFile))
    //                     {
    //                         outData = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>>>(File.ReadAllText(targetStorageFile), serializerSettings);

    //                         Console.WriteLine($"READER Cold data: \n{outData}\n{DescribeDataFriendly(outData)}");
    //                     }
    //                     Console.WriteLine("READER tried LoadFromCold.");
    //                     if (inplace) data = outData;
    //                 }
    //             });
    //     stopwatch.Stop();
    //     Console.WriteLine($"READER took {stopwatch.ElapsedMilliseconds}ms");
    //     if (inplace) return null;
    //     return outData;



    // }



    public static string DescribeDataFriendly(Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> d)
    {
        string r = "";
        foreach (var level1 in d)
        {
            r += "---- start save slot " + level1.Key + "\n";
            foreach (var level2 in level1.Value)
            {
                r += "   " + level2.Key + " [";
                foreach (var level3 in level2.Value)
                {
                    if (r[r.Length - 1] != '[') r += ", ";
                    r += level3.Key + ":" + level3.Value.Count() + ":" + level3.Value.Select((el) => el.roomNumber).Distinct().Count();
                }
                r += "]\n";
            }
            r += "---- end save slot " + level1.Key + "\n";
        }

        return r;
    }


    public static string DescribeDataFriendly()
    {
        return DescribeDataFriendly(data);
    }




    private static string SerializeCustom(Dictionary<int,
                Dictionary<SlugcatStats.Name,
                    Dictionary<string,
                        List<PositionEntry>>>> dd)
    {
        string r = "";
        foreach (var level1 in dd)
        {
            r += "#" + level1.Key + "\n";
            foreach (var level2 in level1.Value)
            {
                r += "_" + level2.Key + "\n";
                foreach (var level3 in level2.Value)
                {
                    r += "@" + level3.Key + "\n";
                    foreach (var level4 in level3.Value)
                    {
                        r += level4.ToStringStore() + "\n";
                    }
                    r += "\n";
                }
                r += "\n";
            }
            r += "\n";
        }

        return r;
    }


    internal static void ResetData()
    {
        data = new();
    }

}


