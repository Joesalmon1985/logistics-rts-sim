using System.Collections.Generic;
using God.Sim.Divinity;
using UnityEngine;

namespace God.Game
{
    /// <summary>U2a: the compact divine toolbar (data-driven from <see cref="DivineCatalog.Slots"/>), the instruct popover and the commandment template picker.</summary>
    public sealed partial class GodHud
    {
        private const int WinPopover = 141, WinTemplate = 142;

        public bool PopoverOpen { get; private set; }
        public bool TemplateOpen { get; private set; }
        private PowerSlot _popSlot;
        private DivineAct _popAct;
        private TargetScope _popScope;
        private int _popExpiryMul = 1;
        private Rect _popRect, _tplRect;
        private string _popMessage;
        private IReadOnlyList<CommandmentTemplateDef> _templates;
        private int _tplIndex, _tplImportance = 1;

        /// <summary>The scope the open popover will send (tests).</summary>
        public TargetScope PopoverScope { get => _popScope; set => _popScope = value; }
        public DivineAct PopoverAct { get => _popAct; set => _popAct = value; }

        // slot checks against the current selection, cached per tick, selection version and mode
        private readonly Dictionary<string, DivineCatalog.SlotCheck> _slotChecks = new Dictionary<string, DivineCatalog.SlotCheck>();
        private long _slotTick = -1; private int _slotSel = -1; private DivinityMode _slotMode;

        /// <summary>What the gate says about a slot on the current selection (cached once per tick).</summary>
        public DivineCatalog.SlotCheck SlotCheckOf(PowerSlot s)
        {
            long tick = DivineTick; int ver = Boot.Selection.Version; var mode = MythicMode;
            if (tick != _slotTick || ver != _slotSel || mode != _slotMode) { _slotChecks.Clear(); _slotTick = tick; _slotSel = ver; _slotMode = mode; }
            if (_slotChecks.TryGetValue(s.Id, out var c)) return c;
            var sel = Boot.Selection.Snapshot();
            var act = s.Family == SlotFamily.Compel ? DivineCatalog.CompelActs[0].Act : DivineCatalog.InstructActs[0].Act;
            var req = s.Family == SlotFamily.Inspect ? null : PlayerCommandBuilder.Request(Boot.HumanGod, DivineCatalog.Intent(s, act, DivineCatalog.DefaultExpiry(s.Family, Boot.Divine.TicksPerDay)), sel);
            c = s.Family == SlotFamily.Inspect ? new DivineCatalog.SlotCheck(true, null, null) : DivineCatalog.Check(Boot.Player.Builder, req);
            _slotChecks[s.Id] = c;
            return c;
        }

        private static Color Rgb(int hex, float a = 1f) => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, a);

        private GUIStyle _slotStyle;

