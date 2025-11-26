using AnodyneSharp.Entities;
using AnodyneSharp.Utilities;
using static AnodyneArchipelago.Entities.ColorPuzzleNotifier;
using Color = Microsoft.Xna.Framework.Color;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Dust), MapCollision = true, KeepOnScreen = true)]
    internal class DustAP : Dust
    {
        private EntityPool<Sparkle> _sparkles;
        private long _locationId;
        private EntityPreset _preset;
        private float sparkleTimer = 0f;

        public DustAP(EntityPreset preset, Player p)
            : base(preset, p)
        {
            _preset = preset;

            Color color = Util.GetSparkleColor(long.Parse(_preset.TypeValue));

            _sparkles = new(10, () => new Sparkle(color));
            _locationId = long.Parse(_preset.TypeValue);
        }

        public override void Update()
        {
            base.Update();
            if (!_preset.Activated && MathUtilities.MoveTo(ref sparkleTimer, 0.15f, 1))
            {
                _sparkles.Spawn(s => s.Spawn(this, false));
                sparkleTimer = 0;
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

        public override IEnumerable<Entity> SubEntities()
        {
            return _sparkles.Entities;
        }
    }
}
