using System;
using System.Collections.Generic;
using System.Text;
using AnodyneArchipelago.Helpers;
using AnodyneArchipelago.Patches;
using AnodyneSharp;
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
        private bool _useShopItems;

        private EntityPreset _preset;

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
                    new(itemBasePos, manager.ShopItems[0], false),
                    new(itemBasePos + Vector2.UnitX * 34, manager.ShopItems[1], false),
                    new(itemBasePos + Vector2.UnitX * 34 * 2, manager.ShopItems[2], GlobalState.events.GetEvent("UsedCardboardBox") == 0)
                ];
            }
            else
            {
                _items =
                [
                    new(itemBasePos + Vector2.UnitX * 34 * 2, manager.ShopItems[2], GlobalState.events.GetEvent("UsedCardboardBox") == 0)
                ];
            }
        }

        public override IEnumerable<Entity> SubEntities()
        {
            if (!_useShopItems)
            {
                var baseSubEntities = base.SubEntities();

                return [baseSubEntities.ElementAt(0), baseSubEntities.ElementAt(1), _items.Last()];
            }
            else
            {
                return _items;
            }
        }

        new public bool PlayerInteraction(Facing player_direction)
        {
            if (GlobalState.events.GetEvent("ReceivedCardboardBox") == 1 && GlobalState.events.GetEvent("UsedCardboardBox") == 0)
            {
                GlobalState.Dialogue = DialogueManager.GetDialogue("misc", "any", "tradenpc", 2) + " " + DialogueManager.GetDialogue("misc", "any", "tradenpc", 4);
                GlobalState.events.SetEvent("UsedCardboardBox", 1);

                _preset.Activated = true;

                Plugin.ArchipelagoManager!.SendLocation("Fields - Shopkeeper Trade");
                _items.Last().ActivateAnim();

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


            private Vector2 _startPos;

            private ShopItem _item;
            private ShopItem? _otherItem;

            private string _itemName = "";
            private string _currency = "";
            private long _value = 0;

            public ShopItemAP(Vector2 pos, ShopItem item, bool isCardboardBox) : base(pos, new StaticSpriteRenderer("fields_npcs", 16, 16, 0), AnodyneSharp.Drawing.DrawOrder.BG_ENTITIES)
            {
                _startPos = pos;
                immovable = true;

                if (!isCardboardBox)
                {
                    _item = item;
                    _otherItem = null;
                }
                else
                {
                    var boxItem = Plugin.ArchipelagoManager!.GetScoutedLocation("Fields - Shopkeeper Trade")!;
                    _item = new ShopItem(boxItem.ItemId, boxItem.Player.Slot);
                    _otherItem = item;
                }

                PrepareItem();
            }

            public override void Collided(Entity other)
            {
                base.Collided(other);
                Separate(this, other);
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
                    acceleration = Vector2.Zero;
                    velocity = Vector2.Zero;
                    Position = _startPos;
                    opacity = 0;

                    _item = _otherItem!.Value;
                    PrepareItem();
                }
                MathUtilities.MoveTo(ref opacity, 1, 0.3f);
            }

            public void ActivateAnim()
            {
                acceleration.Y = -10;
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

            private void PrepareItem()
            {
                var manager = Plugin.ArchipelagoManager!;

                _itemName = manager.GetItemName(_item.itemID);
                var boxItem = manager.GetScoutedLocation("Fields - Shopkeeper Trade")!;
                bool isBox = _otherItem != null;

                (string sprite, int frame) = isBox ? TreasureHelper.GetSpriteWithTraps(_itemName, _item.playerSlot, boxItem.Flags, "Fields - Shopkeeper Trade") : TreasureHelper.GetSprite(_itemName, _item.playerSlot, boxItem.Flags);


                SetTexture(sprite, 16, 16);
                SetFrame(frame);

                long seed = Util.StringToIntVal(manager.GetSeed()) + _item.itemID + _item.playerSlot;

                if (seed < 0)
                {
                    seed *= -1;
                }

                if (isBox && Plugin.ArchipelagoManager!.HideTrapItems && boxItem.Flags.HasFlag(ItemFlags.Trap))
                {
                    _itemName = TreasureHelper.GetTrapName(_itemName, "Fields - Shopkeeper Trade");

                }

                _currency = Currencies[seed % Currencies.Length];
                _value = seed % 999 * 10;
                _checked = false;
            }
        }
    }
}
