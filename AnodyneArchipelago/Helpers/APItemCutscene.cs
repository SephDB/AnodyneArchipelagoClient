using AnodyneSharp.States;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Helpers
{
    public class APItemCutscene : CutsceneState
    {
        public APItemCutscene(IEnumerator<CutsceneEvent> state) : base(state)
        {
            UpdateEntities = false;
        }
    }
}
