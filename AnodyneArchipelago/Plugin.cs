using AnodyneSharp;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.States;
using HarmonyLib;
using HarmonyLib.Tools;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace AnodyneArchipelago
{
    public class Plugin
    {
        public static Plugin Instance = null;

        static FieldInfo playerField = typeof(PlayState).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static AnodyneGame Game => (GlobalState.GameState as AnodyneGame)!;
        public static Player Player => (Player)playerField.GetValue(Game.CurrentState as PlayState)!;
        public static ArchipelagoManager? ArchipelagoManager = null;

        public const string Version = "0.2.0";

        public void Load()
        {
            Instance = this;

            // Make patches
            HarmonyFileLog.Enabled = true;
            HarmonyFileLog.FileWriterPath = "HarmonyLog.txt";
        }

        public static void OnConnect()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(),"anodyneAP");
        }

        public static void OnDisconnect()
        {
            Harmony.UnpatchID("anodyneAP");
        }

        public static bool ReadyToReceive()
        {
            if(Game.CurrentState is PlayState p)
            {
                bool hasStates = (typeof(PlayState).GetField("_childStates", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(p) as List<State>)!.Count != 0;
                PlayStateState s = (PlayStateState)typeof(PlayState).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(p)!;
                return s == PlayStateState.S_NORMAL && !hasStates && Player.state == PlayerState.GROUND;
            }
            return false;
        }
    }
}
