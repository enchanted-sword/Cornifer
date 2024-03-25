using Cornifer.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Windows.Forms;
using Cornifer.Json;

namespace Cornifer
{
    public static class StaticData
    {
        public static Dictionary<string, Color> PearlMainColors = new()
        { 
            [""] = new(0.7f, 0.7f, 0.7f),
        };
        public static Dictionary<string, Color?> PearlHighlightColors = new();

        public static Dictionary<string, Vector2> VistaRooms = new();

        public static List<Slugcat> Slugcats = new();

        public static Dictionary<string, string> GateSymbols = new();

        public static List<string> PlacedObjectTypes = new()
        {
            "GreenToken","WhiteToken","Germinator","RedToken","OEsphere","MSArteryPush","GooieDuck","LillyPuck",
            "GlowWeed","BigJellyFish","RotFlyPaper","MoonCloak","DandelionPeach","KarmaShrine","Stowaway",
            "HRGuard","DevToken","LightSource","FlareBomb","PuffBall","TempleGuard","LightFixture","DangleFruit",
            "CoralStem","CoralStemWithNeurons","CoralNeuron","CoralCircuit","WallMycelia","ProjectedStars","ZapCoil",
            "SuperStructureFuses","GravityDisruptor","SpotLight","DeepProcessing","Corruption","CorruptionTube",
            "CorruptionDarkness","StuckDaddy","SSLightRod","CentipedeAttractor","DandelionPatch","GhostSpot","DataPearl",
            "UniqueDataPearl","SeedCob","DeadSeedCob","WaterNut","JellyFish","KarmaFlower","Mushroom","SlimeMold",
            "FlyLure","CosmeticSlimeMold","CosmeticSlimeMold2","FirecrackerPlant","VultureGrub","DeadVultureGrub",
            "VoidSpawnEgg","ReliableSpear","SuperJumpInstruction","ProjectedImagePosition","ExitSymbolShelter",
            "ExitSymbolHidden","NoSpearStickZone","LanternOnStick","ScavengerOutpost","TradeOutpost","ScavengerTreasury",
            "ScavTradeInstruction","CustomDecal","InsectGroup","PlayerPushback","MultiplayerItem","SporePlant",
            "GoldToken","BlueToken","DeadTokenStalk","NeedleEgg","BrokenShelterWaterLevel","BubbleGrass","Filter",
            "ReliableIggyDirection","Hazer","DeadHazer","Rainbow","LightBeam","NoLeviathanStrandingZone",
            "FairyParticleSettings","DayNightSettings","EnergySwirl","LightningMachine","SteamPipe","WallSteamer",
            "Vine","VultureMask","SnowSource","DeathFallFocus","CellDistortion","LocalBlizzard","NeuronSpawner",
            "HangingPearls","Lantern","ExitSymbolAncientShelter","BlinkingFlower"
        };

        public static HashSet<string> VanillaRegions = new() { "CC", "DS", "HI", "GW", "SI", "SU", "SH", "SL", "LF", "UW", "SB", "SS" };

        // TODO: equivalences.txt
        public static Dictionary<string, List<string>> RegionEquivalences = new()
        {
            ["LM"] = new() { "SL" },
            ["RM"] = new() { "SS" },
            ["UG"] = new() { "DS" },
            ["CL"] = new() { "SH" },
            ["MS"] = new() { "DM" },
            ["SL"] = new() { "LM" },
            ["SS"] = new() { "RM" },
            ["DS"] = new() { "UG" },
            ["SH"] = new() { "CL" },
            ["DM"] = new() { "MS" },
        };

        // Slugcat -> { DefaultRegion -> StoryRegion }
        // Saint -> { DS -> UG }
        // TODO: equivalences.txt Region.GetProperRegionAcronym
        public static Dictionary<string, Dictionary<string, string>> SlugcatRegionReplacements = new()
        {
            [""] = new()
            {
                ["UX"] = "UW",
                ["SX"] = "SS"
            },

            ["Spear"] = new()
            {
                ["SL"] = "LM"
            },

            ["Artificer"] = new()
            {
                ["SL"] = "LM"
            },

            ["Saint"] = new()
            {
                ["DS"] = "UG",
                ["SS"] = "RM",
                ["SH"] = "CL",
            },

            ["Rivulet"] = new()
            {
                ["SS"] = "RM",
            }
        };

