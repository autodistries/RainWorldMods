using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BepInEx.Logging;
using HarmonyLib;
using HUD;
using IL.MoreSlugcats;
using Menu;
using RWCustom;
using UnityEngine;

namespace PathTracer;

public class SlugcatPath
{
    public bool preparedForOnscreen = false;

    public Dictionary<SlugcatStats.Name, List<FSprite>> lines = new();
    // region, slugcat, position
    public static Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>> slugcatRegionalPositions = new();

    public Dictionary<int, Color> pupsColors = new();


    public Map map = null;


    public static int maxBackwardsRooms => ModOptions.maxRoomsToRememberPerRegion.Value;

    public string CurrentRegion
    {
        get => currentRegion;
        set
        {
            if (value != currentRegion)
            {
                foreach (var kpv in slugcatRegionalPositions)
                {
                    (var slugcar, var positions) =( kpv.Key, kpv.Value);
                    lastNRooms.ensureSlugcat(slugcar);
                    lastNRooms[slugcar] = positions.ensureRegion(value).Select((el) => el.roomNumber).Distinct().ToList();
                    Logger.LogInfo($"Updated current region, slugcar {slugcar} has {lastNRooms[slugcar].Count} records");
                }
                currentRegion = value;
            }
        }
    }


    public static void CycleTick()
    {

        Logger.LogInfo("Processing cycle tick !");

        var slugcats  = slugcatRegionalPositions.Keys.ToArray();


        foreach (var slugcat in slugcats)
        {
            var regions = slugcatRegionalPositions[slugcat].Keys.ToArray();
            foreach (var reigon in regions)
            {
                slugcatRegionalPositions[slugcat][reigon] = slugcatRegionalPositions[slugcat][reigon].Select(pos =>
                    {
                        pos.lastSprite=null;
                        pos.ageCycles++;
                        return pos;
                    }).Where(pos => pos.ageCycles <= ModOptions.maxCyclesToRemember.Value).ToList();

            }
        }


    }

    private static int currentCycle = 0;

    public Dictionary<SlugcatStats.Name, List<int>> lastNRooms = new();
    public static ManualLogSource Logger;
    internal bool cycleTick = false;

    private string currentRegion = null;


    /// <summary>
    /// This is for temporary data only. Hard disk saving is directly managed from MetaPathStore WriteColdFile
    /// This depends on the current map
    /// </summary>
    public enum MapMode
    {
        READONLY,//i.e. on a Regions map, or anything where player won't move
        WRITEREAD, // in game
        // WRITEONLY,
        NOTHING // to be interpreted as no read, no write on the map object
    }


    internal MapMode QueryMode(Map m = null)
    {
        m ??= map;
        if (m == null) return MapMode.NOTHING;
        Logger.LogDebug($"QueryMode said owner is {m?.hud?.owner?.GetOwnerType()}");
        if (!(m.hud.owner is FastTravelScreen or KarmaLadderScreen or Player)) return MapMode.NOTHING;
        if (ModOptions.doRecordData.Value && m.hud.owner is Player) return MapMode.WRITEREAD;
        return MapMode.READONLY;
    }





    public SlugcatPath()
    {
    }

    // public SlugcatPath(Dictionary<string, IEnumerable<PositionEntry>> positions, Map map) {
    //     foreach (var regPos in positions) {
    //         this.slugcatPositions.Add(regPos.Key, regPos.Value.ToList());
    //     }
    //     this.map = map;
    //     lastNRooms = slugcatPositions.regionDataOrNew(CurrentRegion).Select((el) => el.roomNumber).Distinct().ToList();
    // }

    public void SetNewMap(Map newMap)
    {
        if (newMap == null)
        {
            Logger.LogError("Tried to set new map but new map is null.");
            return;
        }

        clearLines();
        // clearPositions();
        map = newMap;

        CurrentRegion = newMap.RegionName;
        // Logger.LogInfo($"Loaded new map for sc {GetSlugcat()} in region {CurrentRegion}, {slugcatRegionalPositions.regionDataOrNew(CurrentRegion).Count} records, mode {QueryMode()}");

    }






