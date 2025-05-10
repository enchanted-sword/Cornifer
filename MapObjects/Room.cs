using Cornifer.Json;
using Cornifer.Renderers;
using Cornifer.Structures;
using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using static Cornifer.MapObjects.Room;

namespace Cornifer.MapObjects
{
	public class Room : MapObject {
		public string? replaceRoomName;

		public bool IsGate;
		public bool IsShelter;
		public bool IsAncientShelter;
		public bool IsScavengerTrader;
		public bool IsScavengerOutpost;
		public bool IsScavengerTreasury;
		public bool isWarpRoom;

		public Vector2? TreasuryPos;
		public Vector2? OutpostPos;

		public float TerrainDepth = 0f;

		public Vector2? WarpPos;
		public string? WarpTarget;
		public PlacedObject? SpinningTopObj;

		public Point TileSize = new();
        public bool WaterInFrontOfTerrain;
        public Tile[,] Tiles = null!;

        public Point[] Exits = Array.Empty<Point>();
        public Shortcut[] Shortcuts = Array.Empty<Shortcut>();

        public int Layer;
        public ObjectProperty<int> WaterLevel = new("waterLevel", -1);
        public ObjectProperty<Subregion, string> Subregion = new("subregion", null!);
        public ObjectProperty<bool> Deathpit = new("deathpit", false);
        public ObjectProperty<bool> UseBetterTileCutout = new("betterTileCutout", true);
        public ObjectProperty<bool> CutoutAllSolidTiles = new("cutAllSolid", false);
        public ObjectProperty<bool> DrawInRoomShortcuts = new("inRoomShortcuts", InterfaceState.DrawAllShortcuts.Value);
        public ObjectProperty<bool> AcidWater = new("acidWater", false);
        public ObjectProperty<ColorRef> AcidColor = new("acidWater", new(null, Color.Blue));

        public Effect[] Effects = Array.Empty<Effect>();
		public Connection?[] Connections = Array.Empty<Connection>();
        public HashSet<string> BrokenForSlugcats = new();

		public List<Handle> Handles = new();
		public int HandleSegments = 0;
		private Vector2[] HandleBackPoints = Array.Empty<Vector2>();
		private Vector2[] HandleFrontPoints = Array.Empty<Vector2>();
		private Vector2[] HandleCollisionPoints = Array.Empty<Vector2>();

		private List<Rectangle> AirPockets = new();

		public int? WaterCycleTop;
		public int? WaterCycleBottom;

		public Texture2D? TileMap;
        public bool TileMapDirty = false;

        public bool Loaded = false;
        public bool Positioned = false;

        public string? DataString;
        public string? SettingsString;

        public readonly Region Region = null!;

        public override bool CanSetActive => true;

        protected override Layer DefaultLayer => Main.RoomsLayer;

        public override bool LoadCreationForbidden => true;
        public override int ShadeSize => InterfaceState.DisableRoomCropping.Value ? 0 : 5;
        public override int? ShadeCornerRadius => 6;
        public override bool ParentSelected => BoundRoom is not null && (BoundRoom.Selected || BoundRoom.ParentSelected) || base.ParentSelected;

        public override Vector2 ParentPosition
        {
            get => boundRoom is null ? Position : boundRoom.WorldPosition + Position;
            set
            {
                Position = value;
                if (boundRoom is not null)
                    Position -= boundRoom.WorldPosition;
            }
        }
        public override Vector2 Size => TileSize.ToVector2();

        public Room? BoundRoom
        {
            get => boundRoom;
            set
            {
                if (boundRoom is not null)
                    Position += boundRoom.WorldPosition;

                boundRoom = value;

                if (boundRoom is not null)
                    Position -= boundRoom.WorldPosition;
            }
        }

        public GateRoomData? GateData;
        public GateSymbols? GateSymbols;
        public MapText? GateRegionText;


        public bool CutOutsDirty = true;
        bool[,]? CutOutSolidTiles = null;

        Vector2 Position;
        private Room? boundRoom;

		private bool IsValidTilePos(Point testTilePos) {
			return testTilePos.X >= 0 && testTilePos.Y >= 0 && testTilePos.X < TileSize.X && testTilePos.Y < TileSize.Y;
		}

        public Room()
        {
            Subregion.SaveValue = v => v.Name;
            Subregion.LoadValue = s => Region.Subregions.First(r => r.Name == s);
        }

        public Room(Region region, string id) : this()
        {
            Region = region;
            Name = id;
        }

        public Point TraceShortcut(Point sourcePos, List<Point>? turns = null)
        {
            Point lastPos = sourcePos;
            int? dir = null;
            bool foundDir;

            while (true)
            {
                if (dir is not null)
                {
                    Point dirVal = StaticData.Directions[dir.Value];
                    Point testTilePos = sourcePos + dirVal;

                    if (IsValidTilePos(testTilePos))
                    {
                        Tile tile = Tiles[testTilePos.X, testTilePos.Y];
                        if (tile.Shortcut == Tile.ShortcutType.Normal)
                        {
                            lastPos = sourcePos;
                            sourcePos = testTilePos;
                            continue;
                        }
                        else if (tile.Shortcut != Tile.ShortcutType.None)
                        {
                            sourcePos = testTilePos;
                            break;
                        }
                    }
                }
				foundDir = false;
                for (int j = 0; j < 4; j++)
                {
                    Point dirVal = StaticData.Directions[j];
                    Point testTilePos = sourcePos + dirVal;

                    if (testTilePos == lastPos || !IsValidTilePos(testTilePos)) continue;

                    Tile tile = Tiles[testTilePos.X, testTilePos.Y];
                    if (tile.Shortcut == Tile.ShortcutType.Normal)
                    {
                        if (dir is not null) // not the first iteration
                            turns?.Add(sourcePos);

                        dir = j;
                        foundDir = true;
                        break;
                    }
                    else if (tile.Shortcut != Tile.ShortcutType.None)
                    {
                        sourcePos = testTilePos;
                        foundDir = false;
                        break;
                    }
                }
                if (!foundDir)
                    break;
            }

            return sourcePos;
        }

