﻿using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive;
using AnodyneSharp.Registry;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class DungeonStatueAP : DungeonStatue
    {
        public DungeonStatueAP(EntityPreset preset, Player p) : base(preset.Position, preset.Frame)
        {
            string[] names = ["Temple", "Grotto", "Mountain"];
            if (GlobalState.events.GetEvent($"StatueMoved_{names[preset.Frame]}") > 0)
            {
                Position += FacingDirection(MoveDir(Frame)) * 32;
            }
        }
    }
}
