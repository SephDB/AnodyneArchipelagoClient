using AnodyneSharp.Drawing;
using AnodyneSharp.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Player))]
    public class FreeStandingAP(EntityPreset preset, Player _) : Entity(preset.Position,"archipelago",16,16,DrawOrder.ENTITIES)
    {
        EntityPreset preset = preset;

        public override void Collided(Entity other)
        {
            base.Collided(other);
            Plugin.ArchipelagoManager.SendLocation(preset.TypeValue);
            preset.Alive = exists = false;
        }
    }
}
