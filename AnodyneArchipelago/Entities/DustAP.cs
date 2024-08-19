using System;
using System.Collections.Generic;
using System.Text;
using AnodyneArchipelago.Helpers;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Registry;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Dust), MapCollision = true, KeepOnScreen = true)]
    internal class DustAP : Dust
    {
        private EntityPreset _preset;

        public DustAP(EntityPreset preset, Player p)
            : base(preset, p)
        {
            _preset = preset;

            if (!_preset.Activated)
            {
                sprite.Color = new(255, 199, 79, 255);
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!_preset.Activated && b.dust == this && AnimFinished)
            {
                sprite.Color = Color.White;
                _preset.Activated = true;

                Plugin.ArchipelagoManager!.SendLocation(_preset.TypeValue);
            }
        }
    }
}
