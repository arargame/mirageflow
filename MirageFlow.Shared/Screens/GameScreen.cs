using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MirageFlow.Shared.Entities;
using MirageFlow.Shared.Core;
using System.Collections.Generic;
using System.Linq;

namespace MirageFlow.Shared.Screens
{
    public class GameScreen : IScreen
    {
        private GraphicsDevice _graphicsDevice;
        private Microsoft.Xna.Framework.Content.ContentManager _content;
        
        private Sandbox _sandbox;
        private Texture2D _pixelTexture;
        private Texture2D _sampleImage;
        private Texture2D _conveyorTexture;
        private Texture2D _endCapTexture;
        private SpriteFont _font;
        
        private Vector2 _sandboxPosition;
        private int _pixelScale = 5; 
        
        private ConveyorBelt _conveyorBelt;
        private List<Bucket> _inventoryBuckets;
        
        private Rectangle _restartButtonRect;
        private Rectangle _gridBounds;
        private List<Rectangle> _gridSlots;

        private List<Color> _paletteColors;
        private int _fps;
        private int _frameCount;
        private double _elapsedTime;

        // Level State
        private string[] _levelNames = new string[] { "MinnakKedi", "Sample" };
        private int _currentLevelIndex = 0;
        
        private bool _isLevelComplete;
        private double _levelTimer;
        private int _score;
        private Rectangle _nextLevelBtnRect;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
            _restartButtonRect = new Rectangle(graphicsDevice.Viewport.Width - 160, 20, 140, 50);
            
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            _nextLevelBtnRect = new Rectangle(screenWidth / 2 - 100, screenHeight / 2 + 50, 200, 60);
        }

        public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _content = content;
            _conveyorTexture = content.Load<Texture2D>("ConveyorBeltTexture");
            _endCapTexture = content.Load<Texture2D>("EndCapTexture");
            _font = content.Load<SpriteFont>("Fonts/GameFont"); 
            
