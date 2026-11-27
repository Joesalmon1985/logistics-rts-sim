using System.Collections.Generic;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4a: one continuous ground surface for the island, sea included, sampled from <see cref="IslandField"/> on a
    /// regular grid and cut into chunk meshes (smooth shared normals, vertex colours). Beyond it a lit far sea fades to
    /// the haze, in the same deep colour where they meet so no edge shows.
    /// </summary>
    public sealed class IslandGroundMesh
    {
        public const float Step = 3f;
        public const int ChunkCells = 48;

        public readonly float Extent;
        public readonly int Cells;
        private readonly float[] _h;
        private readonly Color[] _c;
        private readonly IslandField _f;

        public IslandGroundMesh(IslandField f)
        {
            _f = f;
            Extent = BoardSpace.BoardRadius + 190f;
            Cells = Mathf.CeilToInt(Extent * 2f / Step);
            int n = Cells + 1;
            _h = new float[n * n]; _c = new Color[n * n];
            var scratch = new float[f.HexCount];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float x = X(i), z = X(j);
                    float y = f.Height(x, z, scratch);
                    _h[i * n + j] = y;
                    _c[i * n + j] = f.ColourAt(x, z, y, scratch);
                }
        }

        public float X(int i) => -Extent + i * Step;

        /// <summary>Grid height (m) at a cell corner.</summary>
        public float H(int i, int j) { int n = Cells + 1; i = Mathf.Clamp(i, 0, Cells); j = Mathf.Clamp(j, 0, Cells); return _h[i * n + j]; }

        /// <summary>Ground height interpolated from the mesh at a world point (what the eye sees).</summary>
        public float HeightAt(float x, float z)
        {
            float fx = (x + Extent) / Step, fz = (z + Extent) / Step;
            int i = Mathf.Clamp(Mathf.FloorToInt(fx), 0, Cells - 1), j = Mathf.Clamp(Mathf.FloorToInt(fz), 0, Cells - 1);
            float u = Mathf.Clamp01(fx - i), v = Mathf.Clamp01(fz - j);
            return Mathf.Lerp(Mathf.Lerp(H(i, j), H(i + 1, j), u), Mathf.Lerp(H(i, j + 1), H(i + 1, j + 1), u), v);
        }

        private Vector3 Normal(int i, int j)
        {
            float dx = (H(i + 1, j) - H(i - 1, j)) / (2f * Step);
            float dz = (H(i, j + 1) - H(i, j - 1)) / (2f * Step);
            var n = new Vector3(-dx, 1f, -dz);
            if (H(i, j) > IslandField.SeaY + 0.01f) n += _f.Bump(X(i), X(j), 1.6f);
            return n.normalized;
        }

        /// <summary>Builds the chunk objects under a parent.</summary>
        public void Build(Transform parent, Material mat)
        {
            int chunks = Mathf.CeilToInt(Cells / (float)ChunkCells);
            for (int ci = 0; ci < chunks; ci++)
                for (int cj = 0; cj < chunks; cj++)
                {
                    int i0 = ci * ChunkCells, j0 = cj * ChunkCells;
                    int i1 = Mathf.Min(Cells, i0 + ChunkCells), j1 = Mathf.Min(Cells, j0 + ChunkCells);
                    int w = i1 - i0 + 1, hgt = j1 - j0 + 1;
                    var verts = new Vector3[w * hgt]; var norms = new Vector3[w * hgt]; var cols = new Color[w * hgt];
                    int n = Cells + 1;
                    for (int a = 0; a < w; a++)
                        for (int b = 0; b < hgt; b++)
                        {
                            int i = i0 + a, j = j0 + b, k = a * hgt + b;
                            verts[k] = new Vector3(X(i), _h[i * n + j], X(j));
                            norms[k] = Normal(i, j);
                            cols[k] = _c[i * n + j];
                        }
                    var tris = new List<int>((w - 1) * (hgt - 1) * 6);
                    for (int a = 0; a < w - 1; a++)
                        for (int b = 0; b < hgt - 1; b++)
                        {
                            int k00 = a * hgt + b, k10 = (a + 1) * hgt + b, k01 = a * hgt + b + 1, k11 = (a + 1) * hgt + b + 1;
                            // Alternate the diagonal so no grid direction shows in the shading.
                            if (((i0 + a) + (j0 + b)) % 2 == 0) { tris.Add(k00); tris.Add(k01); tris.Add(k11); tris.Add(k00); tris.Add(k11); tris.Add(k10); }
                            else { tris.Add(k00); tris.Add(k01); tris.Add(k10); tris.Add(k10); tris.Add(k01); tris.Add(k11); }
                        }
                    var m = new Mesh { name = "Island ground " + ci + "," + cj };
                    m.vertices = verts; m.normals = norms; m.colors = cols; m.SetTriangles(tris, 0);
                    m.RecalculateBounds();
                    var go = new GameObject(m.name);
                    go.transform.SetParent(parent, false);
                    go.AddComponent<MeshFilter>().sharedMesh = m;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
        }

        /// <summary>The far sea: a lit ring from under the ground grid's edge to the horizon, deep blue fading to haze.</summary>
        public static Mesh FarSea(float inner, Color haze)
        {
            float[] radii = { inner, inner + 400f, 1500f, 2600f, 4000f, 9000f };
            var d = IslandField.Deep;
            Color[] cs = { d, d, Color.Lerp(d, haze, 0.25f), Color.Lerp(d, haze, 0.6f), haze, haze };
            const int seg = 96;
            var verts = new List<Vector3>(); var cols = new List<Color>(); var norms = new List<Vector3>(); var tris = new List<int>();
            for (int k = 0; k < radii.Length; k++)
                for (int i = 0; i < seg; i++)
                {
                    float a = i * Mathf.PI * 2f / seg;
                    verts.Add(new Vector3(Mathf.Cos(a) * radii[k], IslandField.SeaY - 0.5f, Mathf.Sin(a) * radii[k]));
                    cols.Add(cs[k]); norms.Add(Vector3.up);
                }
            for (int k = 0; k < radii.Length - 1; k++)
                for (int i = 0; i < seg; i++)
                {
                    int a = k * seg + i, b = k * seg + (i + 1) % seg, c = (k + 1) * seg + (i + 1) % seg, e = (k + 1) * seg + i;
                    // Clockwise seen from above, so the faces point up.
                    tris.Add(a); tris.Add(c); tris.Add(e); tris.Add(a); tris.Add(b); tris.Add(c);
                }
            var m = new Mesh { name = "Far sea" };
            m.SetVertices(verts); m.SetColors(cols); m.SetNormals(norms); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
