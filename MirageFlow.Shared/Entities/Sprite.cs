using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MirageFlow.Shared.Entities
{
    public class Sprite
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Color Color = Color.White;

        public virtual void Update(GameTime gameTime) {}
        public virtual void Draw(SpriteBatch spriteBatch) 
        {
            if (Texture != null)
            {
                spriteBatch.Draw(Texture, Position, Color);
            }
        }
    }
}