        public Tile GetTile(int x, int y)
        {
            x = Math.Clamp(x, 0, TileSize.X - 1);
            y = Math.Clamp(y, 0, TileSize.Y - 1);
            return Tiles[x, y];
        }

		private static float Lerp(float a, float b, float t) {
			return a + (b - a) * MathF.Max(MathF.Min(t, 1), 0);
		}

		private static float Normalize(float f) {
			if (float.IsNaN(f) || float.IsInfinity(f)) {
				return 0f;
			}
			return f;
		}

		public class Handle {
			public Vector2 Left;
			public Vector2 Middle;
			public Vector2 Right;
			float Height;

			private Handle GetBackHandle() {
				Handle BackHandle = new(Left, Middle, Right, 0);
				BackHandle.Left.Y += Height;
				BackHandle.Middle.Y += Height;
				BackHandle.Right.Y += Height;
				return BackHandle;
			}

			private static float Sample(float a, float b, float c, float d, float t) { // Cubic Bezier sampling at time t
				float num = 1f - t;
				return num * num * num * a + 3f * num * num * t * b + 3f * num * t * t * c + t * t * t * d;
			}
			private static float LerpUnclamped(float a, float b, float t) {
				return a + (b - a) * t;
			}
			private static float InverseLerp(float value, float from, float to) {
				return (value - from) / (to - from);
			}
			public static float Sample(Handle left, Handle right, float x) {
				if (x < left.Middle.X) {
					return LerpUnclamped(left.Middle.Y, left.Left.Y, InverseLerp(x, left.Middle.X, left.Left.X));
				}
				if (x > right.Middle.X) {
					return LerpUnclamped(right.Middle.Y, right.Right.Y, InverseLerp(x, right.Middle.X, right.Right.X));
				}
				float num = 0f;
				float num2 = 1f;
				for (int i = 0; i < 16; i++) { // Finding time t between 0 and 1 where the x component of the point on the curve is closest to our input x position
					float num3 = (num + num2) / 2f;
					if (Sample(left.Middle.X, left.Right.X, right.Left.X, right.Middle.X, num3) < x) {
						num = num3;
					} else {
						num2 = num3;
					}
				}
				return Sample(left.Middle.Y, left.Right.Y, right.Left.Y, right.Middle.Y, (num + num2) / 2f);
			}
			public static float SampleBack(Handle left, Handle right, float x) {
				return Sample(left.GetBackHandle(), right.GetBackHandle(), x);
			}

			public Handle(Vector2 left, Vector2 middle, Vector2 right, float height) {
				Left = left;
				Middle = middle;
				Right = right;
				Height = height;
			}
		}
		private void UpdateCollision() {
			Array.Clear(HandleCollisionPoints, 0, HandleCollisionPoints.Length);
			Array.Resize(ref HandleCollisionPoints, HandleSegments);

			float t = (6f - TerrainDepth) / (35f - TerrainDepth);
			for (int i = 0; i < HandleSegments; i++) {
				HandleCollisionPoints[i] = Vector2.Lerp(HandleFrontPoints[i], HandleBackPoints[i], t);
			}
		}

		private void UpdateHandles() {
			HandleSegments = TileSize.X;
			Handles.Sort((Handle a, Handle b) => Math.Sign(a.Middle.X - b.Middle.X));

			Array.Clear(HandleBackPoints, 0, HandleBackPoints.Length);
			Array.Clear(HandleFrontPoints, 0, HandleFrontPoints.Length);

			Array.Resize(ref HandleBackPoints, HandleSegments);
			Array.Resize(ref HandleFrontPoints, HandleSegments);

			if (Handles.Count >= 2) {
				int i = 0;
				for (int j = 0; j < HandleSegments; j++) {
					for (; i < Handles.Count - 2 && Handles[i + 1].Middle.X < j; i++);
					HandleFrontPoints[j] = new Vector2(j, Handle.Sample(Handles[i], Handles[i + 1], j));
					HandleBackPoints[j] = new Vector2(j, Handle.SampleBack(Handles[i], Handles[i + 1], j));
					HandleBackPoints[j].Y = Normalize(HandleBackPoints[j].Y);
					HandleFrontPoints[j].Y = Normalize(HandleFrontPoints[j].Y);
				}
				UpdateCollision();
			}
		}

		public void Load(string data, string? settings)
        {
            SettingsString = settings;

            string[] lines = data.Split('\n');

			if (lines.TryGet(1, out string sizeWater))
            {
                string[] swArray = sizeWater.Split('|');
                if (swArray.TryGet(0, out string size))
                {
                    string[] sArray = size.Split('*');
                    if (sArray.TryGet(0, out string widthStr) && int.TryParse(widthStr, out int width))
                        TileSize.X = width;
                    if (sArray.TryGet(1, out string heightStr) && int.TryParse(heightStr, out int height))
                        TileSize.Y = height;
                }
                if (swArray.TryGet(1, out string waterLevelStr) && int.TryParse(waterLevelStr, out int waterLevel))
                {
                    WaterLevel.OriginalValue = waterLevel < 0 ? -1 : waterLevel + 1;
                }
                if (swArray.TryGet(2, out string waterInFrontStr))
                {
                    WaterInFrontOfTerrain = waterInFrontStr == "1";
                }
            }

            string? tilesLine = null;

            if (lines.TryGet(11, out string tiles))
                tilesLine = tiles;

            if (tilesLine is null)
            {
                int tileCount = TileSize.X * TileSize.Y;
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    int splitCount = lines[i].Count(c => c == '|');

                    if (splitCount >= tileCount - 1 && splitCount <= tileCount + 1)
                    {
                        tilesLine = lines[i];
                        break;
                    }
                }
            }

