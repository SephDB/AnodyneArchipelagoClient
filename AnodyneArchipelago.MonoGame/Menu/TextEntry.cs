using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Menu
{
    internal class TextEntry(string header, BaseTextEntry.GetValue getValueFunc, BaseTextEntry.CommitChange commitFunc) : BaseTextEntry(header, getValueFunc, commitFunc)
    {
        public override void GetControl()
        {
            base.GetControl();
            Plugin.Game!.Window.TextInput += OnMonoGameTextInput;
        }

        public override void LoseControl()
        {
            base.LoseControl();
            Plugin.Game!.Window.TextInput -= OnMonoGameTextInput;
        }

        private void OnMonoGameTextInput(object? sender, TextInputEventArgs e)
        {
            OnTextInput(e.Character);
        }
    }
}
