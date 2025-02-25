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
                       PositionEntry[]>>> data = new();
    private static object _lock = new object();
    private static bool modifiedSinceLastWrite = true;

    private static string targetStorageFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "tracker.json");

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
    public static Dictionary<string, PositionEntry[]> LoadDataFor(int saveSlot, SlugcatStats.Name slugcat)
    {
        Console.WriteLine($"LOADER {saveSlot} for {slugcat}");
        // saveSlot : manager.rainWorld.options.saveSlot
        if (!data.ContainsKey(saveSlot))
        { // no data has been loaded for this save slot yet
            Console.WriteLine("LOADER created saveslot " + saveSlot);
            data.Add(saveSlot, []);
        }
        if (data[saveSlot].TryGetValue(slugcat, out var slugcatGlobalData))
        { // data for this slugcat exists
          // if (slugcatGlobalData.TryGetValue(regionName, out var regionData)) return regionData; // region for this slugcat exists
          // else return []; // no hot data, return nothing
            Console.WriteLine("LOADER found existing value for  " + slugcat + " length " + slugcatGlobalData.Count);

            return slugcatGlobalData;
        }

        data[saveSlot].Add(slugcat, []);

        Console.WriteLine("LOADER new slugat key, returned empty bc TryLoad gave us null");

        return []; // no cold data for this slugcat on this save slllllllllot
    }

    /// <summary>
    /// Stores the positions for a specific saveslot, slugcat and region
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="saveSlot"></param>
    /// <param name="slugcat"></param>
    /// <param name="region"></param>
    public static void StoreRegion(IEnumerable<PositionEntry> positions, int saveSlot, SlugcatStats.Name slugcat, string region)
    {
        modifiedSinceLastWrite = true;
        if (!data.ContainsKey(saveSlot)) data.Add(saveSlot, []);
        if (!data[saveSlot].ContainsKey(slugcat)) data[saveSlot].Add(slugcat, []);
        if (!data[saveSlot][slugcat].ContainsKey(region)) {data[saveSlot][slugcat].Add(region, positions.ToArray()); return;}
        if (data[saveSlot][slugcat][region].Count() == positions.Count() && positions.Count() != 0 && positions.FirstOrDefault().Equals( data[saveSlot][slugcat][region].FirstOrDefault()) && positions.LastOrDefault().Equals(data[saveSlot][slugcat][region].LastOrDefault())) {
            // No modifications
            modifiedSinceLastWrite = false;
            return;
        }
        data[saveSlot][slugcat][region] = positions.ToArray();

        Console.WriteLine($"SYNC synced {saveSlot}, {slugcat}, {region}, {positions.Count()} records to data blob");
    }


    /// <summary>
    /// write the data object to disk. Might need to get threeeaded, and lock'd
    /// </summary>
    public async static void SyncColdFiles()
    {
        if (!modifiedSinceLastWrite) return;
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
    public async static void TryLoadFromCold()
    {
        modifiedSinceLastWrite = false;
        Console.WriteLine("READER going to read");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Run(() =>
                {
                        Console.WriteLine("in task !");
                    lock (_lock)
                    {
                        Console.WriteLine("In lock !");
                        Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, PositionEntry[]>>> outData = [];
                        if (File.Exists(targetStorageFile))
                        {
                        Console.WriteLine("in exists !");
                            outData = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<SlugcatStats.Name, Dictionary<string, PositionEntry[]>>>>(File.ReadAllText(targetStorageFile), serializerSettings);

                            Console.WriteLine($"READER Cold data: \n{outData}\n{DescribeData(outData)}");
                        }
                        Console.WriteLine("READER tried LoadFromCold.");
                        data = outData;
                    }
                });
        stopwatch.Stop();
        Console.WriteLine($"READER took {stopwatch.ElapsedMilliseconds}ms");


       
    }


    /// <summary>
    /// Puts data in a string/jsonified manner, for debug purposes. THis shows the current data object.
    /// </summary>
    /// <returns></returns>
    public static string DescribeData()
    {
        return DescribeData(data);
    }


    /// <summary>
    /// Puts data dd in a string/jsonified manner, for debug purposes.
    /// </summary>
    /// <param name="dd"></param>
    /// <returns></returns>
    private static string DescribeData(Dictionary<int,
                Dictionary<SlugcatStats.Name,
                    Dictionary<string,
                        PositionEntry[]>>> dd)
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
