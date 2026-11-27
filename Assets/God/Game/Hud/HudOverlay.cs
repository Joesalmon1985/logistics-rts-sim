using System.Collections.Generic;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Translucent world markers of the HUD: soft discs on the legal spots, the hover ring, the placing ghost, the road
    /// preview. Two meshes: a static one (legal spots, rebuilt when the board or the chosen kind changes) and a dynamic one
    /// (rebuilt every frame, a few shapes). View only.
    /// </summary>
    public sealed class HudOverlay : MonoBehaviour
    {
        /// <summary>Per-vertex coloured geometry with alpha.</summary>
        public sealed class Soft
        {
            private readonly List<Vector3> _v = new List<Vector3>();
            private readonly List<Color> _c = new List<Color>();
            private readonly List<int> _t = new List<int>();
            public int VertexCount => _v.Count;

            public void Tri(Vector3 a, Color ca, Vector3 b, Color cb, Vector3 c, Color cc)
            {
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c); _c.Add(ca); _c.Add(cb); _c.Add(cc);
                _t.Add(i); _t.Add(i + 1); _t.Add(i + 2);
                // Both faces, so the shape shows from any side.
                _t.Add(i); _t.Add(i + 2); _t.Add(i + 1);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
            {
                Tri(a, col, b, col, c, col); Tri(a, col, c, col, d, col);
            }

            /// <summary>A flat disc: the centre colour fades to the edge colour (soft markers).</summary>
            public void Disc(Vector3 centre, float radius, Color inner, Color edge, int segs = 14)
            {
                for (int k = 0; k < segs; k++)
                {
                    float a0 = k * Mathf.PI * 2f / segs, a1 = (k + 1) * Mathf.PI * 2f / segs;
                    Tri(centre, inner,
                        centre + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius, edge,
                        centre + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius, edge);
                }
            }

            public void Ring(Vector3 centre, float radius, float width, Color col, int segs = 36)
            {
                float r0 = radius - width * 0.5f, r1 = radius + width * 0.5f;
                for (int k = 0; k < segs; k++)
                {
                    float a0 = k * Mathf.PI * 2f / segs, a1 = (k + 1) * Mathf.PI * 2f / segs;
                    var d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    Quad(centre + d0 * r0, centre + d0 * r1, centre + d1 * r1, centre + d1 * r0, col);
                }
            }

            public void Line(Vector3 a, Vector3 b, float w, Color col)
            {
                var d = b - a; d.y = 0;
                if (d.sqrMagnitude < 1e-8f) return;
                var side = Vector3.Cross(Vector3.up, d.normalized) * (w * 0.5f);
                Quad(a - side, b - side, b + side, a + side, col);
            }

            public void Box(Vector3 centre, Vector3 size, Color col)
            {
                var h = size * 0.5f;
                Vector3 P(float x, float y, float z) => centre + new Vector3(x * h.x, y * h.y, z * h.z);
                Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), col * 1.1f);
                Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), col * 0.85f);
                Quad(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1), col * 0.85f);
                Quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), col * 0.7f);
                Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), col * 0.7f);
            }

            public void Clear() { _v.Clear(); _c.Clear(); _t.Clear(); }

            public void ApplyTo(Mesh m)
            {
                m.Clear();
                if (_v.Count == 0) return;
                if (_v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(_v);
                m.SetColors(_c);
                m.SetTriangles(_t, 0);
                m.RecalculateBounds();
            }
        }

        private MeshFilter _staticF, _dynF;
        private Mesh _staticMesh, _dynMesh;
        private readonly Soft _static = new Soft();
        private readonly Soft _dyn = new Soft();

        public static Color Gold => new Color(1f, 0.86f, 0.45f);

        public static HudOverlay Create(Transform parent)
        {
            var go = new GameObject("HudOverlay");
            go.transform.SetParent(parent, false);
            var o = go.AddComponent<HudOverlay>();
            var shader = Shader.Find("Sprites/Default");
            Material mat = shader != null ? new Material(shader) : MaterialFactory.CreateUnlit(Color.white, "Hud overlay");
            mat.name = "Hud overlay";
            mat.renderQueue = 3100;
            o._staticMesh = new Mesh { name = "Hud spots" };
            o._dynMesh = new Mesh { name = "Hud dynamic" };
            o._staticF = o.MakeChild("Spots", mat, o._staticMesh);
            o._dynF = o.MakeChild("Dynamic", mat, o._dynMesh);
            return o;
        }

        private MeshFilter MakeChild(string name, Material mat, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var f = go.AddComponent<MeshFilter>(); f.sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return f;
        }

        /// <summary>The static layer (legal spots): clear, draw, then <see cref="CommitSpots"/>.</summary>
        public Soft Spots { get { return _static; } }
        public void CommitSpots() { _static.ApplyTo(_staticMesh); }

        /// <summary>The per-frame layer (hover, ghost, preview).</summary>
        public Soft Dynamic => _dyn;
        public void CommitDynamic() { _dyn.ApplyTo(_dynMesh); }

        public void ClearAll() { _static.Clear(); _static.ApplyTo(_staticMesh); _dyn.Clear(); _dyn.ApplyTo(_dynMesh); }

        /// <summary>Vertices in the dynamic layer last committed (for tests).</summary>
        public int DynamicVertices => _dynMesh.vertexCount;
        public int SpotVertices => _staticMesh.vertexCount;

        private void OnDestroy()
        {
            if (_staticMesh != null) Destroy(_staticMesh);
            if (_dynMesh != null) Destroy(_dynMesh);
        }
    }
}
