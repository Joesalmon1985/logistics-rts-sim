using System;
using System.Collections.Generic;

namespace God.Game
{
    /// <summary>The joints a clip drives (angles in degrees, offsets in metres). Arm roll is "outward positive" for both arms.</summary>
    public enum PoseChannel : byte
    {
        BodyPitch, BodyRoll, BodyDrop, BodyFwd, HeadPitch, HeadYaw,
        ArmLPitch, ArmLRoll, ArmRPitch, ArmRRoll, LegLPitch, LegRPitch,
        Count,
    }

    /// <summary>Joint limits: a sampled pose never leaves them (tested).</summary>
    public static class JointLimits
    {
        public static float Min(PoseChannel c)
        {
            switch (c)
            {
                case PoseChannel.BodyPitch: return -30f;
                case PoseChannel.BodyRoll: return -25f;
                case PoseChannel.BodyDrop: return -0.7f;
                case PoseChannel.BodyFwd: return -0.6f;
                case PoseChannel.HeadPitch: return -45f;
                case PoseChannel.HeadYaw: return -80f;
                case PoseChannel.ArmLPitch: case PoseChannel.ArmRPitch: return -180f;
                case PoseChannel.ArmLRoll: case PoseChannel.ArmRRoll: return -60f;
                default: return -100f;
            }
        }

        public static float Max(PoseChannel c)
        {
            switch (c)
            {
                case PoseChannel.BodyPitch: return 60f;
                case PoseChannel.BodyRoll: return 25f;
                case PoseChannel.BodyDrop: return 0.1f;
                case PoseChannel.BodyFwd: return 0.8f;
                case PoseChannel.HeadPitch: return 45f;
                case PoseChannel.HeadYaw: return 80f;
                case PoseChannel.ArmLPitch: case PoseChannel.ArmRPitch: return 60f;
                case PoseChannel.ArmLRoll: case PoseChannel.ArmRRoll: return 100f;
                default: return 100f;
            }
        }
    }

    /// <summary>One joint's keyframes: times 0..1 ascending, values in the channel's unit.</summary>
    public sealed class PoseTrack
    {
        public PoseChannel Channel;
        public float[] T, V;

        public float At(float t)
        {
            if (t <= T[0]) return V[0];
            for (int i = 1; i < T.Length; i++)
            {
                if (t > T[i]) continue;
                float u = (t - T[i - 1]) / Math.Max(1e-5f, T[i] - T[i - 1]);
                u = u * u * (3f - 2f * u);
                return V[i - 1] + (V[i] - V[i - 1]) * u;
            }
            return V[V.Length - 1];
        }
    }

    /// <summary>
    /// A clip: pure keyframe data and nothing else (no engine types, no sim reads). Seconds is the view-time length at
    /// intensity 0.5 (the planner's sim duration can only lengthen it). Held: the ware named by the cue is in the hands over
    /// [HeldFrom, HeldTo] (negative HeldFrom: never); ReleaseT is the moment a handed item passes (Giving releases, Receiving
    /// takes) so the two actors move in lockstep. Lift raises the held item. Directed: the actor steps toward and faces its target.
    /// </summary>
    public sealed class PoseClip
    {
        public AnimKind Kind;
        public float Seconds;
        public string[] Phases;
        public float[] PhaseEnds;
        public PoseTrack[] Tracks;
        public float HeldFrom = -1f, HeldTo = -1f, ReleaseT = -1f, Lift;
        public bool Directed;
        public int Mask;

        public bool Drives(PoseChannel c) => (Mask & (1 << (int)c)) != 0;
        public bool HeldAt(float t) => HeldFrom >= 0f && t >= HeldFrom && t <= HeldTo;

        /// <summary>The phase name at a time (for tests and debugging).</summary>
        public string PhaseAt(float t)
        {
            for (int i = 0; i < PhaseEnds.Length; i++) if (t <= PhaseEnds[i]) return Phases[i];
            return Phases[Phases.Length - 1];
        }

