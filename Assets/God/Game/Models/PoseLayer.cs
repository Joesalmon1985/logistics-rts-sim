using System.Collections.Generic;
using God.Sim;
using God.Sim.Population;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// U3b: plays the gesture vocabulary on the figures (NPC_ANIMATION 4). Sim events go to the pure
    /// <see cref="GestureAnimationPlanner"/>; the cues wait in one <see cref="PoseSequencer"/> per actor; each frame, for the
    /// drawn actor with an active cue, the sampled clip is blended over the walk or work pose NpcLayer just made (arms, head,
    /// body, legs, facing and a small step toward the target), finger pips show the quantity, and the real ware named by the
    /// event is shown in the hands (Held) over the clip's held range. It reads events and positions and decides nothing: with
    /// no events nothing is drawn, and the camera never changes the cues (only whether detail is drawn).
    /// </summary>
    public static class PoseLayer
    {
        /// <summary>Simultaneous detailed sequences (U3 performance cap).</summary>
        public const int MaxDetailed = 40;
        /// <summary>Beyond this camera distance the finger pips are not drawn (rendering detail only).</summary>
        public const float FingerRange = 70f;

        private static GodBootstrap _boot;
        private static readonly GestureAnimationPlanner Planner = new GestureAnimationPlanner();
        private static readonly Dictionary<int, PoseSequencer> Seq = new Dictionary<int, PoseSequencer>();
        private static readonly Dictionary<int, CueObject> HeldNow = new Dictionary<int, CueObject>();
        private static readonly HashSet<int> WasActive = new HashSet<int>();
        private static readonly float[] V = new float[(int)PoseChannel.Count];
        private static double _now;
        private static int _frame, _frameDetailed;

        /// <summary>Every cue planned since the layer was reset (tests; capped).</summary>
        public static readonly List<AnimationCue> Cues = new List<AnimationCue>();
        public const int MaxLog = 20000;
        /// <summary>Cues skipped for drawing over the detailed cap (tests).</summary>
        public static int SkippedDetail { get; private set; }
        public static int DetailedLastFrame { get; private set; }

        public static GestureAnimationPlanner PlannerInstance => Planner;
        public static AnimationCue ActiveCue(int npc) => Seq.TryGetValue(npc, out var s) ? s.Active : null;
        public static bool TryHeld(int npc, out CueObject o) => HeldNow.TryGetValue(npc, out o);

        public static void Reset()
        {
            Planner.Reset(); Seq.Clear(); HeldNow.Clear(); WasActive.Clear(); Cues.Clear(); SkippedDetail = 0;
        }

        /// <summary>Feeds a sim event (the bootstrap's event pump; tests may call it directly).</summary>
        public static void OnEvent(SimEvent e)
        {
            var list = Planner.Plan(e);
            foreach (var c in list)
            {
                if (Cues.Count < MaxLog) Cues.Add(c);
                if (!Seq.TryGetValue(c.Actor, out var s)) { s = new PoseSequencer(); Seq[c.Actor] = s; }
                s.Enqueue(c, _now);
            }
        }

        public static void Subscribe()
        {
            var b = GodBootstrap.Instance;
            if (b == _boot) return;
            if (_boot != null) _boot.SimEventRaised -= OnEvent;
            _boot = b;
            Reset();
            if (_boot != null) _boot.SimEventRaised += OnEvent;
        }

        public static void Apply(FigureParts p, Npc n, GodState s, float time, NpcLayer layer)
        {
            Subscribe();
            _now = time;
            if (Time.frameCount != _frame) { DetailedLastFrame = _frameDetailed; _frame = Time.frameCount; _frameDetailed = 0; }
            if (!Seq.TryGetValue(n.Id, out var q) || !q.Advance(time))
            {
                if (WasActive.Remove(n.Id)) Cleanup(p, n.Id);
                return;
            }
            WasActive.Add(n.Id);
            if (n.Action.Asleep || !p.Root.gameObject.activeSelf) return;
            if (_frameDetailed >= MaxDetailed) { SkippedDetail++; return; }
            _frameDetailed++;
            var cue = q.Active; var clip = q.Clip; float w = q.Weight, t = q.T;
            System.Array.Clear(V, 0, V.Length);
            clip.Sample(t, cue.Intensity, V);
            if (clip.Drives(PoseChannel.BodyPitch) || clip.Drives(PoseChannel.BodyRoll))
                p.Body.localRotation = Quaternion.Slerp(p.Body.localRotation, Quaternion.Euler(V[(int)PoseChannel.BodyPitch], 0, V[(int)PoseChannel.BodyRoll]), w);
            var pos = p.Body.localPosition;
            if (clip.Drives(PoseChannel.BodyDrop)) pos.y += V[(int)PoseChannel.BodyDrop] * w;
            if (clip.Drives(PoseChannel.BodyFwd)) pos.z += V[(int)PoseChannel.BodyFwd] * w;
            p.Body.localPosition = pos;
            if (clip.Drives(PoseChannel.HeadPitch) || clip.Drives(PoseChannel.HeadYaw))
                p.Head.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(V[(int)PoseChannel.HeadPitch], V[(int)PoseChannel.HeadYaw], 0), w);
            if (clip.Drives(PoseChannel.ArmLPitch) || clip.Drives(PoseChannel.ArmLRoll))
                p.ArmL.localRotation = Quaternion.Slerp(p.ArmL.localRotation, Quaternion.Euler(V[(int)PoseChannel.ArmLPitch], 0, -V[(int)PoseChannel.ArmLRoll]), w);
            if (clip.Drives(PoseChannel.ArmRPitch) || clip.Drives(PoseChannel.ArmRRoll))
                p.ArmR.localRotation = Quaternion.Slerp(p.ArmR.localRotation, Quaternion.Euler(V[(int)PoseChannel.ArmRPitch], 0, V[(int)PoseChannel.ArmRRoll]), w);
            if (clip.Drives(PoseChannel.LegLPitch)) p.LegL.localRotation = Quaternion.Slerp(p.LegL.localRotation, Quaternion.Euler(V[(int)PoseChannel.LegLPitch], 0, 0), w);
            if (clip.Drives(PoseChannel.LegRPitch)) p.LegR.localRotation = Quaternion.Slerp(p.LegR.localRotation, Quaternion.Euler(V[(int)PoseChannel.LegRPitch], 0, 0), w);

            // face the person addressed (cosmetic: the sim position is untouched)
            if (clip.Directed && cue.Target != 0 && layer != null && layer.TryPosition(cue.Target, out var tp))
            {
                var d = tp - p.Root.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f)
                {
                    float delta = Mathf.DeltaAngle(p.Root.eulerAngles.y, ModelKit.Yaw(d));
                    p.Root.rotation = Quaternion.Euler(0, delta * w * 0.9f, 0) * p.Root.rotation;
                }
            }

            // finger pips: the quantity
            bool near = true;
            var cam = Camera.main;
            if (cam != null) near = (cam.transform.position - p.Root.position).sqrMagnitude < FingerRange * FingerRange;
            int q1 = near && cue.Quantity > 0 ? cue.Quantity : 0;
            SetFingers(p.FingersR, p.FingersRF, Mathf.Min(q1, 5));
            SetFingers(p.FingersL, p.FingersLF, Mathf.Max(0, q1 - 5));

            // the real item in the hands
            bool held = cue.Object.Any && clip.HeldAt(t);
            if (held)
            {
                var o = cue.Object;
                p.Held.gameObject.SetActive(true);
                var mesh = NpcLayer.LoadMesh(o.Ware);
                p.HeldF.sharedMesh = mesh;
                int stack = Mathf.Clamp(o.Count, 1, 3) - 1;
                for (int i = 0; i < p.HeldStack.Length; i++)
                {
                    var hs = p.HeldStack[i];
                    hs.gameObject.SetActive(i < stack);
                    if (i < stack) hs.GetComponent<MeshFilter>().sharedMesh = mesh;
                }
                var hp = FigureParts.HeldHome;
                hp.y += clip.Lift * Mathf.Min(1f, w);
                if (cue.Kind == AnimKind.Giving) hp.z += 0.5f * Mathf.Clamp01(t / Mathf.Max(0.01f, clip.ReleaseT));
                else if (cue.Kind == AnimKind.Receiving) hp.z += 0.5f * (1f - Mathf.Clamp01((t - clip.ReleaseT) / (1f - clip.ReleaseT)));
                p.Held.localPosition = hp;
                HeldNow[n.Id] = o;
                // the load it now carries is the same lot: not drawn twice
                p.Cargo.gameObject.SetActive(!(o.Ware == FirstLoad(n)));
            }
            else
            {
                if (HeldNow.Remove(n.Id) || p.Held.gameObject.activeSelf) { p.Held.gameObject.SetActive(false); p.Cargo.gameObject.SetActive(true); }
            }
        }

        private static Ware FirstLoad(Npc n)
        {
            foreach (var l in n.Carry) if (!l.Empty && l.Ware != Ware.Axe) return l.Ware;
            return Ware.None;
        }

        private static void SetFingers(Transform t, MeshFilter mf, int count)
        {
            if (count <= 0) { if (t.gameObject.activeSelf) t.gameObject.SetActive(false); return; }
            t.gameObject.SetActive(true);
            mf.sharedMesh = NpcLayer.FingerMesh(count);
        }

        private static void Cleanup(FigureParts p, int id)
        {
            p.Held.gameObject.SetActive(false); p.Cargo.gameObject.SetActive(true);
            p.FingersL.gameObject.SetActive(false); p.FingersR.gameObject.SetActive(false);
            HeldNow.Remove(id);
        }
    }
}
