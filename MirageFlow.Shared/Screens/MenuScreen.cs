using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using MirageFlow.Shared.Core;

namespace MirageFlow.Shared.Screens
{
    public class MenuScreen : IScreen
    {
        private Texture2D _pixel;
        private SpriteFont _font;
        private Rectangle _playButtonRect;
        private Rectangle _quitButtonRect;
        private Rectangle _debugButtonRect;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;

            int btnWidth = 200;
            int btnHeight = 60;
            int xPos = (screenWidth - btnWidth) / 2;

            _playButtonRect = new Rectangle(xPos, screenHeight / 2 - 80, btnWidth, btnHeight);
            _quitButtonRect = new Rectangle(xPos, screenHeight / 2, btnWidth, btnHeight);
            _debugButtonRect = new Rectangle(xPos, screenHeight / 2 + 80, btnWidth, btnHeight);
        }

        public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _font = content.Load<SpriteFont>("Fonts/TitleFont");
        }

        public void Update(GameTime gameTime)
        {
            InputManager.Update();
            
            if (InputManager.IsMouseClicked())
            {
                var mousePos = InputManager.CurrentMouseState.Position;
                if (_playButtonRect.Contains(mousePos))
                {
                    var gameScreen = new GameScreen();
                    gameScreen.Initialize(_pixel.GraphicsDevice);
                    ScreenManager.ChangeScreen(gameScreen, ScreenManager.Content);
                }
                else if (_quitButtonRect.Contains(mousePos))
                {
                    Environment.Exit(0);
                }
                else if (_debugButtonRect.Contains(mousePos))
                {
                    Settings.DebugMode = !Settings.DebugMode;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_pixel == null || _font == null) return;

            spriteBatch.Begin();
            
            spriteBatch.Draw(_pixel, _playButtonRect, Color.DarkGreen);
            var playText = "PLAY";
            var playSize = _font.MeasureString(playText);
            var playPos = new Vector2(_playButtonRect.X + (_playButtonRect.Width - playSize.X) / 2, _playButtonRect.Y + (_playButtonRect.Height - playSize.Y) / 2);
            spriteBatch.DrawString(_font, playText, playPos, Color.White);

            spriteBatch.Draw(_pixel, _quitButtonRect, Color.DarkRed);
            var quitText = "QUIT";
            var quitSize = _font.MeasureString(quitText);
            var quitPos = new Vector2(_quitButtonRect.X + (_quitButtonRect.Width - quitSize.X) / 2, _quitButtonRect.Y + (_quitButtonRect.Height - quitSize.Y) / 2);
            spriteBatch.DrawString(_font, quitText, quitPos, Color.White);

            spriteBatch.Draw(_pixel, _debugButtonRect, Settings.DebugMode ? Color.Goldenrod : Color.Gray);
            var debugText = "DEBUG: " + (Settings.DebugMode ? "ON" : "OFF");
            var debugSize = _font.MeasureString(debugText);
            var debugPos = new Vector2(_debugButtonRect.X + (_debugButtonRect.Width - debugSize.X) / 2, _debugButtonRect.Y + (_debugButtonRect.Height - debugSize.Y) / 2);
            spriteBatch.DrawString(_font, debugText, debugPos, Color.White);

            spriteBatch.End();
        }
    }
}
