using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MirageFlowNew.Rendering
{
    /// <summary>Basit prosedürel mesh üretimi (kase için torna/lathe, kutu, quad, disk).</summary>
    public static class MeshFactory
    {
        public class Mesh
        {
            public VertexPositionNormalTexture[] Vertices;
            public short[] Indices;
            public int TriangleCount => Indices.Length / 3;
        }

        /// <summary>
        /// Profil noktalarını (radius, y) Y ekseni etrafında döndürerek kase benzeri
        /// dönel yüzey üretir. Normaller yumuşatılır.
        /// </summary>
        public static Mesh CreateLathe(Vector2[] profile, int segments)
        {
            var verts = new List<VertexPositionNormalTexture>();
            var indices = new List<short>();

            for (int p = 0; p < profile.Length; p++)
            {
                for (int s = 0; s <= segments; s++)
                {
                    float ang = MathHelper.TwoPi * s / segments;
                    float cos = (float)Math.Cos(ang);
                    float sin = (float)Math.Sin(ang);
                    var pos = new Vector3(profile[p].X * cos, profile[p].Y, profile[p].X * sin);
                    verts.Add(new VertexPositionNormalTexture(
                        pos, Vector3.Up,
                        new Vector2(s / (float)segments, p / (float)(profile.Length - 1))));
                }
            }

            int stride = segments + 1;
            for (int p = 0; p < profile.Length - 1; p++)
            {
                for (int s = 0; s < segments; s++)
                {
                    short a = (short)(p * stride + s);
                    short b = (short)(a + stride);
                    indices.Add(a); indices.Add(b); indices.Add((short)(a + 1));
                    indices.Add((short)(a + 1)); indices.Add(b); indices.Add((short)(b + 1));
                }
            }

            var mesh = new Mesh { Vertices = verts.ToArray(), Indices = indices.ToArray() };
            ComputeSmoothNormals(mesh);
            return mesh;
        }

        /// <summary>Merkezi orijinde, verilen boyutlarda kutu.</summary>
        public static Mesh CreateBox(float width, float height, float depth)
        {
            float hx = width * 0.5f, hy = height * 0.5f, hz = depth * 0.5f;
            var verts = new List<VertexPositionNormalTexture>();
            var indices = new List<short>();

            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                short i0 = (short)verts.Count;
                verts.Add(new VertexPositionNormalTexture(a, n, new Vector2(0, 0)));
                verts.Add(new VertexPositionNormalTexture(b, n, new Vector2(1, 0)));
                verts.Add(new VertexPositionNormalTexture(c, n, new Vector2(1, 1)));
                verts.Add(new VertexPositionNormalTexture(d, n, new Vector2(0, 1)));
                indices.Add(i0); indices.Add((short)(i0 + 1)); indices.Add((short)(i0 + 2));
                indices.Add(i0); indices.Add((short)(i0 + 2)); indices.Add((short)(i0 + 3));
            }

            var p000 = new Vector3(-hx, -hy, -hz); var p100 = new Vector3(hx, -hy, -hz);
            var p010 = new Vector3(-hx, hy, -hz);  var p110 = new Vector3(hx, hy, -hz);
            var p001 = new Vector3(-hx, -hy, hz);  var p101 = new Vector3(hx, -hy, hz);
            var p011 = new Vector3(-hx, hy, hz);   var p111 = new Vector3(hx, hy, hz);

            Face(p010, p110, p111, p011, Vector3.Up);        // üst
            Face(p001, p101, p100, p000, Vector3.Down);      // alt
            Face(p011, p111, p101, p001, Vector3.Backward);  // ön (+Z)
            Face(p110, p010, p000, p100, Vector3.Forward);   // arka (-Z)
            Face(p111, p110, p100, p101, Vector3.Right);
            Face(p010, p011, p001, p000, Vector3.Left);

            return new Mesh { Vertices = verts.ToArray(), Indices = indices.ToArray() };
        }

        /// <summary>+Z yönüne bakan dikey quad (panel/ekran için). Genişlik X, yükseklik Y.</summary>
        public static Mesh CreateVerticalQuad(float width, float height)
        {
            float hx = width * 0.5f;
            var verts = new[]
            {
                new VertexPositionNormalTexture(new Vector3(-hx, height, 0), Vector3.Backward, new Vector2(0, 0)),
                new VertexPositionNormalTexture(new Vector3( hx, height, 0), Vector3.Backward, new Vector2(1, 0)),
                new VertexPositionNormalTexture(new Vector3( hx, 0, 0), Vector3.Backward, new Vector2(1, 1)),
                new VertexPositionNormalTexture(new Vector3(-hx, 0, 0), Vector3.Backward, new Vector2(0, 1)),
            };
            return new Mesh { Vertices = verts, Indices = new short[] { 0, 1, 2, 0, 2, 3 } };
        }

        /// <summary>Yukarı bakan yatay quad (zemin için).</summary>
        public static Mesh CreateHorizontalQuad(float halfSize, float uvRepeat)
        {
            var verts = new[]
            {
                new VertexPositionNormalTexture(new Vector3(-halfSize, 0, -halfSize), Vector3.Up, new Vector2(0, 0)),
                new VertexPositionNormalTexture(new Vector3( halfSize, 0, -halfSize), Vector3.Up, new Vector2(uvRepeat, 0)),
                new VertexPositionNormalTexture(new Vector3( halfSize, 0,  halfSize), Vector3.Up, new Vector2(uvRepeat, uvRepeat)),
                new VertexPositionNormalTexture(new Vector3(-halfSize, 0,  halfSize), Vector3.Up, new Vector2(0, uvRepeat)),
            };
            return new Mesh { Vertices = verts, Indices = new short[] { 0, 1, 2, 0, 2, 3 } };
        }

        /// <summary>Yukarı bakan birim disk (kase içindeki kum yüzeyi; world matrisiyle ölçeklenir).</summary>
        public static Mesh CreateDisc(int segments)
        {
            var verts = new List<VertexPositionNormalTexture>
            {
                new VertexPositionNormalTexture(Vector3.Zero, Vector3.Up, new Vector2(0.5f, 0.5f))
            };
            for (int s = 0; s <= segments; s++)
            {
                float ang = MathHelper.TwoPi * s / segments;
                var p = new Vector3((float)Math.Cos(ang), 0, (float)Math.Sin(ang));
                verts.Add(new VertexPositionNormalTexture(p, Vector3.Up,
                    new Vector2(p.X * 0.5f + 0.5f, p.Z * 0.5f + 0.5f)));
            }
            var indices = new List<short>();
            for (int s = 1; s <= segments; s++)
            {
                indices.Add(0);
                indices.Add((short)(s + 1));
                indices.Add((short)s);
            }
            return new Mesh { Vertices = verts.ToArray(), Indices = indices.ToArray() };
        }

        private static void ComputeSmoothNormals(Mesh mesh)
        {
            var normals = new Vector3[mesh.Vertices.Length];
            for (int i = 0; i < mesh.Indices.Length; i += 3)
            {
                int a = mesh.Indices[i], b = mesh.Indices[i + 1], c = mesh.Indices[i + 2];
                Vector3 n = Vector3.Cross(
                    mesh.Vertices[b].Position - mesh.Vertices[a].Position,
                    mesh.Vertices[c].Position - mesh.Vertices[a].Position);
                if (n.LengthSquared() > 1e-8f) n.Normalize();
                normals[a] += n; normals[b] += n; normals[c] += n;
            }
            for (int i = 0; i < normals.Length; i++)
            {
                var n = normals[i];
                if (n.LengthSquared() > 1e-8f) n.Normalize(); else n = Vector3.Up;
                var v = mesh.Vertices[i];
                mesh.Vertices[i] = new VertexPositionNormalTexture(v.Position, n, v.TextureCoordinate);
            }
        }
    }
}
