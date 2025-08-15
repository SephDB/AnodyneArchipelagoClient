using System.Reflection;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity, Collision(typeof(TreasureChest))]
    public class ChestAPInserter(EntityPreset preset, Player p) : Entity(preset.Position, 16, 16)
    {
        static FieldInfo treasureField = typeof(TreasureChest).GetField("_treasure", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override void Collided(Entity other)
        {
            TreasureChest treasureChest = (TreasureChest)other;

            BaseTreasure treasure = ArchipelagoTreasure.Create(long.Parse(preset.TypeValue), treasureChest.Position);

            treasureField.SetValue(treasureChest, treasure);

            exists = false;
        }
    }


    [NamedEntity, Collision(typeof(Player))]
    public class JokeTreasureChest : Entity, Interactable
    {
        public bool opened;

        EntityPreset _preset;
        int frame = 0;

        public JokeTreasureChest(EntityPreset preset, Player p)
            : base(preset.Position, "treasureboxes", 16, 16, AnodyneSharp.Drawing.DrawOrder.ENTITIES)
        {
            _preset = preset;

            if (_preset.Activated)
            {
                frame++;
                opened = true;
            }
            else
            {
                GlobalState.CurrentMinimap.AddInterest();
            }

            SetFrame(frame);
        }

        public virtual bool PlayerInteraction(Facing player_direction)
        {
            if (opened || player_direction != Facing.UP)
            {
                return false;
            }
            opened = true;
            GlobalState.CurrentMinimap.RemoveInterest();
            GlobalState.Dialogue = $"Huh??^Why???\n^Why would you STILL do this?????";
            SoundManager.PlaySoundEffect("fall_in_hole");
            SoundManager.PlaySoundEffect("sun_guy_scream");

            SetFrame(Frame + 1);
            _preset.Activated = true;
            return true;
        }
    }
}
