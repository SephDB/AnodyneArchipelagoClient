using AnodyneSharp;
using AnodyneSharp.Drawing;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Base.Rendering;
using AnodyneSharp.GameEvents;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using AnodyneSharp.Utilities;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Events(typeof(EndScreenTransition))]
    public class ColorPuzzleNotifier : Entity
    {
        EntityPool<Sparkle> _sparkles;
        int sparkleCount = 20;
        float sparkleTimer = 0f;
        bool active = false;

        public ColorPuzzleNotifier(EntityPreset preset, Player p) : base(preset.Position, 16, 16)
        {
            _sparkles = new(10, () => new Sparkle());

            if (GlobalState.events.GetEvent($"SeenPuzzle{GlobalState.CURRENT_MAP_NAME}") > 0)
            {
                exists = false;
                return;
            }
            GlobalState.events.IncEvent($"SeenPuzzle{GlobalState.CURRENT_MAP_NAME}");
            preset.Alive = false;
        }

        public override void Update()
        {
            base.Update();
            if (active && sparkleCount > 0 && MathUtilities.MoveTo(ref sparkleTimer, 0.1f, 1))
            {
                sparkleCount--;
                _sparkles.Spawn(s => s.Spawn(this, sparkleCount % 4 == 0));
                sparkleTimer = 0;
                if (sparkleCount == 0)
                {
                    exists = false;
                }
            }
        }

        public override void OnEvent(GameEvent e)
        {
            base.OnEvent(e);
            active = true;
        }

        public override IEnumerable<Entity> SubEntities()
        {
            return _sparkles.Entities;
        }

        public class Sparkle : Entity
        {
            public Sparkle(Color? color = null) : base(Vector2.Zero, new AnimatedSpriteRenderer("key_sparkle", 7, 7, new Anim("sparkle", [3, 2, 1, 0], 8, false)), DrawOrder.FG_SPRITES)
            {
                sprite.Color = color ?? Color.White;
            }

            public override void PostUpdate()
            {
                base.PostUpdate();
                if (AnimFinished) exists = false;
            }

            public void Spawn(Entity parent, bool makeSound)
            {
                Play("sparkle");
                Position = parent.Position - new Vector2(4, 4);
                Position.X += (parent.width + 4) * (float)GlobalState.RNG.NextDouble();
                Position.Y += (parent.height - 4) * (float)GlobalState.RNG.NextDouble();

                velocity.Y = 20f;
                if (makeSound)
                {
                    SoundManager.PlaySoundEffect("sparkle_1", "sparkle_1", "sparkle_2", "sparkle_2", "sparkle_3");
                }
            }
        }
    }
}
