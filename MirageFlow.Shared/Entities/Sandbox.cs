using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MirageFlow.Shared.Entities
{
    // 0 = Empty, 1 = Sand, 2 = Wall (if needed)
    public struct Particle
    {
        public byte Type;
        public Color Color;
        public bool HasMoved;
        public bool IsStatic;
        public sbyte Noise; // Used to simulate sand grain texture visually
    }

    public class Sandbox
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Particle[] Particles { get; private set; }
        public int ActiveParticleCount { get; private set; }

        private Random _random;

        public Sandbox(int width, int height)
        {
            Width = width;
            Height = height;
            Particles = new Particle[width * height];
            _random = new Random();
        }

        public void SetParticle(int x, int y, Particle p)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                int index = y * Width + x;
                // keep track of count
                if (Particles[index].Type == 0 && p.Type == 1) ActiveParticleCount++;
                else if (Particles[index].Type == 1 && p.Type == 0) ActiveParticleCount--;

                Particles[index] = p;
                
                // If we erase a particle (Type=0), we must wake up particles above it
                if (p.Type == 0)
                {
                    WakeUpNeighbors(x, y);
                }
            }
        }
        
        public void WakeUpNeighbors(int x, int y)
        {
            // Wake up the immediate top and top-diagonals
            WakeUp(x, y - 1);
            WakeUp(x - 1, y - 1);
            WakeUp(x + 1, y - 1);
            WakeUp(x - 1, y);
            WakeUp(x + 1, y);
        }

        private void WakeUp(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                int index = y * Width + x;
                if (Particles[index].Type == 1 && Particles[index].IsStatic)
                {
                    Particles[index].IsStatic = false;
                    // Wake up its neighbors cascabelly though next frames
                    WakeUpNeighbors(x, y); 
                }
            }
        }

        public Particle GetParticle(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return Particles[y * Width + x];
            }
            return new Particle { Type = 2 }; // Treat out of bounds as wall
        }

        public void Swap(int x1, int y1, int x2, int y2)
        {
            int index1 = y1 * Width + x1;
            int index2 = y2 * Width + x2;

            Particle temp = Particles[index1];
            Particles[index1] = Particles[index2];
            Particles[index2] = temp;
            
            // Wake neighbors of old position
            WakeUpNeighbors(x1, y1);
        }

        public void Update()
        {
            // Reset HasMoved
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i].HasMoved = false;
            }

            // Iterate from bottom to top so lower particles move first
            for (int y = Height - 2; y >= 0; y--)
            {
                int dir = _random.Next(2) == 0 ? -1 : 1; // Randomize horizontal slide direction

                for (int x = 0; x < Width; x++)
                {
                    int actualX = dir == 1 ? x : (Width - 1 - x); // Iterate left-to-right or right-to-left
                    
                    int index = y * Width + actualX;
                    Particle p = Particles[index];

                    if (p.Type == 1 && !p.HasMoved && !p.IsStatic) // If it's active sand
                    {
                        bool moved = false;
                        
                        // Check straight down
                        if (GetParticle(actualX, y + 1).Type == 0)
                        {
                            Particles[index].HasMoved = true;
                            Swap(actualX, y, actualX, y + 1);
                            moved = true;
                        }
                        // Check down diagonal based on random dir
                        else if (GetParticle(actualX + dir, y + 1).Type == 0)
                        {
                            Particles[index].HasMoved = true;
                            Swap(actualX, y, actualX + dir, y + 1);
                            moved = true;
                        }
                        // Check other diagonal
                        else if (GetParticle(actualX - dir, y + 1).Type == 0)
                        {
                            Particles[index].HasMoved = true;
                            Swap(actualX, y, actualX - dir, y + 1);
                            moved = true;
                        }
                        
                        if (!moved) 
                        {
                            // If it reached a stable spot and couldn't move down or diagonally
                            Particles[index].IsStatic = true;
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, int pixelScale, Texture2D pixelTexture)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Particle p = Particles[y * Width + x];
                    if (p.Type != 0)
                    {
                        Rectangle rect = new Rectangle(
                            (int)position.X + x * pixelScale,
                            (int)position.Y + y * pixelScale,
                            pixelScale,
                            pixelScale
                        );
                        
                        Color drawColor = new Color(
                            Math.Clamp(p.Color.R + p.Noise, 0, 255),
                            Math.Clamp(p.Color.G + p.Noise, 0, 255),
                            Math.Clamp(p.Color.B + p.Noise, 0, 255)
                        );
                        spriteBatch.Draw(pixelTexture, rect, drawColor);
                    }
                }
            }
        }
    }
}