            Tiles = new Tile[TileSize.X, TileSize.Y];
            if (tilesLine is not null)
            {
                string[] tilesArray = tilesLine.Split('|');

                int x = 0, y = 0;
                for (int i = 0; i < tilesArray.Length; i++)
                {
                    if (tilesArray[i].Length == 0 || x < 0 || y < 0 || x >= Tiles.GetLength(0) || y >= Tiles.GetLength(1))
                        continue;

                    string[] tileArray = tilesArray[i].Split(',');
                    Tile tile = new();

                    for (int j = 0; j < tileArray.Length; j++)
                    {
                        if (j == 0)
                        {
                            if (!int.TryParse(tileArray[j], out int terrain))
                                continue;

                            tile.Terrain = (Tile.TerrainType)terrain;
                            continue;
                        }

                        switch (tileArray[j])
                        {
                            case "1": tile.Attributes |= Tile.TileAttributes.VerticalBeam; break;
                            case "2": tile.Attributes |= Tile.TileAttributes.HorizontalBeam; break;

                            case "3" when tile.Shortcut == Tile.ShortcutType.None:
                                tile.Shortcut = Tile.ShortcutType.Normal;
                                break;

                            case "4": tile.Shortcut = Tile.ShortcutType.RoomExit; break;
                            case "5": tile.Shortcut = Tile.ShortcutType.CreatureHole; break;
                            case "6": tile.Attributes |= Tile.TileAttributes.WallBehind; break;
                            case "7": tile.Attributes |= Tile.TileAttributes.Hive; break;
                            case "8": tile.Attributes |= Tile.TileAttributes.Waterfall; break;
                            case "9": tile.Shortcut = Tile.ShortcutType.NPCTransportation; break;
                            case "10": tile.Attributes |= Tile.TileAttributes.GarbageHole; break;
                            case "11": tile.Attributes |= Tile.TileAttributes.WormGrass; break;
                            case "12": tile.Shortcut = Tile.ShortcutType.RegionTransportation; break;
                        }
                    }

                    Tiles[x, y] = tile;

                    y++;
                    if (y >= TileSize.Y)
                    {
                        x++;
                        y = 0;
                    }
                }

                List<Point> exits = new();
                List<Point> shortcuts = new();

                for (int j = 0; j < TileSize.Y; j++)
                    for (int i = 0; i < TileSize.X; i++)
                    {
                        Tile tile = Tiles[i, j];

                        if (tile.Terrain == Tile.TerrainType.ShortcutEntrance)
                            shortcuts.Add(new Point(i, j));

                        if (tile.Shortcut == Tile.ShortcutType.RoomExit)
                            exits.Add(new Point(i, j));
                    }

                Point[] exitEntrances = new Point[exits.Count];

                for (int i = 0; i < exits.Count; i++)
                {
                    exitEntrances[i] = TraceShortcut(exits[i]);
                }

                List<Shortcut> tracedShortcuts = new();

                foreach (Point shortcutIn in shortcuts)
                {
                    Point target = TraceShortcut(shortcutIn);
                    Tile targetTile = GetTile(target.X, target.Y);

                    Tile.ShortcutType type = targetTile.Shortcut;
                    if (targetTile.Shortcut == Tile.ShortcutType.Normal && targetTile.Terrain != Tile.TerrainType.ShortcutEntrance)
                        type = Tile.ShortcutType.None;

                    tracedShortcuts.Add(new(shortcutIn, target, type));
                }

                Shortcuts = tracedShortcuts.ToArray();
                Exits = exitEntrances;
            }
            else
            {
                Main.LoadErrors.Add($"Could not find tile data for room {Name}");
            }

            if (lines.Length < 12)
            {
                int oldSize = lines.Length;
                Array.Resize(ref lines, 12);

                for (int i = oldSize - 1; i < lines.Length; i++)
                    lines[i] = "";
            }
            if (tilesLine is not null)
                lines[11] = tilesLine;

            for (int i = 0; i < lines.Length; i++)
            {
                if (i != 0 && i != 1 && i != 11)
                    lines[i] = "";
            }
            DataString = string.Join('\n', lines);

            if (settings is not null)
                foreach (string line in settings.Split('\n', StringSplitOptions.TrimEntries))
                {
                    string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);

