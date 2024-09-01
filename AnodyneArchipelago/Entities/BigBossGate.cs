using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities.Base.Rendering;
using AnodyneSharp.GameEvents;
using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnodyneSharp.Entities.Gadget
{
    [NamedEntity("BossGate")]
    public class BigBossGate : BigGate
    {
        private static readonly Color DigitColor = new(255, 90, 90);

        Entity keyhole;
        Entity[] digits;

        public static Anim[] DigitAnims() => Enumerable.Range(0, 10).Select(i => new Anim(i.ToString(), new int[] { i }, 1)).ToArray();

        public BigBossGate(EntityPreset preset, Player p) : base(preset, p)
        {
            _sentinel.OpensOnInteract = true;

            keyhole = new(Position, new AnimatedSpriteRenderer("gate_green_slots", 32, 16, new RefLayer(layer_def, 1), new Anim("key", [3], 1)));

            digits =
            [
                new(Position + new Vector2(12,6),new AnimatedSpriteRenderer("boss_gate_x",3,5,new RefLayer(layer_def, 2), DigitAnims())),
                new(Position + new Vector2(17,6),new AnimatedSpriteRenderer("gate_green_digits",3,5,new RefLayer(layer_def, 2), DigitAnims()))
            ];

            digits[0].sprite.Color = DigitColor;
            digits[1].sprite.Color = DigitColor;

            digits[1].Play((_preset.Frame % 10).ToString());
        }

        public override bool TryUnlock()
        {
            if (GlobalState.events.BossDefeated.Count >= _preset.Frame)
            {
                GlobalState.Dialogue = "Acknowledging your strength, the gate decides to open.";
                GlobalState.StartCutscene = OpeningSequence();
                return true;
            }
            else
            {
                GlobalState.Dialogue = "The gate thinks you need to fight more.";
                return false;
            }
        }

        public override IEnumerable<Entity> SubEntities()
        {
            return base.SubEntities().Concat([keyhole, digits[0], digits[1]]);
        }

        protected override void BreakLock()
        {
            keyhole.exists = digits[0].exists = digits[1].exists = false;
        }
    }
}
