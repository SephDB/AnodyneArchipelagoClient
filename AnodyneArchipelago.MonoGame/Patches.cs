using AnodyneArchipelago.Menu;
using AnodyneSharp.Registry;
using AnodyneSharp.States.MainMenu;
using AnodyneSharp.States.MenuSubstates;
using HarmonyLib;

namespace AnodyneArchipelago.MonoGame
{
    [HarmonyPatch(typeof(MainMenuState), "GetLabels")]
    class MainMenuUpdatePatch
    {
        static void Postfix(ref List<(string name, Func<Substate> create)> __result)
        {
            __result.Insert(0, ("AP", () => new MenuState()));
        }
    }
}
