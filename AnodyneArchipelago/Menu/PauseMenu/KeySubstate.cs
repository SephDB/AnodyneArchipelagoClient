using AnodyneArchipelago;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Input;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using AnodyneSharp.UI;
using AnodyneSharp.UI.PauseMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnodyneSharp.States.MenuSubstates
{
    public class KeySubstate : DialogueSubstate
    {
        private List<UILabel> _labels;
        private UIEntity[] _keys;
        private UIEntity[] _bigKeys;

        public KeySubstate()
        {
            float x = 65;
            float y = 25;

            string[] names = [
                    "Street",
                    "Temple",
                    "Red Cave",
                    "Cavern",
                    "Apartment",
                    "Circus",
                    "Hotel",
                ];

            string[] counts = [
                    $"x{GlobalState.inventory.GetMapKeys("STREET")}",
                    $"x{GlobalState.inventory.GetMapKeys("BEDROOM")}",
                    $"x{GlobalState.inventory.GetMapKeys("REDCAVE")}",
                    $"x{GlobalState.inventory.GetMapKeys("CROWD")}",
                    $"x{GlobalState.inventory.GetMapKeys("APARTMENT")}",
                    $"x{GlobalState.inventory.GetMapKeys("CIRCUS")}",
                    $"x{GlobalState.inventory.GetMapKeys("HOTEL")}",
                ];

            _labels = Enumerable.Range(0, 7).Select(i => new UILabel(new(x, y + 18 * i), true, names[i])).ToList();

            if (Plugin.ArchipelagoManager.SmallkeyMode == SmallKeyMode.SmallKeys)
            {
                _keys = Enumerable.Range(0, 7).Select(i => new UIEntity(new Vector2(_labels[i].Position.X + 64, _labels[i].Position.Y - 2), "key", 0, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON)).ToArray();

                _labels.AddRange(Enumerable.Range(0, 7).Select(i => new UILabel(new(x + 64 + 12, y + 18 * i), true, counts[i])).ToList());
            }
            else
            {
                _keys = Enumerable.Range(0, 7).Select(i => 
                    new UIEntity(
                            new Vector2(
                                    _labels[i].Position.X + 64 + 14, 
                                    _labels[i].Position.Y - 2
                                ), 
                            "archipelago_items",
                            GlobalState.events.GetEvent($"{ArchipelagoManager.GetMapNameForDungeon(names[i])}_KeyRing_Obtained") == 1 ? 16 : 17, 
                            16, 
                            16, 
                            Drawing.DrawOrder.EQUIPMENT_ICON))
                    .ToArray();
            }

            _bigKeys = Enumerable.Range(0, 3).Select(i => new UIEntity(new Vector2(62 + 16 * i, 150), "key_green", GlobalState.inventory.BigKeyStatus[i] ? i * 2 : i * 2 + 1, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON)).ToArray();
        }

        public override void GetControl()
        {
            Exit = true;
        }

        public override void DrawUI()
        {
            base.DrawUI();

            foreach (var label in _labels)
            {
                label.Draw();
            }

            foreach (var key in _keys)
            {
                key.Draw();
            }

            foreach(var key in _bigKeys)
            {
                key.Draw();
            }
        }
    }
}
