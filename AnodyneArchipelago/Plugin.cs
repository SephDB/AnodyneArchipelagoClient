﻿using AnodyneSharp.Entities;
using BepInEx;
using BepInEx.NET.Common;
using HarmonyLib;
using HarmonyLib.Tools;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;

namespace AnodyneArchipelago
{
    [BepInPlugin("com.fourisland.plugins.anodyne.archipelago", "Anodyne Archipelago", "0.2.0")]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance = null;

        public static GraphicsDevice GraphicsDevice = null;
        public static Player Player = null;
        public static ArchipelagoManager ArchipelagoManager = null;
        public static bool IsGamePaused = false;

        public static string GetVersion()
        {
            return ((BepInPlugin)Attribute.GetCustomAttribute(typeof(Plugin), typeof(BepInPlugin))).Version.ToString();
        }

        public override void Load()
        {
            Instance = this;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Make patches
            HarmonyFileLog.Enabled = true;
            HarmonyFileLog.FileWriterPath = "HarmonyLog.txt";

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