                    if (split[0] == "PlacedObjects")
                    {
                        HashSet<PlacedObject> objects = new();
						HashSet<PlacedObject> TerrainHandles = new();
						List<PlacedObject> filters = new();
						List<PlacedObject> airPockets = new();
						List<PlacedObject> waterCutoffs = new();
						
                        string[] objectStrings = split[1].Split(',', StringSplitOptions.TrimEntries);
						foreach (string str in objectStrings)
                        {
                            PlacedObject? obj = PlacedObject.Load(str);
                            if (obj is not null)
                            {
								switch (obj.Type) {
									case "Filter":
										filters.Add(obj);
										break;
									case "ScavengerTreasury":
										IsScavengerTreasury = true;
										TreasuryPos = new(obj.RoomPos.X, TileSize.Y - obj.RoomPos.Y);
										break;
									case "TerrainHandle":
										TerrainHandles.Add(obj);
										break;
									case "ScavengerOutpost":
										OutpostPos = new(obj.RoomPos.X, TileSize.Y - obj.RoomPos.Y);
										break;
									case "WarpPoint":
									case "SpinningTopSpot":
										isWarpRoom = true;
										WarpPos = new(obj.RoomPos.X, TileSize.Y - obj.RoomPos.Y);
										WarpTarget = obj.TargetRegion;

										if (obj.Type == "SpinningTopSpot") objects.Add(obj);
										break;
									case "WaterCycleTop":
										WaterCycleTop = (int)MathF.Round(obj.RoomPos.Y);
										break;
									case "WaterCycleBottom":
										WaterCycleBottom = (int)MathF.Round(obj.RoomPos.Y);
										break;
									case "AirPocket":
										airPockets.Add(obj);
										break;
									case "WaterCutoff":
										waterCutoffs.Add(obj);
										break;
									default:
										objects.Add(obj);
										break;
								}
                            }
                        }
                        List<PlacedObject> remove = new();

                        foreach (PlacedObject filter in filters)
                        {
                            Vector2 filterPos = filter.RoomPos;
                            float filterRad = filter.HandlePos.Length() / 20;

                            foreach (PlacedObject obj in objects)
                            {
                                Vector2 diff = obj.RoomPos - filterPos;
                                if (diff.Length() > filterRad)
                                    continue;

                                if (obj.SlugcatAvailability.Count == 0)
                                    obj.SlugcatAvailability.UnionWith(StaticData.Slugcats.Where(s => s.Playable).Select(s => s.Id));

                                obj.SlugcatAvailability.IntersectWith(filter.SlugcatAvailability);

                                if (obj.RemoveByAvailability && Main.SelectedSlugcat is not null && obj.SlugcatAvailability.Count > 0 && !obj.SlugcatAvailability.Contains(Main.SelectedSlugcat.Id))
                                    remove.Add(obj);
                            }
                        }

						foreach (PlacedObject handle in TerrainHandles) {
							Vector2 middle = handle.RoomPos;
							Vector2 left = middle + handle.TerrainHandleLeftOffset;
							Vector2 right = middle + handle.TerrainHandleRightOffset;
							Handle item = new(left, middle, right, handle.TerrainHandleBackHeight);
							Handles.Add(item);
						}

						foreach (PlacedObject airPocket in airPockets) { 
							AirPockets.Add(airPocket.AirPocket);
						}

						foreach (PlacedObject waterCutoff in waterCutoffs) {
							int x = (int)Math.Round(waterCutoff.RoomPos.X);
							int y = (int)Math.Round(waterCutoff.RoomPos.Y);
							int height = TileSize.Y - y;
							AirPockets.Add(new(x, y, waterCutoff.WaterCutoffLength, height));
						}

						objects.ExceptWith(remove);

                        foreach (PlacedObject obj in objects)
                        {
							if (isWarpRoom && obj.Type == "SpinningTopSpot") {
								SpinningTopObj = obj;
							}
							else 
							{
								obj.AddAvailabilityIcons();
								Children.Add(obj);
							}
						}
                    }
                    else if (split[0] == "Effects")
                    {
                        List<Effect> effects = new();

                        foreach (string effectStr in split[1].Split(',', StringSplitOptions.TrimEntries))
                        {
                            string[] effectSplit = effectStr.Split('-');
							
							if (effectSplit.Length >= 4)
                            {
								string name = effectSplit[0];

								if (!float.TryParse(effectSplit[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
									amount = 0;

								effects.Add(new(name, amount));
							}
                        }

                        Effects = effects.ToArray();
					}
					else if (split[0] == "TerrainDepth")
					{
						TerrainDepth = float.Parse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture);
					}
                }

			if (IsAncientShelter && SpriteAtlases.Sprites.TryGetValue("Object_AncientShelterMarker", out var ancientShelterMarker))
				Children.Add(new SimpleIcon("AncientShelterMarker", ancientShelterMarker));
            else if (IsShelter && SpriteAtlases.Sprites.TryGetValue("ShelterMarker", out var shelterMarker))
                Children.Add(new SimpleIcon("ShelterMarker", shelterMarker));

            if (IsScavengerOutpost)
            {
                Vector2 align = OutpostPos.HasValue ? OutpostPos.Value / TileSize.ToVector2() : new Vector2(.5f);

                Children.Add(new MapText("TollText", Main.DefaultSmallMapFont, "Scavenger toll")
                {
                    ParentPosAlign = align,
                });
                if (SpriteAtlases.Sprites.TryGetValue("ChieftainA", out var tollIcon))
                    Children.Add(new SimpleIcon("TollIcon", tollIcon)
                    {
                        ParentPosAlign = align,
                    });
            }

            if (IsScavengerTrader)
            {
                Children.Add(new MapText("TraderText", Main.DefaultSmallMapFont, "Scavenger merchant"));
                if (SpriteAtlases.Sprites.TryGetValue("ChieftainA", out var tollIcon))
                    Children.Add(new SimpleIcon("TraderIcon", tollIcon));
            }

            if (IsScavengerTreasury)
            {
                Vector2 align = TreasuryPos.HasValue ? TreasuryPos.Value / TileSize.ToVector2() : new Vector2(.5f);

                Children.Add(new MapText("TreasuryText", Main.DefaultSmallMapFont, "Scavenger treasury")
                {
                    ParentPosAlign = align,
                });
                if (SpriteAtlases.Sprites.TryGetValue("ChieftainA", out var tollIcon))
                    Children.Add(new SimpleIcon("TreasuryIcon", tollIcon)
                    {
                        ParentPosAlign = align,
                    });
            }

			if (isWarpRoom) 
			{
				if (WarpTarget is not null && this.Name != "WAUA_BATH" && this.Name != "WAUA_TOYS") // the SpinningTopSpot in WAUA_BATH has an unused warp point to SB_D06 that absolutely should NOT show up on the map
				{
					Vector2 align = WarpPos.HasValue ? WarpPos.Value / TileSize.ToVector2() : new Vector2(.5f);
					bool isDESERT6 = this.Name == "WORA_DESERT6";

					if (WarpTarget == "NULL" || WarpTarget == null) {
						WarpTarget = Region.Id switch {
							"WARA" => "WAUA",
							"WDSR" or "WGWR" or "WHIR" or "WSUR" => "WORA",
							_ => "WRSA",
						};
					}

					ColorRef warpColor = ColorDatabase.GetRegionColor(WarpTarget, null);

					
					if ((WarpTarget == "WAUA" || WarpTarget == "WRSA") && !isDESERT6 && SpriteAtlases.Sprites.TryGetValue("Object_RippleWarpPoint", out var rippleWarpIcon))
						Children.Add(new SimpleIcon("WarpPointIcon", rippleWarpIcon) {
							ParentPosAlign = align
						});
					else if (SpriteAtlases.Sprites.TryGetValue("Object_WarpPoint", out var warpIcon))
						Children.Add(new SimpleIcon("WarpPointIcon", warpIcon) {
							ParentPosAlign = align
						});

					if (!isDESERT6) Children.Add(new MapText("WarpTargetText", Main.DefaultSmallMapFont, $"To [c:{warpColor.GetKeyOrColorString()}]{RWAssets.GetRegionDisplayName(WarpTarget, null)}[/c]") {
						ParentPosAlign = align
					});
					
				}

				if (SpinningTopObj is not null) Children.Add(SpinningTopObj); // so the icon renders overtop its warp point when applicable
			}

			if (Name is not null && StaticData.ValidWarpTargets.TryGetValue(Name, out var warpTarget) && SpriteAtlases.Sprites.TryGetValue("Object_EchoWarpPoint", out var echoWarpIcon)) {
				string fromRegion = RWAssets.GetRegionDisplayName(warpTarget, null);
				ColorRef warpColor = ColorDatabase.GetRegionColor(warpTarget, null);

				Children.Add(new SimpleIcon("WarpPointIcon", echoWarpIcon));
				Children.Add(new MapText("WarpTargetText", Main.DefaultSmallMapFont, $"From [c:{warpColor.GetKeyOrColorString()}]{fromRegion}[/c]"));
			}

			if (GateData is not null && IsGate) {
				Children.Add(GateSymbols = new GateSymbols(GateData.LeftKarma, GateData.RightKarma));
				Children.Add(GateRegionText = new MapText("TargetRegionText", Main.DefaultBigMapFont, $"Region Text"));

				GateRegionText.NoAlignOverride = true;
				GateRegionText.IconPosAlign = new(.5f);
				GateSymbols.Offset = new(0, MathF.Floor(-Size.Y / 2 - GateSymbols.Size.Y / 2 - 5));
				GateRegionText.Offset = new(0, MathF.Floor(-Size.Y / 2 - GateSymbols.Size.Y - 19 - Main.DefaultBigMapFont.LineSpacing / 2));
			}

            if (!Region.LegacyFormat && StaticData.VistaRooms.TryGetValue(Name!, out Vector2 vistaPoint))
            {
                Vector2 rel = vistaPoint / 20 / Size;

                rel.Y = 1 - rel.Y;

                Children.Add(new MapText("VistaMarker", Main.DefaultSmallMapFont, "Expedition\nvista\npoint")
                {
                    ParentPosAlign = rel,
                    RenderLayer = { OriginalValue = Main.VistaPointsLayer }
                });
            }

            if (BrokenForSlugcats.Count > 0)
            {
                string text = "Broken for " + string.Join(' ', StaticData.Slugcats.Where(s => BrokenForSlugcats.Contains(s.Id)).Select(s => $"[ic:Slugcat_{s.Id}]"));
                Children.Add(new MapText("BrokenShelterText", Main.DefaultSmallMapFont, text));
            }

            Deathpit.OriginalValue = !IsShelter && !IsGate && WaterLevel.Value < 0 && Enumerable.Range(0, TileSize.X).Any(x => Tiles[x, TileSize.Y - 1].Terrain == Tile.TerrainType.Air);

            if (Effects.Any(e => e.Name == "LethalWater"))
            {
                string key = $"reg_{Region.Id}_acid";
                if (ColorDatabase.Colors.TryGetValue(key, out ColorRef? cref))
                {
                    AcidWater.OriginalValue = true;
                    AcidColor.OriginalValue = cref;
                }
            }

            Loaded = true;
        }

