using System;
using System.Collections.Generic;
using System.Text;
using AnodyneSharp;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Base.Rendering;
using AnodyneSharp.Entities.Gadget.Doors;
using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class InactiveNexusPad : Entity
    {
        private EntityPreset _preset;
        private Player _player;

        public InactiveNexusPad(EntityPreset preset, Player player)
            : base(preset.Position, "nexus_pad", 32, 32, AnodyneSharp.Drawing.DrawOrder.VERY_BG_ENTITIES)
        {
            _preset = preset;
            _player = player;

            if (GlobalState.events.ActivatedNexusPortals.Contains(GlobalState.CURRENT_MAP_NAME))
            {
                _preset.Alive = false;
                exists = false;
                return;
            }

            sprite.Color = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            SetFrame(GlobalState.IsCell ? 2 : 0);

            width = 22;
            height = 18;
            offset = new Vector2(6, 4);

            Position += new Vector2(6, 6);
        }

        public override void Update()
        {
            if (GlobalState.events.ActivatedNexusPortals.Contains(GlobalState.CURRENT_MAP_NAME))
            {
                _preset.Alive = false;
                exists = false;

                GlobalState.SpawnEntity(new NexusPad(EntityManager.GetLinkedDoor(EntityManager.GetNexusGateForCurrentMap().Door).Door, _player));
            }
        }
    }
}
