using System.Reflection;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Drawing;
using AnodyneSharp.Registry;
using AnodyneSharp.Resources;
using AnodyneSharp.States.MenuSubstates;
using AnodyneSharp.UI;
using AnodyneSharp.UI.PauseMenu.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AnodyneArchipelago.Menu.MenuSubstate
{
    public class ArchipelagoLocalSettings : ListSubstate
    {
        UIEntity? _bgBox;

        public ArchipelagoLocalSettings()
        {
            SetLabels();
        }

        protected override void OnExit()
        {
            base.OnExit();
            GlobalState.settings.Save();
        }

        public override void DrawUI()
        {
            base.DrawUI();

            _bgBox!.Draw();
        }

        protected override void SetLabels()
        {
            bool isChinese = GlobalState.CurrentLanguage == Language.ZH_CN;

            float x = GameConstants.SCREEN_WIDTH_IN_PIXELS / 2 - 136 / 2;
            float menuX = x + 10;
            float y = -2;
            float yStep = GameConstants.FONT_LINE_HEIGHT - GameConstants.LineOffset + 5 + (isChinese ? 2 : 0) + 4;

            _bgBox = new UIEntity(new Vector2(x, 10), "pop_menu", 136, 126, DrawOrder.TEXTBOX);

            string[] sprites = GetPlayerSprites();
            string[] apItemOptions = [
                    "Default",
                    "Match",
                    "Match+"
                ];

            var playerSpriteLabel = new UILabel(new Vector2(menuX, y + yStep * 1.5f), true, "Player sprite", layer: DrawOrder.TEXT);

            var playerSpriteSetting = new TextSelector(
                new Vector2(menuX, playerSpriteLabel.Position.Y + GameConstants.FONT_LINE_HEIGHT + (GlobalState.CurrentLanguage == Language.ZH_CN ? 5 : 2)),
                110,
                Array.FindIndex(sprites, s => s == MenuState.ArchipelagoSettings.PlayerSprite),
                true,
                DrawOrder.SUBMENU_SLIDER,
                sprites)
            {
                ValueChangedEvent = SpriteChanged
            };

            var apItemLabel = new UILabel(playerSpriteLabel.Position + Vector2.UnitY * yStep * 1.5f, true, "AP item\nlook", layer: DrawOrder.TEXT);

            var apItemSetting = new TextSelector(
                new Vector2(x + 60, apItemLabel.Position.Y + (GlobalState.CurrentLanguage == Language.ZH_CN ? 5 : 2) + 3),
                60,
                (int)MenuState.ArchipelagoSettings.MatchDifferentWorldItem,
                true,
                DrawOrder.SUBMENU_SLIDER,
                apItemOptions)
            {
                ValueChangedEvent = ApItemLookChanged
            };

            var trapItemLabel = new UILabel(apItemLabel.Position + Vector2.UnitY * yStep * 1.5f, true, "Disguise trap\nitems", layer: DrawOrder.TEXT);

            var trapItemSetting = new CheckBox(
                new Vector2(x + 110, trapItemLabel.Position.Y + (GlobalState.CurrentLanguage == Language.ZH_CN ? 3 : 0) + 3),
                MenuState.ArchipelagoSettings.HideTrapItems, 
                MenuStyle.SubMenu,
                DrawOrder.SUBMENU_SLIDER)
            {
                ValueChangedEvent = (val) => { MenuState.ArchipelagoSettings.HideTrapItems = val; }
            };

            var colorPuzzleHelpLabel = new UILabel(trapItemLabel.Position + Vector2.UnitY * yStep * 1.5f, true, "Color puzzle\nhelp", layer: DrawOrder.TEXT);

            var colorPuzzleHelpSetting = new CheckBox(
                new Vector2(x + 110, colorPuzzleHelpLabel.Position.Y + (GlobalState.CurrentLanguage == Language.ZH_CN ? 3 : 0) + 3),
                MenuState.ArchipelagoSettings.ColorPuzzleHelp, 
                MenuStyle.SubMenu,
                DrawOrder.SUBMENU_SLIDER)
            {
                ValueChangedEvent = (val) => { MenuState.ArchipelagoSettings.ColorPuzzleHelp = val; }
            };

            options =
            [
                (playerSpriteLabel, playerSpriteSetting),
                (apItemLabel, apItemSetting),
                (trapItemLabel, trapItemSetting),
                (colorPuzzleHelpLabel, colorPuzzleHelpSetting),
            ];
        }

        private static string[] GetPlayerSprites()
        {
            Dictionary<string, Texture2D> textures = (Dictionary<string, Texture2D>)typeof(ResourceManager).GetField("_textures", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            List<string> sprites = [];

            foreach (var sprite in textures.Keys)
            {
                if (sprite.EndsWith("_cell") && !sprite.StartsWith("broom"))
                {
                    sprites.Add(sprite.Replace("_cell", ""));
                }
            }

            sprites.Add("Random");

            return [.. sprites];
        }

        private void SpriteChanged(string newValue, int index)
        {
            MenuState.ArchipelagoSettings.PlayerSprite = newValue;
        }

        private void ApItemLookChanged(string newValue, int index)
        {
            MenuState.ArchipelagoSettings.MatchDifferentWorldItem = (MatchDifferentWorldItem)index;
        }
    }
}
