using AnodyneArchipelago.Menu;
using AnodyneSharp.Modding;
using AnodyneSharp.States.MenuSubstates;

namespace AnodyneArchipelago.MonoGame
{
    public class Main : IMod
    {
        public Main()
        {
            Plugin plugin = new();
            plugin.Load();
        }

        public void ChangeMainMenu(ref List<(string name, Func<Substate> create)> menuEntries)
        {
            menuEntries.Insert(0, ("AP", () => new MenuState()));
        }

        public void ChangePauseMenu(ref List<(string name, Func<Substate> create)> menuEntries) 
        {
            if (Plugin.ArchipelagoManager != null)
            {
                menuEntries.RemoveAt(1);
                menuEntries.Insert(1, ("Items", () => new APEquipSubstate()));

                if (!Plugin.ArchipelagoManager.UnlockSmallKeyGates)
                {
                    menuEntries.Insert(2, ("Keys", () => new KeySubstate()));
                }
            }
        }

        public void Update()
        {
            Plugin.ArchipelagoManager?.Update();
        }

        public Stream OnManifestLoad(Stream stream, string path)
        {
            return Plugin.ArchipelagoManager?.PatchFile(stream, path) ?? stream;
        }

    }
}
