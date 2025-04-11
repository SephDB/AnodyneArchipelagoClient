using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Menu
{
    internal class TextEntry : BaseTextEntry
    {
        public TextEntry(string header, GetValue getValueFunc, CommitChange commitFunc) : base(header, getValueFunc, commitFunc)
        {
        }

        public override void GetControl()
        {
            base.GetControl();
            Plugin.Game.Window.TextInput += OnMonoGameTextInput;
        }

        public override void LoseControl()
        {
            base.LoseControl();
            Plugin.Game.Window.TextInput -= OnMonoGameTextInput;
        }

        private void OnMonoGameTextInput(object? sender, TextInputEventArgs e)
        {
            OnTextInput(e.Character);
        }
    }
}
