using Cornifer.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace Cornifer
{
    public static class StaticData
    {
        public static readonly Dictionary<string, Color> PearlMainColors = new()
        {
            ["SI_west"] = new(0.01f, 0.01f, 0.01f),
            ["SI_top"] = new(0.01f, 0.01f, 0.01f),
            ["SI_chat3"] = new(0.01f, 0.01f, 0.01f),
            ["SI_chat4"] = new(0.01f, 0.01f, 0.01f),
            ["SI_chat5"] = new(0.01f, 0.01f, 0.01f),
            ["Spearmasterpearl"] = new(0.04f, 0.01f, 0.04f),
            ["SU_filt"] = new(1f, 0.75f, 0.9f),
            ["DM"] = new(0.95686275f, 0.92156863f, 0.20784314f),
            ["LC"] = new(0f, 0.4f, 0.01569f),
            ["LC_second"] = new(0.6f, 0f, 0f),
            ["OE"] = new(0.54901963f, 0.36862746f, 0.8f),
            ["MS"] = new(0.8156863f, 0.89411765f, 0.27058825f),
            ["RM"] = new(0.38431373f, 0.18431373f, 0.9843137f),
            ["Rivulet_stomach"] = new(0.5882353f, 0.87058824f, 0.627451f),
            ["CL"] = new(0.48431373f, 0.28431374f, 1f),
            ["VS"] = new(0.53f, 0.05f, 0.92f),
            ["BroadcastMisc"] = new(0.9f, 0.7f, 0.8f),
            ["CC"] = new(0.9f, 0.6f, 0.1f),
            ["LF_west"] = new(1f, 0f, 0.3f),
            ["LF_bottom"] = new(1f, 0.1f, 0.1f),
            ["HI"] = new(0.007843138f, 0.19607843f, 1f),
            ["SH"] = new(0.2f, 0f, 0.1f),
            ["DS"] = new(0f, 0.7f, 0.1f),
            ["SB_filtration"] = new(0.1f, 0.5f, 0.5f),
            ["SB_ravine"] = new(0.01f, 0.01f, 0.01f),
            ["GW"] = new(0f, 0.7f, 0.5f),
            ["SL_bridge"] = new(0.4f, 0.1f, 0.9f),
            ["SL_moon"] = new(0.9f, 0.95f, 0.2f),
            ["SU"] = new(0.5f, 0.6f, 0.9f),
            ["UW"] = new(0.4f, 0.6f, 0.4f),
            ["SL_chimney"] = new(1f, 0f, 0.55f),
            ["Red_stomach"] = new(0.6f, 1f, 0.9f),
            [""] = new(0.7f, 0.7f, 0.7f),
        };
        public static readonly Dictionary<string, Color?> PearlHighlightColors = new()
        {
            ["SI_chat3"] = new(0.4f, 0.1f, 0.6f),
            ["SI_chat4"] = new(0.4f, 0.6f, 0.1f),
            ["SI_chat5"] = new(0.6f, 0.1f, 0.4f),
            ["Spearmasterpearl"] = new(0.95f, 0f, 0f),
            ["RM"] = new(1f, 0f, 0f),
            ["LC_second"] = new(0.8f, 0.8f, 0f),
            ["CL"] = new(1f, 0f, 0f),
            ["VS"] = new(1f, 0f, 1f),
            ["CC"] = new(1f, 1f, 0f),
            ["GW"] = new(0.5f, 1f, 0.5f),
            ["HI"] = new(0.5f, 0.8f, 1f),
            ["SH"] = new(1f, 0.2f, 0.6f),
            ["SI_top"] = new(0.1f, 0.4f, 0.6f),
            ["SI_west"] = new(0.1f, 0.6f, 0.4f),
            ["SL_bridge"] = new(1f, 0.4f, 1f),
            ["SB_ravine"] = new(0.6f, 0.1f, 0.4f),
            ["UW"] = new(1f, 0.7f, 1f),
            ["SL_chimney"] = new(0.8f, 0.3f, 1f),
            ["Red_stomach"] = new(1f, 1f, 1f),
        };

        public static List<Slugcat> Slugcats = new();

		public static readonly HashSet<string> HollowSlugcats = new() { "White", "Yellow", "Red", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint" };

		public static readonly List<string> PlacedObjectTypes = new()
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
            "HangingPearls","Lantern","ExitSymbolAncientShelter","BlinkingFlower", "SpinningTopSpot", "WarpPoint", "Pomegranate", "PlacedBoxWorm"
		};
		public static readonly Dictionary<string, string[]> TiedSandboxIDs = new() {
			["CicadaA"] = new[] { "CicadaB" },
			["SmallCentipede"] = new[] { "MediumCentipede" },
			["BigNeedleWorm"] = new[] { "SmallNeedleWorm" },
		};

		public static readonly HashSet<string> VanillaRegions = new() { "CC", "DS", "HI", "GW", "SI", "SU", "SH", "SL", "LF", "UW", "SB", "SS" };

        // TODO: equivalences.txt
        public static readonly Dictionary<string, List<string>> RegionEquivalences = new()
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
			["Watcher"] = new() { "SU", "HI", "CC", "SH", "VS", "LF", "WARA", "WARB", "WARC", "WARD", "WARE", "WARF", "WARG", "WAUA", "WBLA", "WDSR", "WGWR", "WHIR", "WORA", "WPTA", "WRFA", "WRFB", "WRRA", "WRSA", "WSKA", "WSKB", "WSKC", "WSKD", "WSSR", "WSUR", "WTDA", "WTDB", "WVWA" },
			[""] = new() { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "SI", "LF", "UW", "SS", "SB" },
        };

		public static readonly Dictionary<string, Vector2> VistaRooms = new() {
			["HI_B04"] = new(214f, 615f),
			["HI_D01"] = new(1765f, 655f),
			["HI_C04"] = new(800f, 768f),
			["SU_B12"] = new(1180f, 382f),
			["SU_A04"] = new(265f, 415f),
			["SU_C01"] = new(450f, 1811f),
			["GW_D01"] = new(1603f, 595f),
			["GW_E02"] = new(2608f, 621f),
			["GW_C09"] = new(607f, 595f),
			["UW_A07"] = new(805f, 616f),
			["UW_J01"] = new(860f, 1534f),
			["UW_C02"] = new(493f, 490f),
			["CC_B12"] = new(455f, 1383f),
			["CC_A10"] = new(734f, 506f),
			["CC_C05"] = new(449f, 2330f),
			["DS_A19"] = new(467f, 545f),
			["DS_A05"] = new(172f, 490f),
			["DS_C02"] = new(541f, 1305f),
			["SI_C07"] = new(539f, 2354f),
			["SI_D07"] = new(200f, 400f),
			["SI_D05"] = new(1045f, 1258f),
			["SH_A14"] = new(273f, 556f),
			["SH_C08"] = new(2159f, 481f),
			["SH_B05"] = new(733f, 453f),
			["SL_B04"] = new(390f, 2258f),
			["SL_B01"] = new(389f, 1448f),
			["SL_C04"] = new(542f, 1295f),
			["LF_C01"] = new(2792f, 423f),
			["LF_A10"] = new(421f, 412f),
			["LF_D02"] = new(1220f, 631f),
			["SB_H02"] = new(1559f, 472f),
			["SB_E04"] = new(1668f, 567f),
			["SB_D04"] = new(483f, 1045f),
			["VS_H02"] = new(603f, 3265f),
			["VS_C03"] = new(82f, 983f),
			["VS_F02"] = new(1348f, 533f),
			["OE_RUINCourtYard"] = new(2133f, 1397f),
			["OE_TREETOP"] = new(468f, 1782f),
			["OE_RAIL01"] = new(2420f, 1378f),
			["LC_FINAL"] = new(2700f, 500f),
			["LC_SUBWAY01"] = new(1693f, 564f),
			["LC_tallestconnection"] = new(153f, 242f),
			["RM_CONVERGENCE"] = new(1860f, 670f),
			["RM_I03"] = new(276f, 2270f),
			["RM_ASSEMBLY"] = new(1550f, 586f),
			["DM_LEG06"] = new(400f, 388f),
			["DM_O06"] = new(2178f, 2159f),
			["DM_LAB1"] = new(486f, 324f),
			["UG_GUTTER02"] = new(163f, 241f),
			["UG_A16"] = new(640f, 354f),
			["UG_D03"] = new(857f, 1826f),
			["CL_C05"] = new(540f, 1213f),
			["CL_H02"] = new(2407f, 1649f),
			["CL_CORE"] = new(471f, 373f),
		};

		public static readonly HashSet<string> NonPickupObjectsWhitelist = new()
		{
			"GhostSpot", "BlueToken", "GoldToken",
			"RedToken", "WhiteToken", "DevToken", "GreenToken",
			"DataPearl", "UniqueDataPearl", "ScavengerOutpost",
			"HRGuard", "TempleGuard", "MoonCloak", "SpinningTopSpot",
			"WarpPoint", "TerrainHandle", "WaterCycleTop", "WaterCycleBottom"
		};

		public static readonly Point[] Directions = new Point[] { new (0, -1), new (1, 0), new (0, 1), new (-1, 0) };

		public static void Init()
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

			if (RWAssets.CurrentInstallation?.IsWatcher is true)
			{
				Slugcats.Add(new("Watcher", "Watcher", true, new(25, 15, 48), Color.White, "HI_W14"));
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
