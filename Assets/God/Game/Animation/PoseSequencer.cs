using System;
using System.Collections.Generic;

namespace God.Game
{
    /// <summary>
    /// One actor's queue of cues played in order on view time (pure: no engine types; the caller passes the clock). A cue's
    /// view length is its clip's length scaled by intensity and never shorter than the sim's own duration; a hand-over
    /// (Giving, Receiving) pre-empts whatever is queued so both partners stay in lockstep. At most <see cref="MaxQueue"/> cues
    /// wait; the oldest waiting one is dropped first (at 10x many events arrive). Nothing here changes the sim.
    /// </summary>
    public sealed class PoseSequencer
    {
        public const int MaxQueue = 4;
        public const float BlendIn = 0.15f, BlendOut = 0.2f;

        private struct Item { public AnimationCue Cue; public double Start; public float Seconds; }
        private readonly List<Item> _q = new List<Item>(MaxQueue + 1);

        public AnimationCue Active { get; private set; }
        public PoseClip Clip { get; private set; }
        /// <summary>Clip time 0..1 of the active cue.</summary>
        public float T { get; private set; }
        /// <summary>Blend weight 0..1 of the active cue (ease in and out).</summary>
        public float Weight { get; private set; }
        public bool Busy => Active != null || _q.Count > 0;
        public int Pending => _q.Count;

        /// <summary>The view length of a cue in seconds.</summary>
        public static float SecondsOf(AnimationCue c)
        {
            var clip = PoseClips.Of(c.Kind);
            float s = clip.Seconds * (0.8f + 0.4f * c.Intensity);
            return Math.Max(s, c.DurationTicks / (float)GestureAnimationPlanner.TicksPerSecond);
        }

        public void Enqueue(AnimationCue c, double now)
        {
            if (c == null || PoseClips.Of(c.Kind) == null) return;
            bool lockstep = c.Kind == AnimKind.Giving || c.Kind == AnimKind.Receiving;
            if (lockstep) { _q.Clear(); Active = null; }
            double start = now;
            if (_q.Count > 0) { var l = _q[_q.Count - 1]; start = l.Start + l.Seconds; }
            else if (Active != null) start = _activeEnd;
            if (start < now) start = now;
            _q.Add(new Item { Cue = c, Start = start, Seconds = SecondsOf(c) });
            while (_q.Count > MaxQueue) _q.RemoveAt(0);
            if (Active == null) Advance(now);
        }

        private double _activeStart, _activeEnd;

        /// <summary>Moves the clock; returns whether a cue is active.</summary>
        public bool Advance(double now)
        {
            if (Active != null && now >= _activeEnd) { Active = null; Clip = null; Weight = 0f; }
            if (Active == null && _q.Count > 0 && now >= _q[0].Start)
            {
                var it = _q[0]; _q.RemoveAt(0);
                if (now < it.Start + it.Seconds) { Active = it.Cue; Clip = PoseClips.Of(it.Cue.Kind); _activeStart = it.Start; _activeEnd = it.Start + it.Seconds; }
                else return Advance(now); // a stale cue (the view fell behind): skip it
            }
            if (Active == null) return false;
            double len = _activeEnd - _activeStart;
            T = (float)((now - _activeStart) / len);
            float inW = Math.Min(1f, (float)(now - _activeStart) / BlendIn), outW = Math.Min(1f, (float)(_activeEnd - now) / BlendOut);
            Weight = Math.Max(0f, Math.Min(inW, outW));
            return true;
        }

        public void Clear() { _q.Clear(); Active = null; Clip = null; Weight = 0f; }
    }
}
