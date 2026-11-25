using System.Collections.Generic;
using System.Text;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The trade half of the HUD (P8): the Trade panel (pick a faction, give and want wares with steppers, send an
    /// envoy), the offer card for an envoy that reached you (accept or refuse within 60 s), the card of a clicked envoy and
    /// the little trade log. Every action is a public method that only sends sim commands. Reads the sim state; never
    /// changes it.
    /// </summary>
    public sealed partial class GodHud
    {
        private const int WinTrade = 105, WinOffer = 106;

        // ---------------------------------------------------------------- panel state

        /// <summary>True while the Trade panel is open.</summary>
        public bool TradeOpen { get; private set; }
        /// <summary>The faction the offer goes to, or null when none is picked.</summary>
        public Faction? TradeTo { get; private set; }
        /// <summary>Units of each ware to give and to ask for (by <see cref="Ware"/> number). At most 8 in total each way.</summary>
        public int[] TradeGive { get; } = new int[Wares.Count];
        public int[] TradeWant { get; } = new int[Wares.Count];
        /// <summary>The envoy whose card is open (clicked in the world), 0 for none.</summary>
        public int SelectedEnvoy { get; private set; }

        private Rect _tradeRect, _offerRect;
        private readonly List<LogLine> _tradeLog = new List<LogLine>();
        private readonly HashSet<int> _logged = new HashSet<int>();

        private struct LogLine { public string Text; public float Until; }

        /// <summary>The trade log lines still showing, oldest first.</summary>
        public IEnumerable<string> TradeLogLines
        {
            get { foreach (var l in _tradeLog) if (Time.unscaledTime < l.Until) yield return l.Text; }
        }

        // ---------------------------------------------------------------- actions

        /// <summary>Opens or closes the Trade panel. Opening picks the first faction you can trade with when none is picked.</summary>
        public void ToggleTrade()
        {
            if (!CanAct) { TradeOpen = false; return; }
            TradeOpen = !TradeOpen;
            if (TradeOpen) EnsurePartner();
        }

        public void CloseTrade() { TradeOpen = false; }

        /// <summary>Factions you can send an envoy to: in the game, not eliminated, not you and not at war with you.</summary>
        public List<Faction> TradePartners()
        {
            var l = new List<Faction>();
            if (Watching) return l;
            var fs = Sim.State.Factions;
            for (int i = 0; i < Sim.State.FactionCount; i++)
            {
                var f = (Faction)i;
                if (f == Me || fs.IsEliminated(f) || fs.AtWar(Me, f)) continue;
                l.Add(f);
            }
            return l;
        }

        private void EnsurePartner()
        {
            var p = TradePartners();
            if (p.Count == 0) { TradeTo = null; return; }
            if (!TradeTo.HasValue || !p.Contains(TradeTo.Value)) TradeTo = p[0];
        }

        public void SelectTradePartner(Faction f)
        {
            if (TradePartners().Contains(f)) TradeTo = f;
        }

        /// <summary>The most of a ware one of your HQs has free to send (an offer leaves from one HQ).</summary>
        public int MaxGive(Ware w)
        {
            int best = 0;
            foreach (var hq in Sim.State.Economy.HqsOf(Me)) best = Mathf.Max(best, TradeRules.FreeStock(hq, w));
            return best;
        }

        public int TradeGiveTotal { get { int n = 0; foreach (int c in TradeGive) n += c; return n; } }
        public int TradeWantTotal { get { int n = 0; foreach (int c in TradeWant) n += c; return n; } }

        /// <summary>
        /// The + and - steppers: changes the give or want count of a ware by <paramref name="delta"/>. Keeps each side to
        /// 8 wares in total, the give side to what one HQ has free, and a ware on one side only. Returns false when nothing changed.
        /// </summary>
        public bool AdjustTrade(bool give, Ware ware, int delta)
        {
            if (!TradeTuning.IsTradable(ware)) return false;
            var mine = give ? TradeGive : TradeWant; var other = give ? TradeWant : TradeGive;
            int i = (int)ware;
            int total = give ? TradeGiveTotal : TradeWantTotal;
            int n = Mathf.Clamp(mine[i] + delta, 0, TradeTuning.MaxWaresPerSide);
            n = Mathf.Min(n, mine[i] + (TradeTuning.MaxWaresPerSide - total));
            if (give) n = Mathf.Min(n, Mathf.Max(MaxGive(ware), 0));
            if (n < 0 || n == mine[i]) return false;
            mine[i] = n;
            if (n > 0) other[i] = 0;
            return true;
        }

        public void ClearTradeLines() { System.Array.Clear(TradeGive, 0, TradeGive.Length); System.Array.Clear(TradeWant, 0, TradeWant.Length); }

        private static WareCount[] Lines(int[] counts)
        {
            var l = new List<WareCount>();
            for (int w = 1; w < Wares.Count; w++) if (counts[w] > 0) l.Add(new WareCount((Ware)w, counts[w]));
            return l.ToArray();
        }

        /// <summary>"3 log for 2 brick" for the offer being built.</summary>
        public string TradeSummary() => WaresText(Lines(TradeGive)) + " for " + WaresText(Lines(TradeWant));

        public static string WaresText(WareCount[] a)
        {
            if (a == null || a.Length == 0) return "nothing";
            var sb = new StringBuilder();
            foreach (var l in a) { if (sb.Length > 0) sb.Append(", "); sb.Append(l.Count).Append(' ').Append(Wares.Name(l.Ware)); }
            return sb.ToString();
        }

        /// <summary>Sends an envoy with the offer in the panel. Refusals show as toasts. The panel keeps its faction and clears the amounts after a success.</summary>
        public CommandResult SendTrade()
        {
            if (!CanAct) return Refuse(Reasons.Disabled);
            EnsurePartner();
            if (!TradeTo.HasValue)
            {
                var r0 = CommandResult.Reject(TradeReasons.NoTargetHq, "There is nobody to trade with.");
                Toast(r0.Message); return r0;
            }
            var r = Send(new OfferTrade(Me, TradeTo.Value, Lines(TradeGive), Lines(TradeWant)));
            if (r.Ok) { Toast("Envoy sent to " + FactionLook.Name(TradeTo.Value) + ": " + TradeSummary()); ClearTradeLines(); }
            return r;
        }

        /// <summary>Answers an offer that reached you.</summary>
        public CommandResult AnswerOffer(int offerId, bool accept)
        {
            var r = Send(new AnswerTrade(Me, offerId, accept));
            if (r.Ok && SelectedEnvoy == offerId) SelectedEnvoy = 0;
            return r;
        }

        /// <summary>Offers waiting for your answer, oldest first.</summary>
        public List<TradeOffer> PendingOffers() => Watching ? new List<TradeOffer>() : Sim.State.Trade.PendingFor(Me);

        /// <summary>The offer the offer card shows: the clicked envoy's when it is waiting for you, else the oldest waiting one, else null.</summary>
        public TradeOffer CardOffer()
        {
            var tr = Sim.State.Trade;
            if (SelectedEnvoy != 0)
            {
                var o = tr.OfferById(SelectedEnvoy);
                if (o != null && o.To == Me && o.Stage == OfferStage.Pending) return o;
            }
            var p = PendingOffers();
            return p.Count > 0 ? p[0] : null;
        }

        /// <summary>Opens the card of an envoy (clicking it in the world). The card answers it when it waits for you.</summary>
        public void SelectEnvoy(int id)
        {
            if (Sim.State.Trade.EnvoyById(id) == null) return;
            ClearSelection(); SelectedEnvoy = id;
        }

        public void ClearEnvoy() { SelectedEnvoy = 0; }

        /// <summary>The envoy drawn nearest the cursor (judged on screen), or 0.</summary>
        public int PickEnvoy(Vector2 mouse)
        {
            int best = 0; float bestD = float.MaxValue;
            foreach (var e in Sim.State.Trade.Envoys)
            {
                var p = ModelKit.OnGround(e.X / 1000f, e.Z / 1000f, 1.6f);
                var sp = Cam.WorldToScreenPoint(p);
                if (sp.z <= 0f) continue;
                float rad = Screen.height * 0.035f;
                float d = Vector2.Distance(mouse, new Vector2(sp.x, sp.y));
                if (d < rad && d < bestD) { bestD = d; best = e.Id; }
            }
            return best;
        }

        // ---------------------------------------------------------------- trade log

        /// <summary>One line of the log for a finished trade that involves <paramref name="me"/>, or null.</summary>
        public static string LogText(TradeRecord r, Faction me)
        {
            string give = WaresText(r.Give), want = WaresText(r.Want);
            if (r.From == me)
            {
                string to = FactionLook.Name(r.To);
                switch (r.Outcome)
                {
                    case TradeOutcome.Accepted: return to + " accepted: " + give + " for " + want;
                    case TradeOutcome.Lost: return "Your envoy to " + to + " was lost: " + give;
                    default:
                        switch (r.Cause)
                        {
                            case RefusalCause.Timeout: return to + " did not answer: " + give + " for " + want;
                            case RefusalCause.CannotPay: return to + " could not pay: " + give + " for " + want;
                            case RefusalCause.War: return "War stopped the envoy to " + to;
                            case RefusalCause.TargetGone: return to + " is gone: your envoy comes home";
                            default: return to + " refused: " + give + " for " + want;
                        }
                }
            }
            if (r.To == me)
            {
                string from = FactionLook.Name(r.From);
                switch (r.Outcome)
                {
                    case TradeOutcome.Accepted: return "You accepted " + from + "'s offer: " + give + " for " + want;
                    case TradeOutcome.Refused:
                        return r.Cause == RefusalCause.Timeout ? from + "'s offer ran out of time" : "You refused " + from + "'s offer: " + give + " for " + want;
                }
            }
            return null;
        }

        private void RefreshTradeLog()
        {
            if (Watching) return;
            var tr = Sim.State.Trade;
            foreach (var r in tr.History)
            {
                if (!_logged.Add(r.OfferId)) continue;
                string text = LogText(r, Me);
                if (text == null) continue;
                _tradeLog.Add(new LogLine { Text = text, Until = Time.unscaledTime + 45f });
                if (_tradeLog.Count > 4) _tradeLog.RemoveAt(0);
                if (r.From == Me && r.Outcome != TradeOutcome.Accepted) Toast(text);
            }
            if (SelectedEnvoy != 0 && tr.EnvoyById(SelectedEnvoy) == null) SelectedEnvoy = 0;
            if (TradeOpen) { if (!CanAct) TradeOpen = false; else EnsurePartner(); }
        }

        // ---------------------------------------------------------------- drawing

        /// <summary>The Trade button of the cloud strip (a tile like the build tools). Returns the x of the next one.</summary>
        private float DrawTradeTile(HudStyle st, float x, float y, float w, float h, bool repaint)
        {
            var r = new Rect(x, y, w, h);
            int waiting = PendingOffers().Count;
            string tip = "Trade\nSend an envoy with goods to another faction. If they accept, the goods are exchanged. Envoys can be killed by enemy soldiers.";
            if (GUI.Button(r, new GUIContent("Trade", tip), TradeOpen ? st.TileSel : st.Tile)) ToggleTrade();
            if (repaint)
            {
                var sw = new Rect(r.x + 9, r.y + 6, r.width - 18, r.height * 0.40f);
                GUI.DrawTexture(sw, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.92f, 0.78f, 0.34f), Vector4.zero, new Vector4(7, 7, 7, 7));
                float dd = sw.height * 0.5f;
                GUI.DrawTexture(new Rect(sw.center.x - dd * 0.5f, sw.center.y - dd * 0.5f, dd, dd), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.55f, 0.36f, 0.18f), Vector4.zero, new Vector4(dd, dd, dd, dd));
                if (waiting > 0)
                    GUI.DrawTexture(new Rect(r.xMax - 14f, r.y - 3f, 16f, 16f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, HudStyle.Bad, Vector4.zero, new Vector4(8, 8, 8, 8));
            }
            return x + w + 5f;
        }

        private void DrawTrade(HudStyle st, float vw, float vh, float stripH, bool repaint)
        {
            if (Watching) return;
            if (SelectedBuilding != 0 || SelectedNode >= 0) SelectedEnvoy = 0;

            // The log, under the standings.
            float ly = stripH + 190f;
            foreach (var line in TradeLogLines)
            {
                var c = new GUIContent(line);
                var size = _tiny.CalcSize(c);
                var rr = new Rect(14f, ly, size.x + 22f, 22f);
                GUI.Box(rr, GUIContent.none, _pill);
                GUI.Label(new Rect(rr.x + 11f, rr.y + 2f, size.x + 10f, 18f), c, _tiny);
                if (repaint) _ui.Add(rr);
                ly += 24f;
            }

            if (!CanAct) return;
            if (TradeOpen)
            {
                var r = new Rect(vw - 452f, stripH + 8f, 440f, 0f);
                _tradeRect = GUILayout.Window(WinTrade, r, id => TradePanel(st), GUIContent.none, st.Window, GUILayout.Width(440f));
                if (repaint) _ui.Add(_tradeRect);
            }

            var offer = CardOffer();
            var env = offer == null && SelectedEnvoy != 0 ? Sim.State.Trade.EnvoyById(SelectedEnvoy) : null;
            if (offer != null || env != null)
            {
                Rect r;
                var e2 = offer != null ? Sim.State.Trade.EnvoyById(offer.Id) : env;
                if (SelectedEnvoy != 0 && e2 != null && e2.Id == SelectedEnvoy)
                    r = CardRect(ModelKit.OnGround(e2.X / 1000f, e2.Z / 1000f, 3f), _offerRect, 300f, stripH, vw, vh);
                else
                    r = new Rect((vw - 300f) * 0.5f - 160f, vh - (_offerRect.height > 1f ? _offerRect.height : 190f) - 76f, 300f, 0f);
                _offerRect = GUILayout.Window(WinOffer, r, id => OfferCard(st, offer, e2), GUIContent.none, st.Window, GUILayout.Width(300f));
                if (repaint) _ui.Add(_offerRect);
            }
        }

        private void TradePanel(HudStyle st)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trade", st.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", st.Btn, GUILayout.Width(24f), GUILayout.Height(22f))) CloseTrade();
            GUILayout.EndHorizontal();

            var partners = TradePartners();
            GUILayout.Label(partners.Count == 0 ? "Nobody to trade with: the others are out or at war with you." : "Send an envoy to:", st.Small);
            if (partners.Count > 0)
            {
                GUILayout.BeginHorizontal();
                foreach (var f in partners)
                {
                    bool on = TradeTo == f;
                    Swatch(FactionLook.Colour(f), 12f);
                    if (GUILayout.Button(FactionLook.Name(f), on ? st.BtnSel : st.Btn, GUILayout.Width(58f), GUILayout.Height(24f))) SelectTradePartner(f);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            TradeColumn(st, true);
            GUILayout.Space(14f);
            TradeColumn(st, false);
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);

            bool any = TradeGiveTotal > 0 && TradeWantTotal > 0 && TradeTo.HasValue;
            GUILayout.Label(any ? TradeSummary() : "Pick what you give and what you want.", any ? st.Label : st.Small);
            GUI.enabled = any;
            if (GUILayout.Button(new GUIContent("Send envoy", "The goods leave your HQ at once. The envoy walks over land; enemy soldiers may kill it."), st.BtnSel, GUILayout.Height(28f))) SendTrade();
            GUI.enabled = true;
        }

        private void TradeColumn(HudStyle st, bool give)
        {
            int total = give ? TradeGiveTotal : TradeWantTotal;
            GUILayout.BeginVertical(GUILayout.Width(190f));
            GUILayout.Label((give ? "You give" : "You want") + "  " + total + "/" + TradeTuning.MaxWaresPerSide, st.Row);
            var arr = give ? TradeGive : TradeWant;
            foreach (var w in StockWares)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(22f));
                Swatch(EconomyView.WareColour(w));
                GUILayout.Space(4f);
                GUILayout.Label(Wares.Name(w), st.Row, GUILayout.Width(52f), GUILayout.Height(22f));
                if (GUILayout.Button("-", st.Btn, GUILayout.Width(24f), GUILayout.Height(22f))) AdjustTrade(give, w, -1);
                GUILayout.Label(arr[(int)w].ToString(), st.Centered, GUILayout.Width(20f), GUILayout.Height(22f));
                if (GUILayout.Button("+", st.Btn, GUILayout.Width(24f), GUILayout.Height(22f))) AdjustTrade(give, w, 1);
                if (give) GUILayout.Label(MaxGive(w).ToString(), st.RowSmall, GUILayout.Width(24f), GUILayout.Height(22f));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void WareCountsRow(string head, WareCount[] a)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(22f));
            GUILayout.Label(head, HudStyle.Get().RowSmall, GUILayout.Width(46f), GUILayout.Height(22f));
            foreach (var l in a)
            {
                Swatch(EconomyView.WareColour(l.Ware));
                GUILayout.Label(l.Count.ToString(), HudStyle.Get().Row, GUILayout.ExpandWidth(false), GUILayout.Height(22f));
                GUILayout.Label(Wares.Name(l.Ware), HudStyle.Get().RowSmall, GUILayout.ExpandWidth(false), GUILayout.Height(22f));
                GUILayout.Space(4f);
            }
            GUILayout.EndHorizontal();
        }

        public static string StageText(EnvoyStage s, bool payment)
        {
            switch (s)
            {
                case EnvoyStage.ToTarget: return "On its way with the offer";
                case EnvoyStage.Waiting: return "Waiting for an answer";
                default: return payment ? "Walking home with the payment" : "Walking home";
            }
        }

        /// <summary>The offer card (when an envoy reached you) or the card of a clicked envoy.</summary>
        private void OfferCard(HudStyle st, TradeOffer offer, Envoy env)
        {
            var from = offer != null ? offer.From : env.Faction;
            GUILayout.BeginHorizontal();
            GUILayout.Label(offer != null ? "An envoy from " + FactionLook.Name(from) : FactionLook.Name(from) + " envoy", st.Title);
            GUILayout.FlexibleSpace();
            if (offer == null && GUILayout.Button("x", st.Btn, GUILayout.Width(24f), GUILayout.Height(22f))) ClearEnvoy();
            GUILayout.EndHorizontal();
            if (offer != null)
            {
                GUILayout.Label(FactionLook.Name(offer.From) + " offers " + WaresText(offer.Give) + " for " + WaresText(offer.Want), st.Label);
                WareCountsRow("Gives", offer.Give);
                WareCountsRow("Wants", offer.Want);
                bool canPay = TradeRules.CanPay(Sim.State, Me, offer.Want);
                float left = Mathf.Max(0f, offer.TicksLeft) / (float)GodSim.TicksPerSecond;
                GUILayout.Label("Answer in " + Mathf.CeilToInt(left) + " s, or it is refused.", st.Small);
                var bar = GUILayoutUtility.GetRect(10f, 8f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.27f, 0.19f, 0.09f, 0.18f), Vector4.zero, new Vector4(4, 4, 4, 4));
                    float k = Mathf.Clamp01(offer.TicksLeft / (float)TradeTuning.AnswerTicks);
                    GUI.DrawTexture(new Rect(bar.x, bar.y, Mathf.Max(6f, bar.width * k), bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, k > 0.25f ? HudStyle.Good : HudStyle.Bad, Vector4.zero, new Vector4(4, 4, 4, 4));
                }
                if (!canPay) GUILayout.Label("You do not have " + WaresText(offer.Want) + " free in your stores.", st.Small);
                GUILayout.BeginHorizontal();
                GUI.enabled = canPay;
                if (GUILayout.Button("Accept", st.BtnSel, GUILayout.Height(28f))) AnswerOffer(offer.Id, true);
                GUI.enabled = true;
                if (GUILayout.Button("Refuse", st.Btn, GUILayout.Height(28f))) AnswerOffer(offer.Id, false);
                GUILayout.EndHorizontal();
                int more = PendingOffers().Count - 1;
                if (more > 0) GUILayout.Label("+" + more + " more waiting", st.Small);
            }
            else
            {
                GUILayout.Label(StageText(env.Stage, env.CarriesPayment), st.Small);
                WareCountsRow("Carries", env.CargoList().ToArray());
                var o = Sim.State.Trade.OfferById(env.Id);
                if (o != null && o.From == Me) GUILayout.Label("Your offer to " + FactionLook.Name(o.To) + ": " + WaresText(o.Give) + " for " + WaresText(o.Want), st.Small);
                if (env.Health < TradeTuning.EnvoyHealth) GUILayout.Label("Health " + env.Health + " / " + TradeTuning.EnvoyHealth, st.Small);
            }
        }
    }
}
