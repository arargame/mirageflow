using Microsoft.Xna.Framework;

namespace MirageFlowNew.Rendering
{
    /// <summary>
    /// Sabit izometrik açılı perspektif kamera. Sahne merkezine önden-üstten bakar.
    /// İstenirse Nudge ile hafifçe kaydırılabilir (şimdilik sabit).
    /// </summary>
    public class IsoCamera
    {
        public Vector3 Target = new Vector3(0f, 3.2f, -0.5f);
        public Vector3 Position = new Vector3(0f, 7.8f, 14.5f);
        public float FieldOfView = MathHelper.ToRadians(42f);

        public Matrix View => Matrix.CreateLookAt(Position, Target, Vector3.Up);

        public Matrix GetProjection(float aspectRatio)
        {
            return Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, 0.5f, 200f);
        }
    }
}