    public void addNewPosition(SlugcatStats.Name slugcar, PositionEntry p)
    {
        if (map == null) return;
        var regionPositions = slugcatRegionalPositions.ensureSlugcat(slugcar).ensureRegion(CurrentRegion);
        Logger.LogInfo($"Added {SlugcatPath.slugcatRegionalPositions.ensureSlugcat(slugcar).ensureRegion(CurrentRegion).LastOrDefault()} reg {CurrentRegion} sc {slugcar}");
        
        regionPositions.Add(p);
        if (lastNRooms.ensureSlugcat(slugcar).LastOrDefault() != p.roomNumber || !lastNRooms[slugcar].Contains(p.roomNumber))
        {

            if (lastNRooms[slugcar].Remove(p.roomNumber))
            {
                Logger.LogInfo($"Removed room {p.roomNumber} to readdit");
            };
            lastNRooms[slugcar].Add(p.roomNumber);
            Logger.LogInfo($"appended {p.roomNumber}");
        }

        if (lastNRooms[slugcar].Count > maxBackwardsRooms)
        {
            int roomToRemove = lastNRooms[slugcar][0];
            Logger.LogInfo("Will be removing entries from room " + roomToRemove);
            regionPositions.FindAll((el) => el.roomNumber == roomToRemove).ForEach((trp) =>
            {
                if (trp.lastSprite != null)
                {
                    lines[slugcar].Remove(trp.lastSprite);
                    trp.lastSprite.RemoveFromContainer();
                    trp.lastSprite = null;
                }
                Logger.LogInfo($"Removed point {trp}");
                regionPositions.Remove(trp);
            });
            if (regionPositions.Count > 0 && regionPositions[0].lastSprite != null) // avoid keeping lines that point to a non existant origin
            {
                lines.ensureSlugcat(slugcar).Remove(regionPositions[0].lastSprite);
                regionPositions[0].lastSprite.RemoveFromContainer();
                regionPositions[0].lastSprite = null;
            }
            lastNRooms[slugcar].Remove(roomToRemove);
        }
        if (lines.ensureSlugcat(slugcar).Count != 0 && lines[slugcar].First().alpha != 0)
        {
            appendLastLine();
        }
    }

    public void clearPositions()
    {
        slugcatRegionalPositions = new();
    }

    public void clearLines()
    {
        foreach (var kpv in lines)
        {
            (var slugcat, var lins) = (kpv.Key, kpv.Value);
            lins.ForEach((line) => line.RemoveFromContainer());
            foreach (var aa in slugcatRegionalPositions)
            {
                foreach (var bbb in aa.Value)
                {
                    bbb.Value.ForEach((pos) => pos.lastSprite = null);
                }
            }
        }
        lines.Clear();

        Logger.LogInfo("Cleared lines");

    }

    private void appendLastLine()
    {
        if (map == null) return;
        if (!ModOptions.doShowData.Value) return;

        foreach (var kvp in slugcatRegionalPositions)
        {
            (var slugcat, var regPos) = (kvp.Key, kvp.Value);
            Color slugColor = PlayerGraphics.SlugcatColor(slugcat);
            var positions = regPos.ensureRegion(CurrentRegion);

            int resumePos = positions.Count;
            if (resumePos < 1) continue;

            PositionEntry lastP = positions[resumePos - 2];
            lastP.storedRealPos = null;

            PositionEntry p = positions[resumePos - 1];

            FSprite line = new FSprite("pixel")
            {
                anchorY = 0F,
                color = slugColor,
                alpha = 0f,
                scaleX = 2f,
            };


            line.SetPosition(lastP.GetPos(map));
            line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
            line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));

