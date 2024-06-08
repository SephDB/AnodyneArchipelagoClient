using AnodyneArchipelago.Menu;
using AnodyneSharp.Modding;

namespace AnodyneArchipelago.MonoGame
{
    public class Main : IMod
    {
        public Main()
        {
            Plugin plugin = new();
            plugin.Load();
        }

        public void ChangeMainMenu(ref List<(string name, Func<AnodyneSharp.States.MenuSubstates.Substate> create)> menuEntries)
        {
            menuEntries.Insert(0, ("AP", () => new MenuState()));
        }
        
        public void Update()
        {
            Plugin.ArchipelagoManager?.Update();
        }

    }
}
