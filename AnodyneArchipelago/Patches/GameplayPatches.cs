using AnodyneSharp.Entities.Events;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Entities.Interactive;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using AnodyneSharp.Entities.Interactive.Npc.RunningTradeNPCs;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Sounds;
using AnodyneSharp.States;
using AnodyneSharp.Entities.Interactive.Npc;
using AnodyneSharp.MapData;
using AnodyneSharp;

namespace AnodyneArchipelago.Patches
{
    [HarmonyPatch(typeof(PlayState), "Warp")]
    class PlayWarpPatch
    {
        static void Postfix()
        {
            if (GlobalState.CURRENT_MAP_NAME == "FIELDS")
            {
                // Place a rock blocking access to Terminal without the red key.
                PatchHelper.SetMapTile(31, 47, 11, Layer.BG);
            }
            else if (GlobalState.CURRENT_MAP_NAME == "CIRCUS")
            {
                Point pos = Plugin.ArchipelagoManager.ColorPuzzle.CircusPos;

                PatchHelper.SetMapTile(72, 15, 60, Layer.BG);
                PatchHelper.SetMapTile(72 + pos.X, 11 + pos.Y, 46, Layer.BG);
            }
            else if (GlobalState.CURRENT_MAP_NAME == "HOTEL")
            {
                Point pos = Plugin.ArchipelagoManager.ColorPuzzle.HotelPos;

                PatchHelper.SetMapTile(78, 116, 82, Layer.BG);
                PatchHelper.SetMapTile(73 + pos.X, 113 + pos.Y, 31, Layer.BG);
            }
            else if (GlobalState.CURRENT_MAP_NAME == "APARTMENT")
            {
                Point pos = Plugin.ArchipelagoManager.ColorPuzzle.ApartmentPos;

                PatchHelper.SetMapTile(86, 52, 153, Layer.BG);
                PatchHelper.SetMapTile(82 + pos.X, 51 + pos.Y, 101, Layer.BG);
            }
            else if (GlobalState.CURRENT_MAP_NAME == "GO")
            {
                PatchHelper.SetMapTile(26, 33, 114, Layer.BG);
                PatchHelper.SetMapTile(27, 35, 114, Layer.BG);
                PatchHelper.SetMapTile(22, 36, 114, Layer.BG);
            }
        }
    }
}
