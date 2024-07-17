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
    [HarmonyPatch(typeof(EntityPreset), nameof(EntityPreset.Create))]
    class EntityPresetCreatePatch
    {
        static void Postfix(EntityPreset __instance, Entity __result)
        {
            if (__instance.Type.FullName == "AnodyneSharp.Entities.Interactive.DungeonStatue")
            {
                if (!Plugin.ArchipelagoManager.SplitWindmill)
                {
                    return;
                }

                __result.Position = __instance.Position + new Vector2(1f, 32f);

                string eventName = "StatueMoved_";
                Facing moveDir = Facing.RIGHT;
                if (__instance.Frame == 0)
                {
                    eventName += "Temple";
                    moveDir = Facing.UP;
                }
                else if (__instance.Frame == 1)
                {
                    eventName += "Grotto";
                }
                else if (__instance.Frame == 2)
                {
                    eventName += "Mountain";
                }

                if (GlobalState.events.GetEvent(eventName) > 0)
                {
                    __result.Position += Entity.FacingDirection(moveDir) * 32f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Box), nameof(Box.PlayerInteraction))]
    class BoxOpenPatch
    {
        static bool Prefix(Box __instance, ref bool __result)
        {
            if (!GlobalState.events.SpookedMonster)
            {
                __result = false;
                return false;
            }

            __instance.Play("open");
            SoundManager.PlaySoundEffect("broom_hit");
            GlobalState.StartCutscene = OnOpened(__instance);
            __result = true;
            return false;
        }

        static IEnumerator<CutsceneState.CutsceneEvent> OnOpened(Box box)
        {
            MethodInfo openedMethod = typeof(Box).GetMethod("OnOpened", BindingFlags.NonPublic | BindingFlags.Instance);
            IEnumerator<CutsceneState.CutsceneEvent> subCutscene = (IEnumerator<CutsceneState.CutsceneEvent>)openedMethod.Invoke(box, new object[] { });

            yield return subCutscene.Current;
            while (subCutscene.MoveNext())
            {
                yield return subCutscene.Current;
            }

            GlobalState.inventory.tradeState = InventoryManager.TradeState.NONE;

            Plugin.ArchipelagoManager.SendLocation("Fields - Cardboard Box");
        }
    }

    [HarmonyPatch(typeof(ShopKeep), nameof(ShopKeep.PlayerInteraction))]
    class ShopKeepTalkPatch
    {
        static bool Prefix(ShopKeep __instance)
        {
            if (GlobalState.events.GetEvent("ReceivedCardboardBox") == 1 && GlobalState.events.GetEvent("UsedCardboardBox") == 0)
            {
                GlobalState.Dialogue = GetDiag(2) + " " + GetDiag(4);
                GlobalState.events.SetEvent("UsedCardboardBox", 1);

                EntityPreset preset = PatchHelper.GetEntityPreset(typeof(ShopKeep), __instance);
                preset.Activated = true;

                Plugin.ArchipelagoManager.SendLocation("Fields - Shopkeeper Trade");

                return false;
            }

            return true;
        }

        static string GetDiag(int i) => DialogueManager.GetDialogue("misc", "any", "tradenpc", i);
    }

    [HarmonyPatch(typeof(MitraFields), "GetInteractionText")]
    class MitraFieldsTextPatch
    {
        static bool Prefix(ref string __result)
        {
            if (GlobalState.events.GetEvent("ReceivedBikingShoes") == 1 && GlobalState.events.GetEvent("UsedBikingShoes") == 0)
            {
                __result = DialogueManager.GetDialogue("misc", "any", "mitra", 1);

                GlobalState.events.SetEvent("UsedBikingShoes", 1);

                Plugin.ArchipelagoManager.SendLocation("Fields - Mitra Trade");

                return false;
            }

            return true;
        }
    }

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

    [HarmonyPatch]
    class BlankConsoleInteractPatch
    {
        static MethodInfo TargetMethod()
        {
            return typeof(AnodyneGame).Assembly.GetType("AnodyneSharp.Entities.Interactive.Npc.Blank.BlankConsole").GetMethod("PlayerInteraction");
        }

        static void Postfix()
        {
            if (Plugin.ArchipelagoManager.VictoryCondition == VictoryCondition.AllCards)
            {
                Plugin.ArchipelagoManager.ActivateGoal();
            }
        }
    }

    [HarmonyPatch(typeof(SwapperControl), MethodType.Constructor, new Type[] {typeof(string)})]
    class SwapperControlCtorPatch
    {
        static void Postfix(SwapperControl __instance, string mapName)
        {
            Type regionType = typeof(SwapperControl).GetNestedType("Region", BindingFlags.NonPublic);
            FieldInfo allowField = regionType.GetField("allow");
            FieldInfo areaField = regionType.GetField("area");

            Type listType = typeof(List<>).MakeGenericType(new Type[] { regionType });
            MethodInfo addMethod = listType.GetMethod("Add");
            object regions = Activator.CreateInstance(listType);

            List<Rectangle> data = SwapData.GetRectanglesForMap(mapName, GlobalState.events.GetEvent("ExtendedSwap") == 1);
            foreach (Rectangle rectangle in data)
            {
                object region = Activator.CreateInstance(regionType);
                allowField.SetValue(region, SwapperControl.State.Allow);
                areaField.SetValue(region, rectangle);

                addMethod.Invoke(regions, new object[] { region });
            }

            FieldInfo regionsField = typeof(SwapperControl).GetField("regions", BindingFlags.NonPublic | BindingFlags.Instance);
            regionsField.SetValue(__instance, regions);
        }
    }
}
