using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MirageFlowNew.Rendering
{
    /// <summary>
    /// 3D çizim çekirdeği: tüm mesh'ler bir kez üretilir, taşınan her nesne yalnızca
    /// world matrisi + renk ile çizilir. Kase ikonları için offscreen render desteği vardır.
    /// </summary>
    public class Scene3D : IDisposable
    {
        public GraphicsDevice Device { get; private set; }
        public IsoCamera Camera { get; private set; }

        private BasicEffect _effect;
        private Texture2D _white;

        // Paylaşılan mesh'ler
        public MeshBuffers Bowl { get; private set; }
        public MeshBuffers FillDisc { get; private set; }
        public MeshBuffers Box { get; private set; }
        public MeshBuffers Panel { get; private set; }
        public MeshBuffers Ground { get; private set; }

        // Kase profil ölçüleri (dolum diski konumu için dışarıya açık)
        public const float BowlHeight = 0.55f;
        public const float BowlRimRadius = 0.60f;
        public const float BowlInnerBottomY = 0.14f;
        public const float BowlInnerTopY = 0.44f;
        public const float BowlInnerBottomR = 0.22f;
        public const float BowlInnerTopR = 0.48f;

        public class MeshBuffers
        {
            public VertexBuffer Vb;
            public IndexBuffer Ib;
            public int TriangleCount;
        }

        public Scene3D(GraphicsDevice device)
        {
            Device = device;
            Camera = new IsoCamera();

            _white = new Texture2D(device, 1, 1);
            _white.SetData(new[] { Color.White });

            _effect = new BasicEffect(device)
            {
                TextureEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
            };
            _effect.EnableDefaultLighting();
            _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.45f, -1f, -0.5f));
            _effect.DirectionalLight0.DiffuseColor = new Vector3(0.95f, 0.93f, 0.88f);
            _effect.AmbientLightColor = new Vector3(0.42f, 0.42f, 0.45f);
            _effect.SpecularColor = new Vector3(0.35f);
            _effect.SpecularPower = 24f;

            Bowl = Upload(MeshFactory.CreateLathe(BowlProfile(), 28));
            FillDisc = Upload(MeshFactory.CreateDisc(24));
            Box = Upload(MeshFactory.CreateBox(1f, 1f, 1f));
            // Birim panel; world matrisiyle level görüntüsünün oranına ölçeklenir.
            Panel = Upload(MeshFactory.CreateVerticalQuad(1f, 1f));
            Ground = Upload(MeshFactory.CreateHorizontalQuad(60f, 12f));
        }

        private static Vector2[] BowlProfile()
        {
            return new[]
            {
                new Vector2(0.00f, 0.02f),
                new Vector2(0.30f, 0.00f),                 // taban
                new Vector2(0.44f, 0.06f),
                new Vector2(0.55f, 0.22f),                 // dış duvar
                new Vector2(BowlRimRadius, 0.46f),
                new Vector2(BowlRimRadius, BowlHeight),    // dış ağız
                new Vector2(0.52f, BowlHeight),            // ağız kalınlığı
                new Vector2(BowlInnerTopR, BowlInnerTopY), // iç duvar
                new Vector2(0.34f, 0.20f),
                new Vector2(BowlInnerBottomR, BowlInnerBottomY),
                new Vector2(0.00f, BowlInnerBottomY),      // iç taban
            };
        }

        private MeshBuffers Upload(MeshFactory.Mesh mesh)
        {
            var vb = new VertexBuffer(Device, VertexPositionNormalTexture.VertexDeclaration,
                mesh.Vertices.Length, BufferUsage.WriteOnly);
            vb.SetData(mesh.Vertices);
            var ib = new IndexBuffer(Device, IndexElementSize.SixteenBits,
                mesh.Indices.Length, BufferUsage.WriteOnly);
            ib.SetData(mesh.Indices);
            return new MeshBuffers { Vb = vb, Ib = ib, TriangleCount = mesh.TriangleCount };
        }

        /// <summary>Her karenin başında 3D state'i kurar.</summary>
        public void BeginFrame(Matrix view, Matrix projection)
        {
            Device.DepthStencilState = DepthStencilState.Default;
            Device.RasterizerState = RasterizerState.CullNone;
            Device.BlendState = BlendState.Opaque;
            Device.SamplerStates[0] = SamplerState.LinearClamp;
            _effect.View = view;
            _effect.Projection = projection;
        }

        public void Draw(MeshBuffers mesh, Matrix world, Color color,
            Texture2D texture = null, float emissive = 0f, float alpha = 1f)
        {
            _effect.World = world;
            _effect.Texture = texture ?? _white;
            _effect.DiffuseColor = color.ToVector3();
            _effect.EmissiveColor = color.ToVector3() * emissive;
            _effect.Alpha = alpha;

            Device.SetVertexBuffer(mesh.Vb);
            Device.Indices = mesh.Ib;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.TriangleCount);
            }
        }

        /// <summary>
        /// Verilen renkte kasenin 3D görüntüsünü offscreen render edip HUD ikonu üretir.
        /// </summary>
        public Texture2D RenderBowlIcon(Color color, int size = 96)
        {
            var rt = new RenderTarget2D(Device, size, size, false,
                SurfaceFormat.Color, DepthFormat.Depth24);
            var prevTargets = Device.GetRenderTargets();

            Device.SetRenderTarget(rt);
            Device.Clear(Color.Transparent);

            var view = Matrix.CreateLookAt(new Vector3(0.9f, 1.15f, 1.5f),
                new Vector3(0f, 0.22f, 0f), Vector3.Up);
            var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(32f), 1f, 0.1f, 10f);

            BeginFrame(view, proj);
            Draw(Bowl, Matrix.Identity, color);

            Device.SetRenderTargets(prevTargets);
            return rt;
        }

        public void Dispose()
        {
            _effect.Dispose();
            _white.Dispose();
            foreach (var m in new[] { Bowl, FillDisc, Box, Panel, Ground })
            {
                m.Vb.Dispose();
                m.Ib.Dispose();
            }
        }
    }
}
