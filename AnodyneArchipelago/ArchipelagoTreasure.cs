using AnodyneArchipelago.Helpers;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Sounds;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago
{
    internal class ArchipelagoTreasure : Treasure
    {
        private long _location;

        public static ArchipelagoTreasure Create(long location, Vector2 pos)
        {
            ItemInfo? item = Plugin.ArchipelagoManager!.GetScoutedLocation(location);
            if (item?.Player == Plugin.ArchipelagoManager.GetPlayer())
            {
                return new(location, pos, "none", -1);
            }

            (string tex, int frame) = TreasureHelper.GetSprite(item?.ItemName ?? "", item?.Player.Slot ?? -1, item?.Flags ?? ItemFlags.None);

            return new(location, pos, tex, frame);
        }

        private ArchipelagoTreasure(long location, Vector2 pos, string textureName, int frame) : base(textureName, pos, frame, -1)
        {
            _location = location;
        }

        public override void GetTreasure()
        {
            //Cards should not be drawn
            if (sprite.Frame != -1)
            {
                base.GetTreasure();
            }
            else
            {
                SoundManager.PlaySoundEffect("gettreasure");
            }

            Plugin.ArchipelagoManager!.SendLocation(_location);
        }
    }
}
