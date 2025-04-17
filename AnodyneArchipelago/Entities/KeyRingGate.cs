using System;
using System.Collections.Generic;
using System.Text;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using AnodyneSharp.Utilities;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity("KeyRingBlock", null, 0)]
    public class KeyRingGate : SmallKeyGate
    {
        public KeyRingGate(EntityPreset preset, Player p) 
            : base(preset, p)
        {
        }

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
