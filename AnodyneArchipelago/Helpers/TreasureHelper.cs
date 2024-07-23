using AnodyneSharp.Entities.Gadget.Treasures;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Helpers
{
    public static class TreasureHelper {
        public static int GetSecretNumber(string secretName)
        {
            List<string> secret_items = [
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
            return secret_items.IndexOf(secretName);
        }

        public static (string, int) GetSprite(string itemName, bool priority = false)
        {
            if (itemName.StartsWith("Small Key"))
            {
                return ("key", 0);
            }
            else if (itemName == "Green Key")
            {
                return ("key_green", 0);
            }
            else if (itemName == "Blue Key")
            {
                return ("key_green", 4);
            }
            else if (itemName == "Red Key")
            {
                return ("key_green", 2);
            }
            else if (itemName == "Jump Shoes")
            {
                return ("item_jump_shoes", 0);
            }
            else if (itemName == "Health Cicada")
            {
                return ("life_cicada", 0);
            }
            else if (itemName == "Heal")
            {
                return ("small_health_pickup", 0);
            }
            else if (itemName == "Broom")
            {
                return ("broom-icon", 0);
            }
            else if (itemName.EndsWith("Swap"))
            {
                return ("item_tranformer", 0);
            }
            else if (itemName == "Extend")
            {
                return ("item_long_attack", 0);
            }
            else if (itemName == "Widen")
            {
                return ("item_wide_attack", 0);
            }
            else if (itemName == "Cardboard Box")
            {
                return ("fields_npcs", 31);
            }
            else if (itemName == "Biking Shoes")
            {
                return ("item_jump_shoes", 0);
            }
            else if (itemName == "Progressive Red Cave")
            {
                return ("archipelago_items", 3);
            }
            else if (itemName.StartsWith("Nexus Gate"))
            {
                return ("archipelago_items", 2);
            }
            else if (itemName.StartsWith("Card"))
            {
                return ("archipelago_items", itemName == "Card (Null)" ? 5 : 4);
            }
            else if (GetSecretNumber(itemName) != -1)
            {
                return ("secret_trophies", GetSecretNumber(itemName));
            }

            return ("archipelago_items", priority ? 0 : 1);
        }
    }

    public class SpriteTreasure(Vector2 pos, string tex, int frame) : Treasure(tex,pos,frame,-1)
    {
        public static SpriteTreasure Get(Vector2 pos, string itemName)
        {
            (string tex, int frame) = TreasureHelper.GetSprite(itemName);
            return new(pos, tex, frame);
        }

    }
}