        /// <summary>
        /// Samples the driven channels into <paramref name="v"/> (length PoseChannel.Count); channels the clip does not drive are
        /// left unchanged. Amplitude scales with intensity (0.6 at 0, 1.2 at 1) and everything is clamped to the joint limits.
        /// </summary>
        public void Sample(float t, float intensity, float[] v)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            float k = 0.6f + 0.6f * (intensity < 0f ? 0f : intensity > 1f ? 1f : intensity);
            foreach (var tr in Tracks)
            {
                float x = tr.At(t);
                if (tr.Channel != PoseChannel.BodyFwd && tr.Channel != PoseChannel.BodyDrop) x *= k; else x *= 0.6f + 0.4f * k;
                v[(int)tr.Channel] = Math.Max(JointLimits.Min(tr.Channel), Math.Min(JointLimits.Max(tr.Channel), x));
            }
        }
    }

    /// <summary>The clip library: one clip per AnimKind, built once from data.</summary>
    public static class PoseClips
    {
        private static readonly PoseClip[] _all = Build();

        public static PoseClip Of(AnimKind k) => _all[(int)k];
        public static IEnumerable<PoseClip> All { get { foreach (var c in _all) if (c != null) yield return c; } }

        private static PoseTrack Tr(PoseChannel ch, params float[] tv)
        {
            int n = tv.Length / 2;
            var t = new PoseTrack { Channel = ch, T = new float[n], V = new float[n] };
            for (int i = 0; i < n; i++) { t.T[i] = tv[2 * i]; t.V[i] = tv[2 * i + 1]; }
            return t;
        }

        private static PoseClip C(AnimKind k, float seconds, string[] phases, float[] ends, bool directed, params PoseTrack[] tr)
        {
            var c = new PoseClip { Kind = k, Seconds = seconds, Phases = phases, PhaseEnds = ends, Tracks = tr, Directed = directed };
            foreach (var t in tr) c.Mask |= 1 << (int)t.Channel;
            return c;
        }

        private const PoseChannel BP = PoseChannel.BodyPitch, BD = PoseChannel.BodyDrop, BF = PoseChannel.BodyFwd,
            HP = PoseChannel.HeadPitch, HY = PoseChannel.HeadYaw, LP = PoseChannel.ArmLPitch, LR = PoseChannel.ArmLRoll,
            RP = PoseChannel.ArmRPitch, RR = PoseChannel.ArmRRoll, GL = PoseChannel.LegLPitch, GR = PoseChannel.LegRPitch;

        private static PoseClip[] Build()
        {
            var a = new PoseClip[(int)AnimKind.Count];
            a[(int)AnimKind.Attention] = C(AnimKind.Attention, 0.9f, new[] { "Wave", "Hold" }, new[] { 0.65f, 1f }, true,
                Tr(RP, 0, 0, 0.2f, -160, 0.35f, -140, 0.5f, -165, 0.65f, -150, 1, -150), Tr(RR, 0, 0, 0.3f, 25, 1, 25),
                Tr(HP, 0, 0, 0.3f, -12, 1, -8), Tr(BF, 0, 0, 0.5f, 0.25f, 1, 0.25f));
            a[(int)AnimKind.Listen] = C(AnimKind.Listen, 0.8f, new[] { "Turn", "Hold" }, new[] { 0.4f, 1f }, true,
                Tr(HY, 0, 0, 0.4f, 20, 1, 20), Tr(HP, 0, 0, 0.5f, 6, 1, 6));
            a[(int)AnimKind.Pointing] = C(AnimKind.Pointing, 1.3f, new[] { "Reach", "Hold", "Return" }, new[] { 0.25f, 0.75f, 1f }, true,
                Tr(RP, 0, 0, 0.25f, -90, 0.75f, -90, 1, 0), Tr(HY, 0, 0, 0.3f, -10, 0.75f, -10, 1, 0), Tr(BP, 0, 0, 0.3f, 8, 0.75f, 8, 1, 0));
            a[(int)AnimKind.Requesting] = C(AnimKind.Requesting, 1.5f, new[] { "Reach", "Palm-to-chest", "Hold" }, new[] { 0.3f, 0.6f, 1f }, true,
                Tr(LP, 0, 0, 0.3f, -80, 0.6f, -50, 1, -60), Tr(RP, 0, 0, 0.3f, -80, 0.6f, -55, 1, -60),
                Tr(LR, 0, 0, 0.3f, 12, 1, 12), Tr(RR, 0, 0, 0.3f, 12, 1, 12), Tr(BP, 0, 0, 0.3f, 8, 1, 8), Tr(HP, 0, 0, 0.3f, -14, 1, -14),
                Tr(BF, 0, 0, 0.4f, 0.2f, 1, 0.2f));
            a[(int)AnimKind.Offering] = C(AnimKind.Offering, 1.6f, new[] { "Raise", "Present", "Hold" }, new[] { 0.25f, 0.65f, 1f }, true,
                Tr(LP, 0, 0, 0.25f, -85, 1, -85), Tr(RP, 0, 0, 0.25f, -85, 1, -85), Tr(LR, 0, 0, 0.3f, 8, 1, 8), Tr(RR, 0, 0, 0.3f, 8, 1, 8),
                Tr(BP, 0, 0, 0.4f, 12, 0.8f, 12, 1, 6), Tr(BF, 0, 0, 0.5f, 0.3f, 1, 0.3f), Tr(HP, 0, 0, 0.4f, 10, 1, 4));
            a[(int)AnimKind.Offering].HeldFrom = 0.1f; a[(int)AnimKind.Offering].HeldTo = 1f;
            a[(int)AnimKind.Displaying] = C(AnimKind.Displaying, 1.7f, new[] { "Lift", "Turn", "Lower" }, new[] { 0.25f, 0.75f, 1f }, false,
                Tr(RP, 0, 0, 0.25f, -150, 0.75f, -150, 1, -40), Tr(RR, 0, 0, 0.3f, 10, 0.4f, 25, 0.55f, -5, 0.7f, 25, 1, 0),
                Tr(LP, 0, 0, 0.25f, -80, 0.75f, -80, 1, -20), Tr(HY, 0, 0, 0.4f, 0, 0.55f, 22, 0.7f, -22, 1, 0));
            a[(int)AnimKind.Displaying].HeldFrom = 0.05f; a[(int)AnimKind.Displaying].HeldTo = 1f; a[(int)AnimKind.Displaying].Lift = 0.6f;
            a[(int)AnimKind.Giving] = C(AnimKind.Giving, 1.5f, new[] { "Extend", "Release", "Withdraw" }, new[] { 0.4f, 0.6f, 1f }, true,
                Tr(LP, 0, 0, 0.4f, -90, 0.6f, -90, 1, -10), Tr(RP, 0, 0, 0.4f, -90, 0.6f, -90, 1, -10), Tr(LR, 0, 0, 0.4f, 6, 1, 0), Tr(RR, 0, 0, 0.4f, 6, 1, 0),
                Tr(BF, 0, 0, 0.4f, 0.35f, 0.6f, 0.35f, 1, 0), Tr(BP, 0, 0, 0.4f, 10, 1, 0));
            a[(int)AnimKind.Giving].HeldFrom = 0f; a[(int)AnimKind.Giving].HeldTo = 0.5f; a[(int)AnimKind.Giving].ReleaseT = 0.5f;
            a[(int)AnimKind.Receiving] = C(AnimKind.Receiving, 1.5f, new[] { "Cup", "Take", "Draw-in" }, new[] { 0.4f, 0.6f, 1f }, true,
                Tr(LP, 0, 0, 0.4f, -80, 0.6f, -80, 1, -45), Tr(RP, 0, 0, 0.4f, -80, 0.6f, -80, 1, -45), Tr(LR, 0, 0, 0.4f, 12, 0.6f, 12, 1, 0), Tr(RR, 0, 0, 0.4f, 12, 0.6f, 12, 1, 0),
                Tr(BP, 0, 0, 0.5f, 6, 0.8f, 10, 1, 0), Tr(HP, 0, 0, 0.6f, 4, 0.85f, 14, 1, 0));
            a[(int)AnimKind.Receiving].HeldFrom = 0.5f; a[(int)AnimKind.Receiving].HeldTo = 1f; a[(int)AnimKind.Receiving].ReleaseT = 0.5f;
            a[(int)AnimKind.Accepting] = C(AnimKind.Accepting, 1.1f, new[] { "Nod", "Extend" }, new[] { 0.5f, 1f }, true,
                Tr(HP, 0, 0, 0.2f, 18, 0.4f, -2, 0.55f, 16, 0.7f, 0, 1, 0), Tr(LP, 0, 0, 0.5f, 0, 0.8f, -75, 1, -75), Tr(RP, 0, 0, 0.5f, 0, 0.8f, -75, 1, -75),
                Tr(LR, 0, 0, 0.8f, 10, 1, 10), Tr(RR, 0, 0, 0.8f, 10, 1, 10), Tr(BF, 0, 0, 0.7f, 0.25f, 1, 0.25f));
            a[(int)AnimKind.Refusing] = C(AnimKind.Refusing, 1.2f, new[] { "Shake", "Palm-out", "Step-back" }, new[] { 0.4f, 0.7f, 1f }, true,
                Tr(HY, 0, 0, 0.1f, -30, 0.2f, 30, 0.3f, -30, 0.4f, 0, 1, 0), Tr(RP, 0, 0, 0.45f, -90, 1, -90), Tr(RR, 0, 0, 0.45f, 5, 1, 5),
                Tr(BF, 0, 0, 0.6f, 0, 1, -0.3f), Tr(BP, 0, 0, 0.7f, -8, 1, -8));
            a[(int)AnimKind.Agreeing] = C(AnimKind.Agreeing, 1.1f, new[] { "Nod", "Nod", "Chest" }, new[] { 0.3f, 0.6f, 1f }, true,
                Tr(HP, 0, 0, 0.15f, 20, 0.3f, 0, 0.45f, 20, 0.6f, 0, 1, 0), Tr(RP, 0, 0, 0.6f, 0, 0.8f, -50, 1, -50), Tr(RR, 0, 0, 0.6f, -40, 0.8f, -30, 1, -30));
            a[(int)AnimKind.Cooperating] = C(AnimKind.Cooperating, 1.8f, new[] { "Approach", "Align", "Mirror" }, new[] { 0.35f, 0.6f, 1f }, true,
                Tr(BF, 0, 0, 0.35f, 0.45f, 1, 0.45f), Tr(RP, 0, 0, 0.4f, -40, 0.6f, -60, 0.8f, -30, 1, -60), Tr(LP, 0, 0, 0.4f, -30, 1, -30), Tr(BP, 0, 0, 0.6f, 12, 1, 12));
            a[(int)AnimKind.Warning] = C(AnimKind.Warning, 1.5f, new[] { "Raise", "Stamp", "Point" }, new[] { 0.35f, 0.65f, 1f }, true,
                Tr(RP, 0, 0, 0.15f, -160, 0.25f, -135, 0.35f, -165, 0.65f, -150, 0.85f, -90, 1, -90), Tr(RR, 0, 0, 0.3f, 20, 0.65f, 20, 1, 0),
                Tr(HP, 0, 0, 0.3f, -18, 1, -4), Tr(BD, 0, 0, 0.45f, 0, 0.5f, -0.06f, 0.6f, 0, 1, 0), Tr(LP, 0, 0, 0.3f, -60, 1, -20));
            a[(int)AnimKind.Threatening] = C(AnimKind.Threatening, 1.9f, new[] { "Raise", "Advance", "Hold" }, new[] { 0.3f, 0.65f, 1f }, true,
                Tr(RP, 0, 0, 0.3f, -140, 1, -140), Tr(RR, 0, 0, 0.3f, 12, 1, 12), Tr(LP, 0, 0, 0.3f, -50, 1, -50), Tr(BP, 0, 0, 0.3f, 20, 1, 20),
                Tr(HP, 0, 0, 0.3f, 22, 1, 22), Tr(BF, 0, 0, 0.65f, 0.55f, 1, 0.55f));
            a[(int)AnimKind.Apologising] = C(AnimKind.Apologising, 1.8f, new[] { "Bow", "Hold", "Offer" }, new[] { 0.3f, 0.7f, 1f }, true,
                Tr(BP, 0, 0, 0.3f, 28, 0.8f, 28, 1, 8), Tr(HP, 0, 0, 0.3f, 25, 0.8f, 25, 1, 10), Tr(LP, 0, 0, 0.3f, -35, 1, -35), Tr(RP, 0, 0, 0.3f, -35, 0.7f, -35, 1, -70),
                Tr(LR, 0, 0, 0.3f, 18, 1, 18), Tr(RR, 0, 0, 0.3f, 18, 1, 18));
            a[(int)AnimKind.Gratitude] = C(AnimKind.Gratitude, 1.5f, new[] { "Bow", "Chest", "Lift" }, new[] { 0.3f, 0.6f, 1f }, true,
                Tr(BP, 0, 0, 0.3f, 20, 0.6f, 14, 1, 0), Tr(HP, 0, 0, 0.3f, 18, 1, -6), Tr(RP, 0, 0, 0.3f, -55, 0.6f, -55, 1, -120), Tr(RR, 0, 0, 0.3f, -35, 0.6f, -35, 1, 25),
                Tr(LP, 0, 0, 0.6f, 0, 1, -120), Tr(LR, 0, 0, 0.6f, 0, 1, 25));
            a[(int)AnimKind.Begging] = C(AnimKind.Begging, 1.9f, new[] { "Kneel", "Hold-out" }, new[] { 0.3f, 1f }, true,
                Tr(BD, 0, 0, 0.3f, -0.45f, 1, -0.45f), Tr(GL, 0, 0, 0.3f, -75, 1, -75), Tr(GR, 0, 0, 0.3f, 80, 1, 80), Tr(LP, 0, 0, 0.35f, -80, 1, -78),
                Tr(RP, 0, 0, 0.35f, -80, 1, -82), Tr(LR, 0, 0, 0.3f, 6, 1, 6), Tr(RR, 0, 0, 0.3f, 6, 1, 6), Tr(HP, 0, 0, 0.3f, -20, 1, -20));
            a[(int)AnimKind.Praying] = C(AnimKind.Praying, 2.6f, new[] { "Kneel", "Raise", "Hold" }, new[] { 0.25f, 0.55f, 1f }, false,
                Tr(BD, 0, 0, 0.25f, -0.45f, 1, -0.45f), Tr(GL, 0, 0, 0.25f, -75, 1, -75), Tr(GR, 0, 0, 0.25f, 80, 1, 80),
                Tr(LP, 0, 0, 0.25f, -60, 0.55f, -150, 1, -150), Tr(RP, 0, 0, 0.25f, -60, 0.55f, -150, 1, -150), Tr(HP, 0, 0, 0.25f, 15, 0.55f, -25, 1, -25), Tr(BP, 0, 0, 0.25f, 14, 1, 5));
            a[(int)AnimKind.Teaching] = C(AnimKind.Teaching, 2.6f, new[] { "Show", "Point", "Hand-over" }, new[] { 0.45f, 0.7f, 1f }, true,
                Tr(RP, 0, 0, 0.1f, -60, 0.2f, -20, 0.3f, -60, 0.4f, -20, 0.45f, -45, 0.55f, -90, 0.7f, -90, 1, -80), Tr(LP, 0, 0, 0.7f, -30, 1, -70),
                Tr(BP, 0, 0, 0.45f, 16, 1, 6), Tr(HY, 0, 0, 0.45f, 0, 0.55f, -15, 1, -5));
            a[(int)AnimKind.Fear] = C(AnimKind.Fear, 1.5f, new[] { "Cower", "Tremble", "Hold" }, new[] { 0.25f, 0.7f, 1f }, false,
                Tr(BP, 0, 0, 0.25f, 22, 1, 22), Tr(BD, 0, 0, 0.25f, -0.2f, 1, -0.2f), Tr(GL, 0, 0, 0.25f, -35, 1, -35), Tr(GR, 0, 0, 0.25f, -35, 1, -35),
                Tr(LP, 0, 0, 0.25f, -140, 1, -140), Tr(RP, 0, 0, 0.25f, -140, 1, -140), Tr(LR, 0, 0, 0.25f, -20, 1, -20), Tr(RR, 0, 0, 0.25f, -20, 1, -20),
                Tr(HY, 0, 0, 0.2f, 25, 0.35f, 20, 0.5f, 30, 0.65f, 20, 1, 25));
            a[(int)AnimKind.Submission] = C(AnimKind.Submission, 2.0f, new[] { "Drop", "Kneel", "Bow" }, new[] { 0.25f, 0.6f, 1f }, true,
                Tr(BD, 0, 0, 0.6f, -0.5f, 1, -0.5f), Tr(GL, 0, 0, 0.6f, -80, 1, -80), Tr(GR, 0, 0, 0.6f, 85, 1, 85), Tr(BP, 0, 0, 0.6f, 5, 1, 30),
                Tr(HP, 0, 0, 0.6f, 5, 1, 40), Tr(LP, 0, 0, 0.3f, -20, 1, -100), Tr(RP, 0, 0, 0.3f, -20, 1, -100), Tr(LR, 0, 0, 0.3f, 10, 1, 25), Tr(RR, 0, 0, 0.3f, 10, 1, 25));
            a[(int)AnimKind.Feeding] = C(AnimKind.Feeding, 1.6f, new[] { "Bend", "Feed", "Rise" }, new[] { 0.3f, 0.75f, 1f }, true,
                Tr(BP, 0, 0, 0.3f, 22, 0.75f, 22, 1, 0), Tr(RP, 0, 0, 0.3f, -75, 0.5f, -95, 0.65f, -75, 0.75f, -95, 1, 0), Tr(LP, 0, 0, 0.3f, -40, 1, -20), Tr(HP, 0, 0, 0.3f, 15, 1, 5));
            a[(int)AnimKind.Tending] = C(AnimKind.Tending, 2.0f, new[] { "Bend", "Tend", "Rise" }, new[] { 0.25f, 0.8f, 1f }, true,
                Tr(BP, 0, 0, 0.25f, 26, 0.8f, 26, 1, 0), Tr(RP, 0, 0, 0.25f, -70, 0.4f, -50, 0.55f, -80, 0.7f, -50, 0.8f, -80, 1, 0), Tr(LP, 0, 0, 0.25f, -45, 1, -45), Tr(HP, 0, 0, 0.25f, 12, 1, 12));
            return a;
        }
    }
}
