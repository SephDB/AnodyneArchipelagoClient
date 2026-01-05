using AnodyneSharp.States;

namespace AnodyneArchipelago.Helpers
{
    public class APItemCutscene : CutsceneState
    {
        public APItemCutscene(IEnumerator<CutsceneEvent?> state) : base(state)
        {
            UpdateEntities = false;
        }
    }
}