        public static Dictionary<string, List<string>> SlugcatRegionAvailability = new()
        {
            ["Rivulet"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "RM", "SB", "MS" },
            ["Artificer"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "LM", "SI", "LF", "UW", "SS", "SB", "LC" },
            ["Saint"] = new() { "SU", "HI", "UG", "CC", "GW", "VS", "CL", "SL", "SI", "LF", "SB", "HR" },
            ["Spear"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "LM", "SI", "LF", "UW", "SS", "SB", "DM" },
            ["Gourmand"] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "SS", "SB", "OE" },
            [""] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "SS", "SB" },
        };

        public static void Init()
        {
            InitSlugcats();
            InitPearls();
            InitVistas();
            InitGateSymbols();
        }

        private static void InitSlugcats()
        {
            string filename = Path.Combine(Main.MainDir, "Assets\\Settings\\Slugcats.json");
            if (File.Exists(filename))
            {

                using FileStream fs = File.OpenRead(filename);
                JsonObject? obj = JsonSerializer.Deserialize<JsonObject>(fs, new JsonSerializerOptions() { AllowTrailingCommas = true });
                if (obj is not null)
                {
                    Slugcats = new();
                    foreach (KeyValuePair<string, JsonNode> entry in obj)
                    {
                        if (entry.Value is not JsonObject dict) continue;
                        Slugcats.Add(new(entry.Key, dict));
                    }
                    return;
                }
            }
            //runs if loading the json fails
            InitOldSlugcats();
        }

        private static void InitOldSlugcats()
        {
            Slugcats = new()
            {
                new("Yellow",    "Monk",        true,  new(255, 255, 115), Color.Black, "SU_C04"),
                new("White",     "Survivor",    true,  new(255, 255, 255), Color.Black, "SU_C04"),
                new("Red",       "Hunter",      true,  new(255, 115, 115), Color.Black, "LF_H01"),
                new("Night",     "Nightcat",    false, new(25,  15,  48),  Color.White, null),
            };

            if (RWAssets.CurrentInstallation?.IsDownpour is true)
            {
                Slugcats.Add(new("Gourmand", "Gourmand", true, new(240, 193, 151), Color.Black, "SH_GOR02"));
                Slugcats.Add(new("Artificer", "Artificer", true, new(112, 35, 60), Color.White, "GW_A24"));
                Slugcats.Add(new("Rivulet", "Rivulet", true, new(145, 204, 240), Color.Black, "DS_RIVSTART"));
                Slugcats.Add(new("Spear", "Spearmaster", true, new(79, 46, 105), Color.White, "GATE_OE_SU"));
                Slugcats.Add(new("Saint", "Saint", true, new(170, 241, 86), Color.Black, "SI_SAINTINTRO"));
                Slugcats.Add(new("Inv", "Inv", false, new(0, 19, 58), Color.White, "SH_E01"));
            }
        }

        private static void InitPearls()
        {
            string filename = Path.Combine(Main.MainDir, "Assets\\Settings\\PearlColors.json");
            if (!File.Exists(filename)) return;

            using FileStream fs = File.OpenRead(filename);
            JsonObject? obj = JsonSerializer.Deserialize<JsonObject>(fs, new JsonSerializerOptions() { AllowTrailingCommas = true });

            if (obj is null) return;

            foreach (KeyValuePair<string, JsonNode> entry in obj)
            {
                if (!obj.TryGet(entry.Key, out JsonArray? colors)) continue;

                string[]? colorArray = colors.Deserialize<string[]>();
                if (colorArray is not null)
                {
                    if (colorArray.Length >= 1)
                    {
                        Color? color = ColorDatabase.ParseColor(colorArray[0]);
                        if (color.HasValue)
                            PearlMainColors[entry.Key] = (Color)color;
                    }
                    if (colorArray.Length >= 2)
                    {
                        Color? color = ColorDatabase.ParseColor(colorArray[1]);
                        if (color.HasValue)
                            PearlHighlightColors[entry.Key] = (Color)color;
                    }
                }
            }
        }

        private static void InitVistas()
        {
            string filename = Path.Combine(Main.MainDir, "Assets\\Settings\\VistaRooms.json");
            if (!File.Exists(filename)) return;

            using FileStream fs = File.OpenRead(filename);
            JsonObject? obj = JsonSerializer.Deserialize<JsonObject>(fs, new JsonSerializerOptions() { AllowTrailingCommas = true });

            if (obj is null) return;

            foreach (KeyValuePair<string, JsonNode> entry in obj)
            {
                if (obj.TryGet(entry.Key, out JsonNode? node) && node != null)
                    VistaRooms[entry.Key] = JsonTypes.LoadVector2(node);
            }
        }
        private static void InitGateSymbols()
        {
            string filename = Path.Combine(Main.MainDir, "Assets\\Settings\\GateSprites.json");
            if (!File.Exists(filename)) return;

            using FileStream fs = File.OpenRead(filename);
            JsonObject? obj = JsonSerializer.Deserialize<JsonObject>(fs, new JsonSerializerOptions() { AllowTrailingCommas = true });

            if (obj is null) return;

            foreach (KeyValuePair<string, JsonNode> entry in obj)
            {
                if (obj.TryGet(entry.Key, out string? symbol))
                    GateSymbols[entry.Key] = symbol;
            }
        }

        [return: NotNullIfNotNull(nameof(acronym))]
        public static string? GetProperRegionAcronym(string? acronym, Slugcat? slugcat)
        {
            if (acronym is null)
                return null;

            if (SlugcatRegionReplacements[""].TryGetValue(acronym, out string? defaultAcronym))
                return defaultAcronym;

            if (slugcat is not null
             && SlugcatRegionReplacements.TryGetValue(slugcat.WorldStateSlugcat, out var slugcatAcronyms)
             && slugcatAcronyms.TryGetValue(acronym, out string? slugcatAcronym))
                return slugcatAcronym;

            return acronym;
        }

        public static bool AreRegionsEquivalent(string a, string b)
        {
            if (a.Equals(b, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (RegionEquivalences.TryGetValue(a, out var equivalencesA) && equivalencesA.Contains(b))
                return true;

            if (RegionEquivalences.TryGetValue(b, out var equivalencesB) && equivalencesB.Contains(a))
                return true;

            return false;
        }

        public static Color GetPearlColor(string type)
        {
            Color color = PearlMainColors.GetValueOrDefault(type, PearlMainColors[""]);
            Color? color2 = PearlHighlightColors.GetValueOrDefault(type, null);
            if (color2.HasValue)
            {
                // color = Custom.Screen(color, color2.Value * Custom.QuickSaturation(color2.Value) * 0.5f);

                float max = Math.Max(color2.Value.R, Math.Max(color2.Value.G, color2.Value.B)) / 255f;
                float min = Math.Min(color2.Value.R, Math.Min(color2.Value.G, color2.Value.B)) / 255f;

                float sat = (min - max) / -max;

                Color v = color2.Value * sat * 0.5f;

                color = new Color(1f - (1f - color.R / 255f) * (1f - v.R / 255f), 1f - (1f - color.G / 255f) * (1f - v.G / 255f), 1f - (1f - color.B / 255f) * (1f - v.B / 255f));
            }
            else
            {
                color = Microsoft.Xna.Framework.Color.Lerp(color, Microsoft.Xna.Framework.Color.White, 0.15f);
            }
            if (color.R / 255f < 0.1f && color.G / 255f < 0.1f && color.B / 255f < 0.1f)
            {
                // color = Color.Lerp(color, Menu.MenuRGB(Menu.MenuColors.MediumGrey), 0.3f);

                Color menurgb = new(169, 164, 178);
                color = Microsoft.Xna.Framework.Color.Lerp(color, menurgb, 0.3f);
            }

            return color;
        }
    }
}
