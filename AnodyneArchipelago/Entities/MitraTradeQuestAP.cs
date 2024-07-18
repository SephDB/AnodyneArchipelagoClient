using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc;
using AnodyneSharp.Registry;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class MitraTradeQuestAP(EntityPreset preset, Player p) : MitraFields(preset,p)
    {
        protected override string GetInteractionText()
        {
            if (GlobalState.events.GetEvent("ReceivedBikingShoes") == 1 && GlobalState.events.GetEvent("UsedBikingShoes") == 0)
            {
                GlobalState.events.SetEvent("UsedBikingShoes", 1);

                Plugin.ArchipelagoManager!.SendLocation("Fields - Mitra Trade");
                
                return DialogueManager.GetDialogue("misc", "any", "mitra", 1);
            }
            return base.GetInteractionText();
        }
    }
}
