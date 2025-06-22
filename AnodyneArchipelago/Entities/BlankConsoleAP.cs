using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc.Blank;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class BlankConsoleAP(EntityPreset preset, Player p) : BlankConsole(preset, p), Interactable
    {
        public new bool PlayerInteraction(Facing player_direction)
        {
            Plugin.ArchipelagoManager!.ActivateGoal();
            return base.PlayerInteraction(player_direction);
        }
    }
}
