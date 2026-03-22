using AnodyneSharp.Drawing;
using AnodyneSharp.Drawing.Effects;
using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using static AnodyneSharp.Registry.GlobalState;

namespace AnodyneArchipelago.Effects
{
    public class APGrayScale : BaseTrapEffect
    {
        public bool active = false;

        private Effect _effect;

        public APGrayScale()
            : base(3f)
        {
            FieldInfo field = typeof(GrayScale).GetField("effect", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _effect = (Effect)field.GetValue(GlobalState.fullScreenEffects.OfType<GrayScale>().First())!;
        }

        public override void Initialize()
        {
            fullScreenEffects.Add(this);
        }

        public override void Remove()
        {
            fullScreenEffects.Remove(this);
        }

        public override bool Active()
        {
            return active;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            active = false;
        }

        public override void Render(SpriteBatch batch, Texture2D screen)
        {
            _effect.Parameters["Projection"].SetValue(SpriteDrawer.Projection(screen.Bounds.Size));
            batch.Begin(effect: _effect);
            batch.Draw(screen, screen.Bounds, Color.White);
            batch.End();
        }

        public override void Activate()
        {
            active = true;
        }
    }
}
