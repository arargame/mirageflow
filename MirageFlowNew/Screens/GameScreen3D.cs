using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MirageFlow.Shared.Core;
using MirageFlow.Shared.Entities;
using MirageFlow.Shared.Screens;
using MirageFlowNew.Game;
using MirageFlowNew.Rendering;
using System;
using System.Collections.Generic;

namespace MirageFlowNew.Screens
{
    /// <summary>
    /// İzometrik 3D sand-loop ekranı:
    /// arkada dik panelde level görseli kum simülasyonuyla erir, eriyen pikseller
    /// 3D kum taneleri olarak öndeki banttan geçen eşleşen renkli 3D kaselere akar.
    /// </summary>
    public class GameScreen3D : IScreen
    {
        private GraphicsDevice _device;
        private Microsoft.Xna.Framework.Content.ContentManager _content;

        private Scene3D _scene;
        private SandPanel3D _panel;
        private Belt3D _belt;
        private SandParticles3D _grains;

        private readonly List<Bowl3D> _inventory = new List<Bowl3D>();
        private readonly Dictionary<Color, Texture2D> _icons = new Dictionary<Color, Texture2D>();
        private readonly List<Rectangle> _inventoryRects = new List<Rectangle>();

        private Texture2D _pixel;
        private SpriteFont _font;

        private readonly string[] _levelNames = { "MinnakKedi", "Sample" };
        private int _levelIndex;

        private bool _isComplete;
        private double _timer;
        private int _score;
        private float _stripePhase;

        private Rectangle _restartRect;
        private Rectangle _nextRect;

        // Sahne yerleşimi
        private const float PanelWorldWidth = 7.5f;
        private const float PanelBottomY = 2.8f;
        private const float PanelZ = -2.2f;
        private const float BeltZ = 0.6f;
        private const float BowlHalfWidth = 0.55f;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _device = graphicsDevice;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _content = content;
            _font = content.Load<SpriteFont>("Fonts/TitleFont");
            _scene = new Scene3D(_device);
            _grains = new SandParticles3D();
            LoadLevel(0);
        }

        private void LoadLevel(int index)
        {
            if (index >= _levelNames.Length)
            {
                BackToMenu();
                return;
            }

            _levelIndex = index;
            _isComplete = false;
            _timer = 0;
            _score = 0;
            _grains.Clear();
            _inventory.Clear();

            if (_panel != null) _panel.Dispose();
            var image = _content.Load<Texture2D>(_levelNames[index]);
            _panel = new SandPanel3D(_device, image, PanelWorldWidth, PanelBottomY, PanelZ);

            _belt = new Belt3D { Z = BeltZ };

            // En çok piksele sahip rengi bul
            int maxPixels = 0;
            foreach (var count in _panel.ColorCounts.Values)
            {
                if (count > maxPixels) maxPixels = count;
            }

            // Maksimum rengin tam 4 kaseye sığacağı standart kapasiteyi belirle
            int standardCapacity = Math.Max(1, (int)Math.Ceiling(maxPixels / 4.0));

            // Palet renklerinden envanter kaseleri üret
            foreach (var kvp in _panel.ColorCounts)
            {
                Color color = kvp.Key;
                int total = kvp.Value;
                int bowlCount = Math.Max(1, (int)Math.Ceiling((double)total / standardCapacity));
                int capacity = standardCapacity;

                if (!_icons.ContainsKey(color))
                    _icons[color] = _scene.RenderBowlIcon(color);

                for (int i = 0; i < bowlCount; i++)
                {
                    _inventory.Add(new Bowl3D
                    {
                        TargetColor = color,
                        Capacity = capacity,
                        IsActive = i == 0, // aynı rengin ilk kasesi aktif
                        Icon = _icons[color],
                    });
                }
            }
        }

        private void BackToMenu()
        {
            var menu = new MenuScreen();
            menu.Initialize(_device);
            ScreenManager.ChangeScreen(menu, ScreenManager.Content);
        }

        // ---------------------------------------------------------------- Update

        public void Update(GameTime gameTime)
        {
            InputManager.Update();

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                BackToMenu();
                return;
            }

            LayoutHud();

            if (InputManager.IsMouseClicked())
            {
                var mouse = InputManager.CurrentMouseState.Position;

                if (_isComplete)
                {
                    if (_nextRect.Contains(mouse))
                        LoadLevel(_levelIndex + 1);
                    return;
                }

                if (_restartRect.Contains(mouse))
                {
                    LoadLevel(_levelIndex);
                    return;
                }

                HandleInventoryClick(mouse);
            }

            if (_isComplete) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _timer += dt;
            _stripePhase += _belt.Speed * dt;

            _belt.Update(gameTime);
            _panel.Sandbox.Update();
            EmitGrains();
            _grains.Update(gameTime, _belt.TopY, _belt.Z);

