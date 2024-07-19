using AnodyneArchipelago.Patches;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.Dialogue;
using System;
using System.Collections.Generic;
using System.Text;
using AnodyneSharp.Entities.Interactive.Npc.Blank;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class BlankConsoleAP(EntityPreset preset, Player p) : BlankConsole(preset,p), Interactable
    {
        new public bool PlayerInteraction(Facing player_direction)
        {
            Plugin.ArchipelagoManager!.ActivateGoal();
            return base.PlayerInteraction(player_direction);
        }
    }
}
