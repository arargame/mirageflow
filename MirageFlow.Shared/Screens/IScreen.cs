using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MirageFlow.Shared.Screens
{
    public interface IScreen
    {
        void Initialize(GraphicsDevice graphicsDevice);
        void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content);
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }
}
