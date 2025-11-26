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

        private string[] _ownTextFlair = 
            [
            "You should go get it!",
            "Maybe check it out?",
            "Good luck!",
            "Maybe worth a check?",
            "That's what Wares told me at least."
            ];

        private string[] _otherTextFlair =
            [
            "Maybe they can help you out?",
            "Have you asked them about it?",
            "Might be worth a check?",
            "If they have time to check it.",
            "Might get you unstuck!"
            ];

        private string[] _ownVagueFlair =
            [
            "That's what Wares told me at least.",
            "I think there's something important there.",
            "Maybe worth a check?",
            "It can be something helpful!",
            "Maybe check it out?",
            ];

        private string[] _otherVagueFlair =
            [
            "Maybe they can help you out?",
            "Might be worth a check?",
            "Have you asked them about it?",
            "Might get you unstuck!"
            "If they have time to check it.",
            ];

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
                if (manager.MitraHintType == MitraHintType.PreciseHint)
                {
                    manager.SendHint(hint.playerSlot, hint.locationID);
                }

                if (manager.GetPlayer() == hint.playerSlot)
                {
                    return $"I heard your {item} is at {location}! {_ownTextFlair[hintIndex % _ownTextFlair.Length]}";
                }
                else
                {
                    return $"I heard your {item} is at {player}'s {location}. {_otherTextFlair[hintIndex % _otherTextFlair.Length]}";
                }
            }
            else
            {
                if (manager.GetPlayer() == hint.playerSlot)
                {
                    return $"Have you tried looking at {location}? {_ownVagueFlair[hintIndex % _ownVagueFlair.Length]}";
                }
                else
                {
                    return $"There has to be something important over at {location}! {_otherVagueFlair[hintIndex % _otherVagueFlair.Length]}";
                }
            }
        }
    }
}
