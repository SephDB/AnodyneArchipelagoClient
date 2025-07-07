using System.Reflection;
using AnodyneSharp;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.States;

namespace AnodyneArchipelago
{
    public class Plugin : IStateSetter
    {
        public static Plugin? Instance = null;

        static FieldInfo playerField = typeof(PlayState).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static AnodyneGame? Game;
        public static Player Player => (Player)playerField.GetValue(Game?.CurrentState as PlayState)!;
        public static ArchipelagoManager? ArchipelagoManager = null;

        public const string Version = "0.4.1";
         
        public void Load()
        {
            Instance = this;

            Game = (AnodyneGame)GlobalState.GameState;
            GlobalState.GameState = this;
        }

        public static bool ReadyToReceive()
        {
            if (Game?.CurrentState is PlayState p)
            {
                bool hasStates = (typeof(PlayState).GetField("_childStates", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(p) as List<State>)!.Count != 0;
                PlayStateState s = (PlayStateState)typeof(PlayState).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(p)!;
                bool slipping = (bool)typeof(Player).GetField("isSlipping", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Player)!;
                return s == PlayStateState.S_NORMAL && !hasStates && Player.state == PlayerState.GROUND && (Player.ANIM_STATE == PlayerAnimState.as_idle || Player.ANIM_STATE == PlayerAnimState.as_walk) && !slipping;
            }
            return false;
        }

        public void SetState<T>() where T : State, new()
        {
            Game!.SetState<T>();
            if (Game.CurrentState is CreditsState)
            {
                ArchipelagoManager?.OnCredits();
            }
            else if (Game.CurrentState is PlayState)
            {
                var s = GlobalState.SetSubstate;
                GlobalState.SetSubstate = state => SubState(s, state);
            }
        }

        public static void SubState(Action<State> subaction, State next)
        {
            subaction(next);
            if (next is DeathState s)
            {
                ArchipelagoManager?.OnDeath(s);
            }
        }
    }
}
