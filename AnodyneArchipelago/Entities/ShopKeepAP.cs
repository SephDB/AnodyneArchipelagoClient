using AnodyneArchipelago.Helpers;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Base.Rendering;
using AnodyneSharp.Entities.Interactive.Npc.RunningTradeNPCs;
using AnodyneSharp.Registry;
using AnodyneSharp.Utilities;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Entities
{
    [NamedEntity]
    public class ShopKeepAP : ShopKeep, Interactable
    {
        private List<ShopItemAP> _items;
        private ShopKeepBoxAP _box;
        private bool _useShopItems;

        private EntityPreset _preset;

        public static readonly Location ShopkeepLoc = new(RegionID.FIELDS, LocationType.AreaEvent, 1);

        public ShopKeepAP(EntityPreset preset, Player p) : base(preset, p)
        {
            _preset = preset;

            Vector2 itemBasePos = Position + new Vector2(-30, 32);

            var manager = Plugin.ArchipelagoManager!;

            _useShopItems = manager.ShopItems.Length == 3;

            if (_useShopItems)
            {
                _items =
                [
                    new(itemBasePos, manager.ShopItems[0]),
                    new(itemBasePos + Vector2.UnitX * 34, manager.ShopItems[1]),
                    new(itemBasePos + Vector2.UnitX * 34 * 2, manager.ShopItems[2])
                ];
                _box = new(itemBasePos + Vector2.UnitX * 34 * 2, _items[2]);
            }
            else
            {
                _items = [];
                _box = new(itemBasePos + Vector2.UnitX * 34 * 2, base.SubEntities().Last());
                base.SubEntities().Last().SetFrame(57); //Make sure the jump shoes aren't shown
            }
        }

        public override IEnumerable<Entity> SubEntities()
        {
            if (!_useShopItems)
            {
                var baseSubEntities = base.SubEntities();
                if(_preset.Activated)
                {
                    return baseSubEntities;
                }

                return [baseSubEntities.ElementAt(0), baseSubEntities.ElementAt(1), _box];
            }
            else
            {
                if(_preset.Activated)
                {
                    return _items;
                }
                return [_items[0],_items[1],_box];
            }
        }

        public new bool PlayerInteraction(Facing player_direction)
        {
            if (GlobalState.events.GetEvent("ReceivedCardboardBox") == 1 && GlobalState.events.GetEvent("UsedCardboardBox") == 0)
            {
                GlobalState.Dialogue = DialogueManager.GetDialogue("misc", "any", "tradenpc", 2) + " " + DialogueManager.GetDialogue("misc", "any", "tradenpc", 4);
                GlobalState.events.SetEvent("UsedCardboardBox", 1);

                _preset.Activated = true;

                Plugin.ArchipelagoManager!.SendLocation(ShopkeepLoc.ID);
                _box.ActivateAnim();

                return true;
            }
            return base.PlayerInteraction(player_direction);
        }

        [Collision(typeof(Player))]
        public class ShopItemAP : Entity, Interactable
        {
            bool _checked = false;

            private static readonly string[] Currencies =
                [
                "Coins",
                "Dollars",
                "Euros",
                "Bucks",
                "Rupees",
                "Rubies",
                "Kromer",
                "Gold"
                ];

            protected ShopItem _item;

            private string _itemName = "";
            private string _currency = "";
            private long _value = 0;

            public ShopItemAP(Vector2 pos) : base(pos, new StaticSpriteRenderer("fields_npcs", 16, 16, 0), AnodyneSharp.Drawing.DrawOrder.BG_ENTITIES)
            {
                immovable = true;
            }

            public ShopItemAP(Vector2 pos, ShopItem item) : this(pos)
            {
                _item = item;

                PrepareItem();
            }

            public override void Update()
            {
                base.Update();
                MathUtilities.MoveTo(ref opacity, 1, 0.3f);
            }

            public override void Collided(Entity other)
            {
                base.Collided(other);
                Separate(this, other);
            }

            public bool PlayerInteraction(Facing player_direction)
            {
                if (_checked)
                {
                    GlobalState.Dialogue = "Flinty: " + DialogueManager.GetDialogue("misc", "any", "tradenpc", 6);
                }
                else
                {
                    var manager = Plugin.ArchipelagoManager!;

                    string player = manager.GetPlayerName(_item.playerSlot);

                    if (_item.playerSlot == manager.GetPlayer())
                    {
                        GlobalState.Dialogue = $"It's your {_itemName}!^\nBut it's priced at {_value} {_currency}!";
                    }
                    else
                    {
                        GlobalState.Dialogue = $"It's {player}'s {_itemName}!^\nBut it's priced at {_value} {_currency}!";
                    }

                    _checked = true;
                }

                return true;
            }

            protected void PrepareItem(bool is_trap = false)
            {
                var manager = Plugin.ArchipelagoManager!;

                long? disguisedItemId = null;

                ItemSpriteInfo info = TreasureHelper.GetSpriteWithTraps(_item.itemID, _item.playerSlot, Plugin.ArchipelagoManager!.GetScoutedLocation(ShopkeepLoc.ID)!.IsReceiverRelatedToActivePlayer, is_trap ? ItemFlags.Trap : ItemFlags.Advancement, ShopkeepLoc.ID, out disguisedItemId);

                SetTexture(info.Sprite, 16, 16);
                SetFrame(info.Frame);

                long seed = Util.StringToIntVal(manager.GetSeed()) + _item.itemID + _item.playerSlot;

                if (seed < 0)
                {
                    seed *= -1;
                }

                _itemName = Plugin.ArchipelagoManager!.GetItemName(disguisedItemId ?? _item.itemID, _item.playerSlot);

                _currency = Currencies[seed % Currencies.Length];
                _value = seed % 999 * 10;
                _checked = false;
            }
        }

        public class ShopKeepBoxAP : ShopItemAP
        {
            Entity post_grab;
            private Vector2 _startPos;

            public ShopKeepBoxAP(Vector2 pos, Entity post_grab) : base(pos)
            {
                _startPos = pos;

                var boxItem = Plugin.ArchipelagoManager!.GetScoutedLocation(ShopkeepLoc.ID)!;
                _item = new ShopItem(boxItem.ItemId, boxItem.Player.Slot);

                PrepareItem(boxItem.Flags.HasFlag(ItemFlags.Trap));
                
                this.post_grab = post_grab;
                post_grab.exists = false;
            }

            public override IEnumerable<Entity> SubEntities()
            {
                return [post_grab];
            }

            public override void Update()
            {
                base.Update();
                if (velocity.Y < -10 && velocity.Y > -15)
                {
                    Flicker(1);
                }
                else if (velocity.Y < -25)
                {
                    post_grab.opacity = 0;
                    post_grab.exists = true;
                    exists = false;
                }
            }

            public void ActivateAnim()
            {
                acceleration.Y = -10;
            }
        }
    }
}
