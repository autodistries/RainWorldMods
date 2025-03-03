using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HUD;
using Menu;
using RWCustom;
using UnityEngine;
using Random = System.Random;

namespace PathTracer;

public class SlugcatPath
{
    public bool preparedForOnscreen = false;

    public Dictionary<SlugcatStats.Name, List<FSprite>> lines = new();
    // region, slugcat, position
    public static Dictionary<SlugcatStats.Name, Dictionary<string, List<PositionEntry>>> slugcatRegionalPositions = new();

    public Dictionary<int, Color> pupsColors = new();


    public Map map = null;

    internal static IEnumerable<SlugcatStats.Name> loadedSlugcars = [];

    internal static bool tickIsRecent = false;


    public static int maxBackwardsRooms => ModOptions.maxRoomsToRememberPerRegion.Value;
    private static Random _random = new Random();

    public string CurrentRegion
    {
        get => currentRegion;
        set
        {
            if (value != currentRegion)
            {
                foreach (var kpv in slugcatRegionalPositions)
                {
                    (var slugcar, var positions) = (kpv.Key, kpv.Value);
                    lastNRooms.ensureSlugcat(slugcar);
                    lastNRooms[slugcar] = positions.ensureRegion(value).Select((el) => el.roomNumber).Distinct().ToList();
                    Logger.LogInfo($"Updated current region, slugcar {slugcar} has {lastNRooms[slugcar].Count} records");
                }
                currentRegion = value;
            }
        }
    }


