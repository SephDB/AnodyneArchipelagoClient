using System;
using System.Collections.Generic;
using System.Text;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy;
using AnodyneSharp.Entities.Enemy.Bedroom;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Registry;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Helpers
{
    public static class TreasureHelper
    {

        private static string[] secret_items = [
                "Golden Poop",
                "Spam Can",
                "Glitch",
                "Heart",
                "Electric Monster",
                "Cat Statue",
                "Melos",
                "Marina",
                "Black Cube",
                "Red Cube",
                "Green Cube",
                "Blue Cube",
                "White Cube",
                "Golden Broom",
            ];

        private static string[] trap_item_sprites = [
                "Green key",
                "Red key",
                "Blue key",
                "Jump Shoes",
                "Broom",
                "Swap",
                "Extend",
                "Widen",
                "Cardboard Box",
                "Biking Shoes",
                "Progressive Red Cave",
                "Card (Null)",
                "Temple of the Seeing One Statue",
                "Red Cave Statue",
                "Mountain Cavern Statue",
                .. secret_items,
            ];

        public static int GetSecretNumber(string secretName)
        {
            return Array.IndexOf(secret_items, Util.ToTitleCase(secretName));
        }

        public static (string, int) GetSpriteWithTraps(string itemName, int player, ItemFlags itemFlags, string location)
        {
            if (Plugin.ArchipelagoManager!.HideTrapItems && itemFlags.HasFlag(ItemFlags.Trap))
            {
                itemName = GetTrapName(itemName, location);
                player = Plugin.ArchipelagoManager.GetPlayer();
                itemFlags = ItemFlags.None;
            }

            return GetSprite(itemName, player, itemFlags);
        }

        public static (string, int) GetSprite(string itemName, int player, ItemFlags itemFlags)
        {
            if (Plugin.ArchipelagoManager!.MatchDifferentWorldItem == MatchDifferentWorldItem.Disabled && player != Plugin.ArchipelagoManager.GetPlayer())
            {
                return ("archipelago_items", itemFlags.HasFlag(ItemFlags.Advancement) ? 0 : itemFlags.HasFlag(ItemFlags.Trap) ? 12 : 1);
            }

            string fullCapsName = itemName;
            itemName = itemName.ToLower();

            if (itemName.StartsWith("small key"))
            {
                return ("key", 0);
            }
            if (itemName.StartsWith("key ring") || itemName.StartsWith("master key") || itemName.StartsWith("skeleton key"))
            {
                return ("archipelago_items", 16);
            }
            else if (itemName == "green key")
            {
                return ("key_green", 0);
            }
            else if (itemName == "blue key")
            {
                return ("key_green", 4);
            }
            else if (itemName == "red key")
            {
                return ("key_green", 2);
            }
            if (itemName.Contains("key"))
            {
                return ("key", 0);
            }
            else if (itemName == "jump shoes")
            {
                return ("item_jump_shoes", 0);
            }
            else if (itemName == "health cicada")
            {
                return ("life_cicada", 0);
            }
            else if (itemName == "big heal")
            {
                return ("archipelago_items", 15);
            }
            else if (itemName == "heal")
            {
                return ("archipelago_items", 14);
            }
            else if (itemName == "broom")
            {
                return ("broom-icon", 0);
            }
            else if (itemName.Contains("swap"))
            {
                return ("item_tranformer", 0);
            }
            else if (itemName == "extend")
            {
                return ("item_long_attack", 0);
            }
            else if (itemName == "widen")
            {
                return ("item_wide_attack", 0);
            }
            else if (itemName == "cardboard box")
            {
                return ("fields_npcs", 31);
            }
            else if (itemName == "biking shoes" || itemName.Contains("shoes") || itemName.Contains("boots"))
            {
                return ("fields_npcs", 56);
            }
            else if (itemName == "progressive red cave" || itemName == "tentacle" || itemName == "whip")
            {
                return ("archipelago_items", 3);
            }
            else if (itemName == "temple of the seeing one statue")
            {
                return ("archipelago_items", 6);
            }
            else if (itemName == "red cave statue")
            {
                return ("archipelago_items", 7);
            }
            else if (itemName == "mountain cavern statue")
            {
                return ("archipelago_items", 8);
            }
            else if (itemName.StartsWith("nexus gate"))
            {
                return ("archipelago_items", 2);
            }
            else if (itemName.StartsWith("card") || itemName.Contains("card"))
            {
                return ("archipelago_items", itemName == "card (null)" ? 5 : 4);
            }
            else if (GetSecretNumber(itemName) != -1)
            {
                return ("secret_trophies", GetSecretNumber(itemName));
            }
            else if (itemName == "person trap")
            {
                return ("person", 0);
            }
            else if (itemName == "gas trap")
            {
                return ("archipelago_items", 13);
            }
            else if (itemName == "miao")
            {
                return ("fields_npcs", 0);
            }
            else if (itemName == "raft")
            {
                return ("archipelago_items", 18);
            }
            else if (itemName == "dust")
            {
                return ("dust", 0);
            }

            if (Plugin.ArchipelagoManager!.MatchDifferentWorldItem == MatchDifferentWorldItem.MatchExtra)
            {
                if (itemName.Contains("cd") || itemName.Contains("disk") || fullCapsName.Contains("TM") || fullCapsName.Contains("HM"))
                {
                    return ("external_items", 15);
                }
                else if (itemName.Contains("jigsaw") || itemName.Contains("jiggy") || itemName.Contains("puzzle"))
                {
                    return ("external_items", 16);
                }
                else if (itemName.Contains("emblem") || itemName.Contains("badge") || itemName.Contains("trophy"))
                {
                    return ("external_items", 17);
                }
                else if (itemName.Contains("mushroom"))
                {
                    return ("forest_npcs", 20);
                }
                else if (itemName.Contains("gun"))
                {
                    return ("fields_npcs", 54);
                }
                else if (itemName.Contains("wallet"))
                {
                    return ("fields_npcs", 55);
                }
                else if (itemName.Contains("vacuum") || itemName.Contains("poltergust"))
                {
                    return ("fields_npcs", 56);
                }
                else if (itemName.Contains("heart container") || itemName.Contains("love"))
                {
                    return ("external_items", 0);
                }
                else if (itemName.Contains("piece of heart") || itemName.Contains("heart piece") || itemName.Contains("heartpiece"))
                {
                    return ("external_items", 1);
                }
                else if (itemName.Contains("stick") || itemName.Contains("branch") || itemName.Contains("wood") || itemName.Contains("rod") || itemName.Contains("staff"))
                {
                    return ("external_items", 3);
                }
                else if (itemName.Contains("feather") || itemName.Contains("roc's"))
                {
                    return ("external_items", 4);
                }
                else if (itemName.Contains("jar") || itemName.Contains("bottle"))
                {
                    return ("external_items", 5);
                }
                else if (itemName.Contains("coin") || fullCapsName.EndsWith('G'))
                {
                    return ("external_items", 6);
                }
                else if (itemName.Contains("rupee") || itemName.Contains("gem") || itemName.Contains("crystal") || itemName.Contains("jewel"))
                {
                    return ("external_items", 7);
                }
                else if (itemName.Contains("bean") || itemName.Contains("seed") || itemName.Contains("nut") || itemName.Contains("berry"))
                {
                    return ("external_items", 8);
                }
                else if (itemName.Contains("bomb"))
                {
                    return ("external_items", 9);
                }
                else if (itemName.Contains("arrow") || itemName.Contains("bow"))
                {
                    return ("external_items", 10);
                }
                else if (itemName.Contains("shield"))
                {
                    return ("external_items", 11);
                }
                else if (itemName.Contains("star") || itemName.Contains("shine"))
                {
                    return ("external_items", 12);
                }
                else if (itemName.Contains("rock") || itemName.Contains("stone"))
                {
                    return ("external_items", 13);
                }
                else if (itemName.Contains("ball") || itemName.Contains("orb") || itemName.Contains("pearl"))
                {
                    return ("external_items", 14);
                }
                else if (itemName.Contains("sword"))
                {
                    return ("external_items", 2);
                }
            }

            if (itemName.Contains("heart") || itemName.Contains("health") || itemName.Contains("heal") || itemName.Contains("hp") || itemName.Contains("potion"))
            {
                return ("archipelago_items", 14);
            }
            else if (itemName.Contains("key"))
            {
                return ("key", 0);
            }

            return ("archipelago_items", itemFlags.HasFlag(ItemFlags.Advancement) ? 0 : itemFlags.HasFlag(ItemFlags.Trap) ? 12 : 1);
        }

        public static string GetTrapName(string itemName, string location)
        {
            long seed = Util.StringToIntVal(Plugin.ArchipelagoManager!.GetSeed()) + Util.StringToIntVal(itemName) + Util.StringToIntVal(location);

            if (seed < 0)
            {
                seed *= -1;
            }

            return trap_item_sprites[seed % trap_item_sprites.Length];
        }
    }

    public class SpriteTreasure(Vector2 pos, string tex, int frame) : Treasure(tex, pos, frame, -1)
    {
        public static SpriteTreasure Get(Vector2 pos, string itemName, int player)
        {
            (string tex, int frame) = TreasureHelper.GetSprite(itemName, player, ItemFlags.None);
            return new(pos, tex, frame);
        }

    }
}
