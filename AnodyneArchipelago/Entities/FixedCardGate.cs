using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity("CardGate","")] //Set type to "" to have higher priority than the actual card gate
    public class FixedCardGate(EntityPreset p, Player player) : BigCardGate(p,player)
    {
        public override bool TryUnlock()
        {
            bool ret = base.TryUnlock();

            //Fix crash on enough cards + card count between 37 and 46
            GlobalState.Dialogue ??= DialogueManager.GetDialogue("misc", "any", "keyblockgate", 4);

            return ret;
        }
    }
}
