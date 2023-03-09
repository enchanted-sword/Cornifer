﻿using Cornifer.Renderers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.MapObjects
{
    public class SlugcatIcon : SelectableIcon
    {
        public int Id;

        public bool ForceSlugcatIcon;

        public override bool CanSetActive => true;

        public override int ShadeSize => InterfaceState.DrawSlugcatDiamond.Value ? 1 : 2;
        public override bool Active => (InterfaceState.DrawSlugcatIcons.Value || ForceSlugcatIcon) && base.Active;
        public override Vector2 Size => InterfaceState.DrawSlugcatDiamond.Value && !ForceSlugcatIcon ? new(9) : new(8);

        public SlugcatIcon(string name)
        {
            Name = name;
        }

        public override void DrawIcon(Renderer renderer)
        {
            Rectangle frame = GetFrame(Id, InterfaceState.DrawSlugcatDiamond.Value && !ForceSlugcatIcon);
            renderer.DrawTexture(Content.SlugcatIcons, WorldPosition, frame);
        }

        public static Rectangle GetFrame(int id, bool diamond)
        {
            return diamond ? new(id * 9, 8, 9, 9) : new(id * 8, 0, 8, 8);
        }
    }
}