            if (_panel.Sandbox.ActiveParticleCount <= 0 && _grains.ActiveCount == 0)
            {
                _isComplete = true;
                _score = Math.Max(100, 10000 - (int)(_timer * 50)); // klasik formül
            }
        }

        /// <summary>
        /// Klasik UpdateSandFlow portu: banttaki her kasenin altına düşen panel
        /// sütunlarının EN ALT satırındaki, renk eşleşen pikseller koparılıp
        /// 3D kum tanesi olarak kaseye yollanır (sütun başına kare başına 1 tane).
        /// </summary>
        private void EmitGrains()
        {
            int bottomY = _panel.Sandbox.Height - 1;

            for (int i = _belt.Bowls.Count - 1; i >= 0; i--)
            {
                var bowl = _belt.Bowls[i];
                if (bowl.IsFull) continue;

                int startX, endX;
                _panel.WorldRangeToColumns(bowl.BeltX - BowlHalfWidth, bowl.BeltX + BowlHalfWidth,
                    out startX, out endX);

                for (int x = startX; x <= endX; x++)
                {
                    Particle p = _panel.Sandbox.GetParticle(x, bottomY);
                    if (p.Type == 1 && p.Color == bowl.TargetColor)
                    {
                        var spawn = new Vector3(_panel.ColumnToWorldX(x), _panel.BottomY + 0.05f, _panel.Z + 0.05f);
                        if (_grains.Spawn(spawn, p.Color, bowl))
                        {
                            _panel.Sandbox.SetParticle(x, bottomY, new Particle { Type = 0 });
                        }
                    }
                }
            }
        }

        private void HandleInventoryClick(Point mouse)
        {
            for (int i = 0; i < _inventory.Count && i < _inventoryRects.Count; i++)
            {
                var bowl = _inventory[i];
                if (!_inventoryRects[i].Contains(mouse) || !bowl.IsActive) continue;

                if (_belt.CanAdd())
                {
                    Color color = bowl.TargetColor;
                    _belt.Add(bowl);
                    _inventory.RemoveAt(i);

                    // aynı renkteki sıradaki kaseyi aktifleştir
                    foreach (var inv in _inventory)
                    {
                        if (inv.TargetColor == color && !inv.IsActive)
                        {
                            inv.IsActive = true;
                            break;
                        }
                    }
                }
                return;
            }
        }

        // ------------------------------------------------------------------ Draw

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_scene == null || _panel == null) return;

            _device.Clear(new Color(16, 19, 26));

            Matrix view = _scene.Camera.View;
            Matrix proj = _scene.Camera.GetProjection(_device.Viewport.AspectRatio);
            _scene.BeginFrame(view, proj);

            DrawWorld();

            DrawHud(spriteBatch);
        }

        private void DrawWorld()
        {
            // Zemin
            _scene.Draw(_scene.Ground, Matrix.CreateTranslation(0f, -0.02f, 0f), new Color(34, 30, 40));

            // Panel çerçevesi (arka kutu) + ekran yüzeyi
            float ph = _panel.WorldHeight;
            _scene.Draw(_scene.Box,
                Matrix.CreateScale(_panel.WorldWidth + 0.7f, ph + 0.7f, 0.35f)
                * Matrix.CreateTranslation(0f, _panel.BottomY + ph * 0.5f, _panel.Z - 0.22f),
                new Color(52, 48, 60));

            _panel.UpdateTexture();
            _device.BlendState = BlendState.NonPremultiplied;
            _scene.Draw(_scene.Panel, _panel.GetWorldMatrix(), Color.White, _panel.Texture, 0.55f);
            _device.BlendState = BlendState.Opaque;

            // Bant gövdesi
            _scene.Draw(_scene.Box,
                Matrix.CreateScale(_belt.Length, 0.5f, 1.5f)
                * Matrix.CreateTranslation(0f, 0.25f, _belt.Z),
                new Color(40, 44, 52));

            // Kayan bant çizgileri (hareket hissi)
            float spacing = 1.0f;
            float offset = _stripePhase % spacing;
            for (float x = _belt.LeftX + offset; x < _belt.RightX; x += spacing)
            {
                _scene.Draw(_scene.Box,
                    Matrix.CreateScale(0.06f, 0.03f, 1.42f)
                    * Matrix.CreateTranslation(x, _belt.TopY + 0.015f, _belt.Z),
                    new Color(70, 76, 88));
            }

            // Uç silindir kapakları (metal görünüm)
            foreach (float sign in new[] { -1f, 1f })
            {
                _scene.Draw(_scene.Box,
                    Matrix.CreateScale(0.5f, 0.66f, 1.7f)
                    * Matrix.CreateTranslation(sign * (_belt.Length * 0.5f + 0.22f), 0.33f, _belt.Z),
                    new Color(150, 152, 158));
            }

            // Banttaki kaseler + kum dolum yüzeyi
            foreach (var bowl in _belt.Bowls)
            {
                Vector3 pos = bowl.WorldPosition(_belt.TopY, _belt.Z);
                _scene.Draw(_scene.Bowl, Matrix.CreateTranslation(pos), bowl.TargetColor);

                if (bowl.CurrentFill > 0)
                {
                    float fy, fr;
                    bowl.GetFillSurface(out fy, out fr);
                    Color sand = Color.Lerp(bowl.TargetColor, Color.Black, 0.25f);
                    _scene.Draw(_scene.FillDisc,
                        Matrix.CreateScale(fr, 1f, fr)
                        * Matrix.CreateTranslation(pos + new Vector3(0f, fy, 0f)),
                        sand, null, 0.15f);
                }
            }

            // Uçan kum taneleri
            _grains.Draw(_scene);
        }

        private void LayoutHud()
        {
            int sw = _device.Viewport.Width;
            int sh = _device.Viewport.Height;

            _restartRect = new Rectangle(sw - 170, 14, 150, 46);
            _nextRect = new Rectangle(sw / 2 - 130, sh / 2 + 40, 260, 60);

            // Envanter: alt şerit, satır başına sığdığı kadar ikon
            _inventoryRects.Clear();
            int icon = 68, gap = 10;
            int perRow = Math.Max(1, (sw - 40) / (icon + gap));
            int rows = (_inventory.Count + perRow - 1) / perRow;
            int totalW = Math.Min(_inventory.Count, perRow) * (icon + gap) - gap;
            int x0 = (sw - totalW) / 2;
            int y0 = sh - rows * (icon + gap) - 14;

            for (int i = 0; i < _inventory.Count; i++)
            {
                int col = i % perRow, row = i / perRow;
                _inventoryRects.Add(new Rectangle(x0 + col * (icon + gap), y0 + row * (icon + gap), icon, icon));
            }
        }

        private void DrawHud(SpriteBatch sb)
        {
            sb.Begin();

            // Üst bilgi
            string info = string.Format("Level {0}: {1}   Kalan: {2}",
                _levelIndex + 1, _levelNames[_levelIndex], _panel.Sandbox.ActiveParticleCount);
            sb.DrawString(_font, info, new Vector2(16, 14), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            string timeStr = TimeSpan.FromSeconds(_timer).ToString(@"m\:ss");
            sb.DrawString(_font, timeStr, new Vector2(16, 44), new Color(255, 220, 140), 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);

            // Restart
            sb.Draw(_pixel, _restartRect, new Color(190, 90, 20));
            DrawCentered(sb, "RESTART", _restartRect, 0.5f, Color.White);

            // Envanter paneli
            if (_inventoryRects.Count > 0)
            {
                var first = _inventoryRects[0];
                var last = _inventoryRects[_inventoryRects.Count - 1];
                var bg = Rectangle.Union(first, last);
                bg.Inflate(12, 12);
                sb.Draw(_pixel, bg, Color.Black * 0.45f);
            }

            for (int i = 0; i < _inventory.Count && i < _inventoryRects.Count; i++)
            {
                var bowl = _inventory[i];
                var rect = _inventoryRects[i];
                Color tint = bowl.IsActive ? Color.White : new Color(60, 60, 60, 160);
                if (bowl.Icon != null)
                    sb.Draw(bowl.Icon, rect, tint);

                string cap = bowl.Capacity.ToString();
                sb.DrawString(_font, cap,
                    new Vector2(rect.Center.X - _font.MeasureString(cap).X * 0.175f, rect.Bottom - 20),
                    bowl.IsActive ? Color.White : Color.Gray,
                    0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
            }

            // Level tamamlandı
            if (_isComplete)
            {
                sb.Draw(_pixel, new Rectangle(0, 0, _device.Viewport.Width, _device.Viewport.Height),
                    Color.Black * 0.6f);
                var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 2f);

                string done = "LEVEL TAMAMLANDI!";
                var ds = _font.MeasureString(done);
                sb.DrawString(_font, done, center - new Vector2(ds.X / 2f, ds.Y / 2f + 60), new Color(160, 230, 130));

                string scoreStr = string.Format("Skor: {0}   Sure: {1}", _score, TimeSpan.FromSeconds(_timer).ToString(@"m\:ss"));
                var ss = _font.MeasureString(scoreStr) * 0.6f;
                sb.DrawString(_font, scoreStr, center - new Vector2(ss.X / 2f, ss.Y / 2f), Color.White,
                    0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

                sb.Draw(_pixel, _nextRect, new Color(30, 120, 50));
                DrawCentered(sb, _levelIndex + 1 < _levelNames.Length ? "SONRAKI LEVEL" : "MENUYE DON", _nextRect, 0.55f, Color.White);
            }

            sb.End();
        }

        private void DrawCentered(SpriteBatch sb, string text, Rectangle rect, float scale, Color color)
        {
            var size = _font.MeasureString(text) * scale;
            sb.DrawString(_font, text,
                new Vector2(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
