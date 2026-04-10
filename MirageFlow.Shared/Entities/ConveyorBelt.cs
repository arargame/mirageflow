using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MirageFlow.Shared.Entities
{
    public class ConveyorBelt : Sprite
    {
        public List<Bucket> BeltBuckets { get; private set; }
        public float Speed { get; set; } = 100f; // Pixels per second
        private float _textureOffset = 0f;
        
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);

        public ConveyorBelt()
        {
            BeltBuckets = new List<Bucket>();
        }

        public void AddBucket(Bucket bucket)
        {
            if (!BeltBuckets.Contains(bucket))
            {
                BeltBuckets.Add(bucket);
                bucket.IsOnBelt = true;
                
                // Snap bucket Y to middle of the belt
                bucket.Position.Y = Position.Y + (Texture.Height - bucket.Height) / 2;
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Update texture offset based on speed for scrolling effect
            _textureOffset -= Speed * dt; 

            int capWidth = GetCapWidth();

            for (int i = BeltBuckets.Count - 1; i >= 0; i--)
            {
                var bucket = BeltBuckets[i];
                bucket.Position.X += Speed * dt;
                
                // Wrap logic relative to Texture.Width and capWidth
                // Teleport when it enters the right cap
                if (bucket.Position.X > Position.X + Texture.Width - capWidth)
                {
                    // Start it emerging from the left cap
                    bucket.Position.X = Position.X - bucket.Width + capWidth;
                }
                
                if (bucket.IsFull)
                {
                    bucket.IsOnBelt = false;
                    BeltBuckets.RemoveAt(i);
                }
            }
        }

        private int GetCapWidth()
        {
            if (EndCapTexture == null) return 0;
            // Matches the calculation in Draw (slightly taller than belt)
            int capVisualHeight = (int)(Texture.Height * 1.25f); 
            float scale = (float)capVisualHeight / EndCapTexture.Height;
            return (int)(EndCapTexture.Width * scale);
        }

        public Texture2D EndCapTexture { get; set; }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture == null) return;

            // 1. Draw the belt itself with scrolling effect (Tucked in by 3 pixels on each side)
            Rectangle sourceRect = new Rectangle((int)_textureOffset, 0, Texture.Width, Texture.Height);
            Rectangle destRect = new Rectangle((int)Position.X + 3, (int)Position.Y, Texture.Width - 6, Texture.Height);
            spriteBatch.Draw(Texture, destRect, sourceRect, Color.White);

            // 2. Draw the buckets on the belt (Drawing them here ensures they are behind the end caps)
            foreach (var bucket in BeltBuckets)
            {
                bucket.Draw(spriteBatch);
            }

            // 3. Draw the end caps on top
            if (EndCapTexture != null)
            {
                int capVisualHeight = (int)(Texture.Height * 1.25f); 
                int capWidth = GetCapWidth();
                int capOffsetY = (Texture.Height - capVisualHeight) / 2;
                
                Rectangle leftRect = new Rectangle((int)Position.X, (int)Position.Y + capOffsetY, capWidth, capVisualHeight);
                Rectangle rightRect = new Rectangle((int)(Position.X + Texture.Width - capWidth), (int)Position.Y + capOffsetY, capWidth, capVisualHeight);

                // Left end cap
                spriteBatch.Draw(EndCapTexture, leftRect, Color.White);

                // Right end cap - flipped
                spriteBatch.Draw(EndCapTexture, rightRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
            }
        }
    }
}
