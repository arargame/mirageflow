using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MirageFlow.Shared.Entities;
using MirageFlow.Shared.Core;
using System.Collections.Generic;
using System.Linq;

namespace MirageFlow.Shared.Screens
{
    public class FlowParticle
    {
        public Vector2 Position;
        public Color Color;
        public Bucket Target;
        public float Speed = 300f;
        public bool IsActive = true;
    }

    public class GameScreen : IScreen
    {
        private GraphicsDevice _graphicsDevice;
        private Microsoft.Xna.Framework.Content.ContentManager _content;
        
        private Sandbox _sandbox;
        private Texture2D _pixelTexture;
        private Texture2D _sampleImage;
        private Texture2D _conveyorTexture;
        private Texture2D _endCapTexture;
        private Texture2D _bucketTexture;
        private Texture2D _filledBucketsTexture;
        private SpriteFont _font;
        
        private Vector2 _sandboxPosition;
        private int _pixelScale = 5; 
        
        private ConveyorBelt _conveyorBelt;
        private List<Bucket> _inventoryBuckets;
        private List<FlowParticle> _flowParticles = new List<FlowParticle>();
        
        private Rectangle _restartButtonRect;
        private Rectangle _gridBounds;
        private List<Rectangle> _gridSlots;
        private Dictionary<Color, int> _levelColorCounts;

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
            _conveyorTexture = content.Load<Texture2D>("BentSurfacePieceTexture");
            _endCapTexture = content.Load<Texture2D>("EndCapTexture");
            _bucketTexture = content.Load<Texture2D>("BucketTexture");
            _filledBucketsTexture = content.Load<Texture2D>("FilledBucketsTexture");
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

            _levelColorCounts = colorCounts;

            // Conveyor Belt placed below sandbox
            int conveyorY = (int)_sandboxPosition.Y + (_sandbox.Height * _pixelScale) + 32;
            _conveyorBelt = new ConveyorBelt();
            _conveyorBelt.Texture = _conveyorTexture;
            _conveyorBelt.EndCapTexture = _endCapTexture;
            
            // Final calibrated dimensions from tuning
            _conveyorBelt.Width = 525f;
            _conveyorBelt.Height = 78f;
            
            // Center exclusively based on the belt width
            int finalX = (_graphicsDevice.Viewport.Width - (int)_conveyorBelt.Width) / 2;
            _conveyorBelt.Position = new Vector2(finalX, conveyorY);
            
            // Set fixed cap position for tuning 2.0 (Centered around 600px area)
            _conveyorBelt.FixedCapPosition = new Vector2((_graphicsDevice.Viewport.Width - 600) / 2, conveyorY);

            // --- DYNAMIC BUCKET CAPACITY ALGORITHM ---
            // 1. Find the color with the most pixels to determine the "Standard Volume"
            int maxPixels = 0;
            foreach (var count in _levelColorCounts.Values)
            {
                if (count > maxPixels) maxPixels = count;
            }

            // 2. Set StandardCapacity so that the max color fits in exactly 4 buckets
            int standardCapacity = (int)System.Math.Ceiling(maxPixels / 4.0);
            if (standardCapacity < 1) standardCapacity = 1;

            // 3. Setup Inventory Grid layout parameters
            _inventoryBuckets = new List<Bucket>();
            _gridSlots = new List<Rectangle>();
            
            int bucketCols = 5;
            float spacingX = 90f; // Increased for better spacing
            float spacingY = 100f; // Increased for better spacing
            
            // Total buckets we'll create across all colors
            int totalInventoryBuckets = 0;
            foreach (var count in _levelColorCounts.Values)
            {
                totalInventoryBuckets += (int)System.Math.Min(4, System.Math.Ceiling(count / (double)standardCapacity));
            }

            // Center the grid based on total needed slots
            int gridRows = (totalInventoryBuckets + bucketCols - 1) / bucketCols;
            float gridWidth = System.Math.Min(bucketCols, totalInventoryBuckets) * spacingX;
            float gridHeight = gridRows * spacingY;
            
            float gridStartX = (_graphicsDevice.Viewport.Width - gridWidth) / 2;
            int gridStartY = (int)_conveyorBelt.Position.Y + (int)_conveyorBelt.Height + 80;

            // Save grid bound for drawing nice background frame
            _gridBounds = new Rectangle((int)gridStartX - 20, gridStartY - 20, (int)gridWidth + 40, (int)gridHeight + 40);

