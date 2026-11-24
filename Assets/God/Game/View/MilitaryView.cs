using System.Collections.Generic;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// P3 debug view of the military, view only (it reads <see cref="MilitaryState"/>, never changes it): placeholder
    /// figures in the faction colour (capsules for infantry, boxes for vehicles) facing their last movement, a small
    /// health bar over each unit, a tracer and flash while a unit fires, health bars over damaged buildings, grey rubble,
    /// numbered target markers over marked enemy buildings and defend flag markers showing the guard count.
    /// The mesh is rebuilt every frame (a handful of units, so it stays cheap).
    /// </summary>
    public sealed class MilitaryView : MonoBehaviour
    {
        private GodSim _sim;
        private MeshFilter _filter;
        private Material _mat;
        private Font _font;
        private readonly List<TextMesh> _labels = new List<TextMesh>();
        private int _labelsUsed;

        public GodSim Sim => _sim;
        /// <summary>Units drawn last frame (for tests).</summary>
        public int UnitsDrawn { get; private set; }
        public int TracersDrawn { get; private set; }

        public static MilitaryView Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("MilitaryView");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<MilitaryView>();
            v._sim = sim;
            v._mat = MaterialFactory.CreateVertexColored(Color.white, "Military lit");
            v._filter = go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = v._mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            v._font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return v;
        }

        private static Color FactionColour(Faction f) => f == Faction.Player ? TerritoryView.PlayerColour : TerritoryView.EnemyColour;

        private static Vector3 P(int xMm, int zMm, float y) => new Vector3(xMm / 1000f, y, zMm / 1000f);

        private void LateUpdate()
        {
            var s = _sim.State; var m = s.Military;
            if (m == null) return;
            var mb = new MeshBuilder();
            _labelsUsed = 0;
            var cam = Camera.main;
            UnitsDrawn = 0; TracersDrawn = 0;

            foreach (var u in m.Units) DrawUnit(mb, m, u);
            DrawBuildingBars(mb, m);
            DrawRubble(mb, m);
            DrawMarks(mb, m);
            DrawDefends(mb, m);

            var old = _filter.sharedMesh;
            _filter.sharedMesh = mb.ToMesh("Military");
            if (old != null) Destroy(old);
            for (int i = _labelsUsed; i < _labels.Count; i++) _labels[i].gameObject.SetActive(false);
            if (cam != null) foreach (var l in _labels) if (l.gameObject.activeSelf) l.transform.rotation = cam.transform.rotation;
        }

        private void DrawUnit(MeshBuilder mb, MilitaryState m, MilUnit u)
        {
            UnitsDrawn++;
            var col = FactionColour(u.Faction);
            var p = P(u.X, u.Z, 0f);
            float yaw = (u.FaceX == 0 && u.FaceZ == 0) ? 0f : Mathf.Atan2(u.FaceX, u.FaceZ) * Mathf.Rad2Deg;
            var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
            float barY;
            if (u.Kind == UnitKind.Infantry)
            {
                mb.Cylinder(p, 0.55f, 0.45f, 1.5f, 8, col);
                mb.Box(p + Vector3.up * 1.85f, new Vector3(0.6f, 0.6f, 0.6f), Color.Lerp(col, Color.white, 0.55f), yaw);
                mb.Box(p + Vector3.up * 1.1f + fwd * 0.55f, new Vector3(0.18f, 0.18f, 1.0f), new Color(0.15f, 0.15f, 0.15f), yaw);
                barY = 3.0f;
            }
            else
            {
                mb.Box(p + Vector3.up * 0.9f, new Vector3(2.2f, 1.1f, 3.6f), Color.Lerp(col, Color.black, 0.15f), yaw);
                mb.Box(p + Vector3.up * 1.8f, new Vector3(1.5f, 0.7f, 1.6f), Color.Lerp(col, Color.white, 0.3f), yaw);
                mb.Box(p + Vector3.up * 1.8f + fwd * 1.8f, new Vector3(0.28f, 0.28f, 2.4f), new Color(0.15f, 0.15f, 0.15f), yaw);
                barY = 3.6f;
            }
            Bar(mb, p + Vector3.up * barY, u.Kind == UnitKind.Infantry ? 2.2f : 3.4f, u.HealthFraction);

            if (u.IsFiring && u.HasTarget)
            {
                Vector3? to = null;
                if (u.TargetKind == TargetKind.Unit) { var t = m.UnitById(u.TargetId); if (t != null) to = P(t.X, t.Z, 1.3f); }
                else { var b = m.BuildingById(u.TargetId); if (b != null) to = P(b.X, b.Z, 1.8f); }
                var from = p + Vector3.up * 1.3f + fwd * (u.Kind == UnitKind.Vehicle ? 3.0f : 0.9f);
                if (to.HasValue)
                {
                    mb.Strip(from, to.Value, u.Kind == UnitKind.Vehicle ? 0.5f : 0.25f, new Color(1f, 0.92f, 0.45f));
                    TracersDrawn++;
                }
                mb.Box(from, new Vector3(0.9f, 0.9f, 0.9f), new Color(1f, 0.85f, 0.3f), yaw + 45f);
            }
        }

        private static void Bar(MeshBuilder mb, Vector3 centre, float width, float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            mb.Box(centre, new Vector3(width + 0.2f, 0.5f, 0.1f), new Color(0.08f, 0.08f, 0.08f));
            if (fraction <= 0f) return;
            var c = Color.Lerp(new Color(0.9f, 0.15f, 0.12f), new Color(0.25f, 0.85f, 0.25f), fraction);
            float w = width * fraction;
            mb.Box(centre + new Vector3(-(width - w) * 0.5f, 0f, -0.1f), new Vector3(w, 0.3f, 0.1f), c);
        }

        private static void DrawBuildingBars(MeshBuilder mb, MilitaryState m)
        {
            foreach (var b in m.Buildings)
            {
                if (!b.Damaged) continue;
                float h = b.Size == BuildingSize.Hq ? 10f : b.Size == BuildingSize.Medium ? 7f : 5.5f;
                Bar(mb, P(b.X, b.Z, h), b.Size == BuildingSize.Hq ? 8f : 5f, b.HealthFraction);
            }
        }

        private static void DrawRubble(MeshBuilder mb, MilitaryState m)
        {
            foreach (var r in m.Rubbles)
            {
                var rnd = new PropRandom(r.Id * 7919 + r.Node);
                int n = r.Size == BuildingSize.Hq ? 14 : r.Size == BuildingSize.Medium ? 9 : 6;
                float spread = r.Size == BuildingSize.Hq ? 4.5f : r.Size == BuildingSize.Medium ? 3f : 2f;
                var c = P(r.X, r.Z, 0f);
                float fade = Mathf.Clamp01(r.TicksLeft / 60f);
                for (int i = 0; i < n; i++)
                {
                    float s = rnd.Range(0.5f, 1.4f) * fade + 0.1f;
                    var o = c + new Vector3(rnd.Range(-spread, spread), s * 0.3f, rnd.Range(-spread, spread));
                    float g = rnd.Range(0.35f, 0.6f);
                    mb.Box(o, new Vector3(s, s * 0.6f, s * 0.9f), new Color(g, g, g * 1.03f), rnd.Range(0f, 90f));
                }
            }
        }

        private void DrawMarks(MeshBuilder mb, MilitaryState m)
        {
            for (int i = 0; i < m.Marked.Count; i++)
            {
                var b = m.BuildingById(m.Marked[i]);
                if (b == null) continue;
                float h = b.Size == BuildingSize.Hq ? 14f : b.Size == BuildingSize.Medium ? 10f : 8f;
                var p = P(b.X, b.Z, h);
                var red = new Color(1f, 0.2f, 0.15f);
                mb.Box(p, new Vector3(1.8f, 1.8f, 1.8f), red, 45f);
                mb.Box(p + Vector3.up * 3.2f, new Vector3(0.3f, 2.4f, 0.3f), red);
                Label(p + Vector3.up * 6.2f, (i + 1).ToString(), new Color(1f, 0.9f, 0.6f), 3.2f);
            }
        }

        private void DrawDefends(MeshBuilder mb, MilitaryState m)
        {
            foreach (var f in m.Defends)
            {
                float h = 0f;
                if (f.AnchorBuildingId != 0) { var b = m.BuildingById(f.AnchorBuildingId); h = b != null && b.IsHq ? 8f : 5f; }
                var cyan = new Color(0.2f, 0.85f, 0.95f);
                var p = P(f.X + 4500, f.Z, h);
                mb.Cylinder(p, 0.15f, 0.15f, 5.2f, 5, new Color(0.9f, 0.9f, 0.9f));
                mb.Box(p + new Vector3(1.0f, 4.5f, 0f), new Vector3(2.0f, 1.2f, 0.12f), cyan);
                Label(p + new Vector3(1.0f, 6.4f, 0f), m.GuardCount(f.Id) + "/" + f.Guards, cyan, 2.6f);
            }
        }

        private void Label(Vector3 pos, string text, Color col, float size)
        {
            TextMesh tm;
            if (_labelsUsed < _labels.Count) tm = _labels[_labelsUsed];
            else
            {
                var go = new GameObject("Label");
                go.transform.SetParent(transform, false);
                tm = go.AddComponent<TextMesh>();
                tm.font = _font;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontSize = 48;
                go.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.TextMaterial(_font);
                _labels.Add(tm);
            }
            _labelsUsed++;
            tm.gameObject.SetActive(true);
            tm.transform.position = pos;
            tm.characterSize = size * 0.1f;
            tm.text = text;
            tm.color = col;
        }
    }
}
