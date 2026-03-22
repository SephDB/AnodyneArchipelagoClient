using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AnodyneArchipelago.Effects
{
    public class ScreenFlip(bool isHorizontal) : BaseTrapEffect(3f)
    {
        public bool active = false;

        private SpriteEffects _flip = isHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.FlipVertically;

        public bool IsHorizontal => _flip == SpriteEffects.FlipHorizontally;

        public override bool Active()
        {
            return active;
        }

        public override void Deactivate()
        {
            active = false;
        }

        public override void Render(SpriteBatch batch, Texture2D screen)
        {
            batch.Begin();
            batch.Draw(screen, screen.Bounds, null, Color.White, 0, Vector2.Zero, _flip, 0);
            batch.End();
        }

        public override void Activate()
        {
            active = true;
        }
    }
}