        private void DrawToolbarPanel(HudStyle st, HudFrame f)
        {
            var slots = VisibleSlots();
            if (_slotStyle == null) _slotStyle = new GUIStyle(st.Btn) { fontSize = 11, wordWrap = false, padding = new RectOffset(2, 2, 2, 2) };
            const float w = 62f, h = 40f, gap = 4f;
            float total = slots.Count * (w + gap) - gap;
            var bar = new Rect((f.Width - total) * 0.5f - 8f, f.Height - 100f, total + 16f, h + 12f);
            f.Block(bar);
            if (f.Repaint) GUI.Box(bar, GUIContent.none, _pill);
            bool disabledMode = MythicMode == DivinityMode.Disabled;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var chk = SlotCheckOf(s);
                bool grey = s.Family != SlotFamily.Inspect && s.Family != SlotFamily.Commandment && !chk.Valid && DivineCatalog.IsUnavailable(chk.Reason);
                var r = new Rect(bar.x + 8f + i * (w + gap), bar.y + 6f, w, h);
                long cost = DivineCatalog.CostOf(Boot.Divine.Costs, s, 1);
                string tip = s.Tip + (cost > 0 ? "\nAbout " + MythicText(cost) + " Mythic" : "") + (grey ? "\n(" + chk.Message + ")" : "");
                var prev = GUI.enabled; GUI.enabled = prev && !grey;
                bool on = Armed == s;
                if (f.Repaint) { var old = GUI.color; GUI.color = Rgb(s.Colour, on ? 1f : 0.75f); GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, 4f), Texture2D.whiteTexture); GUI.color = old; }
                if (GUI.Button(r, new GUIContent(s.Glyph + "\n" + s.Label, tip), on ? st.BtnSel : _slotStyle)) Arm(s.Id);
                GUI.enabled = prev;
            }
            if (disabledMode) GUI.Label(new Rect(bar.x + 8f, bar.y - 20f, 300f, 18f), "Divine power is disabled in this game.", st.Small);
            DrawTemplatePanel(st, f);
        }

        private void DrawPopoverPanel(HudStyle st, HudFrame f)
        {
            if (!PopoverOpen || Armed == null) { PopoverOpen = false; return; }
            var r = new Rect(f.Width * 0.5f - 210f, f.Height - 100f - 330f, 420f, 0f);
            _popRect = GUILayout.Window(WinPopover, r, id => PopoverBody(st), GUIContent.none, st.Window, GUILayout.Width(420f));
            f.Block(_popRect);
        }

        private void PopoverBody(HudStyle st)
        {
            var slot = _popSlot ?? Armed;
            GUILayout.Label(slot.Label + " (no free text: pick an act)", st.Title);
            var sel = Boot.Selection;
            GUILayout.Label("Target: " + sel.Describe(), st.Small);
            // act
            GUILayout.Label("Act", st.Label);
            var acts = slot.Family == SlotFamily.Compel ? DivineCatalog.CompelActs : DivineCatalog.InstructActs;
            int expiry = PopoverExpiry(slot);
            GUILayout.BeginHorizontal();
            int n = 0;
            foreach (var a in acts)
            {
                var req = InstructRequest(slot, a.Act, _popScope, expiry);
                var chk = DivineCatalog.Check(Boot.Player.Builder, req);
                bool grey = !chk.Valid && DivineCatalog.IsUnavailable(chk.Reason);
                var prev = GUI.enabled; GUI.enabled = prev && !grey;
                if (GUILayout.Button(new GUIContent(a.Label, grey ? chk.Message : ""), _popAct == a.Act ? st.BtnSel : st.Btn, GUILayout.Width(66f))) _popAct = a.Act;
                GUI.enabled = prev;
                if (++n % 5 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
            }
            GUILayout.EndHorizontal();
            // scope
            if (slot.Family != SlotFamily.Compel)
            {
                GUILayout.Label("Scope", st.Label);
                GUILayout.BeginHorizontal();
                foreach (var sc in DivineCatalog.Scopes)
                {
                    bool fits = InstructRequest(slot, _popAct, sc.Scope, expiry) != null;
                    var prev = GUI.enabled; GUI.enabled = prev && fits;
                    if (GUILayout.Button(sc.Label, _popScope == sc.Scope ? st.BtnSel : st.Btn)) _popScope = sc.Scope;
                    GUI.enabled = prev;
                }
                GUILayout.EndHorizontal();
            }
            else _popScope = TargetScope.Person;
            // expiry
            GUILayout.BeginHorizontal();
            GUILayout.Label("Expires", st.Label, GUILayout.Width(60f));
            if (GUILayout.Button("-", st.Btn, GUILayout.Width(26f))) _popExpiryMul = Mathf.Max(1, _popExpiryMul - 1);
            GUILayout.Label(slot.Family == SlotFamily.Compel ? expiry + " ticks" : (expiry / Mathf.Max(1, Boot.Divine.TicksPerDay)) + " day(s)", st.Label, GUILayout.Width(90f));
            if (GUILayout.Button("+", st.Btn, GUILayout.Width(26f))) _popExpiryMul = Mathf.Min(5, _popExpiryMul + 1);
            GUILayout.EndHorizontal();
            // check and cost
            var final = InstructRequest(slot, _popAct, _popScope, expiry);
            var check = DivineCatalog.Check(Boot.Player.Builder, final);
            int recips = final != null && final.Persons != null && final.Scope == TargetScope.Group ? final.Persons.Length : 1;
            GUILayout.Label("About " + MythicText(DivineCatalog.CostOf(Boot.Divine.Costs, slot, recips)) + " Mythic" + (check.Valid ? "" : "   Not possible: " + check.Message), check.Valid ? st.Small : st.Small);
            if (!string.IsNullOrEmpty(_popMessage)) GUILayout.Label(_popMessage, st.Small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send", st.BtnSel)) SendPopover();
            if (GUILayout.Button("Cancel", st.Btn)) CancelArmed();
            GUILayout.EndHorizontal();
        }

        private int PopoverExpiry(PowerSlot slot)
        {
            int baseTicks = DivineCatalog.DefaultExpiry(slot.Family, Boot.Divine.TicksPerDay);
            return slot.Family == SlotFamily.Compel ? baseTicks * _popExpiryMul : baseTicks * _popExpiryMul;
        }

        /// <summary>Sends the form as it stands: checked with the gate, then queued. The popover closes on success and shows the refusal otherwise.</summary>
        public DivineSendResult SendPopover()
        {
            var slot = _popSlot ?? Armed;
            if (slot == null) return null;
            var req = InstructRequest(slot, _popAct, _popScope, PopoverExpiry(slot));
            var r = SendRequest(req, slot.Label, slot);
            _popMessage = r.Ok ? null : "Refused: " + (r.Message ?? r.Reason);
            if (r.Ok) { PopoverOpen = false; Armed = null; }
            return r;
        }

        /// <summary>Opens the instruction form on the current selection (the toolbar does it when a target is clicked; tests call it).</summary>
        public void OpenPopover(string slotId, DivineAct act, TargetScope scope)
        {
            var s = DivineCatalog.Find(slotId);
            if (s == null || !DivineCatalog.IsInstruct(s)) return;
            Armed = s; _popSlot = s; PopoverOpen = true; _popAct = act; _popScope = scope; _popExpiryMul = 1; _popMessage = null;
        }

        // ---------------------------------------------------------------- commandment template picker (R11)

        private void DrawTemplatePanel(HudStyle st, HudFrame f)
        {
            if (!TemplateOpen) return;
            if (_templates == null) _templates = CommandmentTemplateList.Provisional();
            var r = new Rect(f.Width * 0.5f - 230f, f.Height - 100f - 300f, 460f, 0f);
            _tplRect = GUILayout.Window(WinTemplate, r, id => TemplateBody(st), GUIContent.none, st.Window, GUILayout.Width(460f));
            f.Block(_tplRect);
        }

        /// <summary>The structured rules of the chosen template (a copy), what the sentence shows and what the command carries.</summary>
        public CommandmentRule[] TemplateRules()
        {
            if (_templates == null) _templates = CommandmentTemplateList.Provisional();
            return (CommandmentRule[])_templates[Mathf.Clamp(_tplIndex, 0, _templates.Count - 1)].Rules.Clone();
        }

        public string TemplateSentence() => CommandmentText.Render(TemplateRules());

        public void PickTemplate(int index, int importance) { _tplIndex = index; _tplImportance = Mathf.Clamp(importance, 0, 2); TemplateOpen = true; }

        /// <summary>Reveals the picked template (structured rules, never text) to the selection, or to everyone when nothing is selected.</summary>
        public DivineSendResult RevealTemplate()
        {
            if (_templates == null) _templates = CommandmentTemplateList.Provisional();
            var t = _templates[Mathf.Clamp(_tplIndex, 0, _templates.Count - 1)];
            var sel = Boot.Selection.Snapshot();
            var intent = new DivineIntent(DivineActs.Reveal, t.Id, CommandmentTemplateList.ImportanceValues[_tplImportance], TemplateRules());
            var req = PlayerCommandBuilder.Request(Boot.HumanGod, intent, sel) ?? new DivineRequest { God = Boot.HumanGod, Act = DivineActs.Reveal, Arg = t.Id, Arg2 = intent.Arg2, Rules = intent.Rules, Node = -1 };
            // a revelation is one command: a group is not expanded (the commandment goes to the whole selection's first person scope)
            if (req.Scope == TargetScope.Group) { req.Scope = TargetScope.Person; req.Persons = new[] { req.Persons[0] }; }
            var r = SendRequest(req, "Reveal \"" + t.Name + "\"", DivineCatalog.Find("law"));
            if (r.Ok) TemplateOpen = false;
            return r;
        }

        private void TemplateBody(HudStyle st)
        {
            GUILayout.Label("Reveal a commandment (pick a template)", st.Title);
            for (int i = 0; i < _templates.Count; i++)
                if (GUILayout.Button(_templates[i].Name, i == _tplIndex ? st.BtnSel : st.Btn)) _tplIndex = i;
            GUILayout.Label(TemplateSentence(), st.Label);
            if (!string.IsNullOrEmpty(_templates[_tplIndex].Note)) GUILayout.Label(_templates[_tplIndex].Note, st.Small);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Importance", st.Label, GUILayout.Width(80f));
            for (int i = 0; i < 3; i++) if (GUILayout.Button(CommandmentTemplateList.ImportanceNames[i], i == _tplImportance ? st.BtnSel : st.Btn)) _tplImportance = i;
            GUILayout.EndHorizontal();
            var law = DivineCatalog.Find("law");
            GUILayout.Label("About " + MythicText(DivineCatalog.CostOf(Boot.Divine.Costs, law)) + " Mythic. Goes to the selection, or to everyone.", st.Small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reveal", st.BtnSel)) RevealTemplate();
            if (GUILayout.Button("Cancel", st.Btn)) CancelArmed();
            GUILayout.EndHorizontal();
        }
    }
}
