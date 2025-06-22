using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity("KeyRingBlock", null, 0)]
    public class KeyRingGate(EntityPreset preset, Player p) : SmallKeyGate(preset, p)
    {
        public override bool TryUnlock()
        {
            if (GlobalState.events.GetEvent($"{GlobalState.CURRENT_MAP_NAME}_KeyRing_Obtained") == 1)
            {
                SoundManager.PlaySoundEffect("unlock");
                Play("Open");
                Solid = false;
                return true;
            }
            else
            {
                GlobalState.Dialogue = DialogueManager.GetDialogue("misc", "any", "keyblock", 0);
                return false;
            }
        }
    }
}
