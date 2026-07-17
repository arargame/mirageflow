using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MirageFlowNew.Rendering;

namespace MirageFlowNew.Game
{
    /// <summary>Banttaki / envanterdeki 3D kase. Renk eşleşen kum taneleriyle dolar.</summary>
    public class Bowl3D
    {
        public Color TargetColor;
        public int Capacity;
        public int CurrentFill;
        public bool IsOnBelt;
        public bool IsActive = true;      // envanterde tıklanabilir mi (aynı rengin sırası)
        public Texture2D Icon;            // HUD ikonu (offscreen render)

        /// <summary>Bant üzerindeki X konumu (dünya birimi).</summary>
        public float BeltX;

        public bool IsFull => CurrentFill >= Capacity;
        public float FillRatio => Capacity <= 0 ? 0f : MathHelper.Clamp((float)CurrentFill / Capacity, 0f, 1f);

        /// <summary>Kase tabanının dünya konumu (bant üstü).</summary>
        public Vector3 WorldPosition(float beltTopY, float beltZ)
        {
            return new Vector3(BeltX, beltTopY, beltZ);
        }

        /// <summary>Kum yüzeyi diskinin lokal yüksekliği ve yarıçapı.</summary>
        public void GetFillSurface(out float y, out float radius)
        {
            float r = FillRatio;
            y = MathHelper.Lerp(Scene3D.BowlInnerBottomY + 0.01f, Scene3D.BowlInnerTopY - 0.02f, r);
            radius = MathHelper.Lerp(Scene3D.BowlInnerBottomR, Scene3D.BowlInnerTopR - 0.03f, r);
        }
    }
}
