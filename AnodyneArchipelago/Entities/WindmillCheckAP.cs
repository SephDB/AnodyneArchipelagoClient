using AnodyneArchipelago.Helpers;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc.Windmill;
using AnodyneSharp.GameEvents;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Events(typeof(OpenedWindmill))]
    public class WindmillCheckAP(EntityPreset preset, Player p) : Entity(preset.Position, 1, 1)
    {
        public override void OnEvent(GameEvent e)
        {
            Plugin.ArchipelagoManager!.SendLocation(new Location(RegionID.WINDMILL, LocationType.AreaEvent, 0).ID);
            preset.Alive = exists = false;
        }
    }
}
