using AnodyneArchipelago;
using AnodyneArchipelago.Helpers;
using AnodyneSharp.Registry;
using AnodyneSharp.UI;
using Microsoft.Xna.Framework;

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
                    "Red Grotto",
                    "Cavern",
                    "Apartment",
                    "Circus",
                    "Hotel",
                ];

            RegionID[] regions = [
                    RegionID.STREET,
                    RegionID.BEDROOM,
                    RegionID.REDCAVE,
                    RegionID.CROWD,
                    RegionID.APARTMENT,
                    RegionID.CIRCUS,
                    RegionID.HOTEL,
                ];

            string[] counts = [.. regions.Select(r => $"x{GlobalState.inventory.GetMapKeys(r.ToString())}")];

            _labels = [.. names.Select((name,i) => new UILabel(new(x, y + 18 * i), true, name))];

            if (Plugin.ArchipelagoManager!.SmallkeyMode == SmallKeyMode.SmallKeys)
            {
                _keys = [.. _labels.Select(label => new UIEntity(new Vector2(label.Position.X + 64, label.Position.Y - 2), "key", 0, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON))];

                _labels.AddRange([.. Enumerable.Range(0, 7).Select(i => new UILabel(new(x + 64 + 12, y + 18 * i), true, counts[i]))]);
            }
            else
            {
                _keys = [.. _labels.Select((label,i) =>
                    new UIEntity(
                            new Vector2(
                                    label.Position.X + 64 + 14,
                                    label.Position.Y - 2
                                ),
                            "archipelago_items",
                            GlobalState.events.GetEvent($"{regions[i]}_KeyRing_Obtained") == 1 ? 16 : 17,
                            16,
                            16,
                            Drawing.DrawOrder.EQUIPMENT_ICON))];
            }

            _bigKeys = [.. GlobalState.inventory.BigKeyStatus.Select((key,i) => new UIEntity(new Vector2(62 + 16 * i, 150), "key_green", key ? i * 2 : i * 2 + 1, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON))];
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

            foreach (var key in _bigKeys)
            {
                key.Draw();
            }
        }
    }
}
