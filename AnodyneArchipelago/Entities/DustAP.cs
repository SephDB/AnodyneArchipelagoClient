using AnodyneSharp.Entities;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Color = Microsoft.Xna.Framework.Color;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Dust), MapCollision = true, KeepOnScreen = true)]
    internal class DustAP : Dust
    {
        private static readonly Color NormalItemColor = new(255, 199, 79, 255);
        private static readonly Color ImportantItemColor = new(76, 255, 0, 255);
        private static readonly Color TrapItemColor = new(255, 0, 0, 255);

        private EntityPreset _preset;

        public DustAP(EntityPreset preset, Player p)
            : base(preset, p)
        {
            _preset = preset;

            if (!_preset.Activated)
            {
                sprite.Color = GetDustColor();
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!_preset.Activated && b.dust == this && AnimFinished)
            {
                sprite.Color = Color.White;
                _preset.Activated = true;

                Plugin.ArchipelagoManager!.SendLocation(long.Parse(_preset.TypeValue));
            }
        }

        private Color GetDustColor()
        {
            ItemInfo? info = Plugin.ArchipelagoManager!.GetScoutedLocation(long.Parse(_preset.TypeValue));

            if (info == null)
            {
                return Color.Black;
            }

            if (info.Flags.HasFlag(ItemFlags.Trap))
            {
                return Plugin.ArchipelagoManager!.HideTrapItems ? ImportantItemColor : TrapItemColor;
            }
            else if (info.Flags.HasFlag(ItemFlags.Advancement))
            {
                return ImportantItemColor;
            }
            else
            {
                return NormalItemColor;
            }
        }
    }
}
