using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MirageFlow.Shared.Screens;

namespace MirageFlow.Desktop;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1200;
        _graphics.PreferredBackBufferHeight = 800;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        ScreenManager.Content = Content;
        var menuScreen = new MenuScreen();
        menuScreen.Initialize(GraphicsDevice);
        ScreenManager.ChangeScreen(menuScreen, Content);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        ScreenManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        ScreenManager.Draw(_spriteBatch, gameTime);
        base.Draw(gameTime);
    }
}