    public static void CycleTick(List<SlugcatStats.Name> names)
    {

        Logger.LogInfo($"Processing cycle tick for slugcats: {string.Join(", ", names)}");

        loadedSlugcars = names;
        tickIsRecent = true;


        foreach (var slugcat in slugcatRegionalPositions.Keys.ToArray().Intersect(loadedSlugcars))
        {
            var regions = slugcatRegionalPositions.ensureSlugcat(slugcat).Keys.ToArray();
            foreach (var reigon in regions)
            {
                slugcatRegionalPositions[slugcat][reigon] = slugcatRegionalPositions[slugcat][reigon].Select(pos =>
                    {
                        pos.lastSprite = null;
                        pos.ageCycles++;
                        return pos;
                    }).Where(pos => ModOptions.maxCyclesToRemember.Value == 0 || pos.ageCycles <= ModOptions.maxCyclesToRemember.Value).ToList();

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
        Logger.LogDebug($"QueryMode said map owner is {m?.hud?.owner?.GetOwnerType()}");
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
            Logger.LogInfo("Tried to set new map but new map is null. Did map get reset, or destroyed ?");
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
        if (tickIsRecent)
        {
            tickIsRecent = false;
            p.iCut = true;
        }
        if (!loadedSlugcars.Contains(slugcar))
        {
            Logger.LogError("WTF, new position for a non-active slugcar ??? " + slugcar);
            Logger.LogError("For comparison, loaded slugcats are " + string.Join(", ", loadedSlugcars));
        }
        var regionPositions = slugcatRegionalPositions.ensureSlugcat(slugcar).ensureRegion(CurrentRegion);


        // culling (if it is what I think it means)
        var a = SmartPositionsCuller(p, slugcar);
        if (a)
        {
            if (regionPositions.Last()?.marked == false)
            {
                Logger.LogError("WTF, switched the line but not marked ?");
            }
        }

        // end culling 


        if (ModMainClass.debug) Logger.LogInfo($"Added {regionPositions.LastOrDefault()} reg {CurrentRegion} sc {slugcar} icut{p.iCut}");

        ILimitMaximumRooms(slugcar, p, regionPositions);

        if (lines.ensureSlugcat(slugcar).Count != 0 && lines[slugcar].First().alpha != 0)
        {
            appendLastLine();
        }
    }

    private bool SmartPositionsCuller(PositionEntry p, SlugcatStats.Name slugcar)
    {
        var regionPositions = slugcatRegionalPositions[slugcar][CurrentRegion];
        regionPositions.Add(p);
        if (regionPositions.Count >= 3)
        {
            var lastThreePositions = regionPositions.Skip(regionPositions.Count - 3).ToList();
            if (lastThreePositions.Any((el) => el.iCut) || !lastThreePositions.Skip(1).All((el) => el.roomNumber == lastThreePositions[0].roomNumber)) return false;
            var v1 = lastThreePositions[1].pos - lastThreePositions[0].pos;
            var v2 = lastThreePositions[2].pos - lastThreePositions[1].pos;


            // float r1 = (v1.x!=0 && v2.x!=0) ? v2.x/v1.x : 0;
            // if (r1 <0.00001f || r1 * v1.y - v2.y < 0.00001f) {

            float crossProduct = v1.x * v2.y - v1.y * v2.x;
            if (Math.Abs(crossProduct) <  ModOptions.positionCullingPrecisionTimes1000.Value / 1000f) {

                Logger.LogDebug($"Removing intermediate position {lastThreePositions[1].pos} between {lastThreePositions[0].pos} and {lastThreePositions[2].pos}");
                if (lastThreePositions[1].lastSprite != null)
                {
                    // if (map != null)
                    // {
                    //     line.SetPosition(lastThreePositions[0].GetPos(map));
                    //     line.scaleY = Custom.Dist(lastThreePositions[0].GetPos(map), lastThreePositions[2].GetPos(map));
                    //     line.rotation = Custom.AimFromOneVectorToAnother(lastThreePositions[0].GetPos(map), lastThreePositions[2].GetPos(map));
                    //     Logger.LogDebug("tranfered ownership of line");
                    // }
                    // else

                    Logger.LogDebug("reased previous line");
                    lastThreePositions[1].lastSprite.RemoveFromContainer();
                    lines.ensureSlugcat(slugcar).Remove(lastThreePositions[1].lastSprite);

                    // regionPositions.Last().lastSprite = line;
                }
                lastThreePositions[0].storedRealPos = null;
                p.marked = true;
                if (!regionPositions.Remove(lastThreePositions[1]))
                {
                    Logger.LogError("erm wtf we could not remove middle point from positions ??");
                };
                return true;
            }
            return false;

        }
        else return false;
    }



    private void ILimitMaximumRooms(SlugcatStats.Name slugcar, PositionEntry p, List<PositionEntry> regionPositions)
    {
        if (lastNRooms.ensureSlugcat(slugcar).LastOrDefault() != p.roomNumber || !lastNRooms[slugcar].Contains(p.roomNumber))
        {

            if (lastNRooms[slugcar].Remove(p.roomNumber))
            {
                Logger.LogInfo($"Removed room {p.roomNumber} to readdit");
            };
            lastNRooms[slugcar].Add(p.roomNumber);
            Logger.LogInfo($"appended {p.roomNumber}");
        }

        if (lastNRooms[slugcar].Count > maxBackwardsRooms && maxBackwardsRooms!=0)
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
                if (ModMainClass.debug) Logger.LogInfo($"Removed point {trp}");
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



    /// <summary>
    /// This is used for adding the last point, for whenever the map is already being shown
    /// </summary>
    private void appendLastLine()
    {
        if (map == null) return;
        if (!ModOptions.doShowData.Value) return;

        foreach (var slugcat in slugcatRegionalPositions.Keys.Intersect(loadedSlugcars))
        {

            var regPos = slugcatRegionalPositions[slugcat];
            Color slugColor = (loadedSlugcars.Count() == 0) ? Color.red : PlayerGraphics.SlugcatColor(slugcat);
            var positions = regPos.ensureRegion(CurrentRegion);

            int resumePos = positions.Count;
            if (resumePos < 1) continue;

            PositionEntry p = positions[resumePos - 1];
            if (p.lastSprite != null) continue;
            PositionEntry lastP = positions[resumePos - 2];
            lastP.storedRealPos = null;

            if (p.iCut)
            {

                if (ModMainClass.debug) Logger.LogInfo("Not drawing new line because iCut !");

                continue;
            }

            FSprite line = new FSprite("pixel")
            {
                anchorY = 0F,
                color = slugColor,
                alpha = map.fade,
                scaleX = 2f,
            };


            line.SetPosition(lastP.GetPos(map));
            line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
            line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));

            lines.ensureSlugcat(slugcat).Add(line);
            if (ModMainClass.debug) Logger.LogInfo($"Adding (live) single line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation} slug {slugcat} reg {CurrentRegion}");
            map.container.AddChild(line);
            p.lastSprite = line;



        }
    }

    public void appendNewLines()
    {
        if (map == null) return;
        if (!ModOptions.doShowData.Value) return;

        foreach (var slugcat in slugcatRegionalPositions.Keys.Intersect(loadedSlugcars))
        {
            var regPos = slugcatRegionalPositions[slugcat];
            var positions = regPos.ensureRegion(CurrentRegion);
            int resumePos = positions.FindIndex((el) => el.lastSprite == null && positions.IndexOf(el) != 0 && el.iCut == false);
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
                Color slugColor = (loadedSlugcars.Count() == 0) ? Color.red : PlayerGraphics.SlugcatColor(slugcat);
                if (p.iCut)
                {
                    if (ModMainClass.debug) Logger.LogInfo($"Not drawing this line between {lastP} and {p} because iCut !");
                    lastP = p;

                    continue;
                }

                FSprite line = new FSprite("pixel")
                {
                    anchorY = 0F,
                    color = slugColor,
                    alpha = map.fade,
                    scaleX = 2f,
                };

                line.SetPosition(lastP.GetPos(map));
                line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
                line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));

                lines.ensureSlugcat(slugcat).Add(line);
                if (ModMainClass.debug) Logger.LogInfo($"Adding line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}  rel {lastP.pos}; {p.pos}; maked?:{p.marked}");
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
        Logger.LogDebug($"Updating lines ! reasons {map.lastFade != map.fade} || {map.depth != map.lastDepth} || ({map.fade != 0} && {map.panVel.magnitude >= 0.01}) || {map.visible}");
        foreach (var kvp in slugcatRegionalPositions)
        {
            if (/* (QueryMode() == MapMode.WRITEREAD) && */ !loadedSlugcars.Contains(kvp.Key)) continue; // do not show other slugcars' paths that are not playing if playing
            (var slugcat, var regionalPositions) = (kvp.Key, kvp.Value);
            if (lines.ensureSlugcat(slugcat).Count == 0) continue;

            var positionsData = regionalPositions.ensureRegion(CurrentRegion);

            PositionEntry lastP = positionsData.FirstOrDefault();
            lastP.storedRealPos = null;
            if (ModMainClass.debug) Logger.LogInfo($"Updating positions of {lines.ensureSlugcat(slugcat).Count} lines w/ alpha {map.fade} sc {slugcat} reg {CurrentRegion}");

            for (int i = 1; i < positionsData.Count; i++)
            {
                PositionEntry p = positionsData[i];
                if (p.iCut)
                {
                    if (ModMainClass.debug) Logger.LogWarning($"Not updating a line between {lastP} and {p} because icut");
                    p.storedRealPos = null;
                    lastP = p;
                    continue;

                }
                if (p.lastSprite == null)
                {

                    if (ModMainClass.debug) Logger.LogWarning($"Not updating a line between {lastP} and {p} because NO SPRITE");
                    continue;
                }
                FSprite line = p.lastSprite;

                // if (p.marked) line.color = Color.green;

                p.storedRealPos = null;
                line.SetPosition(lastP.GetPos(map));
                line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
                line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));
                float alpha;
                if (lastP.roomNumber != p.roomNumber) // when two diff rooms !
                    alpha = Math.Max(map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, true), map.Alpha(map.mapData.LayerOfRoom(lastP.roomNumber), 1, true));
                else alpha = map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, compensateForLayersInFront: true);
                if (lastP.ageCycles != 0 && ModOptions.maxCyclesToRemember.Value != 0)
                    alpha *= 1.0f - (lastP.ageCycles / (ModOptions.maxCyclesToRemember.Value + 1.0f));
                line.alpha = alpha;
                if (p.marked) Logger.LogInfo($"Moved maked line {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation} rel {lastP.pos} ; {p.pos}; col {line.color} aplha {line.alpha}");

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

        public bool iCut = false;

        public bool marked = false;


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
        if (!dict.ContainsKey(slugcat)) { dict.Add(slugcat, new()); Console.WriteLine("Added new slugcat in positions " + slugcat); }
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

