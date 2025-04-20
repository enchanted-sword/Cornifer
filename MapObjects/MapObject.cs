using Cornifer.Json;
using Cornifer.Renderers;
using Cornifer.Structures;
using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cornifer.MapObjects
{
    public abstract class MapObject
    {
        static Regex RandomNameSuffixRegex = new(@"_([0-9A-Fa-f]+)", RegexOptions.Compiled);

        static ShadeRenderer? ShadeTextureRenderer;
        internal static RenderTarget2D? ShadeRenderTarget;

        public virtual bool ParentSelected => Parent != null && (Parent.Selected || Parent.ParentSelected);
        public bool Selected => Main.SelectedObjects.Contains(this);

        public ObjectProperty<bool> ActiveProperty = new("active", true);

        public abstract bool CanSetActive { get; }
        public virtual bool CanCopy { get; } = false;
        public virtual Type CopyType => GetType();

        public virtual bool Active { get => ActiveProperty.Value; set => ActiveProperty.Value = value; }
        public virtual bool Selectable { get; set; } = true;
        public virtual bool LoadCreationForbidden { get; set; } = false;
        public virtual bool NeedsSaving { get; set; } = true;

        public virtual Vector2 ParentPosition { get; set; }
        public virtual Vector2 Size { get; }

        public virtual bool AllowModifyLayer => true;
        public ObjectProperty<Layer, string> RenderLayer = new("layer", null!);
        abstract protected Layer DefaultLayer { get; }

        public Vector2 VisualPosition => WorldPosition + VisualOffset;
        public virtual Vector2 VisualSize => Size + new Vector2(ShadeSize * 2);
        public virtual Vector2 VisualOffset => new Vector2(-ShadeSize);

        public virtual int? ShadeCornerRadius { get; set; }
        public virtual int ShadeSize { get; set; }

        public MapObject? Parent { get; set; }

        public virtual string? Name { get; set; }

        internal UIElement? ConfigCache { get; set; }
        internal Texture2D? ShadeTexture;
        int ShadeTextureShadeSize = 0;

        public bool ShadeTextureDirty { get; set; }
        protected bool Shading { get; private set; }

        public Vector2 WorldPosition
        {
            get => Parent is null ? ParentPosition : Parent.WorldPosition + ParentPosition;
            set
            {
                if (Parent is not null)
                    ParentPosition = value - Parent.WorldPosition;
                else
                    ParentPosition = value;
            }
        }

        public MapObjectCollection Children { get; }

        public MapObject()
        {
            Children = new(this);
            RenderLayer.OriginalValue = DefaultLayer;
            RenderLayer.SaveValue = v => v.Id;
            RenderLayer.LoadValue = v => Main.Layers.FirstOrDefault(l => l.Id == v) ?? RenderLayer.OriginalValue;
        }

        public void DrawShade(Renderer renderer, Layer renderLayer)
        {
            if (!Active)
                return;

            if (renderLayer == RenderLayer.Value)
            {
                EnsureCorrectShadeTexture();

                if (ShadeSize > 0 && ShadeTexture is not null)
                {
                    if (renderer is ICapturingRenderer caps)
                        caps.BeginObjectCapture(this, true);

                    renderer.DrawTexture(ShadeTexture, WorldPosition - new Vector2(ShadeSize));

                    if (renderer is ICapturingRenderer cape)
                        cape.EndObjectCapture();
                }
            }

            foreach (MapObject child in Children)
                child.DrawShade(renderer, renderLayer);
        }

        public void Draw(Renderer renderer, Layer renderLayer)
        {
            if (!Active)
                return;

            if (renderLayer == RenderLayer.Value)
            {
                if (renderer is ICapturingRenderer caps)
                    caps.BeginObjectCapture(this, false);

                DrawSelf(renderer);

                if (renderer is ICapturingRenderer cape)
                    cape.EndObjectCapture();
            }

            foreach (MapObject child in Children)
                child.Draw(renderer, renderLayer);
        }

        protected abstract void DrawSelf(Renderer renderer);

        public UIElement? Config
        {
            get
            {
                ConfigCache ??= BuildConfig();
                UpdateConfig();
                return ConfigCache;
            }
        }

        private UIList? ConfigChildrenList;
        private UIList? ConfigLayerList;
		public static Dictionary<string, string> WarpMap = new()
		{
			["WARA"] = "Shattered Terrace",
			["WARB"] = "Salination",
			["WARC"] = "Fetid Glen",
			["WARD"] = "Cold Storage",
			["WARE"] = "Heat Ducts",
			["WARF"] = "Aether Ridge",
			["WARG"] = "The Surface",
			["WAUA"] = "Ancient Urban",
			["WBLA"] = "Badlands",
			["WDSR"] = "Decaying Tunnels",
			["WGWR"] = "Infested Wastes",
			["WHIR"] = "Corrupted Factories",
			["WORA"] = "Outer Rim",
			["WPTA"] = "Signal Spires",
			["WRFA"] = "Coral Caves",
			["WRFB"] = "Turbulent Pump",
			["WRRA"] = "Rusted Wrecks",
			["WRSA"] = "Daemon",
			["WSKA"] = "Torrential Railways",
			["WSKB"] = "Sunlit Port",
			["WSKC"] = "Stormy Coast",
			["WSKD"] = "Shrouded Coast",
			["WSSR"] = "Unfortunate Evolution",
			["WSUR"] = "Crumbling Fringes",
			["WTDA"] = "Torrid Desert",
			["WTDB"] = "Desolate Tract",
			["WVWA"] = "Verdant Waterways"
		};
		public static Dictionary<string, Vector2> VistaRooms = new() {
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
		public static HashSet<string> NonPickupObjectsWhitelist = new()
		{
			"GhostSpot", "BlueToken", "GoldToken",
			"RedToken", "WhiteToken", "DevToken", "GreenToken",
			"DataPearl", "UniqueDataPearl", "ScavengerOutpost",
			"HRGuard", "TempleGuard", "MoonCloak", "SpinningTopSpot", "WarpPoint",
		};
		public static Point[] Directions = new Point[] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };

        public static HashSet<string> HideObjectTypes = new()
        {
            "DevToken"
        };
		public static Dictionary<string, string[]> TiedSandboxIDs = new()
        {
            ["CicadaA"] = new[] { "CicadaB" },
            ["SmallCentipede"] = new[] { "MediumCentipede" },
            ["BigNeedleWorm"] = new[] { "SmallNeedleWorm" },
        };
		public static HashSet<string> HollowSlugcats = new() { "White", "Yellow", "Red", "Gourmand", "Artificer", "Rivulet", "Spear", "Saint" };

        private UIElement? BuildConfig()
        {
            UIList list = new()
            {
                ElementSpacing = 4,

                Elements =
                {
                    new UILabel
                    {
                        Height = 20,
                        Text = Name,
                        WordWrap = false,
                        TextAlign = new(.5f)
                    },

                    new UICollapsedPanel 
                    {
                        BorderColor = new(100, 100, 100),
                        HeaderText = "Children",
                        Collapsed = true,

                        Content = new UIResizeablePanel
                        {
                            BorderColor = Color.Transparent,
                            BackColor = Color.Transparent,
                            Padding = 4,
                            Height = 100,

                            CanGrabLeft = false,
                            CanGrabRight = false,
                            CanGrabTop = false,

                            MinHeight = 30,

                            Elements =
                            {
                                new UIList
                                {
                                    ElementSpacing = 4,
                                }.Assign(out ConfigChildrenList)
                            }
                        }
                    },
                }
            };

            if (AllowModifyLayer)
            {
                list.Elements.Add(new UICollapsedPanel
                {
                    BorderColor = new(100, 100, 100),
                    HeaderText = "Layer",
                    Collapsed = true,

                    Content = new UIResizeablePanel
                    {
                        BorderColor = Color.Transparent,
                        BackColor = Color.Transparent,
                        Padding = 4,
                        Height = 100,

                        CanGrabLeft = false,
                        CanGrabRight = false,
                        CanGrabTop = false,

                        MinHeight = 30,

                        Elements =
                        {
                            new UIList
                            {
                                ElementSpacing = 4,
                            }.Assign(out ConfigLayerList)
                        }
                    }
                });
            }

            BuildInnerConfig(list);
            return list;
        }
        private void UpdateConfig()
        {
            if (ConfigChildrenList is not null)
            {
                ConfigChildrenList.Elements.Clear();

                if (Children.Count == 0)
                {
                    ConfigChildrenList.Elements.Add(new UILabel
                    {
                        Text = "Empty",
                        Height = 20,
                        TextAlign = new(.5f)
                    });
                }
                else
                {
                    foreach (MapObject obj in Children.OrderBy(o => o.Name))
                    {
                        if (!obj.CanSetActive)
                            continue;

                        UIPanel panel = new()
                        {
                            Padding = 2,
                            Height = 22,
                            BackColor = new(40, 40, 40),

                            Elements =
                            {
                                new UILabel
                                {
                                    Text = obj.Name,
                                    Top = 2,
                                    Left = 2,
                                    Height = 16,
                                    WordWrap = false,
                                    AutoSize = false,
                                    Width = new(-22, 1)
                                },
                                new UIButton
                                {
                                    Text = "A",

                                    Selectable = true,
                                    Selected = obj.ActiveProperty.Value,

                                    SelectedBackColor = Color.White,
                                    SelectedTextColor = Color.Black,

                                    Left = new(0, 1, -1),
                                    Width = 18,
                                }.OnEvent(UIElement.ClickEvent, (btn, _) => obj.ActiveProperty.Value = btn.Selected),
                            }
                        };
                        panel.OnEvent(UIElement.ClickEvent, (p, _) => { if (p.Root?.Hover is not UIButton) Main.FocusOnObject(obj); });
                        ConfigChildrenList.Elements.Add(panel);
                    }
                }
            }
            if (ConfigLayerList is not null)
            {
                ConfigLayerList.Elements.Clear();

                RadioButtonGroup group = new();

                for (int i = Main.Layers.Count - 1; i >= 0; i--)
                {
                    Layer layer = Main.Layers[i];
                    UIButton button = new()
                    {
                        Text = layer.Name,
                        Height = 20,
                        TextAlign = new(.5f),
                        RadioGroup = group,
                        Selectable = true,
                        Selected = layer == RenderLayer.Value,
                        RadioTag = layer,
                        SelectedTextColor = Color.Black,
                        SelectedBackColor = Color.White,
                    };

                    ConfigLayerList.Elements.Add(button);
                }

                group.ButtonClicked += (_, tag) =>
                {
                    if (tag is not Layer layer)
                        return;

                    RenderLayer.Value = layer;
                };
            }

            UpdateInnerConfig();
        }

        public JsonObject? SaveJson(bool forCopy = false)
        {
            if (!NeedsSaving && !forCopy)
                return null;

            JsonNode? inner = SaveInnerJson(forCopy);

            JsonObject json = new();

            if (forCopy)
            {
                json["pos"] = JsonTypes.SaveVector2(WorldPosition);
            }
            else
            {
                json["name"] = Name ?? throw new InvalidOperationException(
                        $"MapObject doesn't have a name and can't be saved.\n" +
                        $"Type: {GetType().Name}\n" +
                        $"Parent: {Parent?.Name ?? Parent?.GetType().Name ?? "null"}");
                json["pos"] = JsonTypes.SaveVector2(ParentPosition);
            }

            ActiveProperty.SaveToJson(json, forCopy);
            RenderLayer.SaveToJson(json, forCopy);

            if (!LoadCreationForbidden)
                json["type"] = forCopy ? CopyType.FullName : GetType().FullName;
            if (inner is not null && (inner is not JsonObject innerobj || innerobj.Count > 0))
                json["data"] = inner;
            if (Children.Count > 0 && !forCopy)
                json["children"] = new JsonArray(Children.Select(c => c.SaveJson()).OfType<JsonNode>().ToArray());

            return json;
        }
        public void LoadJson(JsonNode json, bool shallow)
        {
            if (json.TryGet("data", out JsonNode? data))
                LoadInnerJson(data, shallow);

            if (json.TryGet("name", out string? name))
                Name = name;

            if (json.TryGet("pos", out JsonNode? pos))
                ParentPosition = JsonTypes.LoadVector2(pos);

            ActiveProperty.LoadFromJson(json);
            RenderLayer.LoadFromJson(json);

            if (json.TryGet("children", out JsonArray? children))
                foreach (JsonNode? childNode in children)
                    if (childNode is not null)
                        LoadObject(childNode, Children, shallow);
        }

        public void EnsureCorrectShadeTexture()
        {
            if ((ShadeTexture is null || ShadeTextureDirty || ShadeSize != ShadeTextureShadeSize) && ShadeSize > 0)
            {
                Shading = true;
                GenerateShadeTexture();
                Shading = false;
                ShadeTextureShadeSize = ShadeSize;
            }
            ShadeTextureDirty = false;
        }

        protected virtual void GenerateShadeTexture()
        {
            GenerateDefaultShadeTexture(ref ShadeTexture, this, ShadeSize, ShadeCornerRadius);
        }

        protected static void GenerateDefaultShadeTexture(ref Texture2D? texture, MapObject obj, int shade, int? cornerRadius)
        {
            obj.Shading = true;
            ShadeTextureRenderer ??= new(Main.SpriteBatch);

            Vector2 shadeSize = obj.Size + new Vector2(shade * 2);

            int shadeWidth = (int)Math.Ceiling(shadeSize.X);
            int shadeHeight = (int)Math.Ceiling(shadeSize.Y);

            if (ShadeRenderTarget is null || ShadeRenderTarget.Width < shadeWidth || ShadeRenderTarget.Height < shadeHeight)
            {
                int targetWidth = shadeWidth;
                int targetHeight = shadeHeight;

                if (ShadeRenderTarget is not null)
                {
                    targetWidth = Math.Max(targetWidth, ShadeRenderTarget.Width);
                    targetHeight = Math.Max(targetHeight, ShadeRenderTarget.Height);

                    ShadeRenderTarget?.Dispose();
                }
                ShadeRenderTarget = new(Main.Instance.GraphicsDevice, targetWidth, targetHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            ShadeTextureRenderer.TargetNeedsClear = true;
            ShadeTextureRenderer.Position = obj.WorldPosition - new Vector2(shade);

            obj.DrawSelf(ShadeTextureRenderer);

            int shadePixels = shadeWidth * shadeHeight;
            Color[] pixels = ArrayPool<Color>.Shared.Rent(shadePixels);

            // no texture draw calls
            if (ShadeTextureRenderer.TargetNeedsClear)
            {
                Array.Clear(pixels);
            }
            else
            {
                ShadeRenderTarget.GetData(0, new(0, 0, shadeWidth, shadeHeight), pixels, 0, shadePixels);
                ProcessShade(pixels, shadeWidth, shadeHeight, shade, cornerRadius);
            }
            if (texture is null || texture.Width != shadeWidth || texture.Height != shadeHeight)
            {
                texture?.Dispose();
                texture = new(Main.Instance.GraphicsDevice, shadeWidth, shadeHeight);
            }
            texture.SetData(pixels, 0, shadePixels);
            ArrayPool<Color>.Shared.Return(pixels);
            obj.Shading = false;
        }

        protected virtual JsonNode? SaveInnerJson(bool forCopy) => null;
        protected virtual void LoadInnerJson(JsonNode node, bool shallow) { }

        protected virtual void BuildInnerConfig(UIList list) { }
        protected virtual void UpdateInnerConfig() { }

        public virtual void RegenerateName()
        {
            if (Name is null)
            {
                Name = $"{GetType().Name}_{Random.Shared.Next():x}";
                return;
            }

            string random = $"_{Random.Shared.Next():x}";

            Match match = RandomNameSuffixRegex.Match(Name);
            if (match.Success)
            {
                Name = match.Result(random);
                return;
            }

            Name += random;
        }

        public bool ContainsPoint(Vector2 worldPoint)
        {
            return VisualPosition.X <= worldPoint.X
                 && VisualPosition.Y <= worldPoint.Y
                 && VisualPosition.X + VisualSize.X > worldPoint.X
                 && VisualPosition.Y + VisualSize.Y > worldPoint.Y;
        }

        protected static void ProcessShade(Color[] colors, int width, int height, int size, int? cornerRadius)
        {
            int arraysize = width * height;
            bool[] shade = ArrayPool<bool>.Shared.Rent(arraysize);

            int patternSide = size * 2 + 1;

            bool[] shadePattern = null!;

            if (cornerRadius.HasValue)
            {
                shadePattern = ArrayPool<bool>.Shared.Rent(patternSide * patternSide);

                int patternRadSq = cornerRadius.Value * cornerRadius.Value;

                for (int j = 0; j < patternSide; j++)
                    for (int i = 0; i < patternSide; i++)
                    {
                        float lengthsq = (size - i) * (size - i) + (size - j) * (size - j);
                        shadePattern[i + patternSide * j] = lengthsq <= patternRadSq;
                    }
            }

            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                {
                    int index = width * j + i;

                    shade[index] = false;

                    if (colors[index].A > 0)
                    {
                        shade[index] = true;
                        continue;
                    }

                    if (size <= 0)
                        continue;

                    bool probing = true;
                    for (int l = -size; l <= size && probing; l++)
                        for (int k = -size; k <= size && probing; k++)
                        {
                            if (cornerRadius.HasValue)
                            {
                                int patternIndex = (l + size) * patternSide + k + size;
                                if (!shadePattern[patternIndex])
                                    continue;
                            }

                            int x = i + k;
                            int y = j + l;

                            if (x < 0 || y < 0 || x >= width || y >= height || k == 0 && l == 0)
                                continue;

                            int testIndex = width * y + x;

                            if (colors[testIndex].A > 0)
                            {
                                shade[index] = true;
                                probing = false;
                                continue;
                            }
                        }
                }

            for (int i = 0; i < arraysize; i++)
                colors[i] = shade[i] ? Color.Black : Color.Transparent;

            ArrayPool<bool>.Shared.Return(shade);
            if (cornerRadius.HasValue)
                ArrayPool<bool>.Shared.Return(shadePattern);
        }

        public static MapObject? FindSelectableAtPos(IEnumerable<MapObject> objects, Vector2 pos, bool searchChildren)
        {
            foreach (MapObject obj in objects.SmartReverse())
            {
                if (!obj.Active || !obj.RenderLayer.Value.Visible)
                    continue;

                if (searchChildren)
                {
                    MapObject? child = FindSelectableAtPos(obj.Children, pos, true);
                    if (child is not null)
                        return child;
                }

                if (obj.ContainsPoint(pos))
                    return obj;
            }
            return null;
        }
        public static IEnumerable<MapObject> FindIntersectingSelectables(IEnumerable<MapObject> objects, Vector2 tl, Vector2 br, bool searchChildren)
        {
            foreach (MapObject obj in objects.SmartReverse())
            {
                if (!obj.Active || !obj.RenderLayer.Value.Visible)
                    continue;

                if (searchChildren)
                    foreach (MapObject child in FindIntersectingSelectables(obj.Children, tl, br, true))
                        yield return child;

                bool intersects = obj.VisualPosition.X < br.X
                    && tl.X < obj.VisualPosition.X + obj.VisualSize.X
                    && obj.VisualPosition.Y < br.Y
                    && tl.Y < obj.VisualPosition.Y + obj.VisualSize.Y;
                if (intersects)
                    yield return obj;
            }
        }

        public static bool LoadObject(JsonNode node, IEnumerable<MapObject> objEnumerable, bool shallow)
        {
            if (!node.TryGet("name", out string? name))
                return false;

            MapObject? obj = objEnumerable.FirstOrDefault(o => o.Name == name);
            if (obj is null)
                return false;

            obj.LoadJson(node, shallow);
            return true;
        }
        public static MapObject? CreateObject(JsonNode node, bool shallow)
        {
            if (!node.TryGet("type", out string? typeName))
                return null;

            Type? type = Type.GetType(typeName);
            if (type is null || !type.IsAssignableTo(typeof(MapObject)))
                return null;

            MapObject instance = (MapObject)Activator.CreateInstance(type)!;

            if (node.TryGet("name", out string? name))
                instance.Name = name;
            else if (instance.Name is null)
                instance.RegenerateName();

            instance.LoadJson(node, shallow);
            return instance;
        }

        public override string? ToString()
        {
            return Name ?? base.ToString();
        }

        public class MapObjectCollection : ICollection<MapObject>
        {
            List<MapObject> Objects = new();
            MapObject Parent;

            public MapObjectCollection(MapObject parent)
            {
                Parent = parent;
            }

            public int Count => Objects.Count;
            public bool IsReadOnly => false;

            public void Add(MapObject item)
            {
                item.Parent?.Children.Remove(item);
                item.Parent = Parent;
                Objects.Add(item);
            }

            public void Clear()
            {
                foreach (MapObject obj in Objects)
                    obj.Parent = null;

                Objects.Clear();
            }

            public bool Remove(MapObject item)
            {
                item.Parent = null;
                return Objects.Remove(item);
            }

            public bool Contains(MapObject item)
            {
                return Objects.Contains(item);
            }

            public void CopyTo(MapObject[] array, int arrayIndex)
            {
                Objects.CopyTo(array, arrayIndex);
            }

            public IEnumerator<MapObject> GetEnumerator()
            {
                return Objects.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Objects.GetEnumerator();
            }
        }
    }
}
