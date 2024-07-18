using AnodyneArchipelago.Patches;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc.RunningTradeNPCs;
using AnodyneSharp.Registry;
using AnodyneSharp.Dialogue;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class ShopKeepAP(EntityPreset preset, Player p) : ShopKeep(preset,p), Interactable
    {
        new public bool PlayerInteraction(Facing player_direction)
        {
            if (GlobalState.events.GetEvent("ReceivedCardboardBox") == 1 && GlobalState.events.GetEvent("UsedCardboardBox") == 0)
            {
                GlobalState.Dialogue = DialogueManager.GetDialogue("misc", "any", "tradenpc", 2) + " " + DialogueManager.GetDialogue("misc", "any", "tradenpc", 4);
                GlobalState.events.SetEvent("UsedCardboardBox", 1);

                preset.Activated = true;

                Plugin.ArchipelagoManager!.SendLocation("Fields - Shopkeeper Trade");

                return true;
            }
            return base.PlayerInteraction(player_direction);
        }
    }
}