        public Texture2D GetTileMap()
        {
            if (TileMap is null || TileMapDirty)
                UpdateTileMap();
            return TileMap!;
        }

        public void UpdateTileMap()
        {
            Subregion subregion = Subregion.Value;
            Color[] colors = ArrayPool<Color>.Shared.Rent(TileSize.X * TileSize.Y);
            Color waterColor = AcidWater.Value ? AcidColor.Value.Color : subregion.WaterColor.Color;

			UpdateHandles();
			bool hasHandles = HandleCollisionPoints.Length > 0;

			try
            {
                bool invertedWater = Effects.Any(ef => ef.Name == "InvertedWater");

                int waterLevel = WaterLevel.Value;
				int waterFluxLevel = -1;
				float WaterFluxTransparency = Lerp(InterfaceState.WaterTransparency.Value, 1, 0.35f);

				Effect? waterFluxMin = Effects.FirstOrDefault(ef => ef.Name == "WaterFluxMinLevel");
				Effect? waterFluxMax = Effects.FirstOrDefault(ef => ef.Name == "WaterFluxMaxLevel");


				if (waterFluxMin is not null && waterFluxMax is not null) {
					// float waterMid = 1 - (waterFluxMax.Amount + waterFluxMin.Amount) / 2 * (22f / 20f);
					// waterLevel = (int)(waterMid * TileSize.Y) + 2;
					float waterMax = waterFluxMax.Amount / (22f / 20f);
					float waterMin = waterFluxMin.Amount / (22f / 20f);
					waterFluxLevel = (int)(waterMax * TileSize.Y) + 2;
					waterLevel = (int)(waterMin * TileSize.Y) + 2;
				} else if (WaterCycleTop is not null && WaterCycleBottom is not null) {
					waterFluxLevel = (int)WaterCycleTop;
					waterLevel = (int)WaterCycleBottom;
				}

					for (int j = 0; j < TileSize.Y; j++)
						for (int i = 0; i < TileSize.X; i++) {
							if (!InterfaceState.DisableRoomCropping.Value && CutOutSolidTiles is not null && CutOutSolidTiles[i, j]) {
								colors[i + j * TileSize.X] = Color.Transparent;
								continue;
							}

							Tile tile = GetTile(i, j);

							float gray = 1;

							bool solid = tile.Terrain == Tile.TerrainType.Solid;

							if (solid)
								gray = 0;

							else if (tile.Terrain == Tile.TerrainType.Floor)
								gray = 0.35f;

							else if (tile.Terrain == Tile.TerrainType.Slope)
								gray = .4f;

							else if (InterfaceState.DrawTileWalls.Value && tile.Attributes.HasFlag(Tile.TileAttributes.WallBehind))
								gray = 0.75f;

							if (InterfaceState.RegionBGShortcuts.Value && tile.Terrain == Tile.TerrainType.ShortcutEntrance)
								gray = 1;

							else if (tile.Attributes.HasFlag(Tile.TileAttributes.VerticalBeam) || tile.Attributes.HasFlag(Tile.TileAttributes.HorizontalBeam))
								gray = 0.35f;

							Color color = Color.Lerp(Color.Black, subregion.BackgroundColor.Color, gray);

							if (!solid) {
								bool isAirPocket = false;
								foreach (Rectangle AirPocket in AirPockets) {
									if (AirPocket.Contains(i, TileSize.Y - j)) {
										isAirPocket = true;
										break;
									}
								}
								if (!isAirPocket) {
									if ((invertedWater ? j <= waterLevel : j >= TileSize.Y - waterLevel)) {
										color = Color.Lerp(waterColor, color, InterfaceState.WaterTransparency.Value);
									} else if (waterFluxLevel > 0 && (invertedWater ? j <= waterFluxLevel : j >= TileSize.Y - waterFluxLevel)) {
										color = Color.Lerp(waterColor, color, WaterFluxTransparency);
									}
								}
								
							}

							if (Deathpit.Value && j >= TileSize.Y - 5 && Tiles[i, TileSize.Y - 1].Terrain == Tile.TerrainType.Air && (!hasHandles || HandleCollisionPoints[i].Y == 0))
									color = Color.Lerp(Color.Black, color, (TileSize.Y - j - .5f) / 5f);

							colors[i + j * TileSize.X] = color;
						}
				

				foreach (Vector2 handlePoint in HandleCollisionPoints)
				{
					int x = (int)handlePoint.X;
					int yfloor = (int)MathF.Floor(handlePoint.Y);
					int ytop = Math.Max(TileSize.Y - yfloor, 0);
					float fit = MathF.Abs(handlePoint.Y - yfloor);

					for (int y = ytop; y < TileSize.Y; y++) {
						Tile tile = GetTile(x, y);
						if (tile.Terrain == Tile.TerrainType.Air) {
							if (y == ytop && fit < 0.5) colors[x + y * TileSize.X] = Color.Lerp(Color.Black, subregion.BackgroundColor.Color, 0.4f);
							else colors[x + y * TileSize.X] = Color.Black;
						}
					}
				}

				if (InterfaceState.MarkShortcuts.Value)
                    foreach (Shortcut shortcut in Shortcuts)
                        if ((!InterfaceState.MarkExitsOnly.Value || shortcut.Type == Tile.ShortcutType.RoomExit) && shortcut.Type != Tile.ShortcutType.None)
                            colors[shortcut.Entrance.X + shortcut.Entrance.Y * TileSize.X] = new(255, 0, 0);


                TileMap ??= new(Main.Instance.GraphicsDevice, TileSize.X, TileSize.Y);
                TileMap.SetData(colors, 0, TileSize.X * TileSize.Y);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(colors);
            }
            TileMapDirty = false;
        }