            LoadLevel(_currentLevelIndex);
        }
        
        private void LoadLevel(int levelIndex)
        {
            if (levelIndex >= _levelNames.Length)
            {
                // Go back to menu if finished all levels
                var menuScreen = new MenuScreen();
                menuScreen.Initialize(_graphicsDevice);
                ScreenManager.ChangeScreen(menuScreen, ScreenManager.Content);
                return;
            }
            
            _sampleImage = _content.Load<Texture2D>(_levelNames[levelIndex]);
            ResetLevel();
        }

        private void ResetLevel()
        {
            _isLevelComplete = false;
            _levelTimer = 0;
            _score = 0;
            
            int imgWidth = _sampleImage.Width;
            int imgHeight = _sampleImage.Height;
            
            _sandbox = new Sandbox(imgWidth, imgHeight);
            
            _pixelScale = System.Math.Min(1200 / imgWidth, 500 / imgHeight);
            if (_pixelScale < 1) _pixelScale = 1;

            _sandboxPosition = new Vector2(
                (_graphicsDevice.Viewport.Width - (_sandbox.Width * _pixelScale)) / 2, 
                20
            );

            Color[] pixels = new Color[imgWidth * imgHeight];
            _sampleImage.GetData(pixels);

            Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();
            List<Color> palette = new List<Color>();
            int colorDistanceThreshold = 8000; 

            int ColorDistance(Color c1, Color c2)
            {
                int r = c1.R - c2.R;
                int g = c1.G - c2.G;
                int b = c1.B - c2.B;
                return r * r + g * g + b * b;
            }

            System.Random noiseRandom = new System.Random();

            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    Color originalColor = pixels[y * imgWidth + x];
                    if (originalColor.A > 10) 
                    {
                        Color assignedColor = originalColor;
                        int minDistance = colorDistanceThreshold;
                        bool foundMatch = false;

                        foreach (var palColor in palette)
                        {
                            int dist = ColorDistance(originalColor, palColor);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                assignedColor = palColor;
                                foundMatch = true;
                            }
                        }

                        if (!foundMatch)
                        {
                            palette.Add(originalColor);
                            assignedColor = originalColor;
                        }

                        // Generate random noise between -25 and +25 for visually sandy effect
                        sbyte pixelNoise = (sbyte)noiseRandom.Next(-25, 26);
                        _sandbox.SetParticle(x, y, new Particle { Type = 1, Color = assignedColor, IsStatic = true, Noise = pixelNoise });
                        
                        if (colorCounts.ContainsKey(assignedColor))
                            colorCounts[assignedColor]++;
                        else
                            colorCounts[assignedColor] = 1;
                    } 
                }
            }

            // Conveyor Belt placed below sandbox
            int conveyorY = (int)_sandboxPosition.Y + (_sandbox.Height * _pixelScale) + 20;
            int conveyorX = (_graphicsDevice.Viewport.Width - _conveyorTexture.Width) / 2;
            _conveyorBelt = new ConveyorBelt();
            _conveyorBelt.Texture = _conveyorTexture;
            _conveyorBelt.EndCapTexture = _endCapTexture;
            _conveyorBelt.Position = new Vector2(conveyorX, conveyorY);

            // Inventory grid placement below the conveyor belt
            _inventoryBuckets = new List<Bucket>();
            _gridSlots = new List<Rectangle>();
            
            int bucketCols = 4;
            int bucketWidth = 50;
            int bucketHeight = 50;
            float spacingX = 80f;
            float spacingY = 80f;
            int totalColors = colorCounts.Count;
            int actualCols = System.Math.Min(bucketCols, totalColors);
            if(actualCols == 0) actualCols = 1;
            
            float gridWidth = actualCols * spacingX;
            int rowCount = (totalColors + bucketCols - 1) / bucketCols;
            float gridHeight = rowCount * spacingY;
            float gridStartX = (_graphicsDevice.Viewport.Width - gridWidth) / 2;
            int gridStartY = conveyorY + _conveyorTexture.Height + 40;

            // Save grid bound for drawing nice background frame
            _gridBounds = new Rectangle((int)gridStartX - 20, gridStartY - 20, (int)gridWidth + 40, (int)gridHeight + 40);

            int i = 0;
            foreach (var kvp in colorCounts)
            {
                float bx = gridStartX + (i % bucketCols) * spacingX;
                float by = gridStartY + (i / bucketCols) * spacingY;

                var bucketBounds = new Rectangle((int)bx, (int)by, bucketWidth, bucketHeight);
                _gridSlots.Add(bucketBounds);

                _inventoryBuckets.Add(new Bucket 
                { 
                    Texture = _pixelTexture, 
                    Position = new Vector2(bx, by), 
                    TargetColor = kvp.Key, 
                    Capacity = kvp.Value 
                });
                i++;
            }
            
            _paletteColors = palette; // Save for debug
        }

        public void Update(GameTime gameTime)
        {
            InputManager.Update();
            
            if (InputManager.IsMouseClicked())
            {
                var mousePos = InputManager.CurrentMouseState.Position;
                
                if (_isLevelComplete)
                {
                    if (_nextLevelBtnRect.Contains(mousePos))
                    {
                        _currentLevelIndex++;
                        LoadLevel(_currentLevelIndex);
                        return;
                    }
                }
                else
                {
                    if (_restartButtonRect.Contains(mousePos))
                    {
                        ResetLevel();
                        return;
                    }
                    
                    HandleBucketClick(mousePos);
                }
            }
            
            if (!_isLevelComplete)
            {
                _levelTimer += gameTime.ElapsedGameTime.TotalSeconds;
                
                _conveyorBelt.Update(gameTime);
                HandleAbsorption();
                _sandbox.Update();

                // Check win condition
                if (_sandbox.ActiveParticleCount <= 0)
                {
                    _isLevelComplete = true;
                    // Score calculation: Base 10000 points minus 50 points per elapsed second
                    _score = System.Math.Max(100, 10000 - (int)(_levelTimer * 50));
                }
            }
            
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                var menuScreen = new MenuScreen();
                menuScreen.Initialize(_graphicsDevice);
                ScreenManager.ChangeScreen(menuScreen, ScreenManager.Content);
            }
        }

        private void HandleBucketClick(Point mousePos)
        {
            // Check if user clicked an inventory bucket
            for (int i = 0; i < _inventoryBuckets.Count; i++)
            {
                var bucket = _inventoryBuckets[i];
                if (bucket.Bounds.Contains(mousePos))
                {
                    // Move it to the start of the conveyor belt!
                    bucket.Position = new Vector2(_conveyorBelt.Position.X - bucket.Width, _conveyorBelt.Position.Y + (_conveyorBelt.Texture.Height - bucket.Height) / 2);
                    _conveyorBelt.AddBucket(bucket);
                    _inventoryBuckets.RemoveAt(i);
                    break;
                }
            }
        }

        private void HandleAbsorption()
        {
            for (int i = _conveyorBelt.BeltBuckets.Count - 1; i >= 0; i--)
            {
                var bucket = _conveyorBelt.BeltBuckets[i];
                if (bucket.IsFull) continue;

                float sbLeftX = bucket.Position.X - _sandboxPosition.X;
                float sbRightX = bucket.Position.X + bucket.Width - _sandboxPosition.X;

                int startGridX = (int)(sbLeftX / _pixelScale);
                int endGridX = (int)(sbRightX / _pixelScale);

                if (startGridX < 0) startGridX = 0;
                if (endGridX >= _sandbox.Width) endGridX = _sandbox.Width - 1;

                if (startGridX <= endGridX && endGridX >= 0 && startGridX < _sandbox.Width)
                {
                    bool absorbed = false;
                    for (int y = _sandbox.Height - 1; y >= _sandbox.Height - 10; y--)
                    {
                        for (int x = startGridX; x <= endGridX; x++)
                        {
                            Particle p = _sandbox.GetParticle(x, y);
                            if (p.Type == 1 && p.Color == bucket.TargetColor)
                            {
                                _sandbox.SetParticle(x, y, new Particle { Type = 0 }); 
                                bucket.CurrentFill += 1;
                                absorbed = true;
                                if (bucket.IsFull) break;
                            }
                        }
                        if (absorbed || bucket.IsFull) break;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _elapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
            _frameCount++;
            if (_elapsedTime >= 1.0)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _elapsedTime = 0;
            }

            var clearColor = new Color(20, 25, 30);
            _graphicsDevice.Clear(clearColor);
            
            spriteBatch.Begin(samplerState: SamplerState.PointWrap);
            
            Rectangle bgRect = new Rectangle((int)_sandboxPosition.X, (int)_sandboxPosition.Y, _sandbox.Width * _pixelScale, _sandbox.Height * _pixelScale);
            spriteBatch.Draw(_pixelTexture, bgRect, Color.DarkSlateGray * 0.4f);

            _sandbox.Draw(spriteBatch, _sandboxPosition, _pixelScale, _pixelTexture);
            
            _conveyorBelt.Draw(spriteBatch);
            
            // Draw inventory Grid Background and Border
            spriteBatch.Draw(_pixelTexture, _gridBounds, new Color(40, 45, 50));
            // Top border
            spriteBatch.Draw(_pixelTexture, new Rectangle(_gridBounds.X, _gridBounds.Y, _gridBounds.Width, 2), Color.Gray);
            // Bottom
            spriteBatch.Draw(_pixelTexture, new Rectangle(_gridBounds.X, _gridBounds.Bottom - 2, _gridBounds.Width, 2), Color.Gray);
            // Left
            spriteBatch.Draw(_pixelTexture, new Rectangle(_gridBounds.X, _gridBounds.Y, 2, _gridBounds.Height), Color.Gray);
            // Right
            spriteBatch.Draw(_pixelTexture, new Rectangle(_gridBounds.Right - 2, _gridBounds.Y, 2, _gridBounds.Height), Color.Gray);

            // Draw empty grid slot placeholders
            foreach(var slotRect in _gridSlots)
            {
                spriteBatch.Draw(_pixelTexture, slotRect, new Color(30, 35, 40)); 
            }

            foreach (var bucket in _inventoryBuckets)
            {
                bucket.Draw(spriteBatch);
            }
            
            // Note: _conveyorBelt.BeltBuckets are now drawn inside _conveyorBelt.Draw() to handle layering with end caps

            spriteBatch.Draw(_pixelTexture, _restartButtonRect, Color.DarkOrange);
            if (_font != null)
            {
                string text = "RESTART";
                var size = _font.MeasureString(text);
                var pos = new Vector2(_restartButtonRect.X + (_restartButtonRect.Width - size.X) / 2, _restartButtonRect.Y + (_restartButtonRect.Height - size.Y) / 2);
                spriteBatch.DrawString(_font, text, pos, Color.White);
            }

            if (Settings.DebugMode && _font != null)
            {
                int textY = 20;
                spriteBatch.DrawString(_font, $"FPS: {_fps}", new Vector2(10, textY), Color.LimeGreen);
                textY += 30;
                spriteBatch.DrawString(_font, $"Sand Particles: {_sandbox.ActiveParticleCount}", new Vector2(10, textY), Color.LimeGreen);
                textY += 30;
                spriteBatch.DrawString(_font, $"Total Colors: {_paletteColors.Count}", new Vector2(10, textY), Color.LimeGreen);
                textY += 30;
                
                spriteBatch.DrawString(_font, "Unique Colors (RGB):", new Vector2(10, textY), Color.LimeGreen);
                textY += 25;
                foreach (var c in _paletteColors)
                {
                    spriteBatch.DrawString(_font, $"R:{c.R} G:{c.G} B:{c.B}", new Vector2(25, textY), c);
                    textY += 25;
                }
            }

            // Level Complete Modal overlay
            if (_isLevelComplete)
            {
                // Semi-transparent dark background
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height), Color.Black * 0.8f);

                int cx = _graphicsDevice.Viewport.Width / 2;
                int cy = _graphicsDevice.Viewport.Height / 2;

                string title = "LEVEL COMPLETED!";
                var tSize = _font.MeasureString(title);
                spriteBatch.DrawString(_font, title, new Vector2(cx - tSize.X / 2, cy - 120), Color.Gold);

                string timeText = $"Time: {_levelTimer:F1} sec";
                var timeSize = _font.MeasureString(timeText);
                spriteBatch.DrawString(_font, timeText, new Vector2(cx - timeSize.X / 2, cy - 60), Color.White);

                string scoreText = $"Score: {_score}";
                var scoreSize = _font.MeasureString(scoreText);
                spriteBatch.DrawString(_font, scoreText, new Vector2(cx - scoreSize.X / 2, cy - 20), Color.LawnGreen);

                // Next Level Button
                spriteBatch.Draw(_pixelTexture, _nextLevelBtnRect, Color.SteelBlue);
                string btnText = "NEXT LEVEL";
                var bSize = _font.MeasureString(btnText);
                var bPos = new Vector2(_nextLevelBtnRect.X + (_nextLevelBtnRect.Width - bSize.X) / 2, _nextLevelBtnRect.Y + (_nextLevelBtnRect.Height - bSize.Y) / 2);
                spriteBatch.DrawString(_font, btnText, bPos, Color.White);
            }

            spriteBatch.End();
        }
    }
}
