﻿using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public class Region
    {
        public string Id = "";
        public List<Room> Rooms = new();

        public Subregion[] Subregions = Array.Empty<Subregion>();

        HashSet<string> DrawnRoomConnections = new();

        string? WorldString;
        string? MapString;
        string? GateLockString;

        public Region()
        {
            
        }

        public Region(string id, string worldFilePath, string mapFilePath, string roomsDir)
        {
            Id = id;
            WorldString = File.ReadAllText(worldFilePath);
            MapString = File.ReadAllText(mapFilePath);

            Load();

            List<string> roomDirs = new();

            string worldRooms = Path.Combine(Path.GetDirectoryName(worldFilePath)!, $"../{Id}-rooms");
            if (Directory.Exists(worldRooms))
                roomDirs.Add(worldRooms);

            if (Main.TryFindParentDir(worldFilePath, "mods", out string? mods))
            {
                foreach (string mod in Directory.EnumerateDirectories(mods))
                {
                    string modRooms = Path.Combine(mod, $"world/{Id}-rooms");
                    if (Directory.Exists(modRooms))
                        roomDirs.Add(modRooms);
                }
            }

            roomDirs.Add(roomsDir);

            if (Main.TryFindParentDir(worldFilePath, "mergedmods", out string? mergedmods))
            {
                string rwworld = Path.Combine(mergedmods, "../world");
                if (Directory.Exists(rwworld))
                    roomDirs.Add(Path.Combine(rwworld, Id));
            }

            foreach (Room r in Rooms)
            {
                string? settings = null;
                string? data = null;

                string roomPath = r.IsGate ? $"../gates/{r.Id}" : r.Id;

                foreach (string roomDir in roomDirs)
                {
                    string dataPath = Path.Combine(roomDir, $"{roomPath}.txt");

                    if (data is null && File.Exists(dataPath))
                        data = dataPath;

                    if (Main.TryCheckSlugcatAltFile(dataPath, out string altDataPath))
                        data = altDataPath;

                    string settingsPath = Path.Combine(roomDir, $"{roomPath}_settings.txt");

                    if (settings is null && File.Exists(settingsPath))
                        settings = settingsPath;

                    if (Main.TryCheckSlugcatAltFile(settingsPath, out string altSettingsPath))
                        settings = altSettingsPath;
                }

                if (data is null)
                {
                    Main.LoadErrors.Add($"Could not find data for room {r.Id}");
                    continue;
                }

                r.Load(File.ReadAllText(data!), settings is null ? null : File.ReadAllText(settings));
            }

            

            HashSet<string> gatesProcessed = new();
            List<string> lockLines = new();
            foreach (string roomDir in roomDirs)
            {
                string locksPath = Path.Combine(roomDir, "../gates/locks.txt");
                if (!File.Exists(locksPath))
                    continue;

                AddGateLocks(File.ReadAllText(locksPath), gatesProcessed, lockLines);
            }

            if (lockLines.Count > 0)
                GateLockString = string.Join("\n", lockLines);
        }

        private void Load()
        {
            if (WorldString is null || MapString is null)
                throw new InvalidOperationException($"Region {Id} is missing either world or map data and can't be loaded.");

            Dictionary<string, string[]> connections = new();

            bool readingRooms = false;
            bool readingConditionalLinks = false;

            List<(string room, string? target, int disconnectedTarget, string replacement)> connectionOverrides = new();
            List<(string room, int exit, string replacement)> resolvedConnectionOverrides = new();

            Dictionary<string, HashSet<string>> exclusiveRooms = new();
            Dictionary<string, HashSet<string>> hideRooms = new();

            foreach (string line in WorldString.Split('\n', StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("//"))
                    continue;

                if (line == "ROOMS")
                    readingRooms = true;
                else if (line == "END ROOMS")
                    readingRooms = false;
                else if (line == "CONDITIONAL LINKS")
                    readingConditionalLinks = true;
                else if (line == "END CONDITIONAL LINKS")
                    readingConditionalLinks = false;
                else if (readingRooms)
                {
                    string[] split = line.Split(':', StringSplitOptions.TrimEntries);

                    if (split.Length >= 1)
                    {
                        Room room = new(this, split[0]);

                        if (split.Length >= 2)
                            connections[room.Id] = split[1].Split(',', StringSplitOptions.TrimEntries);

                        if (split.Length >= 3)
                            switch (split[2])
                            {
                                case "GATE": room.IsGate = true; break;
                                case "SHELTER": room.IsShelter = true; break;
                                case "ANCIENTSHELTER": room.IsShelter = room.IsAncientShelter = true; break;
                            }

                        Rooms.Add(room);
                    }
                }
                else if (readingConditionalLinks)
                {
                    string[] split = line.Split(':', StringSplitOptions.TrimEntries);

                    if (split[1] == "EXCLUSIVEROOM")
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);

                            if (!exclusiveRooms.TryGetValue(split[2], out HashSet<string>? roomCatNames))
                            {
                                exclusiveRooms[split[2]] = roomCatNames = new();
                            }

                            roomCatNames.UnionWith(slugcats);
                        }
                    }
                    else if (split[1] == "HIDEROOM")
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);

                            if (!hideRooms.TryGetValue(split[2], out HashSet<string>? roomCatNames))
                            {
                                hideRooms[split[2]] = roomCatNames = new();
                            }

                            roomCatNames.UnionWith(slugcats);
                        }
                    }
                    else
                    {
                        if (Main.SelectedSlugcat is not null)
                        {
                            string[] slugcats = split[0].Split(',', StringSplitOptions.TrimEntries);
                            if (slugcats.Contains(Main.SelectedSlugcat))
                            {
                                if (int.TryParse(split[2], out int disconnectedTarget))
                                    connectionOverrides.Add((split[1], null, disconnectedTarget, split[3]));
                                else
                                    connectionOverrides.Add((split[1], split[2], 0, split[3]));
                            }
                        }

                    }
                }
            }

            if (Main.SelectedSlugcat is not null)
            {
                foreach (var (room, slugcats) in exclusiveRooms)
                    if (!slugcats.Contains(Main.SelectedSlugcat))
                        Rooms.RemoveAll(r => r.Id.Equals(room, StringComparison.InvariantCultureIgnoreCase));

                foreach (var (room, slugcats) in hideRooms)
                    if (slugcats.Contains(Main.SelectedSlugcat))
                        Rooms.RemoveAll(r => r.Id.Equals(room, StringComparison.InvariantCultureIgnoreCase));
            }

            foreach (var (room, target, disconnectedTarget, replacement) in connectionOverrides)
                if (connections.TryGetValue(room, out string[]? roomConnections))
                {
                    if (target is not null)
                    {
                        for (int i = 0; i < roomConnections.Length; i++)
                            if (roomConnections[i].Equals(target, StringComparison.InvariantCultureIgnoreCase))
                                resolvedConnectionOverrides.Add((room, i, replacement));
                    }
                    else
                    {
                        int index = 0;
                        for (int i = 0; i < roomConnections.Length; i++)
                        {
                            if (roomConnections[i] == "DISCONNECTED")
                            {
                                index++;
                                if (index == disconnectedTarget)
                                {
                                    resolvedConnectionOverrides.Add((room, i, replacement));
                                    break;
                                }
                            }
                        }
                    }
                }

            foreach (var (room, exit, replacement) in resolvedConnectionOverrides)
                if (connections.TryGetValue(room, out string[]? roomConnections))
                    roomConnections[exit] = replacement;

            List<string> subregions = new() { "" };
            HashSet<Room> unmappedRooms = new(Rooms);

            foreach (string line in MapString.Split('\n', StringSplitOptions.TrimEntries))
            {
                if (!line.Contains(':'))
                    continue;

                string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);
                if (split[0] == "Connection")
                    continue;

                if (!TryGetRoom(split[0], out Room? room))
                {
                    Main.LoadErrors.Add($"Tried to position unknown room {split[0]}");
                    continue;
                }

                string[] data = split[1].Split("><", StringSplitOptions.TrimEntries);

                if (data.Length > 3)
                {
                    if (float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                        room.WorldPos.X = MathF.Round(x / 2);
                    if (float.TryParse(data[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        room.WorldPos.Y = MathF.Round(-y / 2);
                }

                if (data.TryGet(4, out string layerstr) && int.TryParse(layerstr, out int layer))
                {
                    room.Layer = layer;
                }
                if (data.TryGet(5, out string subregion))
                {
                    int index = subregions.IndexOf(subregion);
                    if (index < 0)
                    {
                        index = subregions.Count;
                        subregions.Add(subregion);
                    }

                    room.Subregion = index;
                }

                unmappedRooms.Remove(room);
            }

            if (unmappedRooms.Count > 0)
            {
                Main.LoadErrors.Add($"{unmappedRooms.Count} rooms aren't positioned! Skipping them.");
                Rooms.RemoveAll(r => unmappedRooms.Contains(r));
            }

            Subregions = subregions.Select(s => new Subregion(s)).ToArray();

            foreach (var (roomName, roomConnections) in connections)
            {
                if (!TryGetRoom(roomName, out Room? room))
                    continue;

                room.Connections = new Room.Connection[roomConnections.Length];
                for (int i = 0; i < roomConnections.Length; i++)
                {
                    if (roomConnections[i] == "DISCONNECTED")
                        continue;

                    if (TryGetRoom(roomConnections[i], out Room? targetRoom))
                    {
                        string[] targetConnections = connections[roomConnections[i]];
                        int targetExit = Array.IndexOf(targetConnections, room.Id);

                        if (targetExit >= 0)
                        {
                            room.Connections[i] = new(targetRoom, i, targetExit);
                        }
                    }
                    else
                    {
                        if (hideRooms.ContainsKey(roomConnections[i]))
                            Main.LoadErrors.Add($"{room.Id} connects to hidden room {roomConnections[i]}!");
                        else if (exclusiveRooms.ContainsKey(roomConnections[i]))
                            Main.LoadErrors.Add($"{room.Id} connects to excluded room {roomConnections[i]}!");
                        else
                            Main.LoadErrors.Add($"{room.Id} connects to a nonexistent room {roomConnections[i]}!");
                    }
                }
            }
        }

        private void AddGateLocks(string data, HashSet<string>? processed, List<string>? lockLines)
        {
            static void AddGateSymbol(Room room, string symbol, bool leftSide)
            {
                string? spriteName = symbol switch
                {
                    "1" => "smallKarmaNoRing0",
                    "2" => "smallKarmaNoRing1",
                    "3" => "smallKarmaNoRing2",
                    "4" => "smallKarmaNoRing3",
                    "5" => "smallKarmaNoRing4",
                    "R" => "smallKarmaNoRingR",
                    _ => null
                };
                if (spriteName is null)
                    return;

                Vector2 align = new(.4f, .5f);
                if (!leftSide)
                    align.X = 1 - align.X;

                if (!GameAtlases.Sprites.TryGetValue(spriteName, out AtlasSprite? sprite))
                    return;

                room.Children.Add(new SimpleIcon($"GateSymbol{(leftSide ? "Left" : "Right")}", sprite)
                {
                    ParentPosAlign = align
                });
            }

            foreach (string line in data.Split('\n', StringSplitOptions.TrimEntries))
            {
                string[] split = line.Split(':', StringSplitOptions.TrimEntries);
                if (processed is not null && processed.Contains(split[0]) || !TryGetRoom(split[0], out Room? gate))
                    continue;

                AddGateSymbol(gate, split[1], true);
                AddGateSymbol(gate, split[2], false);

                processed?.Add(split[0]);
                lockLines?.Add(line);
            }
        }

        public bool TryGetRoom(string id, [NotNullWhen(true)] out Room? room)
        {
            foreach (Room r in Rooms)
                if (r.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                {
                    room = r;
                    return true;
                }
            room = null;
            return false;
        }

        public void MarkRoomTilemapsDirty()
        {
            foreach (Room room in Rooms)
                room.TileMapDirty = true;
        }

        public void Draw(Renderer renderer)
        {
            DrawnRoomConnections.Clear();
            foreach (Room room in Rooms)
            {
                foreach (Room.Connection? connection in room.Connections)
                {
                    if (connection is null || DrawnRoomConnections.Contains(connection.Target.Id) || room.Exits.Length <= connection.Exit)
                        continue;

                    Vector2 start = renderer.TransformVector(room.WorldPos + room.Exits[connection.Exit].ToVector2() + new Vector2(.5f));
                    Vector2 end = renderer.TransformVector(connection.Target.WorldPos + connection.Target.Exits[connection.TargetExit].ToVector2() + new Vector2(.5f));

                    Main.SpriteBatch.DrawLine(start, end, Color.Black, 3);
                    Main.SpriteBatch.DrawRect(start - new Vector2(3), new(5), Color.Black);
                    Main.SpriteBatch.DrawRect(end - new Vector2(3), new(5), Color.Black);

                    Main.SpriteBatch.DrawLine(start, end, Color.White, 1);
                    Main.SpriteBatch.DrawRect(start - new Vector2(2), new(3), Color.White);
                    Main.SpriteBatch.DrawRect(end - new Vector2(2), new(3), Color.White);
                }
                DrawnRoomConnections.Add(room.Id);
            }
        }

        public JsonObject SaveJson()
        {
            return new()
            {
                ["id"] = Id,
                ["world"] = WorldString,
                ["map"] = MapString,
                ["locks"] = GateLockString,
                ["rooms"] = new JsonArray(Rooms.Select(r => new JsonObject() 
                {
                    ["id"] = r.Id,
                    ["data"] = r.DataString,
                    ["settings"] = r.SettingsString
                }).ToArray())
            };
        }

        public void LoadJson(JsonNode node)
        {
            if (node.TryGet("id", out string? id))
                Id = id;

            if (node.TryGet("world", out string? world))
                WorldString = world;

            if (node.TryGet("map", out string? map))
                MapString = map;

            if (node.TryGet("locks", out string? locks))
                GateLockString = locks;

            Load();

            if (GateLockString is not null)
                AddGateLocks(GateLockString, null, null);

            if (node.TryGet("rooms", out JsonArray? rooms))
            {
                foreach (JsonNode? roomNode in rooms)
                    if (roomNode is JsonObject roomObj 
                        && roomObj.TryGet("id", out string? roomId) 
                        && TryGetRoom(roomId, out Room? room)
                        && roomObj.TryGet("data", out string? roomData))
                    {
                        string? settings = roomObj.Get<string>("settings");
                        room.Load(roomData, settings);
                        room.Loaded = true;
                    }
            }
        }

        public class Subregion 
        {
            public string Name;

            public Color BackgroundColor = Color.White;
            public Color WaterColor = Color.Blue;

            public Subregion(string name)
            {
                Name = name;
            }
        }
    }
}