        void ProcessCutouts()
        {
            CutOutSolidTiles = new bool[TileSize.X, TileSize.Y];

            if (CutoutAllSolidTiles.Value)
            {
                for (int j = 0; j < TileSize.Y; j++)
                    for (int i = 0; i < TileSize.X; i++)
                        if (Tiles[i, j].Terrain == Tile.TerrainType.Solid)
                            CutOutSolidTiles[i, j] = true;

                return;
            }

            Queue<Point> queue = new();
            bool[,] noCutTiles = new bool[TileSize.X, TileSize.Y];

            for (int i = 0; i < TileSize.X - 1; i++)
                queue.Enqueue(new(i, 0));

            for (int i = 1; i < TileSize.Y; i++)
                queue.Enqueue(new(0, i));

            for (int i = 0; i < TileSize.Y - 1; i++)
                queue.Enqueue(new(TileSize.X - 1, i));

            for (int i = 1; i < TileSize.X; i++)
                queue.Enqueue(new(i, TileSize.Y - 1));

            int GetTileNeighbors(int x, int y)
            {
                int neighbors = 0;

                if (x <= 0 || Tiles[x - 1, y].Terrain == Tile.TerrainType.Solid) neighbors += 1;
                if (x >= TileSize.X - 1 || Tiles[x + 1, y].Terrain == Tile.TerrainType.Solid) neighbors += 1;
                if (y <= 0 || Tiles[x, y - 1].Terrain == Tile.TerrainType.Solid) neighbors += 1;
                if (y >= TileSize.Y - 1 || Tiles[x, y + 1].Terrain == Tile.TerrainType.Solid) neighbors += 1;

                return neighbors;
            }

            bool IsTileOOB(int x, int y)
            {
                return x < 0 || y < 0 || x >= TileSize.X || y >= TileSize.Y;
            }

            bool Check2TileCutout(int x, int y)
            {
                bool cutout = !IsTileOOB(x, y) && Tiles[x, y].Terrain != Tile.TerrainType.Solid && GetTileNeighbors(x, y) == 3;
                if (cutout)
                    CutOutSolidTiles[x, y] = true;
                return cutout;
            }

            void ClearNoCutout(int x, int y)
            {
                for (int i = 0; i < TileSize.X; i++)
                    noCutTiles[i, y] = false;

                for (int i = 0; i < TileSize.Y; i++)
                    noCutTiles[x, i] = false;
            }

            bool CutTile(int x, int y)
            {
                if (IsTileOOB(x, y) || CutOutSolidTiles[x, y] || noCutTiles[x, y])
                    return false;

                if (UseBetterTileCutout.Value)
                {
                    int searchDist = 20;
                    int searchMaxDist = 30;

                    for (int i = x - 1; i >= Math.Max(x - searchMaxDist, 0); i--)
                    {
                        bool outOfRangeLeft = i < x - searchDist;

                        if (CutOutSolidTiles[i, y])
                            break;

                        if (Tiles[i, y].Terrain == Tile.TerrainType.Solid)
                            continue;

                        for (i = x + 1; i < Math.Min(x + searchMaxDist, TileSize.X); i++)
                        {
                            bool outOfRangeRight = i > x + searchDist;

                            if (CutOutSolidTiles[i, y])
                                break;

                            if (Tiles[i, y].Terrain == Tile.TerrainType.Solid)
                                continue;

                            if (outOfRangeLeft && outOfRangeRight)
                                break;

                            noCutTiles[x, y] = true;
                            return false;
                        }
                        break;
                    }

                    for (int i = y - 1; i >= Math.Max(y - searchMaxDist, 0); i--)
                    {
                        bool outOfRangeLeft = i < y - searchDist;

                        if (CutOutSolidTiles[x, i])
                            break;

                        if (Tiles[x, i].Terrain == Tile.TerrainType.Solid)
                            continue;

                        for (i = y + 1; i < Math.Min(y + searchMaxDist, TileSize.Y); i++)
                        {
                            bool outOfRangeRight = i > y + searchDist;

                            if (CutOutSolidTiles[x, i])
                                break;

                            if (Tiles[x, i].Terrain == Tile.TerrainType.Solid)
                                continue;

                            if (outOfRangeLeft && outOfRangeRight)
                                break;

                            noCutTiles[x, y] = true;
                            return false;
                        }
                        break;
                    }
                }
                CutOutSolidTiles[x, y] = true;
                ClearNoCutout(x, y);
                return true;
            }

            while (queue.TryDequeue(out Point point))
            {
                if (CutOutSolidTiles[point.X, point.Y] || noCutTiles[point.X, point.Y])
                    continue;

                if (Tiles[point.X, point.Y].Terrain != Tile.TerrainType.Solid)
                {
                    int neighbors = GetTileNeighbors(point.X, point.Y);

                    if (neighbors == 4)
                        CutOutSolidTiles[point.X, point.Y] = true;
                    if (neighbors == 3)
                    {
                        if (Check2TileCutout(point.X - 1, point.Y)
                         || Check2TileCutout(point.X + 1, point.Y)
                         || Check2TileCutout(point.X, point.Y - 1)
                         || Check2TileCutout(point.X, point.Y + 1))
                            CutOutSolidTiles[point.X, point.Y] = true;
                    }

                    continue;
                }

                if (!CutTile(point.X, point.Y))
                    continue;

                if (point.X > 0)
                    queue.Enqueue(new(point.X - 1, point.Y));

                if (point.X < TileSize.X - 1)
                    queue.Enqueue(new(point.X + 1, point.Y));

                if (point.Y > 0)
                    queue.Enqueue(new(point.X, point.Y - 1));

                if (point.Y < TileSize.Y - 1)
                    queue.Enqueue(new(point.X, point.Y + 1));
            }

            CutOutsDirty = false;
        }

