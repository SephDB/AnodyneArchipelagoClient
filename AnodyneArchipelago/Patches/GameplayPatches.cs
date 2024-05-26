﻿using AnodyneSharp.Entities.Events;
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

namespace AnodyneArchipelago.Patches
{
    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.PlayerInteraction))]
    class ChestInteractPatch
    {
        static void Prefix(TreasureChest __instance)
        {
            string entityId = PatchHelper.GetEntityPreset(typeof(TreasureChest), __instance).EntityID.ToString();
            Plugin.Instance.Log.LogInfo($"Touched chest: {entityId}");
        }
    }

    [HarmonyPatch(typeof(TreasureChest), "SetTreasure")]
    class ChestSetTreasurePatch
    {
        static bool Prefix(TreasureChest __instance)
        {
            Guid entityId = PatchHelper.GetEntityPreset(typeof(TreasureChest), __instance).EntityID;
            if (Locations.LocationsByGuid.ContainsKey(entityId))
            {
                BaseTreasure treasure = new ArchipelagoTreasure(Locations.LocationsByGuid[entityId], __instance.Position);

                FieldInfo treasureField = typeof(TreasureChest).GetField("_treasure", BindingFlags.NonPublic | BindingFlags.Instance);
                treasureField.SetValue(__instance, treasure);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Big_Key), nameof(Big_Key.PlayerInteraction))]
    class BigKeyTouchPatch
    {
        // We basically just rewrite this method, because we need to get rid of the part that adds the key to the inventory.
        static bool Prefix(Big_Key __instance, ref bool __result)
        {
            MethodInfo statesMethod = typeof(Big_Key).GetMethod("States", BindingFlags.NonPublic | BindingFlags.Instance);

            EntityPreset preset = PatchHelper.GetEntityPreset(typeof(Big_Key), __instance);
            preset.Alive = false;
            __instance.Solid = false;
            GlobalState.StartCutscene = (IEnumerator<AnodyneSharp.States.CutsceneState.CutsceneEvent>)statesMethod.Invoke(__instance, new object[] { });
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Big_Key), "States")]
    class BigKeyStatesPatch
    {
        static void Postfix(Big_Key __instance)
        {
            EntityPreset preset = PatchHelper.GetEntityPreset(typeof(Big_Key), __instance);

            if (preset.Frame == 0)
            {
                Plugin.ArchipelagoManager.SendLocation("Temple of the Seeing One - Green Key");
            }
            else if (preset.Frame == 2)
            {
                Plugin.ArchipelagoManager.SendLocation("Red Grotto - Red Key");
            }
            else if (preset.Frame == 4)
            {
                Plugin.ArchipelagoManager.SendLocation("Mountain Cavern - Blue Key");
            }
        }
    }

    [HarmonyPatch(typeof(HealthCicadaSentinel), nameof(HealthCicadaSentinel.Collided))]
    class HealthCicadaInteractPatch
    {
        static void Prefix(TreasureChest __instance)
        {
            Type hcsType = typeof(HealthCicadaSentinel);
            FieldInfo childField = hcsType.GetField("_child", BindingFlags.NonPublic | BindingFlags.Instance);
            HealthCicada healthCicada = (HealthCicada)childField.GetValue(__instance);

            EntityPreset preset = PatchHelper.GetEntityPreset(typeof(HealthCicada), healthCicada);
            Plugin.Instance.Log.LogInfo($"Touched cicada: {preset.EntityID.ToString()}");
        }
    }

    [HarmonyPatch(typeof(HealthCicada), nameof(HealthCicada.Update))]
    class HealthCicadaUpdatePatch
    {
        static void Postfix(HealthCicada __instance)
        {
            Type cicadaType = typeof(HealthCicada);
            FieldInfo chirpField = cicadaType.GetField("_chirp", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo sentinelField = cicadaType.GetField("_sentinel", BindingFlags.NonPublic | BindingFlags.Instance);

            HealthCicadaSentinel sentinel = (HealthCicadaSentinel)sentinelField.GetValue(__instance);
            Type hcsType = typeof(HealthCicadaSentinel);
            FieldInfo flyDistanceField = hcsType.GetField("_flyDistance", BindingFlags.NonPublic | BindingFlags.Instance);
            float flyDistance = (float)flyDistanceField.GetValue(sentinel);

            if (__instance.visible && !(bool)chirpField.GetValue(__instance) && flyDistance > 0)
            {
                flyDistanceField.SetValue(sentinel, 0f);

                FieldInfo stateField = cicadaType.GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
                stateField.SetValue(__instance, StateLogic(__instance));
            }
        }

        static IEnumerator<string> StateLogic(HealthCicada healthCicada)
        {
            EntityPreset preset = PatchHelper.GetEntityPreset(typeof(HealthCicada), healthCicada);

            while (!MathUtilities.MoveTo(ref healthCicada.opacity, 0.0f, 0.6f))
                yield return "FadingOut";
            preset.Alive = false;

            FieldInfo sentinelField = typeof(HealthCicada).GetField("_sentinel", BindingFlags.NonPublic | BindingFlags.Instance);
            HealthCicadaSentinel sentinel = (HealthCicadaSentinel)sentinelField.GetValue(healthCicada);

            sentinel.exists = false;

            if (Locations.LocationsByGuid.ContainsKey(preset.EntityID))
            {
                Plugin.ArchipelagoManager.SendLocation(Locations.LocationsByGuid[preset.EntityID]);
            }
        }
    }

    [HarmonyPatch(typeof(EntityPreset), nameof(EntityPreset.Create))]
    class EntityPresetCreatePatch
    {
        static void Postfix(EntityPreset __instance, Entity __result)
        {
            if (__instance.Type.FullName == "AnodyneSharp.Entities.Interactive.DungeonStatue")
            {
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
            else if (__instance.Type.FullName.StartsWith("AnodyneSharp.Entities.Decorations.RedCave"))
            {
                string side = __instance.Type.FullName.Substring(41);
                int requiredGrottos = 0;
                if (side == "Left")
                {
                    requiredGrottos = 1;
                }
                else if (side == "Right")
                {
                    requiredGrottos = 2;
                }
                else if (side == "North")
                {
                    requiredGrottos = 3;
                }

                if (GlobalState.events.GetEvent("ProgressiveRedGrotto") < requiredGrottos)
                {
                    __result.exists = false;
                    GlobalState.SpawnEntity((Entity)new DoorToggle(__result.Position, __result.width, __result.height));
                }
            }
            else if (__instance.Type.FullName == "AnodyneSharp.Entities.Interactive.Npc.MitraCliff")
            {
                // We want to get rid of this scene entirely.
                __instance.Alive = false;
                __result.exists = false;
            }
        }
    }

    [HarmonyPatch]
    class BreakChainPatch
    {
        static MethodInfo TargetMethod()
        {
            return typeof(Red_Pillar).GetNestedType("Chain", BindingFlags.NonPublic).GetMethod("Collided");
        }

        static void Postfix(object __instance)
        {
            Type chainType = typeof(Red_Pillar).GetNestedType("Chain", BindingFlags.NonPublic);
            FieldInfo parentField = chainType.GetField("_parent", BindingFlags.NonPublic | BindingFlags.Instance);

            Red_Pillar redPillar = (Red_Pillar)parentField.GetValue(__instance);
            EntityPreset preset = PatchHelper.GetEntityPreset(typeof(Red_Pillar), redPillar);

            Plugin.Instance.Log.LogInfo($"Broke red chain: {preset.EntityID.ToString()}");

            if (Locations.LocationsByGuid.ContainsKey(preset.EntityID))
            {
                Plugin.ArchipelagoManager.SendLocation(Locations.LocationsByGuid[preset.EntityID]);
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

    [HarmonyPatch("AnodyneSharp.Entities.Interactive.Npc.Windmill.Console", "PlayerInteraction")]
    class WindmillInteractPatch
    {
        static void Prefix(object __instance)
        {
            if ((__instance as Entity).CurAnimName == "active")
            {
                Plugin.ArchipelagoManager.SendLocation("Windmill - Activation");
            }
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
        }
    }

    /*[HarmonyPatch(typeof(Player), "Movement")]
    class PlayerMovementPatch
    {
        static void Postfix(Player __instance)
        {
            Plugin.Instance.Log.LogInfo($"Player pos: {GlobalState.Map.ToMapLoc(__instance.Position)}");
        }
    }*/
}