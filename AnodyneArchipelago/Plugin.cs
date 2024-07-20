﻿using AnodyneSharp;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.States;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace AnodyneArchipelago
{
    public class Plugin : IStateSetter
    {
        public static Plugin Instance = null;

        static FieldInfo playerField = typeof(PlayState).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static AnodyneGame Game;
        public static Player Player => (Player)playerField.GetValue(Game.CurrentState as PlayState)!;
        public static ArchipelagoManager? ArchipelagoManager = null;

        public const string Version = "0.1.0";

        public void Load()
        {
            Instance = this;

            Game = (AnodyneGame)GlobalState.GameState;
            GlobalState.GameState = this;
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

        public void SetState<T>() where T : State, new()
        {
            Game.SetState<T>();
            if(Game.CurrentState is CreditsState)
            {
                ArchipelagoManager?.OnCredits();
            }
            else if(Game.CurrentState is DeathState d)
            {
                ArchipelagoManager?.OnDeath(d);
            }
        }
    }
}
