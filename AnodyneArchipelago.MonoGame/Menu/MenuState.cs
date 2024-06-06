using AnodyneSharp.Registry;
using AnodyneSharp.States;

namespace AnodyneArchipelago.Menu
{
    internal partial class MenuState
    {
        protected void ChangeState()
        {
            if (_isNewGame)
            {
                GlobalState.GameState.SetState<IntroState>();
            }
            else
            {
                GlobalState.GameState.SetState<PlayState>();
            }
        }
    }
}
