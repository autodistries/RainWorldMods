using System;
using System.Collections.Generic;
using System.Linq;
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

    public List<FSprite> lines = new();
    public Dictionary<string, List<PositionEntry>> slugcatPositions = new();


    public Map map = null;


    public static int maxBackwardsRooms => ModOptions.maxRoomsToRememberPerRegion.Value;

    public static List<int> lastNRooms = new();
    public static ManualLogSource Logger;
    internal static bool cycleTick = false;

    Color TargetColor => PlayerGraphics.SlugcatColor(GetSlugcat() ?? SlugcatStats.Name.Red);

    internal SlugcatStats.Name GetSlugcat(Map m = null)
    {
        m ??= map;
        if (m == null) return null;
        // Logger.LogInfo("I amo going crazy !  "+m.hud.owner.GetOwnerType());

        if (m.hud.owner is Player p)
        {
            var r = p.slugcatStats.name ?? m.hud.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
            return r ;
        }
        else if (m.hud.owner is FastTravelScreen ts)
        {
            return ts.activeMenuSlugcat;
        }
        else if (m.hud.owner is KarmaLadderScreen kls)
        {
            return kls.myGamePackage.characterStats.name;
        }
        Logger.LogError("Tried to get Slucat but map owner is unsupported "+m.hud.owner.GetOwnerType());
        return null;
    }


    /// <summary>
    /// This is for temporary data only. Hard disk saving is directly managed from MetaPathStore WriteColdFile
    /// This depends on the current map
    /// </summary>
    public enum MapMode {
        READONLY,//i.e. on a Regions map, or anything where player won't move
        WRITEREAD, // in game
        // WRITEONLY,
        NOTHING // to be interpreted as no read, no write on the map object
    }


    internal MapMode QueryMode(Map m = null) {
        m ??= map;
        if (m == null) return MapMode.NOTHING;
        Logger.LogDebug($"QueryMode said owner is {m?.hud?.owner?.GetOwnerType()}");
        if (!(m.hud.owner is FastTravelScreen or KarmaLadderScreen or Player)) return MapMode.NOTHING;
        if (ModOptions.doRecordData.Value && m.hud.owner is Player) return MapMode.WRITEREAD;
        return MapMode.READONLY;
    }



    public string CurrentRegion => map?.RegionName ?? "unknown";


    public SlugcatPath()
    {
    }

    public SlugcatPath(Map m)
    {
        SetNewMap(m);
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
        if (cycleTick && QueryMode(newMap) == MapMode.WRITEREAD)
        {
            Logger.LogInfo("Positions have been cleared because cycle tick");
            cycleTick = false;
            clearLines();
            SetNewPositions(new());
            map = newMap;
            return;
        }
        if (map != null)
        {
            Logger.LogInfo("Map object update");

            clearLines();



            SlugcatStats.Name newSlucat = GetSlugcat(newMap);
            SlugcatStats.Name oldSlucat = GetSlugcat();

            bool slugcatChanged = newSlucat != oldSlucat;
            bool regionChanged = newMap.mapData.regionName != CurrentRegion;

            if ((slugcatChanged || regionChanged || newMap != map) && oldSlucat != null)
            {
                Logger.LogInfo($"Stored changes for {oldSlucat} in {CurrentRegion}");
                MetaPathStore.StoreRegion(slugcatPositions.regionDataOrNew(CurrentRegion), map.hud.rainWorld.options.saveSlot, oldSlucat, CurrentRegion);
                MetaPathStore.WriteColdFiles();
            }

            if (slugcatChanged && newSlucat != null)
            {
                Logger.LogInfo($"Slugcat changed ! from {oldSlucat} -> {newSlucat}");
                SetNewPositions(newMap.hud.rainWorld.options.saveSlot, newSlucat);
            }

            if (regionChanged)
            {
                Logger.LogInfo($"region has changed from {CurrentRegion} to {newMap.RegionName}");
                lastNRooms = slugcatPositions.regionDataOrNew(newMap.RegionName).Select((el) => el.roomNumber).Distinct().ToList();
            }
        }
        else
        {
            Logger.LogInfo($"Map object first, loading forcefully for {GetSlugcat(newMap)} with map mode {QueryMode(newMap)}");
            SetNewPositions(newMap.hud.rainWorld.options.saveSlot, GetSlugcat(newMap));
        }
        map = newMap;
        Logger.LogInfo($"Loaded new map for sc {GetSlugcat()} in region {CurrentRegion}, {slugcatPositions.regionDataOrNew(CurrentRegion).Count} records, mode {QueryMode()}");

    }


    public void SetNewPositions(int saveSlot, SlugcatStats.Name slugcat) {
        SetNewPositions(MetaPathStore.LoadDataFor(saveSlot, slugcat));
    }

    public void SetNewPositions(Dictionary<string, List<PositionEntry>> npos)
    {
        if (slugcatPositions.Count != 0) Logger.LogWarning("Setting new positions when positions not clean");
        slugcatPositions = npos;

    }




    public void addNewPosition(PositionEntry p)
    {
        if (map == null) return;
        slugcatPositions.regionDataOrNew(CurrentRegion).Add(p);
        if (lastNRooms.LastOrDefault() != p.roomNumber || !lastNRooms.Contains(p.roomNumber)) {

            if (lastNRooms.Remove(p.roomNumber)) {
                Logger.LogInfo($"Removed room {p.roomNumber} to readdit");
            };
            lastNRooms.Add(p.roomNumber);
            Logger.LogInfo($"appended {p.roomNumber}");
        }

        if (lastNRooms.Count > maxBackwardsRooms) {
            int roomToRemove = lastNRooms[0];
            Logger.LogInfo("Will be removing entries from room " + roomToRemove);
            slugcatPositions.regionDataOrNew(CurrentRegion).FindAll((el) => el.roomNumber == roomToRemove).ForEach((trp) =>
            {
                if (trp.lastSprite != null)
                {
                    lines.Remove(trp.lastSprite);
                    trp.lastSprite.RemoveFromContainer();
                    trp.lastSprite = null;
                }
                Logger.LogInfo($"Removed point {trp}");
                slugcatPositions[CurrentRegion].Remove(trp);
            });
            if (slugcatPositions[CurrentRegion].Count > 0 && slugcatPositions[CurrentRegion][0].lastSprite != null) // avoid keeping lines that point to a non existant origin
            {
                lines.Remove(slugcatPositions.regionDataOrNew(CurrentRegion)[0].lastSprite);
                slugcatPositions.regionDataOrNew(CurrentRegion)[0].lastSprite.RemoveFromContainer();
                slugcatPositions.regionDataOrNew(CurrentRegion)[0].lastSprite = null;
            }
            lastNRooms.Remove(roomToRemove);
        }
        if (lines.Count != 0 && lines.First().alpha != 0 && map != null)
        {
            appendLastLine();
        }
    }

    private void appendLastLine()
    {
        if (map == null) return;
        if (!ModOptions.doShowData.Value) return;

        int resumePos = slugcatPositions.regionDataOrNew(CurrentRegion).Count;
        if (resumePos < 1) return;

        PositionEntry lastP = slugcatPositions.regionDataOrNew(CurrentRegion)[resumePos - 2];
        lastP.storedRealPos = null;

        PositionEntry p = slugcatPositions.regionDataOrNew(CurrentRegion)[resumePos - 1];

        FSprite line = new FSprite("pixel")
        {
            anchorY = 0F,
            color = TargetColor,
            alpha = 0f,
            scaleX = 2f,
        };


        line.SetPosition(lastP.GetPos(map));
        line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
        line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));

        lines.Add(line);
        // Logger.LogInfo($"Adding single line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");
        map.container.AddChild(line);
        p.lastSprite = line;
    }

    public void clearLines()
    {
        lines.ForEach((line) => line.RemoveFromContainer());
        lines.Clear();
        if (map != null) slugcatPositions.regionDataOrNew(CurrentRegion).ForEach((pos) => pos.lastSprite = null);
        Logger.LogInfo("Cleared lines");

    }

    public void appendNewLines()
    {

        if (map == null)
        {
            Logger.LogInfo("Ayo, map is null from appendNEwLines");
            return;
        }

        if (!ModOptions.doShowData.Value) return;


        int resumePos = slugcatPositions.regionDataOrNew(CurrentRegion).FindIndex((el) => el.lastSprite == null && slugcatPositions.regionDataOrNew(CurrentRegion).IndexOf(el) != 0);
        if (resumePos == -1)
        {
            Logger.LogInfo("APPENDnEWlINE RESUMEpOS WAS -1, no new lines to append");
            return;
        }

        PositionEntry lastP = slugcatPositions.regionDataOrNew(CurrentRegion)[resumePos - 1];

        lastP.storedRealPos = null;

        for (int i = resumePos; i < slugcatPositions.regionDataOrNew(CurrentRegion).Count; i++)
        {
            PositionEntry p = slugcatPositions.regionDataOrNew(CurrentRegion)[i];

            FSprite line = new FSprite("pixel")
            {
                anchorY = 0F,
                color = TargetColor,
                alpha = 0f,
                scaleX = 2f,
            };

            line.SetPosition(lastP.GetPos(map));
            line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
            line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));

            lines.Add(line);
            // Logger.LogInfo($"Adding line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");
            map.container.AddChild(line);
            p.lastSprite = line;

            lastP = p;
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
        if (lines.Count == 0) return; // ok with doshowdata off
        PositionEntry lastP = slugcatPositions.regionDataOrNew(CurrentRegion).FirstOrDefault();
        lastP.storedRealPos = null;
        // if (ModMainClass.debug) Logger.LogInfo($"Updating positions of {lines.Count} lines w/ alpha {map.fade} {map.lastFade != map.fade} || {map.depth != map.lastDepth} || ({map.fade != 0} && {map.panVel.magnitude >=0.01}) || {map.fade is not 0f or 1f}");

        for (int i = 0; i < lines.Count; i++)
        {
            PositionEntry p = slugcatPositions.regionDataOrNew(CurrentRegion)[i + 1];
            FSprite line = lines[i];

            p.storedRealPos = null;
            line.SetPosition(lastP.GetPos(map));
            line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
            line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));
            float alpha;
            if (lastP.roomNumber != p.roomNumber) // when two diff rooms !
                alpha = Math.Max(map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, true), map.Alpha(map.mapData.LayerOfRoom(lastP.roomNumber), 1, true));
            else alpha = map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, compensateForLayersInFront:true);
            line.alpha = alpha;
            // Logger.LogInfo($"Moved line {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");

            lastP = p;
        }

    }




    public class PositionEntry
    {
        public int roomNumber;
        public Vector2 pos;

        internal Vector2? storedRealPos = null;

        public FSprite lastSprite;


        public PositionEntry(int roomNumber, Vector2 pos)
        {
            this.roomNumber = roomNumber;
            this.pos = pos;
        }
        public PositionEntry(int roomNumber, float x, float y)
        {
            this.roomNumber = roomNumber;
            this.pos = new(x,y);
        }


        private static Vector2 lastMapPan = new();

        public override string ToString()
        {
            return $"position {pos} in room {roomNumber}";
        }


        public Vector2 GetPos(Map map)
        {
            if (!storedRealPos.HasValue)
            {
                storedRealPos = map.RoomToMapPos(pos, roomNumber, 1);
                lastMapPan = map.panPos;
            }
            return storedRealPos.Value;
        }

        public string ToStringStore() {
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
            hashCode = hashCode * -1521134295 + roomNumber.GetHashCode();
            hashCode = hashCode * -1521134295 + pos.GetHashCode();
            return hashCode;
        }
    }


}


static class ExtensionsMethods {

    /// <summary>
    ///  Returns the region data.
    /// If target region does not exist, creates it.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="region"></param>
    /// <returns></returns>
    public static List<SlugcatPath.PositionEntry> regionDataOrNew(this Dictionary<string, List<SlugcatPath.PositionEntry>> dictionary, string region)
    {
        if (!dictionary.ContainsKey(region)) dictionary.Add(region, new());
        return dictionary[region];
    }
}