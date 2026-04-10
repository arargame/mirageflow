using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MirageFlow.Shared.Entities
{
    public class Bucket : Sprite
    {
        public Color TargetColor { get; set; }
        public int Capacity { get; set; }
        public int CurrentFill { get; set; }
        public Texture2D FilledTexture { get; set; }
        public bool IsInverted { get; set; } = false;

        public bool IsFull => CurrentFill >= Capacity;
        
        public int Width = 50;
        public int Height = 50;

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public bool IsDragging { get; set; }
        public Vector2 DragOffset { get; set; }
        
        public bool IsOnBelt { get; set; } = false;

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture == null) return;

            float fillRatio = (float)CurrentFill / Capacity;
            Rectangle destRect = Bounds;
            Color tint = TargetColor * 0.9f;

            // Determine if we use the simple bucket texture or the animated atlas
            if (fillRatio < 0.10f || FilledTexture == null)
            {
                // Empty state logic using 3rd row of atlas if available
                if (FilledTexture != null)
                {
                    int frameWidth = FilledTexture.Width / 2;
                    int frameHeight = FilledTexture.Height / 3; // FIXED: Atlas has 3 rows
                    int row = 2; // Fixed 3rd row for Empty/Inverted
                    int col = IsInverted ? 1 : 0; // Col 0: Active, Col 1: Inverted
                    
                    Rectangle sourceRect = new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
                    spriteBatch.Draw(FilledTexture, destRect, sourceRect, tint);
                }
                else
                {
                    // Fallback to basic texture
                    spriteBatch.Draw(Texture, destRect, tint);
                }
            }
            else
            {
                // 10% and above -> Use the 2x2 atlas (Rows 0 and 1)
                int frameWidth = FilledTexture.Width / 2;
                int frameHeight = FilledTexture.Height / 3; // FIXED: Atlas has 3 rows
                
                int row = 0;
                int col = 0;

                if (fillRatio >= 0.80f) { row = 1; col = 1; } // Frame 4 (1,1)
                else if (fillRatio >= 0.60f) { row = 1; col = 0; } // Frame 3 (1,0)
                else if (fillRatio >= 0.40f) { row = 0; col = 1; } // Frame 2 (0,1)
                else { row = 0; col = 0; } // Frame 1 (0,0) - already >= 0.10f

                Rectangle sourceRect = new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
                spriteBatch.Draw(FilledTexture, destRect, sourceRect, tint);
            }

            // Draw full highlight
            if (IsFull)
            {
                // Draw a small golden shine on top of the full bucket
                spriteBatch.Draw(Texture, new Rectangle(Bounds.X, Bounds.Y, Width, 2), Color.Gold * 0.6f);
            }
        }
    }
}
