using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HUD;
using Menu;
using RWCustom;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace PathTracer;

public class SlugcatPath
{
    public bool preparedForOnscreen = false;

    public List<FSprite> lines = new();
    public Dictionary<string, List<PositionEntry>> slugcatPositions = new();


     Map map = null;


    public static int maxBackwardsRooms = 5;

    public static List<int> lastNRooms;
    public static ManualLogSource Logger;



    Color TargetColor => PlayerGraphics.SlugcatColor(GetSlugcat() ?? SlugcatStats.Name.Red);

    private SlugcatStats.Name GetSlugcat(Map m = null)
    {
        if (m == null) m = map;

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

        else return SlugcatStats.Name.Red;



    }

    public string CurrentRegion => map?.RegionName ?? "unknown";
    
    
    public SlugcatPath()
    {
        lastNRooms = new();
    }

    public SlugcatPath(Dictionary<string, PositionEntry[]> positions, Map map) {
        foreach (var regPos in positions) {
            this.slugcatPositions.Add(regPos.Key, regPos.Value.ToList());
        }
        this.map = map;
        lastNRooms = slugcatPositions[CurrentRegion].Select((el) => el.roomNumber).Distinct().ToList();
    }





    // clear potitions on region change !


    public void addNewPosition(PositionEntry p)
    {
        if (!slugcatPositions.ContainsKey(CurrentRegion)) slugcatPositions.Add(CurrentRegion, []);
        slugcatPositions[CurrentRegion].Add(p);
        if (!lastNRooms.Contains(p.roomNumber) || lastNRooms.Last() != p.roomNumber) {

            if (lastNRooms.Remove(p.roomNumber)) {
                Logger.LogInfo($"Removed room {p.roomNumber} to readdit");
            };
            lastNRooms.Add(p.roomNumber);
            Logger.LogInfo($"appended {p.roomNumber}");
        }

        if (lastNRooms.Count > maxBackwardsRooms) {
            int roomToRemove = lastNRooms[0];
            Logger.LogInfo("Will be removing entries from room "+roomToRemove);
            while (slugcatPositions[CurrentRegion].Count > 0 && slugcatPositions[CurrentRegion][0].roomNumber == roomToRemove) {
                var trp = slugcatPositions[CurrentRegion][0];
                if (trp.lastSprite!=null) {
                    lines.Remove(trp.lastSprite);
                    trp.lastSprite.RemoveFromContainer();
                    trp.lastSprite = null;
                }
                Logger.LogInfo($"Removed point {trp}");
                slugcatPositions[CurrentRegion].Remove(trp);
            }
            if (slugcatPositions[CurrentRegion].Count > 0 && slugcatPositions[CurrentRegion][0].lastSprite != null) // avoid keeping lines that point to a non existant origin
            {
                lines.Remove(slugcatPositions[CurrentRegion][0].lastSprite);
                slugcatPositions[CurrentRegion][0].lastSprite.RemoveFromContainer();
                slugcatPositions[CurrentRegion][0].lastSprite = null;
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
        int resumePos = slugcatPositions[CurrentRegion].Count;
        if (resumePos < 1) return;

        PositionEntry lastP = slugcatPositions[CurrentRegion][resumePos - 2];
        lastP.storedRealPos = null;

        PositionEntry p = slugcatPositions[CurrentRegion][resumePos - 1];

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
        Logger.LogInfo($"Adding single line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");
        map.container.AddChild(line);
        p.lastSprite = line;
    }

    public void appendNewLines(Map newMap = null)
    {
        if (newMap != null)
        {
            if (map != null) {
                lines.ForEach((line) => line.RemoveFromContainer());
                lines.Clear();
                slugcatPositions[CurrentRegion].ForEach((pos) => pos.lastSprite = null);
                Logger.LogInfo("Cleared lines from previous region");
                
                bool slugcatChanged = GetSlugcat(newMap) != GetSlugcat();
                bool regionChanged = newMap.mapData.regionName != CurrentRegion;

                if (slugcatChanged || regionChanged || newMap != map) {
                    MetaPathStore.StoreRegion(slugcatPositions[CurrentRegion], map.hud.rainWorld.options.saveSlot, GetSlugcat(), CurrentRegion);
                    Logger.LogInfo("Stored changes");
                    MetaPathStore.SyncColdFiles();
                }

                if (slugcatChanged)
                {
                    Logger.LogInfo($"Slugcat changed ! from {GetSlugcat()} -> {GetSlugcat(newMap)}");
                    slugcatPositions.Clear();
                    var positionsFresh = MetaPathStore.LoadDataFor(newMap.hud.rainWorld.options.saveSlot, GetSlugcat(newMap));
                    foreach (var regPos in positionsFresh)
                    {
                        this.slugcatPositions.Add(regPos.Key, regPos.Value.ToList());
                    }
                }

                if (regionChanged)
                {
                    Logger.LogInfo($"region has changed from {CurrentRegion} to {newMap.RegionName}");
                    if (slugcatPositions.ContainsKey(newMap.RegionName))
                        lastNRooms = slugcatPositions[newMap.RegionName].Select((el) => el.roomNumber).Distinct().ToList();
                    else lastNRooms.Clear();
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
            if (!slugcatPositions.ContainsKey(CurrentRegion))
            {
                slugcatPositions.Add(CurrentRegion, []);
                Logger.LogInfo("Created new key for region " + CurrentRegion);
            } else {
                Logger.LogInfo("Region has "+slugcatPositions[CurrentRegion].Count+" records");
            }
        }

        if (map == null)
        {
            Logger.LogInfo("Ayo, map is null from appendNEwLines");
            return;
        }


        int resumePos = slugcatPositions[CurrentRegion].FindIndex((el) => el.lastSprite == null && slugcatPositions[CurrentRegion].IndexOf(el) != 0);
        if (resumePos == -1)
        {
            Logger.LogInfo("APPENDnEWlINE RESUMEpOS WAS -1");
            return;
        }

        PositionEntry lastP = slugcatPositions[CurrentRegion][resumePos - 1];

        lastP.storedRealPos = null;

        for (int i = resumePos; i < slugcatPositions[CurrentRegion].Count; i++)
        {
            PositionEntry p = slugcatPositions[CurrentRegion][i];

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
            Logger.LogInfo($"Adding line from {lastP.GetPos(map)} to {p.GetPos(map)} length {line.scaleY} rot {line.rotation}");
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
        if (lines.Count == 0) return;
        PositionEntry lastP = slugcatPositions[CurrentRegion].FirstOrDefault();
        lastP.storedRealPos = null;
        if (ModMainClass.debug) Logger.LogInfo($"Updating positions of {lines.Count} alpha {map.fade}");

        for (int i = 0; i < lines.Count; i++)
        {
            PositionEntry p = slugcatPositions[CurrentRegion][i + 1];
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

        
    }


}