            lines.ensureSlugcat(slugcat).Add(line);
            Logger.LogInfo($"Adding single line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation} slug {slugcat} reg {CurrentRegion}");
            map.container.AddChild(line);



        }
    }

    public void appendNewLines()
    {
        if (map == null) return;
        if (!ModOptions.doShowData.Value) return;

        foreach (var kvp in slugcatRegionalPositions)
        {
            (var slugcat, var regPos) = (kvp.Key, kvp.Value);
            Color slugColor = PlayerGraphics.SlugcatColor(slugcat);
            var positions = regPos.ensureRegion(CurrentRegion);
            int resumePos = positions.FindIndex((el) => el.lastSprite == null && positions.IndexOf(el) != 0);
            if (resumePos == -1)
            {
                Logger.LogInfo($"APPENDnEWlINE RESUMEpOS {slugcat} WAS -1 in {CurrentRegion}, no new lines to append");
                continue;
            }

            PositionEntry lastP = positions[resumePos - 1];

            lastP.storedRealPos = null;

            for (int i = resumePos; i < positions.Count; i++)
            {
                PositionEntry p = positions[i];

                FSprite line = new FSprite("pixel")
                {
                    anchorY = 0F,
                    color = slugColor,
                    alpha = 0f,
                    scaleX = 2f,
                };

                line.SetPosition(lastP.GetPos(map));
                line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
                line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));

                lines.ensureSlugcat(slugcat).Add(line);
                // Logger.LogInfo($"Adding line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");
                map.container.AddChild(line);
                p.lastSprite = line;

                lastP = p;
            }
        }

    }


    internal void UpdateLines(float timeStacker)
    {

        // if (ModMainClass.debug) Logger.LogInfo("Drawing updating positions");
        if (map == null)
        {
            if (ModMainClass.debug) Logger.LogInfo("WE ARE NOT PREPARED to update lines, no map");
            return;
        }
        foreach (var kvp in slugcatRegionalPositions)
        {
            (var key, var value) = (kvp.Key, kvp.Value);
            if (lines.ensureSlugcat(key).Count == 0) continue; 
            Color slugColor = PlayerGraphics.SlugcatColor(key);

            var positionsData = value.ensureRegion(CurrentRegion);

            PositionEntry lastP = positionsData.FirstOrDefault();
            lastP.storedRealPos = null;
            if (ModMainClass.debug) Logger.LogInfo($"Updating positions of {lines.Count} lines w/ alpha {map.fade} sc {key} reg {CurrentRegion}");

            for (int i = 0; i < positionsData.Count - 2; i++)
            {
                PositionEntry p = positionsData[i + 1];
                if (p.lastSprite == null)
                {

                    Logger.LogWarning("Not updating a line because it did not exisst.");
                    continue;
                }
                FSprite line = p.lastSprite;

                p.storedRealPos = null;
                line.SetPosition(lastP.GetPos(map));
                line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
                line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));
                float alpha;
                if (lastP.roomNumber != p.roomNumber) // when two diff rooms !
                    alpha = Math.Max(map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, true), map.Alpha(map.mapData.LayerOfRoom(lastP.roomNumber), 1, true));
                else alpha = map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, compensateForLayersInFront: true);
                if (lastP.ageCycles != 0 && ModOptions.maxCyclesToRemember.Value != 0)
                    alpha*=1.0f - (lastP.ageCycles / (ModOptions.maxCyclesToRemember.Value + 1.0f));
                line.alpha = alpha;
                // Logger.LogInfo($"Moved line {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");

                lastP = p;
            }
        }


    }




    public class PositionEntry
    {
        public int roomNumber;
        public Vector2 pos;

        internal Vector2? storedRealPos = null;

        public FSprite lastSprite;

        public int ageCycles;


        public PositionEntry(int roomNumber, Vector2 pos)
        {
            this.roomNumber = roomNumber;
            this.pos = pos;
            ageCycles = 0;
        }



        public override string ToString()
        {
            return $"position {pos} in room {roomNumber}";
        }


        public Vector2 GetPos(Map map)
        {
            if (!storedRealPos.HasValue)
            {
                storedRealPos = map.RoomToMapPos(pos, roomNumber, 1);
            }
            return storedRealPos.Value;
        }

        public string ToStringStore()
        {
            return $"{roomNumber}, {pos.x}, {pos.y}";
        }

        public override bool Equals(object obj)
        {
            if (obj is not PositionEntry pe) return false;
            return pe.pos == this.pos && pe.roomNumber == this.roomNumber;
        }

        public override int GetHashCode()
        {
            int hashCode = -874986084;
            hashCode = hashCode * -1521134295 + roomNumber.GetHashCode() + pos.GetHashCode();
            return hashCode;
        }
    }


}


static class ExtensionsMethods
{

    /// <summary>
    ///  Returns the region data.
    /// If target region does not exist, creates it.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="region"></param>
    /// <returns></returns>
    public static List<SlugcatPath.PositionEntry> ensureRegion(this Dictionary<string, List<SlugcatPath.PositionEntry>> dictionary, string region)
    {
        if (!dictionary.ContainsKey(region)) dictionary.Add(region, new());
        return dictionary[region];
    }

    public static Dictionary<string, List<SlugcatPath.PositionEntry>> ensureSlugcat(this Dictionary<SlugcatStats.Name, Dictionary<string, List<SlugcatPath.PositionEntry>>> dict, SlugcatStats.Name slugcat)
    {
        if (!dict.ContainsKey(slugcat)) {dict.Add(slugcat, new()); Console.WriteLine("Added new slugcat in positions "+slugcat);}
        return dict[slugcat];
    }

    public static List<FSprite> ensureSlugcat(this Dictionary<SlugcatStats.Name, List<FSprite>> dict, SlugcatStats.Name slugcar)
    {
        if (!dict.ContainsKey(slugcar)) dict.Add(slugcar, new());
        return dict[slugcar];
    }

    public static List<int> ensureSlugcat(this Dictionary<SlugcatStats.Name, List<int>> dict, SlugcatStats.Name slugcat)
    {
        if (!dict.ContainsKey(slugcat)) dict.Add(slugcat, new());
        return dict[slugcat];

    }
}

