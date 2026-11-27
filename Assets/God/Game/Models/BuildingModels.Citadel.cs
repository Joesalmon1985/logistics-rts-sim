using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>The Citadel model: a walled fortress with corner towers, a gate and a keep, in the faction's style (P10).</summary>
    public static partial class BuildingModels
    {
        /// <summary>Model kind of a Citadel (an upgraded HQ). Not a building kind of the sim.</summary>
        public const string CitadelKind = "citadel";

        private const float CitHalfX = 9f, CitHalfZ = 8f, CitWallH = 3.4f, CitWallT = 1.1f;

        private static void Citadel(MeshBuilder mb, ModelStyle st, BuildingRig rig)
        {
            // The platform it stands on.
            mb.Box(new Vector3(0, 0.15f, 0), new Vector3(2 * CitHalfX + 3.4f, 0.3f, 2 * CitHalfZ + 3.4f), st.Plinth);

            var c = new[] { new Vector3(-CitHalfX, 0, -CitHalfZ), new Vector3(CitHalfX, 0, -CitHalfZ), new Vector3(CitHalfX, 0, CitHalfZ), new Vector3(-CitHalfX, 0, CitHalfZ) };
            // Walls: back, left and right whole; the front has a gate gap.
            CitWall(mb, st, c[0], c[1]);
            CitWall(mb, st, c[3], c[0]);
            CitWall(mb, st, c[1], c[2]);
            CitWall(mb, st, c[3], new Vector3(-2.1f, 0, CitHalfZ));
            CitWall(mb, st, new Vector3(2.1f, 0, CitHalfZ), c[2]);
            // The gate: two posts, a lintel in the faction colour and shut doors.
            foreach (float x in new[] { -2.1f, 2.1f }) mb.Box(new Vector3(x, 2.2f, CitHalfZ), new Vector3(0.8f, 4.4f, 1.4f), st.Stone ? st.Wall * 1.1f : st.Frame);
            mb.Box(new Vector3(0, 4.1f, CitHalfZ), new Vector3(4.8f, 0.8f, 1.4f), st.Accent);
            mb.Box(new Vector3(0, 1.6f, CitHalfZ - 0.1f), new Vector3(3.5f, 3.2f, 0.3f), st.Door);
            mb.Box(new Vector3(0, 1.6f, CitHalfZ + 0.08f), new Vector3(0.18f, 3.2f, 0.12f), st.Frame);

            // Four corner towers.
            foreach (var p in c) CitTower(mb, st, p);

            // The keep, towards the back so the courtyard shows in front.
            var kp = new Vector3(0, 0, -2.2f);
            if (st.Stone)
            {
                mb.Box(kp + Vector3.up * 4.2f, new Vector3(7.4f, 8.4f, 7f), st.Wall);
                for (int k = 0; k < 3; k++) mb.Box(kp + Vector3.up * (1.6f + k * 2.4f), new Vector3(7.5f, 0.22f, 7.1f), st.Wall * 0.82f);
                mb.Box(kp + Vector3.up * 5.6f, new Vector3(7.6f, 0.7f, 7.2f), st.Accent);
                for (int i = -3; i <= 3; i++)
                {
                    mb.Box(kp + new Vector3(i * 1.05f, 8.9f, 3.45f), new Vector3(0.7f, 0.9f, 0.6f), st.Wall * 1.1f);
                    mb.Box(kp + new Vector3(i * 1.05f, 8.9f, -3.45f), new Vector3(0.7f, 0.9f, 0.6f), st.Wall * 1.1f);
                    if (i > -3 && i < 3)
                    {
                        mb.Box(kp + new Vector3(3.65f, 8.9f, i * 1.05f), new Vector3(0.6f, 0.9f, 0.7f), st.Wall * 1.1f);
                        mb.Box(kp + new Vector3(-3.65f, 8.9f, i * 1.05f), new Vector3(0.6f, 0.9f, 0.7f), st.Wall * 1.1f);
                    }
                }
                mb.Box(kp + new Vector3(0, 1.5f, 3.55f), new Vector3(1.4f, 3f, 0.2f), st.Door);
                foreach (float x in new[] { -2.2f, 2.2f }) mb.Box(kp + new Vector3(x, 6.8f, 3.55f), new Vector3(0.5f, 1.3f, 0.12f), ModelKit.Soot);
                Banner(mb, rig, kp + Vector3.up * 9.3f, 5.0f, st);
                rig.Height = 15.3f;
            }
            else
            {
                // A great timber hall with a tall watch tower behind it.
                ModelKit.House(mb, kp + new Vector3(0, 0, 0.8f), 10f, 7f, 4.6f, 4.4f, st, true, 1.8f);
                var tp = kp + new Vector3(0, 0, -2.6f);
                foreach (var o in new[] { new Vector3(-1.3f, 0, -1.3f), new Vector3(1.3f, 0, -1.3f), new Vector3(1.3f, 0, 1.3f), new Vector3(-1.3f, 0, 1.3f) })
                    mb.Cylinder(tp + o + Vector3.down * 0.4f, 0.26f, 0.22f, 9f, 6, st.Frame);
                mb.Box(tp + Vector3.up * 7.4f, new Vector3(3.6f, 2f, 3.6f), st.Wall);
                mb.Box(tp + Vector3.up * 6.4f, new Vector3(3.9f, 0.3f, 3.9f), st.Frame);
                mb.Box(tp + Vector3.up * 8.45f, new Vector3(4.0f, 0.5f, 4.0f), st.Accent);
                mb.Cone(tp + Vector3.up * 8.6f, 3.1f, 3.6f, 8, st.Roof);
                Banner(mb, rig, tp + Vector3.up * 12f, 4.6f, st);
                rig.Height = 17.0f;
            }
            ModelKit.Chimney(mb, kp + new Vector3(3.6f, 4.4f, 0f), 1.5f, 0.4f, st.Stone ? st.Wall * 0.9f : ModelKit.Rock);
            rig.Smoke.Add(kp + new Vector3(3.6f, 6.3f, 0f));
            rig.AlwaysSmoke = true;
            // Courtyard: a few crates and barrels by the gate.
            ModelKit.Crate(mb, new Vector3(-5.2f, 0.3f, 4.6f), 0.9f, 12f);
            ModelKit.Crate(mb, new Vector3(-4.2f, 0.3f, 5.1f), 0.8f, -20f);
            ModelKit.Barrel(mb, new Vector3(5.0f, 0.3f, 4.6f));
            ModelKit.Barrel(mb, new Vector3(5.8f, 0.3f, 3.9f));
            rig.Radius = 11f;
        }

        /// <summary>A wall run: a palisade of pointed logs (timber) or a battlemented stone wall, with a rail or band in the faction colour.</summary>
        private static void CitWall(MeshBuilder mb, ModelStyle st, Vector3 a, Vector3 b)
        {
            var d = b - a; float len = d.magnitude;
            var dir = d / len;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // box local Z along the wall
            if (st.Stone)
            {
                var mid = (a + b) * 0.5f;
                mb.Box(mid + Vector3.up * CitWallH * 0.5f, new Vector3(CitWallT, CitWallH, len), st.Wall, yaw);
                mb.Box(mid + Vector3.up * 2.9f, new Vector3(CitWallT + 0.12f, 0.45f, len), st.Accent, yaw);
                int n = Mathf.Max(2, Mathf.RoundToInt(len / 1.5f));
                for (int i = 0; i < n; i++)
                {
                    var p = a + dir * ((i + 0.5f) * len / n);
                    mb.Box(p + Vector3.up * (CitWallH + 0.35f), new Vector3(CitWallT + 0.1f, 0.7f, len / n * 0.55f), st.Wall * 1.1f, yaw);
                }
            }
            else
            {
                int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.8f));
                for (int i = 0; i < n; i++)
                {
                    var p = a + dir * ((i + 0.5f) * len / n);
                    float v = 0.88f + 0.12f * ((i * 7) % 5) / 4f;
                    float h = CitWallH + 0.4f + 0.25f * ((i * 3) % 4);
                    mb.Cylinder(p + Vector3.down * 0.2f, 0.4f, 0.36f, h, 6, st.Frame * v);
                    mb.Cone(p + Vector3.up * (h - 0.2f), 0.38f, 0.8f, 6, st.Frame * (v * 1.12f));
                }
                ModelKit.Beam(mb, a + Vector3.up * 2.4f, b + Vector3.up * 2.4f, 0.18f, st.Accent);
                ModelKit.Beam(mb, a + Vector3.up * 0.9f, b + Vector3.up * 0.9f, 0.14f, st.Frame * 0.8f);
            }
        }

        private static void CitTower(MeshBuilder mb, ModelStyle st, Vector3 p)
        {
            if (st.Stone)
            {
                mb.Cylinder(p + Vector3.down * 0.2f, 2.1f, 1.8f, 7.4f, 10, st.Wall);
                mb.Cylinder(p + Vector3.up * 5.0f, 1.95f, 1.95f, 0.55f, 10, st.Accent, false);
                mb.Cylinder(p + Vector3.up * 7.2f, 2.35f, 2.2f, 0.4f, 10, st.Plinth);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * 2f / 8f;
                    mb.Box(p + new Vector3(Mathf.Cos(a) * 2.1f, 7.9f, Mathf.Sin(a) * 2.1f), new Vector3(0.8f, 0.9f, 0.8f), st.Wall * 1.1f, -a * Mathf.Rad2Deg);
                }
                mb.Cone(p + Vector3.up * 7.6f, 1.9f, 3.3f, 10, st.Roof);
                mb.Box(p + new Vector3(0, 4.2f, 1.75f), new Vector3(0.3f, 0.9f, 0.14f), ModelKit.Soot);
            }
            else
            {
                mb.Cylinder(p + Vector3.down * 0.2f, 1.9f, 1.7f, 6.4f, 10, st.Frame);
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI * 2f / 10f;
                    mb.Cylinder(p + new Vector3(Mathf.Cos(a) * 1.7f, -0.2f, Mathf.Sin(a) * 1.7f), 0.3f, 0.26f, 6.6f, 5, st.Frame * (0.9f + 0.1f * (i & 1)));
                }
                mb.Cylinder(p + Vector3.up * 4.6f, 2.15f, 2.15f, 0.5f, 10, st.Accent, false);
                mb.Cylinder(p + Vector3.up * 6.4f, 2.4f, 2.2f, 0.4f, 10, st.Wall);
                mb.Cone(p + Vector3.up * 6.8f, 2.6f, 3.6f, 10, st.Roof);
            }
        }
    }
}
