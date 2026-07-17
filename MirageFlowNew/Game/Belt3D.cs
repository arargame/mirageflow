using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MirageFlowNew.Game
{
    /// <summary>
    /// 3D konveyör bant mantığı — klasik ekrandaki ConveyorBelt'in dünya-uzayı portu:
    /// kaseler soldan girer, sağ uçta başa sarar, dolunca banttan çıkar.
    /// </summary>
    public class Belt3D
    {
        public float Length = 13f;                 // X ekseni boyunca
        public float Speed = 1.6f;                 // birim/sn
        public float TopY = 0.5f;                  // bant üst yüzeyi
        public float Z = 0f;                       // bant merkez Z
        public const float BowlSpacing = 1.5f;     // kase başına gereken yer

        public List<Bowl3D> Bowls { get; private set; } = new List<Bowl3D>();

        /// <summary>Dolan kaseler çıktığında tetiklenir (skor/ses için).</summary>
        public event Action<Bowl3D> BowlCompleted;

        public float LeftX => -Length * 0.5f;
        public float RightX => Length * 0.5f;

        public bool CanAdd()
        {
            int maxBowls = (int)(Length / BowlSpacing);
            if (Bowls.Count >= maxBowls) return false;

            float spawnX = LeftX - 0.2f;
            float minDistance = 1.2f; // Kaselerin fiziksel genişliği ~1.1f

            foreach (var b in Bowls)
            {
                // Giriş noktasında kase var mı?
                if (Math.Abs(b.BeltX - spawnX) < minDistance)
                    return false;

                // Sağ uçtaki kase başa sarmak üzere mi? (12.8f wrap mesafesi)
                float virtualX = b.BeltX - (RightX - 0.4f - (LeftX - 0.2f)); 
                if (Math.Abs(virtualX - spawnX) < minDistance)
                    return false;
            }
            return true;
        }

        public void Add(Bowl3D bowl)
        {
            bowl.BeltX = LeftX - 0.2f;
            bowl.IsOnBelt = true;
            Bowls.Add(bowl);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = Bowls.Count - 1; i >= 0; i--)
            {
                var bowl = Bowls[i];
                bowl.BeltX += Speed * dt;

                // sağ uçtan başa sar
                if (bowl.BeltX > RightX - 0.4f)
                    bowl.BeltX = LeftX - 0.2f;

                if (bowl.IsFull)
                {
                    bowl.IsOnBelt = false;
                    Bowls.RemoveAt(i);
                    if (BowlCompleted != null) BowlCompleted(bowl);
                }
            }

            // Çarpışma ve iç içe geçme engelleme (Repulsion)
            float collisionDist = 1.2f;
            for (int step = 0; step < 2; step++)
            {
                for (int i = 0; i < Bowls.Count; i++)
                {
                    for (int j = i + 1; j < Bowls.Count; j++)
                    {
                        float dist = Bowls[j].BeltX - Bowls[i].BeltX;
                        if (Math.Abs(dist) < collisionDist)
                        {
                            float overlap = collisionDist - Math.Abs(dist);
                            float push = overlap * 0.5f;
                            if (dist > 0)
                            {
                                Bowls[j].BeltX += push;
                                Bowls[i].BeltX -= push;
                            }
                            else
                            {
                                Bowls[j].BeltX -= push;
                                Bowls[i].BeltX += push;
                            }
                        }
                    }
                }
            }
        }
    }
}
