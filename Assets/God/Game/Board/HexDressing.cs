using System;
using System.Collections.Generic;
using UnityEngine;
using God.Sim;
using Terrain = God.Sim.Terrain;

namespace God.Game
{
    /// <summary>
    /// Abundance dressing of the resource hexes (god board plan AB-2, AB-5): every hex shows its pip value (2/12 = 1 ...
    /// 6/8 = 5, <see cref="BoardGenerator.Pips"/>) as five clearly different levels of the same terrain: Forest tree
    /// count and height, Hills clay cliff size, Fields extent, Pasture flock size, Mountain height. It is built at load
    /// from the board that is dealt, never baked into a scene, and shows no number, die or odds (AB-1). Original
    /// primitive shapes only, merged into one mesh per hex through <see cref="MeshBuilder"/> (vertex colours, one shared
    /// material). Pure view code: it reads a <see cref="Board"/> and never writes state.
    /// </summary>
    public static class HexDressing
    {
        /// <summary>Number of abundance levels (one per pip value 1..5).</summary>
        public const int Levels = 5;
        /// <summary>Radius kept free at the hex centre for the stock patch and its token (as the live stock patch).</summary>
        public const float CentreClear = 8.5f;
        /// <summary>Props keep this far from the hex edges (main roads), like <see cref="TerrainBuilder.RoadMargin"/>.</summary>
        public const float EdgeClear = 4f;

        /// <summary>Placement test: (world point, margin to the hex edge, clear radius) to true when a prop may stand there.</summary>
        public delegate bool PlaceOk(Vector3 p, float edgeMargin, float clearRadius);

        /// <summary>Pips of a hex (1..5), 0 for Meadow or a hex without a number.</summary>
        public static int PipsOf(Board board, int hex) => BoardGenerator.Pips(board.HexNumber[hex]);

        /// <summary>Level index 0..4 for a pip value (clamped), or -1 when there are no pips.</summary>
        public static int Level(int pips) => pips <= 0 ? -1 : Mathf.Clamp(pips, 1, Levels) - 1;

        // ---- Per-level tables (index = level 0..4). Chosen so that neighbouring levels differ at a glance from the god camera. ----
        public static readonly int[] TreeCount = { 9, 20, 36, 58, 88 };
        public static readonly float[] TreeScale = { 0.8f, 1.0f, 1.25f, 1.55f, 1.9f };
        public static readonly float[] CliffRadius = { 5f, 7.5f, 10f, 13f, 17f };
        public static readonly float[] CliffHeight = { 3f, 5.5f, 8f, 11.5f, 16f };
        public static readonly int[] CliffMounds = { 0, 1, 2, 4, 6 };
        public static readonly float[] FieldHalfExtent = { 12f, 19f, 27f, 35f, 44f };
        public static readonly int[] FlockSize = { 3, 6, 10, 15, 22 };
        public static readonly float[] PeakHeights = { 13f, 21f, 30f, 41f, 55f };
        public static readonly float[] SnowFraction = { 0f, 0.14f, 0.24f, 0.34f, 0.44f };

        // ---- Package W (owner brief 21 Nov): the centrepiece round each hex centre, sized by richness. ----
        /// <summary>Outer radius (m) of the centrepiece: crop patch, fold, clay pit, quarry crown. Read from the god camera at 1080p and on a phone.</summary>
        public static readonly float[] CentreRadius = { 15f, 20f, 26f, 32f, 38f };
        public static readonly int[] StookCount = { 1, 3, 5, 8, 12 };
        public static readonly int[] KilnCount = { 1, 1, 2, 3, 4 };
        public static readonly int[] BrickStacks = { 1, 2, 4, 6, 9 };
        public static readonly int[] CrownPeaks = { 3, 4, 5, 6, 7 };
        public static readonly float[] CrownPeakRadius = { 5.5f, 7f, 8.5f, 10f, 11.5f };
        public static readonly int[] QuarryBlocks = { 2, 3, 5, 7, 9 };

