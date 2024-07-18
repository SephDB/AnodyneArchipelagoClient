using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class TradeQuestStarterAP(EntityPreset preset, Player p) : Entity(preset.Position,0,0)
    {
        public override void Update()
        {
            base.Update();
            if(GlobalState.inventory.tradeState == InventoryManager.TradeState.BOX)
            {
                GlobalState.inventory.tradeState = InventoryManager.TradeState.NONE;
                Plugin.ArchipelagoManager!.SendLocation("Fields - Cardboard Box");
                preset.Alive = exists = false;
            }
        }
    }
}
