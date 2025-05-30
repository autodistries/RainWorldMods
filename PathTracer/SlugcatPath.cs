using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    internal static int regularOldPositionsReduction = 0;


    public static int MaxBackwardsRooms  => Mathf.Clamp((ModOptions.minQtyOfAccuratePositions?.Value ?? 450)/90, 1,15);
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

    
    // internal MapMode QueryMode(Map m = null)
    // {
    //     m ??= map;
    //     if (m == null) return MapMode.NOTHING;
    //     Logger.LogDebug($"QueryMode said map owner is {m?.hud?.owner?.GetOwnerType()}");
    //     if (!(m.hud.owner is FastTravelScreen or KarmaLadderScreen or Player)) return MapMode.NOTHING;
    //     if (ModOptions.doRecordData.Value && m.hud.owner is Player) return MapMode.WRITEREAD;
    //     return MapMode.READONLY;
    // }

    internal static Color SpeedInterpol(float f) {
        // green slow (0)
        // red fast (38)
        float i = Custom.LerpMap(f,0,16,0,1);
        if (ModMainClass.debug) Logger.LogInfo($"Speed from {f} to {i}");
        return new Color(i, (1-i),0,1);
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
         Logger.LogInfo($"Loaded new map");

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


        // culling
        LinearPositionsCuller(p, slugcar);
        // end culling 


        if (lastNRooms.ensureSlugcat(slugcar).LastOrDefault() != p.roomNumber) {
            lastNRooms[slugcar].Add(p.roomNumber);
            Logger.LogInfo($"appended {p.roomNumber}, room size : {(map.hud.owner as Player).room.world.GetAbstractRoom(p.roomNumber).size}.");

            /*
                One-sized rooms
x: 48, y: 35
x: 48, y: 35.
x: 48, y: 35.
                Two-sized rooms
x: 98, y: 35.

                three-sized rooms
x: 155, y: 35.


                four sized rooms

                five sized rooms

                8-sized 
                x: 444, y: 38. 10,04285714285714

                9-sized
                x: 152, y: 121.  10,94761904761905

            */
            
        }

        // ILimitMaximumRooms(slugcar, p, regionPositions);

        // regularOldPositionsReduction++;
        // regularOldPositionsReduction%=33;
        // if (lastNRooms[slugcar].Count > maxBackwardsRooms) {
       
            OldPositionsCuller(slugcar);
        // }


        if (ModMainClass.debug) Logger.LogInfo($"Added {regionPositions.LastOrDefault()} reg {CurrentRegion} sc {slugcar}");


        if (lines.ensureSlugcat(slugcar).Count != 0 && map.visible)
        {
            appendLastLine();
        }
    }

    public int GetRelativeRoomScreensSize(int roomId) {
        if (map.hud.owner is not Player p) return 1;
        var ars = p.room?.world?.GetAbstractRoom(roomId);
        if (ars == null) return 1;
        return Mathf.RoundToInt(Mathf.Clamp(ars.size.x * ars.size.y/(float)(48*35),1,19));
    }


    /// <summary>
    /// if there are more than n lines for this slugcat ,
    /// reduce the qty of points in each far away room
    /// </summary>
    /// <param name="slugcar"></param>
    public void OldPositionsCuller(SlugcatStats.Name slugcar, bool force = false)
    {
        if (force || lastNRooms[slugcar].Count > MaxBackwardsRooms)
        {
            List<PositionEntry> positionEntries = slugcatRegionalPositions[slugcar][CurrentRegion];
            Logger.LogInfo($"PROCESSING WITH CULLING!!!!! Before culling, {positionEntries.Count}. Rooms:{lastNRooms[slugcar].Count}>{MaxBackwardsRooms} min rooms {ModOptions.minQtyOfAccuratePositions.Value}");
            // The oldest the points, the less reluctant we should be to trimming data
            // run through the list of positions. 
            // For each set of points in a room, remove a proportion of them (based on what ?)
            // minimum qty of points to keep in a room : say 10 min
            lastNRooms[slugcar].Clear();
            int resumeIndex = 0;
            bool anyModif = false;
            bool direTimes = lines.ensureSlugcat(slugcar).Count > 1500 + (ModOptions.minQtyOfAccuratePositions?.Value ?? 450) * 1.1f;
            if (direTimes)
            {
                Logger.LogWarning($"We think there are too many lines ! {lines.ensureSlugcat(slugcar).Count}/{1500 + (ModOptions.minQtyOfAccuratePositions?.Value ?? 450) * 1.1f} with accuracy min {(ModOptions.minQtyOfAccuratePositions?.Value ?? 450)}\nAll lines will suffer sparsing !");
            }

            while (resumeIndex < positionEntries.Count)
            {
                Logger.LogInfo($"StartIndex is {resumeIndex}");
                var startSlice = positionEntries.Skip(resumeIndex);
                var operatingSlice = startSlice.TakeWhile((el) => el.roomNumber == startSlice.First().roomNumber && el.ageCycles == startSlice.First().ageCycles);
                if (positionEntries.IndexOf(operatingSlice.Last()) > positionEntries.Count - ModOptions.minQtyOfAccuratePositions.Value) {
                    Logger.LogInfo($"OldCuller exited because it wants at least {ModOptions.minQtyOfAccuratePositions.Value} intact pos, culling would have {resumeIndex}-{positionEntries.IndexOf(operatingSlice.Last())}");
                    break;
                }

                int relativeRoomSize = GetRelativeRoomScreensSize(operatingSlice.First().roomNumber);
                Logger.LogInfo($"slice age:{operatingSlice.First().ageCycles} room {operatingSlice.First().roomNumber}:{relativeRoomSize} qty {operatingSlice.Count()} btw {positionEntries.IndexOf(operatingSlice.First())} and {positionEntries.IndexOf(operatingSlice.Last())}\n{operatingSlice.First()}\n{operatingSlice.Last()}");
                if (operatingSlice.Count() <= 4+5*relativeRoomSize && !direTimes)
                {
                    resumeIndex = positionEntries.IndexOf(operatingSlice.Last()) +1;
                    Logger.LogInfo($"Continuing from {resumeIndex} cuz not enough entries in this room (min relative qty is 4+5*{relativeRoomSize}={4+5*relativeRoomSize})");
                    continue;
                }
                if (!anyModif)
                {
                    clearLines();
                    anyModif = true;
                }
                List<PositionEntry> newSlice = [operatingSlice.First()];
                // linar rep of 6 points on count-2 indexes
                int increment = operatingSlice.Count() / (5*relativeRoomSize);
                Logger.LogInfo($"Increment: {increment}");
                if (increment == 0) {Logger.LogError("Increment was 0, wtf"); increment=1;}
                for (int i = increment; i < operatingSlice.Count()-1; i += increment)
                {
                    newSlice.Add(operatingSlice.ElementAt(i));
                }
                newSlice.Add(operatingSlice.Last());
                Logger.LogInfo($"----------------\nnew contents:\n{newSlice.First()} \n{newSlice.Last()}\n-------------");

                positionEntries.RemoveRange(resumeIndex, operatingSlice.Count());
                positionEntries.InsertRange(resumeIndex, newSlice);
                resumeIndex = positionEntries.IndexOf(newSlice.Last()) + 1;
                Logger.LogInfo($"after modif, resumeIndex became {resumeIndex} and the new ones are btw {positionEntries.IndexOf(newSlice.First())} and {positionEntries.IndexOf(newSlice.Last())}");
            }
            Logger.LogInfo($"After culling, {positionEntries.Count}");
        }

    }


    /// <summary>
    /// If the latestst, yet unadded position seems linear with the two previous points, removes the middle point
    /// </summary>
    /// <param name="p"></param>
    /// <param name="slugcar"></param>
    /// <returns></returns>
    private bool LinearPositionsCuller(PositionEntry p, SlugcatStats.Name slugcar)
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

                if (ModMainClass.debug) Logger.LogDebug($"Removing intermediate position {lastThreePositions[1].pos} between {lastThreePositions[0].pos} and {lastThreePositions[2].pos}");
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

                    if (ModMainClass.debug) Logger.LogDebug("reased previous line");
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

            // if (lastNRooms[slugcar].Remove(p.roomNumber))
            // {
            //     if (ModMainClass.debug) Logger.LogInfo($"Removed room {p.roomNumber} to readdit");
            // };
            // lastNRooms[slugcar].Add(p.roomNumber);
            // Logger.LogInfo($"appended {p.roomNumber}");
        }

        // if (lastNRooms[slugcar].Count > maxBackwardsRooms && maxBackwardsRooms!=0)
        // {
        //     int roomToRemove = lastNRooms[slugcar][0];
        //     Logger.LogInfo("Will be removing entries from room " + roomToRemove);
        //     regionPositions.FindAll((el) => el.roomNumber == roomToRemove).ForEach((trp) =>
        //     {
        //         if (trp.lastSprite != null)
        //         {
        //             lines[slugcar].Remove(trp.lastSprite);
        //             trp.lastSprite.RemoveFromContainer();
        //             trp.lastSprite = null;
        //         }
        //         if (ModMainClass.debug) Logger.LogInfo($"Removed point {trp}");
        //         regionPositions.Remove(trp);
        //     });
        //     if (regionPositions.Count > 0 && regionPositions[0].lastSprite != null) // avoid keeping lines that point to a non existant origin
        //     {
        //         lines.ensureSlugcat(slugcar).Remove(regionPositions[0].lastSprite);
        //         regionPositions[0].lastSprite.RemoveFromContainer();
        //         regionPositions[0].lastSprite = null;
        //     }
        //     lastNRooms[slugcar].Remove(roomToRemove);
        // }
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

            Color slugColor;
            if (loadedSlugcars.Count() == 1 && p.ageCycles == 0 && ModOptions.doSpeedColorData.Value) slugColor = lastP.speedColor;
            else
            {
                slugColor = (loadedSlugcars.Count() == 1  && ModOptions.doRedColor.Value) ? Color.red : PlayerGraphics.SlugcatColor(slugcat);
                if (ModMainClass.debug) Logger.LogWarning($"Did not change line color. {loadedSlugcars.Count()} {p.ageCycles} {ModOptions.doSpeedColorData.Value} set color to {slugColor}");

            }

            FSprite line = new FSprite("pixel")
            {
                anchorY = 0F,
                color = slugColor,
                alpha = map.fade,
                scaleX = ModOptions.lineWidth.Value,
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
            } else {
                Logger.LogInfo($"resumePos is {resumePos} out of {positions.Count}");
            }

            PositionEntry lastP = positions[resumePos - 1];

            lastP.storedRealPos = null;

            for (int i = resumePos; i < positions.Count; i++)
            {
                PositionEntry p = positions[i];
                Color slugColor;
                if (loadedSlugcars.Count() == 1 && p.ageCycles == 0 && ModOptions.doSpeedColorData.Value) slugColor = lastP.speedColor;
                else
                {
                    slugColor = (loadedSlugcars.Count() == 1 && ModOptions.doRedColor.Value) ? Color.red : PlayerGraphics.SlugcatColor(slugcat);
                     Logger.LogWarning($"Did not change line color. {loadedSlugcars.Count()} {p.ageCycles} {ModOptions.doSpeedColorData.Value} set color to {slugColor}");

                }
                if (p.iCut)
                {
                     Logger.LogInfo($"Not drawing this line between {lastP} and {p} because iCut !");
                    lastP = p;

                    continue;
                }

                FSprite line = new FSprite("pixel")
                {
                    anchorY = 0F,
                    color = slugColor,
                    alpha = map.fade,
                    scaleX = ModOptions.lineWidth.Value,
                };

                line.SetPosition(lastP.GetPos(map));
                line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
                line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));
                if (loadedSlugcars.Count() == 1 && p.ageCycles == 0 && ModOptions.doSpeedColorData.Value) line.color = lastP.speedColor;

                lines.ensureSlugcat(slugcat).Add(line);
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
        if (ModMainClass.debug) Logger.LogDebug($"Updating lines ! reasons {map.lastFade != map.fade} || {map.depth != map.lastDepth} || ({map.fade != 0} && {map.panVel.magnitude >= 0.01}) || {map.visible}");
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
                if (ModMainClass.debug && p.marked) Logger.LogInfo($"Moved maked line {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation} rel {lastP.pos} ; {p.pos}; col {line.color} aplha {line.alpha}");
                lastP = p;
            }
        }


    }

    internal void debuginfo()
    {
        StringBuilder r = new();
        r.AppendLine($"Slugcars in keys:{slugcatRegionalPositions.Keys.Count} ({string.Join(", ",slugcatRegionalPositions.Keys)})");
        r.AppendLine($"Slugcars in loadedSlugcars:{loadedSlugcars.Count()} ({string.Join(", ", loadedSlugcars)})");
        foreach (var slugcat in slugcatRegionalPositions.Keys.ToArray().Intersect(loadedSlugcars))
        {
            r.AppendLine("  slugcar "+slugcat);
            var regions = slugcatRegionalPositions.ensureSlugcat(slugcat).Keys.ToArray();
            foreach (var reigon in regions)
            {
                r.Append($"{reigon}:{slugcatRegionalPositions[slugcat][reigon].Count}:{slugcatRegionalPositions[slugcat][reigon].Count((el) => el.lastSprite != null)}, ");
            }
            r.AppendLine();
        }
        Logger.LogDebug(r);
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
        public Color  speedColor;

        public PositionEntry(int roomNumber, Vector2 pos)
        {
            this.roomNumber = roomNumber;
            this.pos = pos;
            ageCycles = 0;
        }

        public PositionEntry(int roomNumber, Vector2 pos, float magnitude) : this(roomNumber, pos)
        {
            // magnitude;
            speedColor = SpeedInterpol(magnitude);
            // Logger.LogInfo($"recorded speed : {magnitude}");
        }

        public override string ToString()
        {
            return $"pos {pos} @{roomNumber}{(iCut ? " (CUT)" : "")}";
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


/// <summary>
/// Should the tracker follow slugpups ?
/// </summary>
public class SlugcatStatsKey
{
    public SlugcatStats.Name Name { get; set; }
    public int PupID { get; set; }
    public bool IsName { get; set; }

    public SlugcatStatsKey(SlugcatStats.Name name)
    {
        Name = name;
        IsName = true;
    }

    public SlugcatStatsKey(int id)
    {
        PupID = id;
        IsName = false;
    }
}