        public static readonly Color CropA = new Color(0.96f, 0.80f, 0.24f), CropB = new Color(0.86f, 0.62f, 0.16f), Stook = new Color(0.98f, 0.86f, 0.46f);
        public static readonly Color Brick = new Color(0.72f, 0.30f, 0.18f), ClayLight = new Color(0.84f, 0.56f, 0.34f), ClayDeep = new Color(0.56f, 0.26f, 0.15f);
        public static readonly Color QuarryStone = new Color(0.80f, 0.78f, 0.72f), WallStone = new Color(0.66f, 0.64f, 0.58f), HedgeGreen = new Color(0.20f, 0.36f, 0.15f), FoldGrass = new Color(0.56f, 0.80f, 0.30f);

        /// <summary>Ring of segments round c at radius r; each segment's midpoint must pass ok. Returns the drawn segment midpoints.</summary>
        private static void RingSegments(Vector3 c, float r, int n, PlaceOk ok, float clear, System.Action<Vector3, Vector3> draw)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r;
                var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r;
                if (!ok((p0 + p1) * 0.5f, EdgeClear, clear)) continue;
                draw(p0, p1);
            }
        }

        /// <summary>A low hedge round a crop patch (dark green, about 1.4 m wide and high).</summary>
        public static void Hedge(MeshBuilder mb, Vector3 c, float r, PlaceOk ok, PropRandom rnd)
        {
            int n = Mathf.Max(16, Mathf.RoundToInt(r * 1.2f));
            RingSegments(c, r, n, ok, 1f, (a, b) =>
            {
                var mid = (a + b) * 0.5f; var d = b - a;
                mb.Box(mid + Vector3.up * 0.8f, new Vector3(1.6f, 1.6f * rnd.Range(0.9f, 1.15f), d.magnitude * 1.05f), HedgeGreen * rnd.Range(0.9f, 1.1f), BoardSpace.YawOf(d));
            });
        }

        /// <summary>A dry-stone fold wall with a gap (the gate) towards the south-east.</summary>
        public static void Wall(MeshBuilder mb, Vector3 c, float r, PlaceOk ok, PropRandom rnd)
        {
            int n = Mathf.Max(18, Mathf.RoundToInt(r * 1.3f));
            int k = 0;
            RingSegments(c, r, n, ok, 1f, (a, b) =>
            {
                if (k++ == n - 3) return; // the gate
                var mid = (a + b) * 0.5f; var d = b - a;
                mb.Box(mid + Vector3.up * 0.65f, new Vector3(1.3f, 1.3f, d.magnitude * 1.04f), WallStone * rnd.Range(0.9f, 1.08f), BoardSpace.YawOf(d));
            });
        }

        /// <summary>A terraced clay pit round c out to radius r: rings step down towards the centre, pale ochre to deep red. Returns the rings drawn.</summary>
        public static int ClayPit(MeshBuilder mb, Vector3 c, float r, PlaceOk ok, PropRandom rnd)
        {
            int drawn = 0;
            const int steps = 4;
            for (int s = 0; s < steps; s++)
            {
                float r0 = Mathf.Lerp(r, CentreClear + 0.5f, s / (float)steps), r1 = Mathf.Lerp(r, CentreClear + 0.5f, (s + 1) / (float)steps);
                float y = 1.4f - s * 0.3f; // the outer lip stands highest: from an angle it reads as a dug bowl
                var col = Color.Lerp(ClayLight, ClayDeep, s / (float)(steps - 1));
                int n = Mathf.Max(18, Mathf.RoundToInt(r0 * 1.1f));
                for (int i = 0; i < n; i++)
                {
                    float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                    var o0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var o1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    if (!ok(c + (o0 + o1) * 0.5f * ((r0 + r1) * 0.5f), EdgeClear, 0.5f)) continue;
                    var cc = col * rnd.Range(0.95f, 1.05f); cc.a = 1f;
                    var up = Vector3.up * y;
                    // Flat tread (faces up: clockwise from above is outer0, outer1, inner1, inner0 seen this way round).
                    QuadUp(mb, c + up + o0 * r1, c + up + o0 * r0, c + up + o1 * r0, c + up + o1 * r1, cc);
                    // Riser down to the next tread, facing the centre.
                    var down = Vector3.up * (y - 0.3f);
                    Quad2(mb, c + down + o0 * r1, c + up + o0 * r1, c + up + o1 * r1, c + down + o1 * r1, cc * 0.8f);
                    if (s == 0) Quad2(mb, c + o0 * r0, c + o1 * r0, c + up + o1 * r0, c + up + o0 * r0, ClayLight * 0.85f); // outer bank
                }
                drawn++;
            }
            return drawn;
        }

        /// <summary>A brick kiln: a round clay body with a dark mouth and a short chimney.</summary>
        public static void Kiln(MeshBuilder mb, Vector3 p, float r, PropRandom rnd)
        {
            mb.Cylinder(p, r, r * 0.8f, r * 0.9f, 10, Brick * rnd.Range(0.9f, 1.05f));
            mb.Cylinder(p + Vector3.up * r * 0.9f, r * 0.8f, r * 0.35f, r * 0.6f, 10, Brick * 0.85f);
            mb.Cylinder(p + Vector3.up * r * 1.5f, r * 0.3f, r * 0.26f, r * 0.9f, 8, new Color(0.35f, 0.24f, 0.18f));
            mb.Cylinder(p + Vector3.up * r * 2.4f, r * 0.22f, r * 0.22f, 0.15f, 8, new Color(0.08f, 0.07f, 0.06f));
        }

        /// <summary>The Mountain centrepiece: a crown of snow peaks round a pale quarry floor with cut blocks. Returns the items placed.</summary>
        public static int QuarryCrown(MeshBuilder mb, Vector3 c, int level, PlaceOk ok, PropRandom rnd)
        {
            int placed = 0;
            float pr = CrownPeakRadius[level];
            float ring = Mathf.Max(CentreClear + pr + 1f, CentreRadius[level] - pr * 0.6f);
            // Quarry floor: a pale stone tread at the centre, cut blocks on it.
            ClayPitLike(mb, c, CentreClear + 4f + level * 1.2f, ok, rnd);
            for (int i = 0, n = 0; i < 40 && n < QuarryBlocks[level]; i++)
            {
                var p = c + BoardSpace.DirOf(rnd.Range(0f, 360f)) * rnd.Range(CentreClear + 0.5f, CentreClear + 4f + level * 1.2f);
                if (!ok(p, EdgeClear, 1.2f)) continue;
                mb.Box(p + Vector3.up * 0.9f, new Vector3(2.4f, 1.8f, 1.6f), QuarryStone * rnd.Range(0.92f, 1.06f), rnd.Range(0f, 90f));
                placed++; n++;
            }
            int count = CrownPeaks[level];
            float a0 = rnd.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float a = a0 + i * 360f / count + rnd.Range(-12f, 12f);
                var p = c + BoardSpace.DirOf(a) * (ring + rnd.Range(-1.5f, 1.5f));
                float r = pr * rnd.Range(0.85f, 1.1f);
                if (!ok(p, EdgeClear + r * 0.6f, r)) continue;
                float height = PeakHeight(level, rnd.Next()) * rnd.Range(0.9f, 1.15f);
                Peak(mb, p, r, height, level, rnd);
                placed++;
            }
            return placed;
        }

        /// <summary>A flat quad wound to face up (MeshBuilder faces are clockwise from the front).</summary>
        public static void QuadUp(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
        {
            if (Vector3.Cross(b - a, c - a).y > 0f) mb.Quad(a, b, c, d, col); else mb.Quad(a, d, c, b, col);
        }

        /// <summary>A quad drawn from both sides (risers and banks seen from any angle).</summary>
        public static void Quad2(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col) { mb.Quad(a, b, c, d, col); mb.Quad(a, d, c, b, col); }

        /// <summary>A flat pale-stone floor (one tread) of radius r round c.</summary>
        private static void ClayPitLike(MeshBuilder mb, Vector3 c, float r, PlaceOk ok, PropRandom rnd)
        {
            int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                var o0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var o1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                if (!ok(c + (o0 + o1) * 0.5f * (r + CentreClear) * 0.5f, EdgeClear, 0.5f)) continue;
                var up = Vector3.up * 0.35f;
                var col = QuarryStone * rnd.Range(0.9f, 1.0f); col.a = 1f;
                QuadUp(mb, c + up + o0 * CentreClear, c + up + o0 * r, c + up + o1 * r, c + up + o1 * CentreClear, col);
            }
        }

        /// <summary>Height (m) of one Mountain crag at a level; <paramref name="variation"/> 0..1 gives the crags a slight spread.</summary>
        public static float PeakHeight(int level, float variation) => PeakHeights[level] * Mathf.Lerp(0.85f, 1.05f, variation);

        /// <summary>Draws one crag (rock body and snow cap) at the given level; used by the preview and the terrain builder.</summary>
        public static void Peak(MeshBuilder mb, Vector3 p, float radius, float height, int level, PropRandom rnd)
        {
            float snow = SnowFraction[level];
            float body = height * (1f - snow);
            var rock = new Color(0.47f, 0.45f, 0.43f) * rnd.Range(0.9f, 1.1f);
            mb.Cylinder(p, radius, radius * 0.24f, body, 9, rock, false);
            if (snow > 0f) mb.Cone(p + Vector3.up * body, radius * 0.24f + (1f - snow) * 0.5f, height * snow, 9, new Color(0.95f, 0.95f, 0.97f));
            else mb.Cone(p + Vector3.up * body, radius * 0.24f, 0.01f, 9, rock);
        }

        /// <summary>
        /// Adds the dressing of resource hex <paramref name="h"/> to <paramref name="mb"/> (world coordinates). Meadow and
        /// numberless hexes add nothing. Mountain crags are added only when <paramref name="withPeaks"/> (the terrain
        /// builder keeps its own crag colliders and passes false). Returns the number of main items placed (trees, cliff
        /// mounds, crop rows, sheep, crags) for tests.
        /// </summary>
        public static int Fill(MeshBuilder mb, Board board, int h, PlaceOk ok, bool withPeaks)
        {
            var terrain = board.HexTerrain[h];
            int level = Level(PipsOf(board, h));
            if (terrain == Terrain.Meadow || level < 0) return 0;
            var t = board.Topology;
            var centre = BoardSpace.Hex(t, h);
            float R = BoardSpace.HexSize;
            var rnd = new PropRandom(9000 + h * 7919 + level * 131);
            Vector3 RandomInHex()
            {
                for (int i = 0; i < 24; i++)
                {
                    var p = centre + new Vector3(rnd.Range(-R, R), 0, rnd.Range(-R, R));
                    if (TerrainBuilder.EdgeMargin(p - centre, R) >= EdgeClear) return p;
                }
                return centre;
            }
            int placed = 0;
            switch (terrain)
            {
                case Terrain.Forest:
                {
                    float sc = TreeScale[level];
                    int want = TreeCount[level];
                    for (int i = 0; i < want * 6 && placed < want; i++)
                    {
                        var p = RandomInHex();
                        if (!ok(p, EdgeClear, 2f * sc)) continue;
                        TerrainBuilder.Tree(mb, p, rnd, sc * rnd.Range(0.9f, 1.15f));
                        placed++;
                    }
                    for (int i = 0; i < 14; i++)
                    {
                        var p = RandomInHex();
                        if (!ok(p, EdgeClear, 1f)) continue;
                        mb.Rock(p, rnd.Range(0.7f, 1.4f), new Color(0.22f, 0.38f, 0.18f), rnd.Next);
                    }
                    break;
                }
                case Terrain.Hills:
                {
                    // Centrepiece: a terraced clay pit round the hex centre (its size is the richness), kilns and brick stacks on its rim.
                    float pr = CentreRadius[level];
                    placed += ClayPit(mb, centre, pr, ok, rnd);
                    for (int k = 0; k < KilnCount[level]; k++)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            var p = centre + BoardSpace.DirOf(rnd.Range(0f, 360f)) * (pr + 4.5f);
                            if (!ok(p, EdgeClear + 4f, 4f)) continue;
                            Kiln(mb, p, 3.2f + level * 0.25f, rnd);
                            placed++;
                            break;
                        }
                    }
                    for (int k = 0; k < BrickStacks[level]; k++)
                    {
                        var p = centre + BoardSpace.DirOf(rnd.Range(0f, 360f)) * (pr + rnd.Range(2f, 9f));
                        if (!ok(p, EdgeClear + 2f, 2f)) continue;
                        float yaw = rnd.Range(0f, 90f);
                        mb.Box(p + Vector3.up * 0.8f, new Vector3(3.2f, 1.6f, 2.2f), Brick * rnd.Range(0.92f, 1.08f), yaw);
                        mb.Box(p + Vector3.up * 1.9f, new Vector3(2.4f, 0.6f, 1.6f), Brick * 1.1f, yaw);
                        placed++;
                    }
                    for (int m = 0; m < CliffMounds[level]; m++)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            var p = RandomInHex();
                            float s = rnd.Range(3f, 5.5f);
                            if ((p - centre).magnitude < pr + 8f || !ok(p, s * 1.1f + 2f, s)) continue;
                            mb.Cylinder(p, s * 1.1f, s * 0.45f, s * 0.5f, 10, new Color(0.62f, 0.42f, 0.26f) * rnd.Range(0.95f, 1.08f), true);
                            break;
                        }
                    }
                    for (int i = 0; i < 22; i++)
                    {
                        var p = RandomInHex();
                        if ((p - centre).magnitude < pr + 2f || !ok(p, EdgeClear, 1f)) continue;
                        mb.Rock(p, rnd.Range(0.4f, 1.3f), new Color(0.58f, 0.52f, 0.44f), rnd.Next);
                    }
                    break;
                }
                case Terrain.Fields:
                {
                    // Centrepiece: a round patch of ripe crop strips round the hex centre inside a hedge; its radius is the
                    // richness (FieldHalfExtent keeps the old per-level spread for the stooks). Outside it the ground lies fallow.
                    float e = CentreRadius[level];
                    float ang = rnd.Range(0f, 180f);
                    var dir = BoardSpace.DirOf(ang); var side = new Vector3(dir.z, 0, -dir.x);
                    const float rowStep = 3.2f, piece = 6f;
                    int rows = Mathf.FloorToInt(e / rowStep), cols = Mathf.FloorToInt(e / piece);
                    for (int row = -rows; row <= rows; row++)
                        for (int s = -cols; s <= cols; s++)
                        {
                            var p = centre + side * (row * rowStep) + dir * (s * piece);
                            if ((p - centre).magnitude > e - 1.5f) continue;
                            var a = p - dir * (piece * 0.45f); var b = p + dir * (piece * 0.45f);
                            if (!ok(a, EdgeClear, 0.5f) || !ok(b, EdgeClear, 0.5f) || !ok(p, EdgeClear, 0.5f)) continue;
                            var col = ((row / 3) & 1) == 0 ? CropA : CropB; // bands of three rows: a patchwork, not stripes
                            mb.Box(p + Vector3.up * 0.45f, new Vector3(2.4f, 0.9f + rnd.Range(-0.05f, 0.1f), piece * 0.95f), col * rnd.Range(0.96f, 1.04f), ang);
                            placed++;
                        }
                    Hedge(mb, centre, e + 1.2f, ok, rnd);
                    // Stooks of cut sheaves stand inside the patch: more for a richer field.
                    for (int i = 0, n = 0; i < 60 && n < StookCount[level]; i++)
                    {
                        var p = centre + BoardSpace.DirOf(rnd.Range(0f, 360f)) * rnd.Range(CentreClear + 2f, Mathf.Max(CentreClear + 3f, e - 2f));
                        if (!ok(p, EdgeClear + 2f, 1.5f)) continue;
                        mb.Cylinder(p, 1.7f, 0.5f, 2.6f, 8, Stook);
                        mb.Cone(p + Vector3.up * 2.6f, 0.9f, 1.2f, 8, Stook * 1.08f);
                        placed++; n++;
                    }
                    break;
                }
                case Terrain.Mountain:
                {
                    var sides = t.HexSides(h);
                    for (int i = 0; i < sides.Count; i++)
                    {
                        var mid = BoardSpace.HexEdgeMid(t, sides[i]);
                        var dir = mid - centre; dir.y = 0; dir.Normalize();
                        var p = centre + dir * TerrainBuilder.PeakDistance;
                        float height = PeakHeight(level, rnd.Next()) * (i % 2 == 0 ? 0.8f : 0.65f);
                        if (withPeaks) Peak(mb, p, TerrainBuilder.PeakRadius * rnd.Range(0.9f, 1f), height, level, rnd);
                        placed++;
                    }
                    // Centrepiece: a crown of peaks round a quarry floor at the hex centre; more, wider and snowier peaks for a richer hex.
                    placed += QuarryCrown(mb, centre, level, ok, rnd);
                    for (int i = 0; i < 40; i++)
                    {
                        var p = RandomInHex();
                        if (!ok(p, EdgeClear, 1f)) continue;
                        mb.Rock(p, rnd.Range(0.6f, 2.0f), new Color(0.52f, 0.50f, 0.48f), rnd.Next);
                    }
                    break;
                }
                case Terrain.Pasture:
                {
                    // Centrepiece: a fold (dry-stone wall) round the hex centre, its size the richness, the flock inside it.
                    int want = FlockSize[level];
                    float fr = CentreRadius[level];
                    Wall(mb, centre, fr, ok, rnd);
                    // Lush grazed grass inside the fold: its size reads from far away even when the sheep do not.
                    {
                        int n = Mathf.Max(18, Mathf.RoundToInt(fr * 1.1f));
                        for (int i = 0; i < n; i++)
                        {
                            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                            var o0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var o1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                            if (!ok(centre + (o0 + o1) * 0.5f * (fr + CentreClear) * 0.5f, EdgeClear, 0.5f)) continue;
                            var up = Vector3.up * 0.3f;
                            var g = FoldGrass * rnd.Range(0.95f, 1.05f); g.a = 1f;
                            QuadUp(mb, centre + up + o0 * CentreClear, centre + up + o0 * (fr - 0.6f), centre + up + o1 * (fr - 0.6f), centre + up + o1 * CentreClear, g);
                        }
                    }
                    for (int i = 0; i < want * 12 && placed < want; i++)
                    {
                        var p = centre + BoardSpace.DirOf(rnd.Range(0f, 360f)) * rnd.Range(CentreClear + 1.5f, fr - 3f);
                        if (!ok(p, EdgeClear, 2.4f)) continue;
                        Sheep(mb, p, rnd.Range(0f, 360f), 3.2f, rnd);
                        placed++;
                    }
                    for (int i = 0; i < 70; i++)
                    {
                        var p = RandomInHex();
                        if (!ok(p, EdgeClear - 1f, 0f)) continue;
                        mb.Cone(p, rnd.Range(0.3f, 0.5f), rnd.Range(0.25f, 0.45f), 5, new Color(0.30f, 0.56f, 0.22f) * rnd.Range(0.95f, 1.08f));
                    }
                    break;
                }
            }
            return placed;
        }

        /// <summary>A terraced clay cliff: stacked frustums that step in, red-brown bands, and a bare scrape at its foot.</summary>
        private static void Cliff(MeshBuilder mb, Vector3 p, float r, float height, PropRandom rnd)
        {
            var clay = new Color(0.66f, 0.38f, 0.24f);
            TerrainBuilder.Disc(mb, p + Vector3.up * 0.05f, r * 1.25f, 14, clay * 0.8f);
            int steps = 3;
            float y = 0f, rr = r;
            for (int i = 0; i < steps; i++)
            {
                float h = height / steps;
                var col = Color.Lerp(clay * 0.85f, new Color(0.78f, 0.52f, 0.34f), i / (float)(steps - 1)) * rnd.Range(0.95f, 1.05f);
                mb.Cylinder(p + Vector3.up * y, rr, rr * 0.72f, h, 9, col, true);
                y += h; rr *= 0.72f;
            }
        }

        /// <summary>A sheep of body length 1.25 * scale (stock patch sheep are scale 1).</summary>
        public static void Sheep(MeshBuilder mb, Vector3 p, float yaw, float scale, PropRandom rnd)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            var wool = new Color(1f, 0.99f, 0.94f) * rnd.Range(0.95f, 1.02f); wool.a = 1;
            var dark = new Color(0.16f, 0.14f, 0.13f);
            Vector3 P(float x, float y, float z) => p + q * (new Vector3(x, y, z) * scale);
            mb.Box(P(0, 0.75f, 0), new Vector3(0.8f, 0.65f, 1.25f) * scale, wool, yaw);
            mb.Box(P(0, 0.95f, 0.72f), new Vector3(0.36f, 0.4f, 0.42f) * scale, dark, yaw);
            foreach (float x in new[] { -0.25f, 0.25f }) foreach (float z in new[] { -0.4f, 0.4f })
                mb.Box(P(x, 0.22f, z), new Vector3(0.12f, 0.44f, 0.12f) * scale, dark, yaw);
        }
    }
}
