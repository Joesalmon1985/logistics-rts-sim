using System.Collections.Generic;

namespace God.Game
{
    /// <summary>The distinct minimal cues of Tier 1 (R10): one shape and colour per kind. The rich effects are U3b's.</summary>
    public enum CueKind { None, Suggest, Command, Compel, Heal, Smite, Create, Death }

    /// <summary>One live cue: where (a person, or a node when Person is 0) and when it began (real seconds).</summary>
    public struct Cue
    {
        public CueKind Kind;
        public int Person, Node;
        public float Born;
    }

    /// <summary>
    /// The live cues (pure, no Unity types): spawned from <see cref="CueSignal"/>s, expiring after <see cref="Life"/> real seconds. The HUD
    /// draws each as a small procedural marker whose shape depends on the kind: suggest a soft blue ring rising, command a gold double ring,
    /// compulsion violet bars around the body, heal a green cross, smite a red bolt from above, create a pale sparkle disc.
    /// </summary>
    public sealed class DivineCues
    {
        public const float Life = 1.8f;
        public const int Max = 48;
        private readonly List<Cue> _live = new List<Cue>();

        public IReadOnlyList<Cue> Live => _live;
        /// <summary>Cues spawned in total, by kind (tests).</summary>
        public readonly int[] Spawned = new int[8];

        public void Spawn(CueSignal s, float now)
        {
            if (s.Kind == CueKind.None || s.Kind == CueKind.Death) return;
            Spawned[(int)s.Kind]++;
            _live.Add(new Cue { Kind = s.Kind, Person = s.Person, Node = s.Node, Born = now });
            while (_live.Count > Max) _live.RemoveAt(0);
        }

        public void Prune(float now) { _live.RemoveAll(c => now - c.Born > Life); }

        public int CountOf(CueKind k) { int n = 0; foreach (var c in _live) if (c.Kind == k) n++; return n; }

        /// <summary>0 at birth to 1 at the end of its life.</summary>
        public static float Age01(Cue c, float now) => UnityEngine.Mathf.Clamp01((now - c.Born) / Life);

        /// <summary>The colour of a kind as 0xRRGGBB (the toolbar uses the same).</summary>
        public static int Colour(CueKind k)
        {
            switch (k)
            {
                case CueKind.Suggest: return DivineCatalog.ColSuggest;
                case CueKind.Command: return DivineCatalog.ColCommand;
                case CueKind.Compel: return DivineCatalog.ColCompel;
                case CueKind.Heal: return DivineCatalog.ColHeal;
                case CueKind.Smite: return DivineCatalog.ColSmite;
                case CueKind.Create: return DivineCatalog.ColCreate;
                default: return 0xFFFFFF;
            }
        }
    }
}
