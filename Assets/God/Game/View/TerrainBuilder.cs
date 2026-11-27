using UnityEngine;
using System.Collections.Generic;
using God.Sim;
using Terrain = God.Sim.Terrain;

namespace God.Game
{
    /// <summary>
    /// Small shared helpers of the island view: terrain colours, the disc and ring shapes, trees and sheep, and the
    /// hex-edge margin test the dressing uses to keep props off the hex borders. (The live tile builder of the
    /// WorldMVP game, with its walk grid and camp clearings, is not part of this project; P4 builds the live island on
    /// <see cref="HexDressingView"/> and <see cref="IslandGround"/>.)
    /// </summary>
    public static class TerrainBuilder
    {
        /// <summary>Props stay this far (m) from the hex-edge lines.</summary>
        public const float RoadMargin = 4f;
        /// <summary>Mountain crags: distance from the hex centre towards each hex-edge midpoint, and base radius.</summary>
        public const float PeakDistance = 41f, PeakRadius = 6.5f;

        public static Color TerrainColor(Terrain t)
        {
            switch (t)
            {
                case Terrain.Forest: return new Color(0.27f, 0.45f, 0.20f);
                case Terrain.Hills: return new Color(0.55f, 0.55f, 0.32f);
                case Terrain.Fields: return new Color(0.70f, 0.62f, 0.30f);
                case Terrain.Mountain: return new Color(0.48f, 0.46f, 0.44f);
                case Terrain.Pasture: return new Color(0.34f, 0.60f, 0.27f);
                case Terrain.Desert: return new Color(0.80f, 0.72f, 0.48f);
                default: return new Color(0.50f, 0.62f, 0.30f);
            }
        }

        /// <summary>The prop mesh object of a hex. The standalone dressing view keeps its own props, so there is none here.</summary>
        public static GameObject HexProps(int hex) => null;

        public static void Disc(MeshBuilder mb, Vector3 c, float r, int seg, Color col)
        {
            var ring = new List<Vector3>(seg);
            for (int k = 0; k < seg; k++)
            {
                float a = k * Mathf.PI * 2f / seg;
                ring.Add(c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            mb.FanUp(c, ring, col);
        }

        public static void Ring(MeshBuilder mb, Vector3 c, float r, float w, int seg, Color col)
        {
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
                Vector3 D(float a) => new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                mb.Quad(c + D(a0) * (r - w / 2), c + D(a1) * (r - w / 2), c + D(a1) * (r + w / 2), c + D(a0) * (r + w / 2), col);
            }
        }

        public static void Sheep(MeshBuilder mb, Vector3 p, float yaw, PropRandom rnd)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            var wool = new Color(1f, 0.99f, 0.94f) * rnd.Range(0.95f, 1.02f); wool.a = 1;
            var dark = new Color(0.16f, 0.14f, 0.13f);
            mb.Box(p + q * new Vector3(0, 0.75f, 0), new Vector3(0.8f, 0.65f, 1.25f), wool, yaw);
            mb.Box(p + q * new Vector3(0, 0.95f, 0.72f), new Vector3(0.36f, 0.4f, 0.42f), dark, yaw);
            foreach (float x in new[] { -0.25f, 0.25f }) foreach (float z in new[] { -0.4f, 0.4f })
                mb.Box(p + q * new Vector3(x, 0.22f, z), new Vector3(0.12f, 0.44f, 0.12f), dark, yaw);
        }

        public static void Tree(MeshBuilder mb, Vector3 p, PropRandom rnd, float scale = 1f)
        {
            float s = rnd.Range(1.0f, 1.6f) * scale;
            mb.Cylinder(p, 0.25f * s, 0.18f * s, 2.2f * s, 6, new Color(0.38f, 0.26f, 0.16f));
            var g = new Color(0.16f, 0.36f, 0.16f) * rnd.Range(0.85f, 1.15f);
            mb.Cone(p + Vector3.up * 1.6f * s, 2.0f * s, 3.4f * s, 7, g);
            mb.Cone(p + Vector3.up * 3.3f * s, 1.5f * s, 3.0f * s, 7, g * 1.1f);
        }

        /// <summary>Distance from a point (relative to the hex centre) to the border of a pointy-top hex of circumradius r (negative outside).</summary>
        public static float EdgeMargin(Vector3 d, float r)
        {
            float ax = Mathf.Abs(d.x), az = Mathf.Abs(d.z), k = r * 0.8660254f;
            return k - Mathf.Max(ax, ax * 0.5f + az * 0.8660254f);
        }

        /// <summary>True when a point lies on a board hex or within margin (m) of one (checked at six points round it).</summary>
        public static bool OnBoard(BoardTopology t, Vector3 p, float margin)
        {
            if (t.HexAt(p.x, p.z) >= 0) return true;
            for (int k = 0; k < 6; k++)
            {
                var q = p + BoardSpace.DirOf(k * 60f) * margin;
                if (t.HexAt(q.x, q.z) >= 0) return true;
            }
            return false;
        }
    }
}
