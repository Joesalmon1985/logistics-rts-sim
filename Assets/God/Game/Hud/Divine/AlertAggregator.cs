using System;
using System.Collections.Generic;
using System.Text;
using God.Sim;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Social;

namespace God.Game
{
    public enum AlertClass { Critical, Important, Digest }

    public enum AlertKind { Death, Birth, Trade, MythicExhausted, PossessionEnded }

    /// <summary>One thing that happened, reduced to what alerts need (no Unity or sim types).</summary>
    public struct AlertInput
    {
        public long Tick;
        public AlertKind Kind;
        public int Person, Cause, Area;
    }

    public sealed class AlertCard
    {
        public int Id;
        public AlertClass Class;
        public AlertKind Kind;
        public string Text;
        public int Count;
        public long Tick, Updated;
        public int Person;
    }

    /// <summary>Alert limits (configurable defaults, noted in the DEV_LOG).</summary>
    public sealed class AlertTuning
    {
        /// <summary>Important events of one (kind, cause, area) merge into one card within this window.</summary>
        public int WindowTicks = 600;
        /// <summary>Area bucket: a node is grouped with the others of its bucket (the caller maps the node).</summary>
        public int MaxCards = 8;
    }

    /// <summary>
    /// Alert aggregation (brief 30, PLAYER_UI 14): critical events get their own card (a followed person dies, the Mythic is exhausted, a
    /// possession ended); important events merge per (kind, cause, area, window) into one counted card; everything else (trades, births and
    /// deaths of strangers) only counts toward <b>one digest per day</b>, written when the day ends. Never a card per trade or per meal. Pure.
    /// </summary>
    public sealed class AlertAggregator
    {
        private readonly AlertTuning _t;
        private readonly int _ticksPerDay;
        private readonly Func<int, bool> _isWatched;
        private readonly List<AlertCard> _cards = new List<AlertCard>();
        private readonly Dictionary<(AlertKind, int, int, long), AlertCard> _merge = new Dictionary<(AlertKind, int, int, long), AlertCard>();
        private int _nextId;
        private long _day = -1;
        private int _trades, _births, _deaths;

        public IReadOnlyList<AlertCard> Cards => _cards;
        public int DigestsWritten { get; private set; }

        public AlertAggregator(int ticksPerDay, Func<int, bool> isWatched = null, AlertTuning tuning = null)
        {
            _ticksPerDay = Math.Max(1, ticksPerDay); _isWatched = isWatched ?? (_ => false); _t = tuning ?? new AlertTuning();
        }

        public void Add(AlertInput e)
        {
            Advance(e.Tick);
            switch (e.Kind)
            {
                case AlertKind.Death:
                    _deaths++;
                    if (_isWatched(e.Person)) NewCard(AlertClass.Critical, e, "Someone you watch has died (" + CauseText(e.Cause) + ").", 1);
                    else Merge(e, "died");
                    break;
                case AlertKind.MythicExhausted: NewCard(AlertClass.Critical, e, "Your Mythic is exhausted.", 1); break;
                case AlertKind.PossessionEnded: NewCard(AlertClass.Critical, e, "The possession has ended.", 1); break;
                case AlertKind.Birth: _births++; break;
                case AlertKind.Trade: _trades++; break;
            }
        }

        /// <summary>Moves time on: when a new day begins, the finished day's digest is written (once).</summary>
        public void Advance(long tick)
        {
            long day = tick / _ticksPerDay;
            if (_day < 0) { _day = day; return; }
            if (day <= _day) return;
            WriteDigest(_day, tick);
            _day = day; _trades = _births = _deaths = 0;
        }

        private void WriteDigest(long day, long tick)
        {
            if (_trades + _births + _deaths == 0) return;
            var sb = new StringBuilder("Day " + (day + 1) + ": ");
            bool first = true;
            void Part(int n, string one, string many) { if (n == 0) return; if (!first) sb.Append(", "); first = false; sb.Append(n).Append(' ').Append(n == 1 ? one : many); }
            Part(_trades, "trade", "trades"); Part(_births, "birth", "births"); Part(_deaths, "death", "deaths");
            Push(new AlertCard { Id = ++_nextId, Class = AlertClass.Digest, Kind = AlertKind.Trade, Text = sb.ToString(), Count = 1, Tick = tick, Updated = tick });
            DigestsWritten++;
        }

        private void Merge(AlertInput e, string verb)
        {
            var key = (e.Kind, e.Cause, e.Area, e.Tick / Math.Max(1, _t.WindowTicks));
            if (_merge.TryGetValue(key, out var c) && _cards.Contains(c))
            {
                c.Count++; c.Updated = e.Tick; c.Text = MergedText(c.Count, e);
                return;
            }
            var card = new AlertCard { Id = ++_nextId, Class = AlertClass.Important, Kind = e.Kind, Count = 1, Tick = e.Tick, Updated = e.Tick, Person = e.Person, Text = MergedText(1, e) };
            _merge[key] = card; Push(card);
        }

        private static string MergedText(int n, AlertInput e) => n + (n == 1 ? " person died" : " people died") + " (" + CauseText(e.Cause) + ")";

        private static string CauseText(int cause)
        {
            switch ((DeathCause)cause)
            {
                case DeathCause.Starvation: return "hunger";
                case DeathCause.Wound: return "a wound";
                case DeathCause.Disease: return "disease";
                case DeathCause.OldAge: return "old age";
                case DeathCause.Childbirth: return "childbirth";
                case DeathCause.Exposure: return "exposure";
                case DeathCause.Violence: return "violence";
                case DeathCause.Accident: return "an accident";
                default: return "unknown cause";
            }
        }

        private void NewCard(AlertClass cls, AlertInput e, string text, int count) =>
            Push(new AlertCard { Id = ++_nextId, Class = cls, Kind = e.Kind, Text = text, Count = count, Tick = e.Tick, Updated = e.Tick, Person = e.Person });

        private void Push(AlertCard c)
        {
            _cards.Add(c);
            while (_cards.Count > _t.MaxCards) _cards.RemoveAt(0);
        }

        /// <summary>The input an event gives, or false for an event alerts ignore (a meal, a step, a hand-over).</summary>
        public static bool Map(SimEvent e, long tick, out AlertInput input)
        {
            input = new AlertInput { Tick = tick };
            switch (e)
            {
                case NpcDied d: input.Kind = AlertKind.Death; input.Person = d.Npc; input.Cause = (int)d.Cause; input.Area = d.Node / 64; return true;
                case NpcBorn _: input.Kind = AlertKind.Birth; return true;
                case ExchangeClosed x when x.Outcome == DealOutcome.Completed: input.Kind = AlertKind.Trade; return true;
                case TradeAccepted _: input.Kind = AlertKind.Trade; return true;
                case MythicSpent ms when ms.Balance <= 0 && ms.Amount > 0: input.Kind = AlertKind.MythicExhausted; return true;
            }
            if (e != null && e.GetType().Name == "PossessionEnded") { input.Kind = AlertKind.PossessionEnded; return true; }
            return false;
        }
    }
}
