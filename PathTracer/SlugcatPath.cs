using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BepInEx.Logging;
using HUD;
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

    public static List<int> lastNRooms;
    public static ManualLogSource Logger;
    internal static bool cycleTick = false;

    Color TargetColor => PlayerGraphics.SlugcatColor(GetSlugcat() ?? SlugcatStats.Name.Red);

    internal SlugcatStats.Name GetSlugcat(Map m = null)
    {
        if (m == null) m = map;
        if (map == null) return null;

        if (m.hud.owner is Player p)
        {
            return p.slugcatStats.name;
        }
        else if (m.hud.owner is FastTravelScreen ts)
        {
            return ts.activeMenuSlugcat;
        }
        else if (m.hud.owner is KarmaLadderScreen kls)
        {
            return kls.myGamePackage.characterStats.name;
        }

        else return null;
    }

    internal bool ModeWrite(Map m = null) {
        if (m == null) m = map;
        if (map==null) return false; //Read-only
        if (map.hud.owner is Player) return true; // PLaying, yay ! 
        else return false;
    }

    internal bool ReadOnly(Map m = null) => !ModeWrite(m);

    public string CurrentRegion => map?.RegionName ?? "unknown";
    
    
    public SlugcatPath()
    {
        lastNRooms = new();
    }

    public SlugcatPath(Dictionary<string, IEnumerable<PositionEntry>> positions, Map map) {
        foreach (var regPos in positions) {
            this.slugcatPositions.Add(regPos.Key, regPos.Value.ToList());
        }
        this.map = map;
        lastNRooms = slugcatPositions.regionDataOrNew(CurrentRegion).Select((el) => el.roomNumber).Distinct().ToList();
    }





    // clear potitions on region change !


    public void addNewPosition(PositionEntry p)
    { 
        if (map == null) return;
        slugcatPositions.regionDataOrNew(CurrentRegion).Add(p);
        if (!lastNRooms.Contains(p.roomNumber) || lastNRooms.Last() != p.roomNumber) {

            if (lastNRooms.Remove(p.roomNumber)) {
                Logger.LogInfo($"Removed room {p.roomNumber} to readdit");
            };
            lastNRooms.Add(p.roomNumber);
            Logger.LogInfo($"appended {p.roomNumber}");
        }

        while (lastNRooms.Count > maxBackwardsRooms) {
            int roomToRemove = lastNRooms[0];
            Logger.LogInfo("Will be removing entries from room "+roomToRemove);
            while (slugcatPositions.regionDataOrNew(CurrentRegion).Count > 0 && slugcatPositions.regionDataOrNew(CurrentRegion)[0].roomNumber == roomToRemove) {
                var trp = slugcatPositions.regionDataOrNew(CurrentRegion)[0];
                if (trp.lastSprite!=null) {
                    lines.Remove(trp.lastSprite);
                    trp.lastSprite.RemoveFromContainer();
                    trp.lastSprite = null;
                }
                Logger.LogInfo($"Removed point {trp}");
                slugcatPositions.regionDataOrNew(CurrentRegion).Remove(trp);
            }
            if (slugcatPositions.regionDataOrNew(CurrentRegion).Count > 0 && slugcatPositions.regionDataOrNew(CurrentRegion)[0].lastSprite != null) // avoid keeping lines that point to a non existant origin
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
        Logger.LogInfo("Cleared lines from previous region");

    }

    public void appendNewLines(Map newMap = null)
    {
        if (newMap != null)
        {
            if (map != null)
            {

                clearLines();

                bool slugcatChanged = GetSlugcat(newMap) != GetSlugcat();
                bool regionChanged = newMap.mapData.regionName != CurrentRegion;

                if ((slugcatChanged || regionChanged || newMap != map) && !cycleTick)
                {
                    MetaPathStore.StoreRegion(slugcatPositions.regionDataOrNew(CurrentRegion), map.hud.rainWorld.options.saveSlot, GetSlugcat(), CurrentRegion);
                    Logger.LogInfo("Stored changes");
                    MetaPathStore.WriteColdFiles();
                }

                if (slugcatChanged)
                {
                    Logger.LogInfo($"Slugcat changed ! from {GetSlugcat()} -> {GetSlugcat(newMap)}");
                    clearPositions();
                    var positionsFresh = MetaPathStore.LoadDataFor(newMap.hud.rainWorld.options.saveSlot, GetSlugcat(newMap));
                    foreach (var regPos in positionsFresh)
                    {
                        this.slugcatPositions.Add(regPos.Key, regPos.Value.ToList());
                    }
                }

                if (regionChanged)
                {
                    Logger.LogInfo($"region has changed from {CurrentRegion} to {newMap.RegionName}");
                    slugcatPositions.regionDataOrNew(newMap.RegionName).Select((el) => el.roomNumber).Distinct().ToList();
                }
            }
            else
            {
                Logger.LogInfo("First ever data, loading forcefully");
                var positionsFresh = MetaPathStore.LoadDataFor(newMap.hud.rainWorld.options.saveSlot, newMap.hud.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat);
                foreach (var regPos in positionsFresh)
                {
                    this.slugcatPositions.Add(regPos.Key, regPos.Value.ToList());
                }

            }
            map = newMap;
            Logger.LogInfo($"Stored new map for {GetSlugcat()} in region {CurrentRegion}, ");
           
            Logger.LogInfo("Region has "+slugcatPositions.regionDataOrNew(CurrentRegion).Count+" records");
        
        }

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

    public void clearPositions()
    {
        slugcatPositions.Clear();
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
        // if (ModMainClass.debug) Logger.LogInfo($"Updating positions of {lines.Count} alpha {map.fade}");

        for (int i = 0; i < lines.Count; i++)
        {
            PositionEntry p = slugcatPositions.regionDataOrNew(CurrentRegion)[i + 1];
            FSprite line = lines[i];

            p.storedRealPos = null;
            line.SetPosition(lastP.GetPos(map));
            line.scaleY = Custom.Dist(lastP.GetPos(map), p.GetPos(map));
            line.rotation = Custom.AimFromOneVectorToAnother(lastP.GetPos(map), p.GetPos(map));
            line.alpha = (map.fade == 0) ? 0f : map.Alpha(map.mapData.LayerOfRoom(p.roomNumber), 1, compensateForLayersInFront:true) + 0.3f;
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

    public static List<SlugcatPath.PositionEntry> regionDataOrNew(this Dictionary<string, List<SlugcatPath.PositionEntry>> dictionary, string region)
    {
        if (!dictionary.ContainsKey(region)) dictionary.Add(region, new());
        return dictionary[region];
    }
}