        void FixGateData()
        {
            if (GateData is null)
                return;

            bool? leftConnection = null;

            foreach (Connection? connection in Connections)
            {
                if (connection is null || connection.Target.IsShelter)
                    continue;

                leftConnection = Exits[connection.Exit].X < TileSize.X / 2;
            }

            if (leftConnection is null)
                return;

            string? otherRegion = GateData.LeftRegionId == Region.Id ? GateData.RightRegionId : GateData.LeftRegionId;
            if (otherRegion is null)
                return;

            if (leftConnection.Value)
            {
                GateData.LeftRegionId = Region.Id;
                GateData.RightRegionId = otherRegion;
            }
            else
            {
                GateData.RightRegionId = Region.Id;
                GateData.LeftRegionId = otherRegion;
            }
        }

        public void PostRegionLoad()
        {
            if (IsShelter && Connections.Length == 1 && Connections[0] is not null)
                BoundRoom = Connections[0]!.Target;

            if (GateData is not null && IsGate)
            {
                ColorRef? leftColor = null;
                ColorRef? rightColor = null;
                ColorRef? regionColor = null;

                if (GateData.LeftRegionId is not null && GateData.RightRegionId is not null
                 && (GateData.LeftRegionId.Equals(Region.Id, StringComparison.InvariantCultureIgnoreCase)
                  || GateData.RightRegionId.Equals(Region.Id, StringComparison.InvariantCultureIgnoreCase)))
                    FixGateData();

                if (GateData.LeftRegionId is not null)
                    leftColor = ColorDatabase.GetRegionColor(GateData.LeftRegionId, null);

                if (GateData.RightRegionId is not null)
                    rightColor = ColorDatabase.GetRegionColor(GateData.RightRegionId, null);

                string? targetRegion = null;

                if (GateData.LeftRegionId is not null && !GateData.LeftRegionId.Equals(Region.Id, StringComparison.InvariantCultureIgnoreCase))
                    targetRegion = GateData.LeftRegionId;

                else if (GateData.RightRegionId is not null && !GateData.RightRegionId.Equals(Region.Id, StringComparison.InvariantCultureIgnoreCase))
                    targetRegion = GateData.RightRegionId;

                if (targetRegion is not null)
                    regionColor = ColorDatabase.GetRegionColor(targetRegion, null);

                leftColor ??= ColorRef.White;
                rightColor ??= ColorRef.White;
                regionColor ??= ColorRef.White;

                if (GateSymbols is not null)
                {
                    GateSymbols.LeftArrowColor.OriginalValue = leftColor;
                    GateSymbols.RightArrowColor.OriginalValue = rightColor;
                }

                if (GateRegionText is not null)
                {
                    if (GateData.TargetRegionName is null)
                    {
                        GateRegionText.Parent = null;
                    }
                    else
                    {
                        GateRegionText.Text.OriginalValue = $"To [c:{regionColor.GetKeyOrColorString()}]{GateData.TargetRegionName}[/c]";
                        GateRegionText.ParamsChanged();
                    }
                }
            }
        }

