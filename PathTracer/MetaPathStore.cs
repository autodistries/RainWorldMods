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

    public static string targetStorageFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "tracker.json");

    static MetaPathStore()
    {
        // adds the TypeDescriptor for string -> Name
        TypeDescriptor.AddAttributes(
        typeof(SlugcatStats.Name),
        new TypeConverterAttribute(typeof(SlugcatNameTypeConverter))
        );

        // NewtonSoft's serialization settings
        serializerSettings = new JsonSerializerSettings();
        serializerSettings.Converters.Add(new PositionEntryJsonConverterNewton());
        serializerSettings.Formatting = Formatting.Indented;


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
    public async static void WriteColdFiles()
    {
        if (!modifiedSinceLastWrite) return;
        if (!ModOptions.doWriteData.Value) return;
        else modifiedSinceLastWrite = false;
        Console.WriteLine($"WRITER going to write");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        Console.WriteLine($"WRITER description : {DescribeData()}");
                        string json = JsonConvert.SerializeObject(data, serializerSettings);
                        Console.WriteLine($"WRITER Serialized data gives: \n{json}");
                        File.WriteAllText(targetStorageFile, json);
                    }
                });
        stopwatch.Stop();
        Console.WriteLine($"WRITER took {stopwatch.ElapsedMilliseconds}ms");
    }


    /// <summary>
    /// Loads Tracker data into memory. COmpletely overwrites previous data. SHuold be ran only once, early
    /// </summary>
    public async static Task<Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>>> TryLoadFromCold(bool inplace = true)
    {
        modifiedSinceLastWrite = false;
        Console.WriteLine("READER going to read");
        Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> outData = [];
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
                {
                    lock (_lock)
                    {

                        if (File.Exists(targetStorageFile))
                        {
                            outData = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>>>(File.ReadAllText(targetStorageFile), serializerSettings);

                            Console.WriteLine($"READER Cold data: \n{outData}\n{DescribeDataFriendly(outData)}");
                        }
                        Console.WriteLine("READER tried LoadFromCold.");
                        if (inplace) data = outData;
                    }
                });
        stopwatch.Stop();
        Console.WriteLine($"READER took {stopwatch.ElapsedMilliseconds}ms");
        if (inplace) return null;
        return outData;



    }


    /// <summary>
    /// Puts data in a string/jsonified manner, for debug purposes. THis shows the current data object.
    /// </summary>
    /// <returns></returns>
    public static string DescribeData()
    {
        return DescribeData(data);
    }

    public static string DescribeDataFriendly(Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> d)
    {
        string r = "File: " + targetStorageFile + "\n";
        if (File.Exists(targetStorageFile)) r += "File size is " + new FileInfo(targetStorageFile).Length / 1024f + "kB\n";
        else r += "Files does not exist\n";
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


    /// <summary>
    /// Puts data dd in a string/jsonified manner, for debug purposes.
    /// </summary>
    /// <param name="dd"></param>
    /// <returns></returns>
    private static string DescribeData(Dictionary<int,
                Dictionary<SlugcatStats.Name,
                    Dictionary<string,
                        List<PositionEntry>>>> dd)
    {
        string r = "";
        foreach (var level1 in dd)
        {
            r += level1.Key + " {\n";
            foreach (var level2 in level1.Value)
            {
                r += " " + level2.Key + " {\n";
                foreach (var level3 in level2.Value)
                {
                    r += "  " + level3.Key + " {\n";
                    foreach (var level4 in level3.Value)
                    {
                        r += "   " + level4.ToStringStore() + "\n";
                    }
                    r += "  }\n";
                }
                r += " }\n";
            }
            r += "}\n";
        }

        return r;
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

    public async static Task<Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>>> TryLoadFromColdCustom(bool inplace = true)
    {
        modifiedSinceLastWrite = false;
        Console.WriteLine("READER custom going to readdd");
        Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> outData = [];
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (File.Exists(targetStorageFile + "2"))
                        {
                            // Console.WriteLine("file exists00");
                            outData = DeserializeCustom(targetStorageFile + "2");
                        }
                        else Console.WriteLine("Custom logger said no file");
                        // Console.WriteLine("READER tried LoadFromCold.");
                        // if (inplace) data = outData;
                    }
                });
        Console.WriteLine($"READER Cold data: \n{outData}\n{DescribeDataFriendly(outData)}");
        stopwatch.Stop();
        Console.WriteLine($"READER custom took {stopwatch.ElapsedMilliseconds}ms");
        if (inplace) return null;
        return outData;



    }

    private static Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> DeserializeCustom(string path)
    {
        // Console.WriteLine("hi from deserailaizer");


        Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>>> result = new();

        string[] lines = File.ReadAllLines(path);
        // Console.WriteLine("hi 2 from deserailaizer");

        int currentLevel1Key = 0;
        SlugcatStats.Name level2KeyName = null;
        string level3KeyString = "";

        try
        {
            // Console.WriteLine("We have lines "+lines.Length);

            foreach (string line in lines)
            {
                // Console.WriteLine("trating line "+line);
                string trimmedLine = line.Trim();
                if (trimmedLine.Length == 0) continue;
                if (trimmedLine[0] == '#')
                {
                    // Level 1 key, saveSlot
                    currentLevel1Key = int.Parse(trimmedLine.Substring(1));
                    // Console.WriteLine("is a save slot "+currentLevel1Key);
                    result.Add(currentLevel1Key, new());
                }
                else if (trimmedLine[0] == '_')
                {
                    // Level 2 key, SLugcat
                    string level2Key = trimmedLine.Substring(1);
                    level2KeyName = ExtEnumBase.Parse(typeof(SlugcatStats.Name), level2Key, false) as SlugcatStats.Name;
                    result[currentLevel1Key].Add(level2KeyName, new());
                    // Console.WriteLine("is a slugcat" + level2KeyName);
                }
                else if (trimmedLine[0] == '@')
                {
                    // Level 3 key, Region
                    level3KeyString = trimmedLine.Substring(1);
                    result[currentLevel1Key][level2KeyName].Add(level3KeyString, new());
                    // Console.WriteLine("is a region +");
                }
                else if (trimmedLine.Split(',').Length == 3)
                {
                    // Level 4 value
                    // Console.WriteLine("is a position");

                    string[] parts = trimmedLine.Split(',').Select(p => p.Trim()).ToArray();
                    PositionEntry positionEntry = new(int.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                    result[currentLevel1Key][level2KeyName][level3KeyString].Add(positionEntry);
                }
                else Console.WriteLine("Unknown line format : " + line);
            }
            // Console.WriteLine("Exited foreeach");
        }
        catch (System.Exception)
        {

            throw;
        }

        // Console.WriteLine("bye from deserailaizer");
        return result;
    }


    internal static void ResetData()
    {
        data = new();
    }

    internal static async Task WriteColdFileCustomAsync()
    {
        if (!modifiedSinceLastWrite) return;
        if (!ModOptions.doWriteData.Value) return;
        else modifiedSinceLastWrite = false;
        Console.WriteLine($"WRITER CUSTOM going to write");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        Console.WriteLine($"WRITER description : {DescribeDataFriendly()}");
                        string json = SerializeCustom(data);
                        // Console.WriteLine($"WRITER Serialized data gives: \n{json}");
                        File.WriteAllText(targetStorageFile + "2", json);
                    }
                });
        stopwatch.Stop();
        Console.WriteLine($"WRITER took {stopwatch.ElapsedMilliseconds}ms");
    }
}




