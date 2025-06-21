using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc.RunningTradeNPCs;
using AnodyneSharp.Registry;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(Player))]
    public class MiaoAP : MiaoXiao
    {
        public MiaoAP(EntityPreset preset, Player p)
            : base(preset, p)
        {
            exists = exists && GlobalState.events.GetEvent("ReceivedMiao") == 1;
        }
    }
}
