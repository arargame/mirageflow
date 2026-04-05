using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MirageFlow.Shared.Entities
{
    public class ConveyorBelt : Sprite
    {
        public List<Bucket> BeltBuckets { get; private set; }
        public float Speed { get; set; } = 100f; // Pixels per second
        
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
                
                // Move bucket to start of belt if it was dropped far right? 
                // Or just let it slide from where it was dropped. Let's let it slide from drop point.
            }
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            for (int i = BeltBuckets.Count - 1; i >= 0; i--)
            {
                var bucket = BeltBuckets[i];
                bucket.Position.X += Speed * dt;
                
                // If bucket moves off the right edge, wrap back to the left edge!
                if (bucket.Position.X > Position.X + Texture.Width)
                {
                    bucket.Position.X = Position.X - bucket.Width;
                }
                
                // If bucket becomes completely full, remove it from the belt (it disappears / is collected)
                if (bucket.IsFull)
                {
                    bucket.IsOnBelt = false;
                    BeltBuckets.RemoveAt(i);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture == null) return;
            spriteBatch.Draw(Texture, Position, Color.White);
        }
    }
}