/// <summary>
/// Allows Newtonsoft JSON to convert a PositionEntry to serialized, and vice-versa
/// </summary>
public class PositionEntryJsonConverterNewton : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(PositionEntry);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Expecting a string.
        if (reader.TokenType != JsonToken.String)
            throw new JsonSerializationException("Expected string");

        string value = reader.Value.ToString();
        // Split the value, e.g. "roomNumber, pos.x, pos.y"
        var parts = value.Split(',');
        if (parts.Length != 3)
            throw new JsonSerializationException("Invalid format for PositionEntry");

        if (!int.TryParse(parts[0].Trim(), out int roomNumber))
            throw new JsonSerializationException("Invalid room number");
        if (!float.TryParse(parts[1].Trim(), out float x))
            throw new JsonSerializationException("Invalid x-coordinate");
        if (!float.TryParse(parts[2].Trim(), out float y))
            throw new JsonSerializationException("Invalid y-coordinate");

        return new PositionEntry(roomNumber, new Vector2(x, y));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        PositionEntry entry = (PositionEntry)value;
        // Write as a string using ToStringStore.
        writer.WriteValue(entry.ToStringStore());
    }


}


/// <summary>
/// Dict keys are treated differently than other stuff so this is useless
/// </summary>
// public class SlugcatNameJsonConverter : JsonConverter
// {

//       public override bool CanConvert(Type objectType)
//     {
//         return objectType == typeof(SlugcatStats.Name);
//     }
//     public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//     {
//         // Write out the string representation of the name.
//         // Here, we assume that the override of ToString() or a property returns the underlying string.
//         writer.WriteValue(value.ToString());
//     }


//     public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//     {
//         if (reader.TokenType != JsonToken.String)
//             throw new JsonException("Expected string value for SlugcatStats.Name");

//         string nameString = reader.Value.ToString();
//         return (SlugcatStats.Name)ExtEnumBase.Parse(typeof(SlugcatStats.Name), nameString, false);
//     }
// }



/// <summary>
/// This TypeConverter allows to convert a string to a registered SlugcatState.Name
/// </summary>
public class SlugcatNameTypeConverter : TypeConverter
{
    // Indicates whether conversion from another type is possible.
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }


    // Convert from a string to SlugcatStats.Name using ExtEnumBase.Parse.
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string nameString)
        {
            object parsed = ExtEnumBase.Parse(typeof(SlugcatStats.Name), nameString, false);
            if (parsed is SlugcatStats.Name result)
                return result;
            throw new ArgumentException($"Unable to parse {nameString} into a SlugcatStats.Name.");
        }
        return base.ConvertFrom(context, culture, value);
    }


}
