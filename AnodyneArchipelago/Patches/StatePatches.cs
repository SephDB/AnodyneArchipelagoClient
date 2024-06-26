﻿using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.States;
using AnodyneSharp;
using HarmonyLib;
using System.Reflection;
using AnodyneSharp.Resources;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using AnodyneSharp.UI;
using System;
using System.Collections.Generic;
using System.IO;

namespace AnodyneArchipelago.Patches
{
    [HarmonyPatch(typeof(PlayState), nameof(PlayState.Create))]
    class PlayStateCreatePatch
    {
        static void Prefix(PlayState __instance)
        {
            // Get player for later access.
            FieldInfo playerField = typeof(PlayState).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance);
            Plugin.Player = (Player)playerField.GetValue(__instance);

            // Handle Red Cave stuff.
            if (!Plugin.ArchipelagoManager.VanillaRedCave)
            {
                GlobalState.events.SetEvent("red_cave_l_ss", 999);
                GlobalState.events.SetEvent("red_cave_n_ss", 999);
                GlobalState.events.SetEvent("red_cave_r_ss", 999);
            }

            // Reset death link info.
            Plugin.ArchipelagoManager.DeathLinkReason = null;

            // Pretend we're always in a pre-credits state so that swap is an allowlist, not a denylist.
            GlobalState.events.SetEvent("SeenCredits", 0);
        }
    }

    [HarmonyPatch(typeof(CreditsState), MethodType.Constructor, new Type[] {})]
    static class CreateCreditsPatch
    {
        static void Postfix()
        {
            GlobalState.events.SetEvent("DefeatedBriar", 1);
            if (Plugin.ArchipelagoManager.PostgameMode == PostgameMode.Vanilla)
            {
                Plugin.ArchipelagoManager.EnableExtendedSwap();
            }

            if (Plugin.ArchipelagoManager.VictoryCondition == VictoryCondition.DefeatBriar)
            {
                Plugin.ArchipelagoManager.ActivateGoal();
            }
            else if (Plugin.ArchipelagoManager.VictoryCondition == VictoryCondition.AllCards)
            {
                Plugin.ArchipelagoManager.SendLocation("GO - Defeat Briar");
            }
        }
    }

    [HarmonyPatch(typeof(DeathState), MethodType.Constructor, new Type[] {typeof(Player)})]
    static class DeathStateCtorPatch
    {
        static void Postfix(DeathState __instance)
        {
            if (Plugin.ArchipelagoManager.DeathLinkEnabled)
            {
                if (Plugin.ArchipelagoManager.ReceivedDeath)
                {
                    string message = Plugin.ArchipelagoManager.DeathLinkReason ?? "Received unknown death.";
                    message = Util.WordWrap(message, 20);

                    FieldInfo labelInfo = typeof(DeathState).GetField("_continueLabel", BindingFlags.NonPublic | BindingFlags.Instance);
                    UILabel label = (UILabel)labelInfo.GetValue(__instance);
                    label.SetText(message);
                    label.Position = new Vector2(8, 8);

                    Plugin.ArchipelagoManager.ReceivedDeath = false;
                    Plugin.ArchipelagoManager.DeathLinkReason = null;
                }
                else
                {
                    Plugin.ArchipelagoManager.SendDeath();
                }
            }
        }
    }
}
