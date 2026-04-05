using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MirageFlow.Shared.Screens
{
    public static class ScreenManager
    {
        public static Microsoft.Xna.Framework.Content.ContentManager Content { get; set; }
        private static IScreen _currentScreen;

        public static void ChangeScreen(IScreen newScreen, Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _currentScreen = newScreen;
            _currentScreen.LoadContent(content);
        }

        public static void Update(GameTime gameTime)
        {
            _currentScreen?.Update(gameTime);
        }

        public static void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _currentScreen?.Draw(spriteBatch, gameTime);
        }
    }
}
