using AnodyneArchipelago.Effects;
using AnodyneSharp;
using AnodyneSharp.Drawing;
using AnodyneSharp.Drawing.Effects;
using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace AnodyneArchipelago
{
    public class APPIxelate : BaseTrapEffect
    {
        private const float Max = 2f;
        private Effect _effect;

        public float Pixelation { get; private set; } = 1f;

        public APPIxelate()
            :base(3f)
        {
            FieldInfo field = typeof(Pixelate).GetField("effect", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _effect = (Effect)field.GetValue(GlobalState.fullScreenEffects.OfType<Pixelate>().First())!;
        }

        public override bool Active()
        {
            return (int)Pixelation > 1;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            Pixelation = 1;
        }

        public override void Render(SpriteBatch batch, Texture2D screen)
        {
            _effect.Parameters["Projection"].SetValue(SpriteDrawer.Projection(screen.Bounds.Size));
            _effect.Parameters["ScreenSize"].SetValue(new Vector2(screen.Width, screen.Height));
            batch.Begin(effect: _effect);
            batch.Draw(screen, screen.Bounds, Color.White);
            batch.End();
        }

        public override bool Update()
        {
            _effect.Parameters["StrideSize"].SetValue((int)Pixelation);
            return base.Update();
        }

        public override void Activate()
        {
            Pixelation = Max;
        }
    }
}
