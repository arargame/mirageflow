using Microsoft.Xna.Framework;
using MirageFlowNew.Rendering;
using System;
using System.Collections.Generic;

namespace MirageFlowNew.Game
{
    /// <summary>
    /// Panelden kaselere uçan 3D kum taneleri. Havuzlanır (pool) — kum miktarı
    /// ne olursa olsun allocation sabit kalır.
    /// </summary>
    public class SandParticles3D
    {
        public struct Grain
        {
            public Vector3 Position;
            public Color Color;
            public Bowl3D Target;
            public float Speed;
            public int Seed;
            public bool Active;
        }

        private readonly Grain[] _pool;
        private int _activeCount;
        private readonly Random _rng = new Random();

        public int ActiveCount => _activeCount;

        public SandParticles3D(int capacity = 2048)
        {
            _pool = new Grain[capacity];
        }

        public bool Spawn(Vector3 position, Color color, Bowl3D target)
        {
            if (_activeCount >= _pool.Length) return false; // havuz dolu — bu kare atla
            _pool[_activeCount++] = new Grain
            {
                Position = position,
                Color = color,
                Target = target,
                Speed = 5.5f + (float)_rng.NextDouble() * 3f,
                Seed = _rng.Next(1000),
                Active = true,
            };
            return true;
        }

        public void Update(GameTime gameTime, float beltTopY, float beltZ)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float t = (float)gameTime.TotalGameTime.TotalSeconds;

            for (int i = _activeCount - 1; i >= 0; i--)
            {
                ref Grain g = ref _pool[i];

                if (g.Target == null || !g.Target.IsOnBelt)
                {
                    RemoveAt(i);
                    continue;
                }

                // hedef: kase ağzının merkezi
                var target = new Vector3(g.Target.BeltX, beltTopY + Scene3D.BowlInnerTopY + 0.15f, beltZ);
                Vector3 dir = target - g.Position;
                float dist = dir.Length();

                if (dist < 0.18f)
                {
                    g.Target.CurrentFill++;
                    RemoveAt(i);
                    continue;
                }

                dir /= dist;
                g.Position += dir * g.Speed * dt;

                // organik titreşim
                float jx = (float)Math.Sin(t * 17f + g.Seed) * 0.9f;
                float jz = (float)Math.Cos(t * 13f + g.Seed * 1.7f) * 0.9f;
                g.Position.X += jx * dt;
                g.Position.Z += jz * dt;
            }
        }

        /// <summary>Aktif taneleri küçük küpler olarak çizer.</summary>
        public void Draw(Scene3D scene)
        {
            var scale = Matrix.CreateScale(0.075f);
            for (int i = 0; i < _activeCount; i++)
            {
                scene.Draw(scene.Box, scale * Matrix.CreateTranslation(_pool[i].Position),
                    _pool[i].Color, null, 0.25f);
            }
        }

        public void Clear() => _activeCount = 0;

        private void RemoveAt(int index)
        {
            _pool[index] = _pool[_activeCount - 1];
            _activeCount--;
        }
    }
}
