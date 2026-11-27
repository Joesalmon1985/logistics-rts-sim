using System;
using System.Collections.Generic;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>What a registered HUD drawer is told each OnGUI pass: the HUD's virtual size (units of the scaled GUI, top-left origin) and whether this is the repaint.</summary>
    public readonly struct HudFrame
    {
        public readonly float Width, Height, StripHeight;
        public readonly bool Repaint;
        private readonly GodHud _hud;
        public HudFrame(GodHud hud, float w, float h, float strip, bool repaint) { _hud = hud; Width = w; Height = h; StripHeight = strip; Repaint = repaint; }
        /// <summary>Marks a rectangle as HUD: pointer presses on it do not reach the map. Call every repaint for each panel drawn.</summary>
        public void Block(Rect r) { if (Repaint) _hud.BlockPointer(r); }
    }

    /// <summary>One tab of the people card. The built-in tabs (Mind, Household, Relations) are registrations like any other (R9).</summary>
    public sealed class PersonTabDef
    {
        public string Id, Title;
        public int Order;
        /// <summary>Null: always shown.</summary>
        public Func<NpcInspection, bool> Visible;
        public Action<HudStyle, NpcInspection> Draw;
    }

    /// <summary>
    /// The registration hook of the HUD (LEAD_RULINGS R9): other packages add panels and people-card tabs without editing
    /// GodHud.cs. Register from <see cref="GodHud.Created"/> (static event, raised as the HUD is built) or on <c>GodBootstrap.Hud</c>.
    /// Built-in tab orders: Mind 0, Household 10, Relations 20; choose a larger order for new tabs (U2 Divine, U5 Economy, Life, Family, I6 industrial).
    /// </summary>
    public sealed partial class GodHud
    {
        /// <summary>Raised once for every HUD built (before its first frame), so a package can register its additions.</summary>
        public static event Action<GodHud> Created;

        private sealed class PanelReg { public string Id; public int Order; public Action<HudStyle, HudFrame> Draw; }
        private readonly List<PanelReg> _panels = new List<PanelReg>();
        private readonly List<PersonTabDef> _personTabs = new List<PersonTabDef>();
        private readonly List<PersonTabDef> _visibleTabs = new List<PersonTabDef>();

        private void InitHooks()
        {
            RegisterPersonTab("mind", "Mind", 0, DrawMindTab);
            RegisterPersonTab("household", "Household", 10, DrawHouseholdTab);
            RegisterPersonTab("relations", "Relations", 20, DrawRelationsTab);
            Created?.Invoke(this);
        }

        /// <summary>Adds (or replaces, by id) a panel drawn every OnGUI pass after the built-in ones, in order of <paramref name="order"/>.</summary>
        public void RegisterPanel(string id, int order, Action<HudStyle, HudFrame> draw)
        {
            _panels.RemoveAll(p => p.Id == id);
            _panels.Add(new PanelReg { Id = id, Order = order, Draw = draw });
            _panels.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id));
        }

        public void UnregisterPanel(string id) { _panels.RemoveAll(p => p.Id == id); }

        public int PanelCount => _panels.Count;

        /// <summary>Adds (or replaces, by id) a tab of the people card. <paramref name="visible"/> may hide it for some people.</summary>
        public void RegisterPersonTab(string id, string title, int order, Action<HudStyle, NpcInspection> draw, Func<NpcInspection, bool> visible = null)
        {
            _personTabs.RemoveAll(t => t.Id == id);
            _personTabs.Add(new PersonTabDef { Id = id, Title = title, Order = order, Draw = draw, Visible = visible });
            _personTabs.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id));
        }

        public void UnregisterPersonTab(string id) { _personTabs.RemoveAll(t => t.Id == id); }

        /// <summary>Ids of the registered people-card tabs in display order.</summary>
        public List<string> PersonTabIds() { var l = new List<string>(); foreach (var t in _personTabs) l.Add(t.Id); return l; }

        private List<PersonTabDef> VisiblePersonTabs(NpcInspection c)
        {
            _visibleTabs.Clear();
            foreach (var t in _personTabs) if (t.Visible == null || t.Visible(c)) _visibleTabs.Add(t);
            return _visibleTabs;
        }

        internal void BlockPointer(Rect r) { _ui.Add(r); }

        private void DrawRegisteredPanels(HudStyle st, float vw, float vh, float stripH, bool repaint)
        {
            var frame = new HudFrame(this, vw, vh, stripH, repaint);
            for (int i = 0; i < _panels.Count; i++) _panels[i].Draw(st, frame);
        }
    }
}
