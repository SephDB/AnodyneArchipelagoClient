using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc.Blue;
using AnodyneSharp.Entities.Interactive.Npc.RunningTradeNPCs;
using AnodyneSharp.Registry;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Player))]
    public class DamAP : Dam
    {
        private string _openEvent;
        private EntityPreset _preset;

        public DamAP(EntityPreset preset, Player p)
            : base(preset, p)
        {
            _openEvent = GlobalState.CURRENT_MAP_NAME == "BLUE" ? "BlueDone" : "HappyDone";
            _preset = preset;

            exists = GlobalState.events.GetEvent(_openEvent) == 0;
        }

        public override void Update()
        {
            if (!_preset.Alive)
            {
                return;
            }

            if (GlobalState.events.GetEvent(_openEvent) == 1)
            {
                Play("fall");
                _preset.Alive = false;
            }
        }
    }
}
