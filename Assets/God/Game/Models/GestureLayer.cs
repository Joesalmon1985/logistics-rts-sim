using System.Collections.Generic;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Population;
using God.Sim.Social;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Draws the gestures people make (COMMUNICATION 4.9; OWNER: M5), driven only by the sim's <see cref="GestureMade"/>
    /// events: for a few seconds after a gesture, the speaker raises an arm, a line runs from it to the person it addresses
    /// (none for a message to anyone watching), and the composed message is written above its head ("offer: wants 1 fish,
    /// gives 3 berries"). Called by <see cref="NpcLayer"/> once per drawn person per frame. It reads the sim and its events
    /// and decides nothing; with no events it draws nothing.
    /// </summary>
    public static class GestureLayer
    {
        /// <summary>How long (seconds of view time) a gesture stays drawn.</summary>
        public const float Show = 3.5f;

        private sealed class Shown
        {
            public GestureMade E;
            public float Since = -1;
            public string Text;
        }

        private static GodBootstrap _boot;
        private static readonly Dictionary<int, Shown> _bySpeaker = new Dictionary<int, Shown>();
        private static Font _font;

        /// <summary>Gestures received from the sim so far (tests).</summary>
        public static int Received { get; private set; }

        /// <summary>The gesture currently drawn for a person (tests and the inspector); null when none.</summary>
        public static GestureMade Current(int npc) => _bySpeaker.TryGetValue(npc, out var s) ? s.E : null;

        /// <summary>Feeds a sim event (the bootstrap's event pump; tests may call it directly).</summary>
        public static void OnEvent(SimEvent e)
        {
            if (!(e is GestureMade g)) return;
            Received++;
            _bySpeaker[g.Speaker] = new Shown { E = g, Text = Words(g) };
        }

        private static void Subscribe()
        {
            var b = GodBootstrap.Instance;
            if (b == _boot) return;
            if (_boot != null) _boot.SimEventRaised -= OnEvent;
            _boot = b;
            _bySpeaker.Clear();
            if (_boot != null) _boot.SimEventRaised += OnEvent;
        }

        /// <summary>The composed message in words, part by part (act, topic, quantity, counter).</summary>
        public static string Words(GestureMade g)
        {
            string act = g.Act.ToString().ToLowerInvariant();
            switch (g.Act)
            {
                case GestureAct.Offer:
                    if (g.TopicKind != TopicKind.Ware) return "offer: gives " + g.CounterSub + " " + Wares.Name((Ware)g.CounterId);
                    return "offer: wants " + g.Quantity + " " + Wares.Name((Ware)g.TopicId) + ", gives " + g.CounterSub + " " + Wares.Name((Ware)g.CounterId);
                case GestureAct.NeedFood: return "need food";
                case GestureAct.NeedHelp: return "need help" + (g.TopicKind == TopicKind.Ware ? ": " + g.Quantity + " " + Wares.Name((Ware)g.TopicId) : "");
                case GestureAct.Cooperate: return "cooperate" + (g.TopicKind == TopicKind.Ware ? ": " + g.Quantity + " " + Wares.Name((Ware)g.TopicId) : "");
                case GestureAct.Warn: return "warn!" + (g.TopicKind == TopicKind.Place ? " danger there" : "");
                default: return act;
            }
        }

        /// <summary>
        /// Accessibility caption: plain words from the observable fields of the gesture only (what a watcher could have seen);
        /// it carries no truth flag, so it never reveals a lie.
        /// </summary>
        public static string Caption(GestureMade g)
        {
            switch (g.Act)
            {
                case GestureAct.Offer:
                    if (g.TopicKind != TopicKind.Ware) return "offers " + g.CounterSub + " " + Wares.Name((Ware)g.CounterId);
                    return "offers " + g.CounterSub + " " + Wares.Name((Ware)g.CounterId) + " for " + g.Quantity + " " + Wares.Name((Ware)g.TopicId);
                case GestureAct.NeedFood: return "asks for food";
                case GestureAct.NeedHelp: return "asks for help";
                case GestureAct.Cooperate: return "offers to work together";
                case GestureAct.Warn: return "warns of danger";
                case GestureAct.Accept: return "agrees";
                case GestureAct.Refuse: return "refuses";
                default: return g.Act.ToString().ToLowerInvariant();
            }
        }

        private static Color ColourOf(GestureAct a)
        {
            switch (a)
            {
                case GestureAct.Accept: case GestureAct.Cooperate: return new Color(0.35f, 0.85f, 0.35f);
                case GestureAct.Refuse: return new Color(0.9f, 0.35f, 0.3f);
                case GestureAct.Warn: return new Color(1f, 0.6f, 0.1f);
                case GestureAct.NeedFood: case GestureAct.NeedHelp: return new Color(0.95f, 0.85f, 0.4f);
                default: return new Color(0.85f, 0.9f, 1f);
            }
        }

        public static void Apply(Transform figure, Npc n, GodState s, float time)
        {
            Subscribe();
            var label = figure.Find("GestureLabel");
            var line = figure.Find("GestureLine");
            if (!_bySpeaker.TryGetValue(n.Id, out var sh) || !n.Alive)
            {
                if (label != null) label.gameObject.SetActive(false);
                if (line != null) line.gameObject.SetActive(false);
                return;
            }
            if (sh.Since < 0) sh.Since = time;
            if (time - sh.Since > Show) { _bySpeaker.Remove(n.Id); return; }
            var col = ColourOf(sh.E.Act);
            // the body animation is PoseLayer's (U3b vocabulary); this layer keeps only the developer text and line
            // text and line only for the developer decode overlay (M5.18) or, text only, the captions option
            bool decode = DevOverlays.DecodedMessages;
            if (!decode && !DevOverlays.Captions)
            {
                if (label != null) label.gameObject.SetActive(false);
                if (line != null) line.gameObject.SetActive(false);
                return;
            }
            // the message as text above the head (billboarded)
            if (label == null)
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var go = new GameObject("GestureLabel");
                go.transform.SetParent(figure, false);
                go.transform.localPosition = new Vector3(0, 3.4f, 0);
                var tm = go.AddComponent<TextMesh>();
                tm.font = _font; tm.fontSize = 48; tm.characterSize = 0.08f; tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center;
                go.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.TextMaterial(_font);
                label = go.transform;
            }
            label.gameObject.SetActive(true);
            var text = label.GetComponent<TextMesh>();
            text.text = decode ? sh.Text : Caption(sh.E); text.color = col;
            var cam = Camera.main;
            if (cam != null) label.rotation = Quaternion.LookRotation(label.position - cam.transform.position);
            // a line to the addressee (developer overlay only)
            if (!decode)
            {
                if (line != null) line.gameObject.SetActive(false);
                return;
            }
            var p = s.Population;
            var target = sh.E.Target != 0 ? p?.Find(sh.E.Target) : null;
            if (target == null || !target.Alive)
            {
                if (line != null) line.gameObject.SetActive(false);
                return;
            }
            if (line == null)
            {
                var go = new GameObject("GestureLine");
                go.transform.SetParent(figure, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true; lr.positionCount = 2; lr.widthMultiplier = 0.12f;
                lr.sharedMaterial = MaterialFactory.SharedUnlit(Color.white);
                line = go.transform;
            }
            line.gameObject.SetActive(true);
            var r = line.GetComponent<LineRenderer>();
            var g = s.Territory.Grid;
            var to = new Vector3((float)g.X(target.Node), 2.2f, (float)g.Z(target.Node)) + new Vector3(((target.Id * 37) % 7 - 3) * 0.35f, 0, ((target.Id * 53) % 7 - 3) * 0.35f);
            r.startColor = col; r.endColor = new Color(col.r, col.g, col.b, 0.3f);
            r.SetPosition(0, figure.position + new Vector3(0, 2.2f, 0));
            r.SetPosition(1, to);
        }
    }
}
