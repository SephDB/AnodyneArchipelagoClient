using AnodyneArchipelago.Helpers;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Interactive.Npc;
using AnodyneSharp.Registry;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class MitraTradeQuestAP(EntityPreset preset, Player p) : MitraFields(preset, p)
    {
        private const int MaxHints = 9;
        private int AllowedHints
        {
            get
            {
                return GlobalState.FUCK_IT_MODE_ON ? MaxHints : GlobalState.events.BossDefeated.Count + 1;
            }
        }

        protected override string GetInteractionText()
        {
            if (GlobalState.events.GetEvent("ReceivedBikingShoes") == 1 && GlobalState.events.GetEvent("UsedBikingShoes") == 0)
            {
                GlobalState.events.SetEvent("UsedBikingShoes", 1);

                Plugin.ArchipelagoManager!.SendLocation(new Location(RegionID.FIELDS, LocationType.AreaEvent, 2).ID);

                return DialogueManager.GetDialogue("misc", "any", "mitra", 1);
            }

            if (Plugin.ArchipelagoManager!.MitraHintType != MitraHintType.None)
            {
                return GetMitraHint();
            }
            else
            {
                //No hints, default behavior
                return "Good luck with whatever you're doing, Young!";
            }
        }

        private string GetMitraHint()
        {
            int hintIndex = GlobalState.events.GetEvent("mitra_hint_index");

            if (hintIndex >= MaxHints)
            {
                GlobalState.events.SetEvent("mitra_hint_index", 0);
                return "That's everything I could find out. Good luck Young!";
            }
            else if (hintIndex >= AllowedHints)
            {
                GlobalState.events.SetEvent("mitra_hint_index", 0);
                return "I think there's more I can find out. Maybe you should try and defeat some of the big guys?";
            }

            GlobalState.events.IncEvent("mitra_hint_index");

            ArchipelagoManager manager = Plugin.ArchipelagoManager!;

            MitraHint hint = manager.MitraHints[hintIndex];
            string item = manager.GetItemName(hint.itemID);
            string player = manager.GetPlayerName(hint.playerSlot);
            string location = manager.GetPlayerLocationName(hint.locationID, hint.playerSlot);

            if (manager.MitraHintType != MitraHintType.Vague)
            {
                if (manager.GetPlayer() == hint.playerSlot)
                {
                    if (manager.MitraHintType == MitraHintType.PreciseHint)
                    {
                        manager.SendHint(hint.locationID);
                    }

                    return $"I heard your {item} is at {location}! You should go get it!";
                }
                else
                {
                    return $"I heard your {item} is at {player}'s {location}. Maybe they can help you out?";
                }
            }
            else
            {
                if (manager.GetPlayer() == hint.playerSlot)
                {
                    return $"Have you tried looking at {location}? I think there's something important there.";
                }
                else
                {
                    return $"There has to be something important over at {location}! Have you asked {player} about it?";
                }
            }
        }
    }
}
