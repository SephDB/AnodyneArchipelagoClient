using AnodyneSharp.Input;
using AnodyneSharp.Sounds;
using AnodyneSharp.UI;
using AnodyneSharp.UI.PauseMenu.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AnodyneArchipelago.Menu
{
    internal class BaseTextEntry : UIOption
    {
        public delegate string GetValue();
        public delegate void CommitChange(string value);

        private readonly GetValue _getValueFunc;
        private readonly CommitChange _commitFunc;

        private string _value;
        private string _originalValue;

        private UILabel _headerLabel;
        private UILabel _valueLabel;
        private UIEntity _bgBox;

        public bool Active { get; private set; } = false;

        public BaseTextEntry(string header, GetValue getValueFunc, CommitChange commitFunc)
        {
            _getValueFunc = getValueFunc;

            _value = _getValueFunc();
            _originalValue = _value;
            _commitFunc = commitFunc;

            _headerLabel = new(new Vector2(20f, 44f), false, header, new Color(226, 226, 226), AnodyneSharp.Drawing.DrawOrder.TEXT);
            _valueLabel = new(new Vector2(20f, 52f), false, "", new Color(), AnodyneSharp.Drawing.DrawOrder.TEXT);
            _bgBox = new UIEntity(new Vector2(16f, 40f), "pop_menu", 16, 16, AnodyneSharp.Drawing.DrawOrder.TEXTBOX);

            UpdateDisplay();
        }

        public override void GetControl()
        {
            Active = true;

            _value = _getValueFunc();
            _originalValue = _value;

            UpdateDisplay();
        }

        public override void LoseControl()
        {
            Active = false;
        }

        protected void OnTextInput(char ch)
        {
            if (ch == '\b')
            {
                if (_value.Length > 0)
                {
                    _value = _value[..^1];
                    UpdateDisplay();

                    SoundManager.PlaySoundEffect("menu_move");
                }
            }
            //else if (ch == 22)
            //{
            //    _value += Functions.GetClipboard();
            //    UpdateDisplay();
            //}
            else if (!char.IsControl(ch))
            {
                _value += ch;
                UpdateDisplay();

                SoundManager.PlaySoundEffect("menu_move");
            }
        }

        public override void Update()
        {
            if (Active)
            {
                if (KeyInput.JustPressedKey(Keys.Escape) || (KeyInput.ControllerMode && KeyInput.JustPressedRebindableKey(KeyFunctions.Cancel)))
                {
                    SoundManager.PlaySoundEffect("menu_select");
                    this.Exit = true;

                    _value = _originalValue;
                    UpdateDisplay();
                }
                else if (KeyInput.JustPressedKey(Keys.Enter) || (KeyInput.ControllerMode && KeyInput.JustPressedRebindableKey(KeyFunctions.Accept)))
                {
                    SoundManager.PlaySoundEffect("menu_select");
                    _commitFunc(_value);
                    this.Exit = true;
                }
                else if ((KeyInput.IsKeyPressed(Keys.LeftControl) || KeyInput.IsKeyPressed(Keys.RightControl)) && KeyInput.JustPressedKey(Keys.V))
                {
                    _value += Functions.GetClipboard().Trim();
                    UpdateDisplay();

                    SoundManager.PlaySoundEffect("menu_move");
                }
            }
        }

        public override void Draw()
        {
            if (Active)
            {
                _bgBox.Draw();
                _headerLabel.Draw();
                _valueLabel.Draw();
            }
        }

        private void UpdateDisplay()
        {
            if (_value.Length == 0)
            {
                _valueLabel.SetText("[empty]");
                _valueLabel.Color = new Color(116, 140, 144);
            }
            else
            {
                string finalText = "";
                string tempText = _value;

                while (tempText.Length > 18)
                {
                    finalText += tempText[..18];
                    finalText += "\n";
                    tempText = tempText[18..];
                }

                finalText += tempText;

                _valueLabel.SetText(finalText);
                _valueLabel.Color = new Color(184, 32, 0);
            }

            float innerHeight = 8f + _valueLabel.Writer.TotalTextHeight();

            _bgBox = new UIEntity(new Vector2(16f, 40f), "pop_menu", 136, (int)innerHeight + 8, AnodyneSharp.Drawing.DrawOrder.TEXTBOX);
        }
    }
}
