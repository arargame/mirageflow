using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MirageFlow.Shared.Entities
{
    public class ConveyorBelt : Sprite
    {
        public List<Bucket> BeltBuckets { get; private set; }
        public float Speed { get; set; } = 100f; // Pixels per second
        public float Width { get; set; } = 525f; // User's tuned width
        public float Height { get; set; } = 78f; // User's tuned height
        private float _textureOffset = 0f;
        
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, (int)Width, (int)Height);

        public ConveyorBelt()
        {
            BeltBuckets = new List<Bucket>();
        }

        public bool CanAddBucket(float bucketWidth)
        {
            // Calculate max capacity based on visible belt width
            // Each bucket needs its width + 6px (left/right margin)
            float requiredSpacePerBucket = bucketWidth + 6;
            int maxBuckets = (int)(Width / requiredSpacePerBucket);

            // 1. Check total count to ensure total loop capacity isn't exceeded
            if (BeltBuckets.Count >= maxBuckets) return false;

            // 2. Check entry space (Must be clear from both newly added and wrapping buckets)
            float entryX = Position.X - bucketWidth;
            foreach (var b in BeltBuckets)
            {
                // Is any bucket still in the 'entry/re-entry' zone?
                // We check if it's within [entryX - 3, Position.X + 3]
                if (b.Position.X < Position.X + 3) 
                    return false;
            }

            return true;
        }

        public void AddBucket(Bucket bucket)
        {
            if (!BeltBuckets.Contains(bucket))
            {
                BeltBuckets.Add(bucket);
                bucket.IsOnBelt = true;
                
                // Snap bucket Y to middle of the belt - Lifted 5px
                bucket.Position.Y = Position.Y + (Height - bucket.Height) / 2 - 5;
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
                
                // Wrap logic relative to Width and capWidth
                if (bucket.Position.X > Position.X + Width - capWidth)
                {
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
            // Calculations now based on fixed Height
            int capVisualHeight = (int)(Height * 1.55f); 
            float scale = (float)capVisualHeight / EndCapTexture.Height;
            return (int)(EndCapTexture.Width * scale);
        }

        public Texture2D EndCapTexture { get; set; }
        public Vector2 FixedCapPosition { get; set; } // For tuning 2.0

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture == null) return;

            // 1. Draw the belt surface with scrolling and tiling
            Rectangle sourceRect = new Rectangle((int)_textureOffset, 0, (int)Width - 6, Texture.Height);
            Rectangle destRect = new Rectangle((int)Position.X + 3, (int)Position.Y, (int)Width - 6, (int)Height);
            spriteBatch.Draw(Texture, destRect, sourceRect,Color.White);

            // 2. Draw the buckets on the belt (Drawing them here ensures they are behind the end caps)
            foreach (var bucket in BeltBuckets)
            {
                bucket.Draw(spriteBatch);
            }

            // 3. Draw the end caps on top - FULLY FIXED FOR FINAL CALIBRATION
            if (EndCapTexture != null)
            {
                // User's ideal cap parameters from the screenshot
                int fixedCapHeight = 100; 
                float scale = (float)fixedCapHeight / EndCapTexture.Height;
                int capWidth = (int)(EndCapTexture.Width * scale);
                
                // FIXED GLOBAL POSITIONS AS REQUESTED (300, 293)
                float globalCapX = 300;
                float globalCapY = 293;
                float globalAreaWidth = 600; 

                Rectangle leftRect = new Rectangle((int)globalCapX, (int)globalCapY, capWidth, fixedCapHeight);
                Rectangle rightRect = new Rectangle((int)(globalCapX + globalAreaWidth - capWidth), (int)globalCapY, capWidth, fixedCapHeight);

                // Left end cap
                spriteBatch.Draw(EndCapTexture, leftRect, Color.White);

                // Right end cap - flipped
                spriteBatch.Draw(EndCapTexture, rightRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
            }
        }
    }
}
