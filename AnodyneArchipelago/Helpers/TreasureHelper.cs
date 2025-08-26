using AnodyneSharp.Entities.Gadget.Treasures;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Helpers
{
    public static class TreasureHelper
    {
        private static Dictionary<Item, ItemSpriteInfo> SpriteDict = [];

        private static ItemType[] trapItemTypes = [
            ItemType.Inventory,
            ItemType.TradingQuest,
            ItemType.Secret,
            ItemType.BigKey,
            ItemType.StatueUnlocks
        ];

        static TreasureHelper()
        {
            SpriteDict = new()
            {
                [new(ItemType.Inventory, 0)] = new("broom-icon", 0, ["broom"]),
                [new(ItemType.Inventory, 1)] = new("item_wide_attack", 0, ["wide", "widen"]),
                [new(ItemType.Inventory, 2)] = new("item_long_attack", 0, ["long", "extend"]),
                [new(ItemType.Inventory, 3)] = new("item_jump_shoes", 0, ["jump", "jump shoes"]),
                [new(ItemType.Inventory, 4)] = new("item_tranformer", 0, ["transformer", "swap"]),
                [new(ItemType.Inventory, 5)] = new("item_tranformer", 0, ["transformer", "swap"]),
                [new(ItemType.Cicada, 0)] = new("life_cicada", 0, ["life cicada", "health cicada"]),
                [new(ItemType.StatueUnlocks, 0, RegionID.BEDROOM)] = new("archipelago_items", 6, ["temple of the seeing one statue"]),
                [new(ItemType.StatueUnlocks, 0, RegionID.REDCAVE)] = new("archipelago_items", 7, ["red cavern statue", "statue"]),
                [new(ItemType.StatueUnlocks, 0, RegionID.CROWD)] = new("archipelago_items", 8, ["mountain cavern statue", "statue"]),
                [new(ItemType.Heal, 0)] = new("archipelago_items", 14, ["heal", "health", "heart", "hp", "potion"]),
                [new(ItemType.Heal, 1)] = new("archipelago_items", 15, ["big heal"]),
                [new(ItemType.Trap, 0)] = new("person", 0, ["person trap", "human", "anxiety"]),
                [new(ItemType.Trap, 1)] = new("archipelago_items", 13, ["gas trap", "gas", "poison"]),
                [new(ItemType.RedCaveUnlock, 0, RegionID.REDCAVE)] = new("archipelago_items", 3, ["progressive red cavern", "progressive red cave", "tentacle", "whip"]),
                [new(ItemType.Fountain, 0, RegionID.BLUE)] = new("archipelago_items", 22, ["blue done", "blue"]),
                [new(ItemType.Fountain, 0, RegionID.HAPPY)] = new("archipelago_items", 23, ["happy done", "happy", "red"]),
                [new(ItemType.TradingQuest, 0)] = new("fields_npcs", 0, ["miao"]),
                [new(ItemType.TradingQuest, 1)] = new("fields_npcs", 31, ["cardboard", "box"]),
                [new(ItemType.TradingQuest, 2)] = new("fields_npcs", 56, ["biking shoes", "shoes"]),
            };

            RegionID[] dungeonRegions = [
                RegionID.STREET,
                RegionID.BEDROOM,
                RegionID.REDCAVE,
                RegionID.CROWD,
                RegionID.APARTMENT,
                RegionID.CIRCUS,
                RegionID.HOTEL,
                ];

            foreach (RegionID regionID in dungeonRegions)
            {
                SpriteDict.Add(new(ItemType.Keys, 0, regionID), new("key", 0, ["small key"]));
                SpriteDict.Add(new(ItemType.Keys, 1, regionID), new("archipelago_items", 16, ["key ring", "skeleton key", "master key"]));
            }

            foreach (RegionID regionID in Enum.GetValues(typeof(RegionID)))
            {
                SpriteDict.Add(new(ItemType.Nexus, 0 , regionID), new("archipelago_items", 2, ["gate"]));
            }

            for (int i = 0; i < 3; i++)
            {
                SpriteDict.Add(new(ItemType.BigKey, i), new("key_green", i * 2, ["big key", "boss key"]));
            }

            for (int i = 0; i < 49; i++)
            {
                SpriteDict.Add(new(ItemType.Card, i), new("archipelago_items", i == 49 ? 5 : 4, ["card"]));
            }
                                                        
            for (int i = 0; i < 14; i++)
            {
                SpriteDict.Add(new(ItemType.Secret, i), new("secret_trophies", i, ["secret"]));
            }
        }

        public static ItemSpriteInfo GetSpriteWithTraps(long itemId, int player, ItemFlags itemFlags, long location, out long? disguisedItemId)
        {
            if (Plugin.ArchipelagoManager!.HideTrapItems && itemFlags.HasFlag(ItemFlags.Trap))
            {
                (string sprite, int frame, disguisedItemId) = GetTrapSprite(itemId, location);
                return new(sprite, frame, []);
            }
            else
            {
                disguisedItemId = null;
                return GetSprite(itemId, player, itemFlags);
            }
        }

        public static ItemSpriteInfo GetSprite(long itemId, int player, ItemFlags itemFlags)
        {
            ArchipelagoManager manager = Plugin.ArchipelagoManager!;

            if (manager.MatchDifferentWorldItem == MatchDifferentWorldItem.Disabled && player != manager.GetPlayer())
            {
                return new("archipelago_items", itemFlags.HasFlag(ItemFlags.Advancement) ? 0 : itemFlags.HasFlag(ItemFlags.Trap) ? 12 : 1, []);
            }
            else if (manager.GetGameName(player) == "Anodyne")
            {
                Item itemInfo = Item.Create(itemId);
                return SpriteDict[itemInfo];
            }

            string itemName = manager.GetItemName(itemId, player).ToLower();
            string fullCapsName = itemName.ToUpper();

            string sprite = "external_items";
            int frame = -1;

            ItemSpriteInfo? info = SpriteDict.Values.FirstOrDefault(v => v.ContainsSynonym(itemName));

            if (info != null)
            {
                sprite = info.Sprite;
                frame = info.Frame;
            }
            else if (Plugin.ArchipelagoManager!.MatchDifferentWorldItem == MatchDifferentWorldItem.MatchExtra)
            {
                if (itemName.Contains("cd") || itemName.Contains("disk") || fullCapsName.Contains("TM") || fullCapsName.Contains("HM"))
                {
                    frame = 15;
                }
                else if (itemName.Contains("jigsaw") || itemName.Contains("jiggy") || itemName.Contains("puzzle"))
                {
                    frame = 16;
                }
                else if (itemName.Contains("emblem") || itemName.Contains("badge") || itemName.Contains("trophy"))
                {
                    frame = 17;
                }
                else if (itemName.Contains("mushroom"))
                {
                    sprite = "forest_npcs";
                    frame = 20;
                }
                else if (itemName.Contains("gun"))
                {
                    sprite = "fields_npcs";
                    frame = 54;
                }
                else if (itemName.Contains("wallet"))
                {
                    sprite = "fields_npcs";
                    frame = 55;
                }
                else if (itemName.Contains("vacuum") || itemName.Contains("poltergust"))
                {
                    sprite = "fields_npcs";
                    frame = 56;
                }
                else if (itemName.Contains("heart container") || itemName.Contains("love"))
                {
                    frame = 20;
                }
                else if (itemName.Contains("piece of heart") || itemName.Contains("heart piece") || itemName.Contains("heartpiece"))
                {
                    frame = 1;
                }
                else if (itemName.Contains("stick") || itemName.Contains("branch") || itemName.Contains("wood") || itemName.Contains("rod") || itemName.Contains("staff"))
                {
                    frame = 3;
                }
                else if (itemName.Contains("feather") || itemName.Contains("roc's"))
                {
                    frame = 4;
                }
                else if (itemName.Contains("jar") || itemName.Contains("bottle"))
                {
                    frame = 5;
                }
                else if (itemName.Contains("coin") || fullCapsName.EndsWith('G'))
                {
                    frame = 6;
                }
                else if (itemName.Contains("rupee") || itemName.Contains("gem") || itemName.Contains("crystal") || itemName.Contains("jewel"))
                {
                    frame = 7;
                }
                else if (itemName.Contains("bean") || itemName.Contains("seed") || itemName.Contains("nut") || itemName.Contains("berry"))
                {
                    frame = 8;
                }
                else if (itemName.Contains("bomb"))
                {
                    frame = 9;
                }
                else if (itemName.Contains("arrow") || itemName.Contains("bow"))
                {
                    frame = 10;
                }
                else if (itemName.Contains("shield"))
                {
                    frame = 11;
                }
                else if (itemName.Contains("star") || itemName.Contains("shine"))
                {
                    frame = 12;
                }
                else if (itemName.Contains("rock") || itemName.Contains("stone"))
                {
                    frame = 13;
                }
                else if (itemName.Contains("ball") || itemName.Contains("orb") || itemName.Contains("pearl"))
                {
                    frame = 14;
                }
                else if (itemName.Contains("sword"))
                {
                    frame = 2;
                }
            }

            if (frame == -1)
            {
                sprite = "archipelago_items";
                frame = itemFlags.HasFlag(ItemFlags.Advancement) ? 0 : itemFlags.HasFlag(ItemFlags.Trap) ? 12 : 1;
            }

            return new(sprite, frame, []);
        }

        public static (string sprite, int frame, long id) GetTrapSprite(long itemId, long location)
        {
            long seed = Util.StringToIntVal(Plugin.ArchipelagoManager!.GetSeed()) + itemId + location;
            (string sprite, int frame, long id)[] itemList = [.. SpriteDict.Where(s => trapItemTypes.Contains(s.Key.Type)).Select(s => (s.Value.Sprite, s.Value.Frame, s.Key.ID))];

            if (seed < 0)
            {
                seed *= -1;
            }

            return itemList[seed % itemList.Length];
        }
    }

    public class SpriteTreasure(Vector2 pos, string tex, int frame) : Treasure(tex, pos, frame, -1)
    {
        public static SpriteTreasure Get(Vector2 pos, long itemId, int player)
        {
            (string tex, int frame, _) = TreasureHelper.GetSprite(itemId, player, ItemFlags.None);
            return new(pos, tex, frame);
        }
    }

    public record ItemSpriteInfo(string Sprite, int Frame, string[] Synonyms)
    {
        public bool ContainsSynonym(string itemName)
        {
            return Synonyms.Any(s => s.Contains(itemName.ToLower(), StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
