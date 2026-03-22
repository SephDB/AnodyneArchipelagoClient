using AnodyneSharp;
using AnodyneSharp.Drawing.Effects;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static AnodyneSharp.Registry.GlobalState;

namespace AnodyneArchipelago.Effects
{
    public class BaseTrapEffect(float trapIncrease, int trapCountLimit = 5) : IFullScreenEffect
    {
        protected float trapIncrease = trapIncrease;
        protected int trapCountLimit = trapCountLimit;

        protected float trapTimer = 0;

        ~BaseTrapEffect()
        {
            Remove();
        }

        public virtual void Initialize()
        {
            gameEffects.Add(this);
        }

        public virtual void Remove()
        {
            gameEffects.Remove(this);
        }

        public virtual bool Active()
        {
            return false;
        }

        public virtual void Deactivate()
        {
            trapTimer = 0;
        }

        public virtual void Load(ContentManager content, GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public virtual void Render(SpriteBatch batch, Texture2D screen)
        { }

        public virtual bool Update()
        {
            if (!Active())
            {
                return false;
            }

            trapTimer -= GameTimes.DeltaTime;

            if (trapTimer <= 0)
            {
                Deactivate();
                return false;
            }

            return true;
        }

        public virtual void Activate()
        { }

        public void AddTimer()
        {
            if (trapTimer <= 0)
            {
                Activate();
            }

            trapTimer = Math.Min(trapTimer + trapIncrease, trapIncrease * trapCountLimit);
        }
    }
}