            // 4. Create buckets and slots
            int colorIndex = 0;
            foreach (var kvp in _levelColorCounts)
            {
                Color color = kvp.Key;
                int count = kvp.Value;
                int bucketsForThisColor = (int)System.Math.Min(4, System.Math.Ceiling(count / (double)standardCapacity));

                for (int b = 0; b < bucketsForThisColor; b++)
                {
                    int row = colorIndex / bucketCols;
                    int col = colorIndex % bucketCols;

                    float bx = gridStartX + col * spacingX;
                    float by = gridStartY + row * spacingY;

                    _gridSlots.Add(new Rectangle((int)bx, (int)by, 50, 50));
                    _inventoryBuckets.Add(new Bucket 
                    { 
                        Texture = _bucketTexture, 
                        FilledTexture = _filledBucketsTexture,
                        Position = new Vector2(bx, by), 
                        TargetColor = color, 
                        Capacity = standardCapacity,
                        IsInverted = (b > 0) // Only the first bucket of each color is active (upright)
                    });

                    colorIndex++;
                }
            }
            
            _paletteColors = palette; // Save for debug
        }

        private void HandleDebugInput(GameTime gameTime)
        {
            var kState = Keyboard.GetState();
            float adjustSpeed = 50f; // Units per second
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_conveyorBelt != null)
            {
                // Adjust Height with Up/Down
                if (kState.IsKeyDown(Keys.Up)) _conveyorBelt.Height += adjustSpeed * dt;
                if (kState.IsKeyDown(Keys.Down)) _conveyorBelt.Height -= adjustSpeed * dt;

                // Adjust Width with Left/Right
                if (kState.IsKeyDown(Keys.Right)) _conveyorBelt.Width += adjustSpeed * dt;
                if (kState.IsKeyDown(Keys.Left)) _conveyorBelt.Width -= adjustSpeed * dt;

                // Move Position with WASD
                if (kState.IsKeyDown(Keys.W)) _conveyorBelt.Position.Y -= adjustSpeed * dt;
                if (kState.IsKeyDown(Keys.S)) _conveyorBelt.Position.Y += adjustSpeed * dt;
                if (kState.IsKeyDown(Keys.A)) _conveyorBelt.Position.X -= adjustSpeed * dt;
                if (kState.IsKeyDown(Keys.D)) _conveyorBelt.Position.X += adjustSpeed * dt;

                // Dynamically update grid position during tuning to keep it below the belt
                UpdateInventoryGridPosition();
            }
        }

        private void UpdateInventoryGridPosition()
        {
            if (_conveyorBelt == null || _levelColorCounts == null) return;
            
            int bucketCols = 5;
            float spacingX = 90f;
            float spacingY = 100f;
            
            // 1. Determine StandardCapacity (same as ResetLevel)
            int maxPixels = 0;
            foreach (var count in _levelColorCounts.Values)
            {
                if (count > maxPixels) maxPixels = count;
            }
            int standardCapacity = (int)System.Math.Ceiling(maxPixels / 4.0);
            if (standardCapacity < 1) standardCapacity = 1;

            // 2. Calculate total inventory buckets generated
            int totalInventoryBuckets = 0;
            foreach (var count in _levelColorCounts.Values)
            {
                totalInventoryBuckets += (int)System.Math.Min(4, System.Math.Ceiling(count / (double)standardCapacity));
            }

            // 3. Calculate grid dimensions
            int actualCols = System.Math.Min(bucketCols, totalInventoryBuckets);
            if (actualCols < 1) actualCols = 1;
            
            int gridRows = (totalInventoryBuckets + bucketCols - 1) / bucketCols;
            float gridWidth = actualCols * spacingX;
            float gridHeight = gridRows * spacingY;
            
            float gridStartX = (_graphicsDevice.Viewport.Width - gridWidth) / 2;
            int gridStartY = (int)_conveyorBelt.Position.Y + (int)_conveyorBelt.Height + 80;

            // 4. Update the visual frame bounds
            _gridBounds = new Rectangle((int)gridStartX - 20, gridStartY - 20, (int)gridWidth + 40, (int)gridHeight + 40);

            // 5. Update actual bucket and slot positions
            _gridSlots.Clear();
            int bucketNum = 0;
            foreach (var kvp in _levelColorCounts)
            {
                int countForThisColor = (int)System.Math.Min(4, System.Math.Ceiling(kvp.Value / (double)standardCapacity));

                for (int b = 0; b < countForThisColor; b++)
                {
                    int row = bucketNum / bucketCols;
                    int col = bucketNum % bucketCols;

                    float bx = gridStartX + col * spacingX;
                    float by = gridStartY + row * spacingY;

                    _gridSlots.Add(new Rectangle((int)bx, (int)by, 50, 50));

                    // If we already have initialized buckets, update their positions
                    if (bucketNum < _inventoryBuckets.Count)
                    {
                        _inventoryBuckets[bucketNum].Position = new Vector2(bx, by);
                    }

                    bucketNum++;
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            HandleDebugInput(gameTime);
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
                UpdateSandFlow(gameTime);
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

        private void UpdateSandFlow(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 1. Spawning FlowParticles from sandbox bottom
            if (_sandbox != null && _conveyorBelt != null)
            {
                int bottomY = _sandbox.Height - 1;
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
                        for (int x = startGridX; x <= endGridX; x++)
                        {
                            Particle p = _sandbox.GetParticle(x, bottomY);
                            // Only match color
                            if (p.Type == 1 && p.Color == bucket.TargetColor)
                            {
                                // Remove from sandbox
                                _sandbox.SetParticle(x, bottomY, new Particle { Type = 0 });
                                
                                // Create flow particle starting at the bottom of the sandbox
                                float worldX = _sandboxPosition.X + (x * _pixelScale) + (_pixelScale / 2f);
                                float worldY = _sandboxPosition.Y + (_sandbox.Height * _pixelScale);
                                
                                _flowParticles.Add(new FlowParticle
                                {
                                    Position = new Vector2(worldX, worldY),
                                    Color = p.Color,
                                    Target = bucket
                                });
                                // We emit one particle per column per frame per bucket to keep flow smooth but not too fast
                            }
                        }
                    }
                }
            }

            // 2. Update flowing particles
            for (int i = _flowParticles.Count - 1; i >= 0; i--)
            {
                var fp = _flowParticles[i];
                if (fp.Target == null || !fp.Target.IsOnBelt)
                {
                    _flowParticles.RemoveAt(i);
                    continue;
                }

                // Vectorial tracking: move towards bucket top center
                Vector2 targetPos = new Vector2(fp.Target.Position.X + fp.Target.Width / 2, fp.Target.Position.Y);
                Vector2 dir = targetPos - fp.Position;
                float dist = dir.Length();

                if (dist < 8) // Snap to bucket on impact
                {
                    fp.Target.CurrentFill++;
                    _flowParticles.RemoveAt(i);
                }
                else
                {
                    dir.Normalize();
                    fp.Position += dir * fp.Speed * dt;
                    
                    // Add Jitter (organic randomness in both X and Y)
                    // We use the particle's hash and position for a unique seed per frame
                    System.Random rnd = new System.Random(fp.GetHashCode() + (int)(fp.Position.X * 10) + (int)(fp.Position.Y * 10));
                    fp.Position.X += (float)(rnd.NextDouble() * 4.0 - 2.0); // -2 to +2 jitter
                    fp.Position.Y += (float)(rnd.NextDouble() * 4.0 - 2.0); // -2 to +2 jitter
                }
            }
        }

        private void HandleBucketClick(Point mousePos)
        {
            // Check if user clicked an inventory bucket
            for (int i = 0; i < _inventoryBuckets.Count; i++)
            {
                var bucket = _inventoryBuckets[i];
                
                // Only allow clicking upright (active) buckets
                if (bucket.Bounds.Contains(mousePos) && !bucket.IsInverted)
                {
                    // USE THE NEW CAPACITY CHECK FROM CONVEYOR
                    if (_conveyorBelt.CanAddBucket(bucket.Width))
                    {
                        Color bucketColor = bucket.TargetColor;
                        float entryX = _conveyorBelt.Position.X - bucket.Width;
                        // Lifted 5px up from the default middle
                        float entryY = _conveyorBelt.Position.Y + (_conveyorBelt.Height - bucket.Height) / 2 - 5;

                        bucket.Position = new Vector2(entryX, entryY);
                        _conveyorBelt.AddBucket(bucket);
                        _inventoryBuckets.RemoveAt(i);

                        // ACTIVATE NEXT BUCKET OF SAME COLOR (if any)
                        foreach (var invBucket in _inventoryBuckets)
                        {
                            if (invBucket.TargetColor == bucketColor && invBucket.IsInverted)
                            {
                                invBucket.IsInverted = false;
                                break;
                            }
                        }
                    }
                    break;
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
            
            // Draw Flowing Particles
            foreach (var fp in _flowParticles)
            {
                // Slightly larger particles for better visibility (+2px over standard pixel scale)
                int renderSize = _pixelScale + 2; 
                spriteBatch.Draw(_pixelTexture, new Rectangle((int)fp.Position.X, (int)fp.Position.Y, renderSize, renderSize), fp.Color);
            }
            
            // --- CALIBRATION DEBUG INFO 2.0 ---
            // Cap info (currently locked in ConveyorBelt.cs)
            int fixedCapHeight = 100;
            int globalCapX = (int)_conveyorBelt.FixedCapPosition.X;
            int globalCapY = (int)_conveyorBelt.FixedCapPosition.Y;
            int globalWidth = 600;
            
            string beltInfo = $"[BELT] Pos: {_conveyorBelt.Position.X:F0}, {_conveyorBelt.Position.Y:F0} | Size: {_conveyorBelt.Width:F0} x {_conveyorBelt.Height:F0}";
            string capInfo = $"[CAPS] Pos: {globalCapX}, {globalCapY} | Base Area: {globalWidth} x {fixedCapHeight}";
            
            spriteBatch.DrawString(_font, beltInfo, new Vector2(10, 10), Color.Yellow);
            spriteBatch.DrawString(_font, capInfo, new Vector2(10, 35), Color.Cyan);
            // ----------------------------------
            
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
