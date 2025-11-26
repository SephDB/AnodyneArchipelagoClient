using AnodyneArchipelago.Helpers;
using AnodyneSharp.Drawing;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Base.Rendering;
using Archipelago.MultiClient.Net.Models;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Player))]
    public class FreeStandingAP(EntityPreset preset, Player p) : Entity(preset.Position, GetSprite(long.Parse(preset.TypeValue)), DrawOrder.ENTITIES)
    {
        private static StaticSpriteRenderer GetSprite(long location)
        {
            ScoutedItemInfo? item = Plugin.ArchipelagoManager!.GetScoutedLocation(location);
            if (item is null)
            {
                return new("archipelago_items", 16, 16);
            }

            (string tex, int frame, _) = TreasureHelper.GetSpriteWithTraps(item.ItemId, item.Player.Slot, item.IsReceiverRelatedToActivePlayer, item.Flags, location, out _);
            return new(tex, 16, 16, frame);
        }

        public override void Collided(Entity other)
        {
            base.Collided(other);
            Plugin.ArchipelagoManager!.SendLocation(long.Parse(preset.TypeValue));
            preset.Alive = exists = false;
        }
    }
}
