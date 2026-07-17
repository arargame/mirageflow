using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MirageFlow.Shared.Entities;
using System;
using System.Collections.Generic;

namespace MirageFlowNew.Game
{
    /// <summary>
    /// Arkadaki dik "ekran": level görseli mevcut Sandbox kum simülasyonuyla erir,
    /// sonuç her kare bir dokuya yazılıp 3D panel quad'ına kaplanır.
    /// Ayrıca grid sütunu ↔ dünya X eşlemesini sağlar (dökülme noktaları).
    /// </summary>
    public class SandPanel3D : IDisposable
    {
        public Sandbox Sandbox { get; private set; }
        public Texture2D Texture { get; private set; }

        public float WorldWidth { get; private set; }
        public float WorldHeight { get; private set; }
        public float BottomY { get; private set; }
        public float Z { get; private set; }

        public Dictionary<Color, int> ColorCounts { get; private set; }
        public List<Color> Palette { get; private set; }

        private Color[] _buffer;
        private readonly Random _rng = new Random();

        private const int ColorDistanceThreshold = 25000; // Artırıldı: daha fazla renk birleştirilecek
        private const int MaxPaletteSize = 8; // Azaltıldı: ekranda çok fazla kase türü olması önlendi

        public SandPanel3D(GraphicsDevice device, Texture2D levelImage,
            float worldWidth, float bottomY, float z)
        {
            int w = levelImage.Width;
            int h = levelImage.Height;

            WorldWidth = worldWidth;
            WorldHeight = worldWidth * h / w;
            BottomY = bottomY;
            Z = z;

            Sandbox = new Sandbox(w, h);
            Texture = new Texture2D(device, w, h);
            _buffer = new Color[w * h];

            BuildFromImage(levelImage);
        }

        private void BuildFromImage(Texture2D image)
        {
            int w = image.Width, h = image.Height;
            var pixels = new Color[w * h];
            image.GetData(pixels);

            ColorCounts = new Dictionary<Color, int>();
            Palette = new List<Color>();

            int Distance(Color a, Color b)
            {
                int r = a.R - b.R, g = a.G - b.G, bl = a.B - b.B;
                return r * r + g * g + bl * bl;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color src = pixels[y * w + x];
                    if (src.A < 128) continue; // şeffaf = boş

                    // paletten en yakın rengi bul / yoksa ekle
                    Color mapped = default(Color);
                    bool found = false;
                    int bestDist = int.MaxValue;
                    foreach (var p in Palette)
                    {
                        int d = Distance(src, p);
                        if (d < bestDist) { bestDist = d; mapped = p; }
                        if (d <= ColorDistanceThreshold) { found = true; }
                    }
                    if (!found)
                    {
                        if (Palette.Count < MaxPaletteSize)
                        {
                            mapped = src;
                            Palette.Add(src);
                        }
                        // palet doluysa en yakına atanır (mapped zaten en yakın)
                    }

                    Sandbox.SetParticle(x, y, new Particle
                    {
                        Type = 1,
                        Color = mapped,
                        Noise = (sbyte)_rng.Next(-12, 13),
                    });

                    int count;
                    ColorCounts.TryGetValue(mapped, out count);
                    ColorCounts[mapped] = count + 1;
                }
            }
        }

        /// <summary>Grid sütununun dünya X'i (panel X=0 merkezli).</summary>
        public float ColumnToWorldX(int gridX)
        {
            return -WorldWidth * 0.5f + (gridX + 0.5f) * (WorldWidth / Sandbox.Width);
        }

        /// <summary>Dünya X aralığını grid sütun aralığına çevirir.</summary>
        public void WorldRangeToColumns(float left, float right, out int startX, out int endX)
        {
            float cell = WorldWidth / Sandbox.Width;
            startX = (int)((left + WorldWidth * 0.5f) / cell);
            endX = (int)((right + WorldWidth * 0.5f) / cell);
            if (startX < 0) startX = 0;
            if (endX >= Sandbox.Width) endX = Sandbox.Width - 1;
        }

        /// <summary>Sandbox durumunu dokuya yazar (her kare çağrılır).</summary>
        public void UpdateTexture()
        {
            var particles = Sandbox.Particles;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].Type == 1)
                {
                    Color c = particles[i].Color;
                    int n = particles[i].Noise;
                    _buffer[i] = new Color(
                        Clamp(c.R + n), Clamp(c.G + n), Clamp(c.B + n), (int)255);
                }
                else
                {
                    _buffer[i] = Color.Transparent;
                }
            }
            Texture.SetData(_buffer);
        }

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        /// <summary>Panelin world matrisi (alt kenar BottomY'de, X merkezli).</summary>
        public Matrix GetWorldMatrix()
        {
            return Matrix.CreateScale(WorldWidth, WorldHeight, 1f)
                 * Matrix.CreateTranslation(0f, BottomY, Z);
        }

        public void Dispose()
        {
            Texture.Dispose();
        }
    }
}
