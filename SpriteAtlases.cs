using Cornifer.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cornifer
{
    public static class SpriteAtlases
    {
        const string AtlasesPath = "Assets/Atlases";

        public static Dictionary<string, AtlasSprite> Sprites = new();

        static Dictionary<string, (string Atlas, Color Color)> ObjectSprites = new();

        static Dictionary<string, (Rectangle Frame, Color Color)> ObjectSpriteFrames = new()
        {
            { "KarmaFlower",      (new(76, 0,   23, 23), new(255, 255, 255, 255)) },
            { "SeedCob",          (new(40, 0,   35, 38), new(255, 255, 255, 255)) },
            { "GhostSpot",        (new(0,  0,   38, 48), new(255, 255, 255, 255)) },
            { "BlueToken",        (new(78, 24,  10, 20), new(255, 255, 255, 150)) },
            { "GoldToken",        (new(89, 24,  10, 20), new(255, 255, 255, 150)) },
            { "PurpleToken",      (new(111, 24, 10, 20), new(255, 255, 255, 150)) },
            { "RedToken",         (new(100, 0,  10, 20), new(255, 255, 255, 150)) },
            { "DevToken",         (new(100, 24, 10, 20), new(255, 255, 255, 150)) },
            { "GreenToken",       (new(111, 0,  10, 20), new(255, 255, 255, 150)) },
            { "DataPearl",        (new(39, 39,  11, 11), new(255, 255, 255, 255)) },
            { "UniqueDataPearl",  (new(39, 39,  11, 11), new(255, 255, 255, 255)) },
            { "Slugcat",          (new(51, 39,  20, 19), new(255, 255, 255, 255)) },
            { "ScavengerOutpost", (new(90, 45,  11, 15), new(255, 255, 255, 255)) },
            { "KarmaShrine",      (new(72, 45,  17, 17), new(255, 255, 255, 255)) },
            { "MoonCloak",        (new(1, 49,   21, 25), new(255, 255, 255, 255)) },
        };

        static Dictionary<string, (Rectangle Frame, Color Color)> MiscSpriteFrames = new()
        {
            { "ArrowLeft",     (new(0, 0,  22, 13), new(255, 255, 255, 255)) },
            { "ArrowRight",    (new(0, 13, 22, 13), new(255, 255, 255, 255)) },
            { "KarmaR",        (new(23, 0, 36, 36), new(255, 255, 255, 255)) },
        };

        static List<string> SlugcatIconOrder = new() { "White", "Yellow", "Red", "Night", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint", "Inv" };

        public static void Load()
        {
            foreach (var (objectName, objectSprite) in ObjectSpriteFrames)
            {
                string name = "Object_" + objectName;
                Sprites[name] = new(name, Content.Objects, objectSprite.Frame, objectSprite.Color, false);
            }

            foreach (var (objectName, objectSprite) in MiscSpriteFrames)
            {
                string name = "Misc_" + objectName;
                Sprites[name] = new(name, Content.MiscSprites, objectSprite.Frame, objectSprite.Color, false);
            }

            string atlasesPath = Path.Combine(Main.MainDir, AtlasesPath);

            if (!Directory.Exists(atlasesPath))
                return;

            foreach (string atlasFile in Directory.EnumerateFiles(atlasesPath, "*.txt"))
            {
                string textureFile = Path.ChangeExtension(atlasFile, ".png");
                if (!File.Exists(atlasFile))
                    continue;

                JsonNode json;
                using (FileStream fs = File.OpenRead(atlasFile))
                {
                    json = JsonNode.Parse(fs)!;
                }

                if (json["frames"] is JsonObject frames)
                {
                    Texture2D texture = Texture2D.FromFile(Main.Instance.GraphicsDevice, textureFile);
                    foreach (var (assetName, assetFrameData) in frames)
                        if (assetName is not null && assetFrameData is not null)
                        {
                            string asset = Path.ChangeExtension(assetName, null);

                            try
                            {
                                JsonObject? frame = assetFrameData["frame"] as JsonObject;

                                if (frame is null)
                                    continue;

                                int x = (int)frame["x"]!;
                                int y = (int)frame["y"]!;
                                int w = (int)frame["w"]!;
                                int h = (int)frame["h"]!;

                                Sprites[asset] = new(asset, texture, new(x, y, w, h), Color.White, true);
                            }
                            catch { }
                        }
                }
            }

            string filepath = Path.Combine(Main.MainDir, "Assets\\Settings\\ObjectSprites.json");
            if (File.Exists(filepath))
            {
                using FileStream fs = File.OpenRead(filepath);
                JsonObject? obj = JsonSerializer.Deserialize<JsonObject>(fs, new JsonSerializerOptions() { AllowTrailingCommas = true });
                if (obj is not null)
                {
                    foreach (KeyValuePair<string, JsonNode> entry in obj)
                    {
                        if (entry.Value is not JsonObject dict) continue;

                        if (!dict.TryGet("sprite", out string? sprite)) continue;

                        Color? color = null;
                        if (dict.TryGet("color", out string? colorString))
                            color = ColorDatabase.ParseColor(colorString);

                        if (!color.HasValue) color = Color.White;

                        ObjectSprites[entry.Key] = (sprite, color.Value);
                    }
                }
            }

            foreach (var (spriteName, spriteData) in ObjectSprites)
                if (Sprites.TryGetValue(spriteData.Atlas, out AtlasSprite? sprite))
                {
                    string name = "Object_" + spriteName;
                    Sprites[name] = new(name, sprite.Texture, sprite.Frame, spriteData.Color, true);
                }

            for (int i = 0; i < SlugcatIconOrder.Count; i++)
            {
                string slugcat = SlugcatIconOrder[i];

                string name = "SlugcatIcon_" + slugcat;
                Rectangle frame = new(i * 8, 0, 8, 8);
                Sprites[name] = new(name, Content.SlugcatIcons, frame, Color.White, false);

                name = "SlugcatDiamond_" + slugcat;
                frame = new(i * 9, 8, 9, 9);
                Sprites[name] = new(name, Content.SlugcatIcons, frame, Color.White, false);

                name = "SlugcatHollowDiamond_" + slugcat;
                frame = new(i * 9, 17, 9, 9);
                Sprites[name] = new(name, Content.SlugcatIcons, frame, Color.White, false);

                name = "Slugcat_" + slugcat;
                frame = new(i * 20, 26, 20, 19);
                Sprites[name] = new(name, Content.SlugcatIcons, frame, Color.White, false);
            }
        }

        public static AtlasSprite? GetSpriteOrNull(string name)
        {
            if (!Sprites.TryGetValue(name, out AtlasSprite? sprite))
                sprite = null;
            return sprite;
        }
    }
}
