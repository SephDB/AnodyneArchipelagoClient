using System.Collections;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.Utilities;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class BossItemAP : FreeStandingAP
    {
        IEnumerator _state;

        public BossItemAP(EntityPreset preset, Player p) : base(preset, p)
        {
            offset.Y = 180 - MapUtilities.GetInGridPosition(Position).Y;
            _state = StateLogic();
        }

        public override void Update()
        {
            base.Update();
            _state.MoveNext();
        }

        private IEnumerator StateLogic()
        {
            while (!GlobalState.events.BossDefeated.Contains(GlobalState.CURRENT_MAP_NAME))
            {
                yield return null;
            }

            while (!MathUtilities.MoveTo(ref offset.Y, 0, 200 - offset.Y)) yield return null;

            Parabola_Thing bounce = new(this, 20, 0.6f);

            while (bounce.Progress() < 1f)
            {
                bounce.Tick();
                yield return null;
            }
            offset.Y = 0;
            yield break;
        }

        public override void Collided(Entity other)
        {
            if (offset.Y < 20)
            {
                base.Collided(other);
            }
        }

    }
}
