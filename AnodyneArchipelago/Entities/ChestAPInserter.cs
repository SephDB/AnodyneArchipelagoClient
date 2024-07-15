using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Entities.Gadget.Treasures;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity,Collision(typeof(TreasureChest))]
    public class ChestAPInserter(EntityPreset preset, Player p) : Entity(preset.Position, 16, 16)
    {
        static FieldInfo treasureField = typeof(TreasureChest).GetField("_treasure", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override void Collided(Entity other)
        {
            TreasureChest treasureChest = (TreasureChest)other;

            BaseTreasure treasure = ArchipelagoTreasure.Create(preset.TypeValue, treasureChest.Position);

            treasureField.SetValue(treasureChest, treasure);

            exists = false;
        }
    }
}
