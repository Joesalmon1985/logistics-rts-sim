using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b order markers in the world, styled as things the people would plant rather than gizmos: a war standard with a
    /// painted number beside each marked enemy building (attack order), and a defend standard with a small guard count
    /// ("2/3") at each defend flag. They turn to face the camera. Pooled. Reads the sim only.
    /// </summary>
    public sealed class OverlayLayer : MonoBehaviour
    {
        private sealed class Marker { public GameObject Go; public TextMesh Text; }

        private GodSim _sim;
        private readonly List<Marker> _targets = new List<Marker>(), _defends = new List<Marker>();
        private Font _font;

        public int TargetsShown { get; private set; }
        public int DefendsShown { get; private set; }

        public static OverlayLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("OverlayLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<OverlayLayer>();
            l._sim = sim;
            l._font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return l;
        }

        private Marker Get(List<Marker> list, int i, Mesh mesh, Vector3 textAt, float textSize, Color textCol)
        {
            if (i < list.Count) { list[i].Go.SetActive(true); return list[i]; }
            var m = new Marker { Go = ModelKit.Part("Standard", transform, mesh) };
            var tgo = new GameObject("Label");
            tgo.transform.SetParent(m.Go.transform, false);
            tgo.transform.localPosition = textAt;
            m.Text = tgo.AddComponent<TextMesh>();
            m.Text.font = _font; m.Text.fontSize = 64; m.Text.anchor = TextAnchor.MiddleCenter; m.Text.alignment = TextAlignment.Center;
            m.Text.characterSize = textSize; m.Text.color = textCol; m.Text.fontStyle = FontStyle.Bold;
            tgo.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.TextMaterial(_font);
            list.Add(m);
            return m;
        }

        private static float RadiusOf(BuildingSize s) => s == BuildingSize.Hq ? 7f : s == BuildingSize.Medium ? 5f : 3.2f;

        private void LateUpdate()
        {
            var s = _sim.State; var m = s.Military;
            var cam = Camera.main;
            float camYaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            var rot = Quaternion.Euler(0, camYaw, 0);
            var right = rot * Vector3.right; var back = rot * Vector3.back;
            int ti = 0, di = 0;
            if (m != null)
            {
                for (int i = 0; i < m.Marked.Count; i++)
                {
                    var b = m.BuildingById(m.Marked[i]);
                    if (b == null) continue;
                    var mk = Get(_targets, ti++, PropModels.TargetStandard(), new Vector3(0, 6.0f, -0.9f), 0.2f, new Color(0.18f, 0.12f, 0.08f));
                    float r = RadiusOf(b.Size);
                    var p = new Vector3(b.X / 1000f, 0, b.Z / 1000f) - right * r * 0.8f + back * r * 0.6f;
                    mk.Go.transform.SetPositionAndRotation(ModelKit.OnGround(p.x, p.z), rot);
                    mk.Text.text = (i + 1).ToString();
                    if (cam != null) mk.Text.transform.rotation = cam.transform.rotation;
                }
                foreach (var f in m.Defends)
                {
                    float r = 2f;
                    if (f.AnchorBuildingId != 0) { var b = m.BuildingById(f.AnchorBuildingId); if (b != null) r = RadiusOf(b.Size); }
                    var mk = Get(_defends, di++, PropModels.DefendStandard(), new Vector3(0, 6.4f, -0.6f), 0.12f, new Color(0.98f, 0.95f, 0.85f));
                    var p = new Vector3(f.X / 1000f, 0, f.Z / 1000f) + right * r * 0.85f + back * r * 0.5f;
                    mk.Go.transform.SetPositionAndRotation(ModelKit.OnGround(p.x, p.z), rot);
                    mk.Text.text = m.GuardCount(f.Id) + "/" + f.Guards;
                    if (cam != null) mk.Text.transform.rotation = cam.transform.rotation;
                }
            }
            for (int i = ti; i < _targets.Count; i++) _targets[i].Go.SetActive(false);
            for (int i = di; i < _defends.Count; i++) _defends[i].Go.SetActive(false);
            TargetsShown = ti; DefendsShown = di;
        }
    }
}
