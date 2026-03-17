using AnodyneSharp;
using AnodyneSharp.Drawing;
using AnodyneSharp.Drawing.Effects;
using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AnodyneArchipelago
{
    public class APPIxelate : IFullScreenEffect
    {
        private Effect effect;
        private const float Max = 0.2f;
        public float Pixelation { get; private set; } = 1f;

        public void AddPixelation(float speed)
        {
            Pixelation += speed * GameTimes.DeltaTime;
            Pixelation = Math.Min(2f, Pixelation);
        }

        public void RemovePixelation(float speed)
        {
            Pixelation -= speed * GameTimes.DeltaTime;
            Pixelation = Math.Max(1, Pixelation);
        }


        public APPIxelate() 
        {
            var field = typeof(Pixelate).GetField("effect", BindingFlags.NonPublic | BindingFlags.Instance)!;
            effect = (Effect)field.GetValue(GlobalState.fullScreenEffects.OfType<Pixelate>().First())!;
        }

        public bool Active()
        {
            return (int)Pixelation > 1;
        }

        public void Deactivate()
        {
            Pixelation = 1;
        }

        public void Load(ContentManager content, GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public void Render(SpriteBatch batch, Texture2D screen)
        {
            effect.Parameters["Projection"].SetValue(SpriteDrawer.Projection(screen.Bounds.Size));
            effect.Parameters["ScreenSize"].SetValue(new Vector2(screen.Width, screen.Height));
            batch.Begin(effect: effect);
            batch.Draw(screen, screen.Bounds, Color.White);
            batch.End();
        }

        public void Update()
        {
            effect.Parameters["StrideSize"].SetValue((int)Pixelation);
        }
    }
}
