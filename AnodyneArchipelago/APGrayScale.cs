using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnodyneSharp.Drawing;
using AnodyneSharp.Drawing.Effects;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace AnodyneArchipelago
{
    public class APGrayScale : IFullScreenEffect
    {
        public bool active = false;

        private Effect effect;

        public APGrayScale() 
        {
            var field = typeof(GrayScale).GetField("effect", BindingFlags.NonPublic | BindingFlags.Instance)!;
            effect = (Effect)field.GetValue(GlobalState.fullScreenEffects.OfType<GrayScale>().First())!;
        }

        public bool Active()
        {
            return active;
        }

        public void Deactivate()
        {
            active = false;
        }

        public void Load(ContentManager content, GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public void Render(SpriteBatch batch, Texture2D screen)
        {
            effect.Parameters["Projection"].SetValue(SpriteDrawer.Projection(screen.Bounds.Size));
            batch.Begin(effect: effect);
            batch.Draw(screen, screen.Bounds, Color.White);
            batch.End();
        }
    }
}
