using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Short toasts for the big events (war declared, peace made, HQ captured, faction eliminated, Citadel raised): a faction
    /// colour swatch and one line, fading after a few seconds. Fed by the bootstrap's sim events; drawn under the cloud strip.
    /// </summary>
    public sealed class ToastFeed : MonoBehaviour
    {
        public const float Life = 4.5f;
        public const int MaxShown = 4;

        public struct Item
        {
            public string Text; public Color Colour; public float Born;
        }

        private readonly List<Item> _items = new List<Item>();
        private GodBootstrap _boot;
        private GUIStyle _style;

        public IReadOnlyList<Item> Items => _items;
        public int ActiveCount { get { Prune(); return _items.Count; } }

        public static ToastFeed Create(GodBootstrap boot)
        {
            var go = new GameObject("ToastFeed");
            go.transform.SetParent(boot.transform, false);
            var t = go.AddComponent<ToastFeed>();
            t._boot = boot;
            boot.SimEventRaised += t.OnEvent;
            return t;
        }

        private void OnDestroy() { if (_boot != null) _boot.SimEventRaised -= OnEvent; }

        public void Add(string text, Color colour)
        {
            _items.Add(new Item { Text = text, Colour = colour, Born = Time.unscaledTime });
            while (_items.Count > MaxShown) _items.RemoveAt(0);
        }

        private void Prune() { _items.RemoveAll(i => Time.unscaledTime - i.Born > Life); }

        /// <summary>The line for a sim event, or null when it is not one of the big ones. Colour is the acting faction's.</summary>
        public static string Describe(SimEvent e, out Faction who)
        {
            who = Faction.F0;
            switch (e)
            {
                case WarDeclared w: who = w.By; return FactionInfo.Name(w.By) + " declares war on " + FactionInfo.Name(w.Against);
                case PeaceMade p: who = p.A; return FactionInfo.Name(p.A) + " and " + FactionInfo.Name(p.B) + " make peace";
                case HqCaptured c:
                    who = c.To;
                    return FactionInfo.Name(c.To) + " captures " + FactionInfo.Name(c.From) + "'s " + (c.Citadel ? "Citadel" : "HQ");
                case FactionEliminated f: who = f.By >= 0 ? (Faction)f.By : f.Faction; return FactionInfo.Name(f.Faction) + " is out of the game";
                case CitadelCompleted cc: who = cc.Faction; return FactionInfo.Name(cc.Faction) + " raises a Citadel";
                default: return null;
            }
        }

        private void OnEvent(SimEvent e)
        {
            if (_boot.GodOnly) return; // U2a: the god-only game uses aggregated alerts, not toasts
            string text = Describe(e, out var who);
            if (text == null) return;
            // The eliminated faction is shown in its own colour.
            if (e is FactionEliminated fe) who = fe.Faction;
            Add(text, FactionLook.Colour(who));
        }

        private void OnGUI()
        {
            if (_boot == null || _items.Count == 0) return;
            Prune();
            if (_items.Count == 0) return;
            var st = HudStyle.Get();
            if (_style == null) _style = new GUIStyle(st.Window) { padding = new RectOffset(34, 16, 5, 7), border = new RectOffset(24, 24, 24, 24), fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { background = st.Panel, textColor = HudStyle.Ink } };
            float s = _boot.Hud != null ? _boot.Hud.Scale : 1f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float vw = Screen.width / s, vh = Screen.height / s;
            float y = vh * 0.13f + 52f;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var it = _items[i];
                float age = Time.unscaledTime - it.Born;
                float a = Mathf.Clamp01(Mathf.Min(age / 0.2f, (Life - age) / 1.0f));
                var c = new GUIContent(it.Text);
                float w = _style.CalcSize(c).x + 6f;
                var r = new Rect((vw - w) * 0.5f, y, w, 30f);
                var old = GUI.color; GUI.color = new Color(1, 1, 1, a);
                GUI.Box(r, c, _style);
                if (Event.current.type == EventType.Repaint)
                    GUI.DrawTexture(new Rect(r.x + 13f, r.y + 8f, 14f, 14f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, it.Colour, Vector4.zero, new Vector4(4, 4, 4, 4));
                GUI.color = old;
                y += 34f;
            }
        }
    }
}
