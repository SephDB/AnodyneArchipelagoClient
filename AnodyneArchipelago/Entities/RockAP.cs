using System;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Drawing;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc;
using AnodyneSharp.Registry;
using AnodyneSharp.Utilities;
using static AnodyneArchipelago.Entities.ColorPuzzleNotifier;
using Color = Microsoft.Xna.Framework.Color;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Player))]
    internal class RockAP : Entity, Interactable
    {
        private EntityPool<Sparkle> _sparkles;
        private EntityPreset _preset;

        private string scene;
        private float sparkleTimer = 0f;

        public RockAP(EntityPreset preset, Player p)
            : base(preset.Position, Rock.GetSprite(), DrawOrder.ENTITIES)
        {
            _preset = preset;

            immovable = true;

            scene = MathUtilities.IntToString(preset.Frame + 1);

            Color color = Util.GetSparkleColor(long.Parse(_preset.TypeValue));

            _sparkles = new(10, () => new Sparkle(color));
        }

        public override void Collided(Entity other)
        {
            Separate(other, this);
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

        public override IEnumerable<Entity> SubEntities()
        {
            return _sparkles.Entities;
        }

        public bool PlayerInteraction(Facing player_direction)
        {
            if (!_preset.Activated)
            {
                sprite.Color = Color.White;
                _preset.Activated = true;

                Plugin.ArchipelagoManager!.SendLocation(long.Parse(_preset.TypeValue));
            }
            else
            {
                GlobalState.Dialogue = DialogueManager.GetDialogue("rock", scene);

                if (GlobalState.events.GetEvent("RockTalk") == 0)
                {
                    GlobalState.Dialogue = DialogueManager.GetDialogue("misc", "any", "rock", 0) + "^\n" + GlobalState.Dialogue;

                    GlobalState.events.IncEvent("RockTalk");
                }

            }

            return true;
        }
    }
}