        protected override void DrawSelf(Renderer renderer)
        {
            if (!Loaded)
                return;

            if (CutOutsDirty)
                ProcessCutouts();

            renderer.DrawTexture(GetTileMap(), WorldPosition);

            if (base.Name is not null)
                Main.SpriteBatch.DrawStringAligned(Content.Consolas10, base.Name, renderer.TransformVector(WorldPosition + new Vector2(TileSize.X / 2, .5f)), Color.Yellow, new(.5f, 0), Color.Black);
        }
        protected override void BuildInnerConfig(UIList list)
        {
            if (Region is not null)
            {
                list.Elements.Add(new UIResizeablePanel
                {
                    Height = 100,

                    Padding = 4,

                    CanGrabTop = false,
                    CanGrabLeft = false,
                    CanGrabRight = false,
                    CanGrabBottom = true,

                    Elements =
                    {
                        new UILabel
                        {
                            Text = "Subregion",
                            Height = 15,
                            TextAlign = new(.5f)
                        },
                        new UIList
                        {
                            Top = 20,
                            Height = new(-20, 1),
                            ElementSpacing = 4
                        }.Assign(out UIList subregionList)
                    }
                });

                RadioButtonGroup group = new();

                for (int i = 0; i < Region.Subregions.Length; i++)
                {
                    Subregion subregion = Region.Subregions[i];
                    UIButton button = new()
                    {
                        Text = subregion.Name.Length == 0 ? "Main region" : subregion.Name,
                        Height = 20,
                        TextAlign = new(.5f),
                        RadioGroup = group,
                        Selectable = true,
                        Selected = subregion == Subregion.Value,
                        RadioTag = subregion,
                        SelectedTextColor = Color.Black,
                        SelectedBackColor = Color.White,
                    };

                    subregionList.Elements.Add(button);
                }

                group.ButtonClicked += (_, tag) =>
                {
                    if (tag is not Subregion subregion)
                        return;
                    Subregion.Value = subregion;
                    TileMapDirty = true;
                };
            }

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Selectable = true,
                Selected = Deathpit.Value,
                Text = "Deathpit",

                SelectedBackColor = Color.White,
                SelectedTextColor = Color.Black,

                TextAlign = new(.5f)

            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
            {
                Deathpit.Value = btn.Selected;
                TileMapDirty = true;
            }));

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Selectable = true,
                Selected = UseBetterTileCutout.Value,
                Text = "Better tile cutouts",

                SelectedBackColor = Color.White,
                SelectedTextColor = Color.Black,

                TextAlign = new(.5f)

            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
            {
                UseBetterTileCutout.Value = btn.Selected;

                CutOutsDirty = true;
                TileMapDirty = true;
                ShadeTextureDirty = true;
            }));

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Selectable = true,
                Selected = CutoutAllSolidTiles.Value,
                Text = "Cut all solid tiles",

                SelectedBackColor = Color.White,
                SelectedTextColor = Color.Black,

                TextAlign = new(.5f)

            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
            {
                CutoutAllSolidTiles.Value = btn.Selected;

                CutOutsDirty = true;
                TileMapDirty = true;
                ShadeTextureDirty = true;
            }));

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Selectable = true,
                Selected = DrawInRoomShortcuts.Value,
                Text = "Draw in-room shortcuts",

                SelectedBackColor = Color.White,
                SelectedTextColor = Color.Black,

                TextAlign = new(.5f)

            }.OnEvent(UIElement.ClickEvent, (btn, _) => DrawInRoomShortcuts.Value = btn.Selected));

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Selectable = true,
                Selected = AcidWater.Value,
                Text = "Acid water",

                SelectedBackColor = Color.White,
                SelectedTextColor = Color.Black,

                TextAlign = new(.5f)

            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
            {
                AcidWater.Value = btn.Selected;
                TileMapDirty = true;
            }));

            list.Elements.Add(new UIButton
            {
                Height = 20,

                Text = "Set acid color",
                TextAlign = new(.5f)

            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Acid color", AcidColor.Value, (_, c) =>
            {
                AcidColor.Value = c;
                Region?.MarkRoomTilemapsDirty();
            })));

            list.Elements.Add(new UIPanel
            {
                Height = 27,
                Padding = 4,

                Elements =
                {
                    new UILabel
                    {
                        Top = 3,
                        Width = 90,
                        Height = 20,
                        Text = "Water level:",
                        WordWrap = false,
                    },
                    new UINumberInput
                    {
                        Width = new(-90, 1),
                        Left = new(0, 1, -1),
                        Value = WaterLevel.Value,
                        AllowDecimal = false,
                        AllowNegative = true,

                    }.OnEvent(UINumberInput.ValueChanged, (inp, _) => { WaterLevel.Value = (int)inp.Value; TileMapDirty = true; }),
                }
            });
        }

        protected override JsonNode? SaveInnerJson(bool forCopy)
        {
            JsonObject obj = new JsonObject()
            .SaveProperty(Deathpit)
            .SaveProperty(Subregion)
            .SaveProperty(UseBetterTileCutout)
            .SaveProperty(CutoutAllSolidTiles)
            .SaveProperty(WaterLevel)
            .SaveProperty(DrawInRoomShortcuts);

            if (IsGate && GateData is not null)
                obj["gateData"] = GateData.SaveJson();

            return obj;
        }
        protected override void LoadInnerJson(JsonNode node, bool shallow)
        {
            Subregion.LoadFromJson(node);
            Deathpit.LoadFromJson(node);
            UseBetterTileCutout.LoadFromJson(node);
            CutoutAllSolidTiles.LoadFromJson(node);
            WaterLevel.LoadFromJson(node);
            DrawInRoomShortcuts.LoadFromJson(node);

            if (!shallow && node.TryGet("gateData", out JsonObject? gateData))
            {
                GateData ??= new();
                GateData.LoadJson(gateData);

                // Set non-colored text to properly align it before loading new position
                if (GateData.TargetRegionName is not null && GateRegionText is not null)
                {
                    GateRegionText.Text.OriginalValue = $"To {GateData.TargetRegionName}";
                    GateRegionText.ParamsChanged();
                }
            }

            TileMapDirty = true;
        }

        public override string ToString()
        {
            return Name!;
        }

        public record class Connection(Room Target, int Exit, int TargetExit)
        {
            public override string ToString()
            {
                return $"{Exit} -> {Target.Name}[{TargetExit}]";
            }
        }
        public record class Shortcut(Point Entrance, Point Target, Tile.ShortcutType Type);
        public record class Effect(string Name, float Amount);

        public struct Tile
        {
            public TerrainType Terrain;
            public ShortcutType Shortcut;
            public TileAttributes Attributes;

            [Flags]
            public enum TileAttributes
            {
                None = 0,
                VerticalBeam = 1,
                HorizontalBeam = 2,
                WallBehind = 4,
                Hive = 8,
                Waterfall = 16,
                GarbageHole = 32,
                WormGrass = 64
            }

            public enum TerrainType
            {
                Air,
                Solid,
                Slope,
                Floor,
                ShortcutEntrance
            }

            public enum ShortcutType
            {
                None,
                Normal,
                RoomExit,
                CreatureHole,
                NPCTransportation,
                RegionTransportation,
            }
        }
    }
}