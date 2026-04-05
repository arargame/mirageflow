using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MirageFlow.Shared.Entities
{
    public class Bucket : Sprite
    {
        public Color TargetColor { get; set; }
        public int Capacity { get; set; }
        public int CurrentFill { get; set; }

        public bool IsFull => CurrentFill >= Capacity;
        
        // Let's assume a standard 50x50 size for the bucket for now
        public int Width = 50;
        public int Height = 50;

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public bool IsDragging { get; set; }
        public Vector2 DragOffset { get; set; }
        
        public bool IsOnBelt { get; set; } = false;

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture == null) return;

            // Draw base bucket color
            spriteBatch.Draw(Texture, Bounds, TargetColor * 0.5f); // Dimmed version to show it's a bucket of that color
            
            // Draw fill level
            float fillRatio = (float)CurrentFill / Capacity;
            int fillHeight = (int)(Height * fillRatio);
            Rectangle fillRect = new Rectangle(Bounds.X, Bounds.Y + Height - fillHeight, Width, fillHeight);
            
            spriteBatch.Draw(Texture, fillRect, TargetColor);

            // Draw border
            // (A simple border representation just by drawing a slightly larger rect behind or lines, 
            // but we'll keep it simple: draw a small inner border)
            spriteBatch.Draw(Texture, new Rectangle(Bounds.X, Bounds.Y, Width, 2), Color.DarkGray);
            spriteBatch.Draw(Texture, new Rectangle(Bounds.X, Bounds.Y + Height - 2, Width, 2), Color.DarkGray);
            spriteBatch.Draw(Texture, new Rectangle(Bounds.X, Bounds.Y, 2, Height), Color.DarkGray);
            spriteBatch.Draw(Texture, new Rectangle(Bounds.X + Width - 2, Bounds.Y, 2, Height), Color.DarkGray);
        }
    }
}
