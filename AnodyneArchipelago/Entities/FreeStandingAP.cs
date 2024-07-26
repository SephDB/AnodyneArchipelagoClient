using AnodyneArchipelago.Helpers;
using AnodyneSharp.Drawing;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Base.Rendering;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Player))]
    public class FreeStandingAP(EntityPreset preset, Player _) : Entity(preset.Position, GetSprite(preset.TypeValue), DrawOrder.ENTITIES)
    {
        private static StaticSpriteRenderer GetSprite(string location)
        {
            ItemInfo? item = Plugin.ArchipelagoManager!.GetScoutedLocation(location);
            if(item is null)
            {
                return new("archipelago_items", 16, 16);
            }
            (string tex, int frame) = TreasureHelper.GetSpriteWithTraps(item.ItemName, item.Flags, location);
            return new(tex, 16, 16, frame);
        }

        public override void Collided(Entity other)
        {
            base.Collided(other);
            Plugin.ArchipelagoManager!.SendLocation(preset.TypeValue);
            preset.Alive = exists = false;
        }
    }
}
