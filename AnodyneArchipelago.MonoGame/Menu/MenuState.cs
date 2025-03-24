using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using AnodyneSharp.States;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Menu
{
    internal partial class MenuState
    {
        protected void ChangeState()
        {
            if (_isNewGame)
            {
                GlobalState.checkpoint = new GlobalState.CheckPoint("NEXUS", new(704,1392));
                GlobalState.checkpoint.Warp(Vector2.Zero);
                GlobalState.events.ActivatedNexusPortals.Add("STREET");
                GlobalState.NewMapFacing = Facing.UP;
            }
            GlobalState.GameState.SetState<PlayState>();
        }
    }
}
