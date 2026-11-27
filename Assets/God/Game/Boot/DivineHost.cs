using God.Sim;
using God.Sim.Divinity;
using God.Sim.Information;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// U2a: the host-side adapter between the sim and the divine HUD. It is the only place that touches the sim for the divine interface:
    /// it builds the human god's <see cref="IDivineView"/>, hands out the tuning costs the toolbar estimates with, and feeds the sim's events
    /// to the <see cref="DivineFeed"/> (chips, miracle notes, person rows), the <see cref="DivineCues"/> and the <see cref="AlertAggregator"/>.
    /// The HUD reads these, never the state (HudViewOnlyTests). Not a HUD type.
    /// </summary>
    public sealed class DivineHost
    {
        private readonly GodBootstrap _boot;
        private IDivineView _view;
        private int _viewGod;

        public readonly DivineFeed Feed = new DivineFeed();
        public readonly DivineCues Cues = new DivineCues();
        public AlertAggregator Alerts { get; private set; }

        /// <summary>False in a game without gods (Divinity.Enabled = 0).</summary>
        public bool Available => _boot.Sim.State.Divinity != null;

        public DivineHost(GodBootstrap boot)
        {
            _boot = boot;
            int tpd = boot.Sim.State.Setup?.TicksPerDay ?? SimCalendar.DefaultTicksPerDay;
            Alerts = new AlertAggregator(tpd, id => boot.Ui != null && (boot.Ui.IsFollowed(id) || boot.Ui.IsFavourite(id)));
            Feed.Cue += s => Cues.Spawn(s, Time.unscaledTime);
            boot.SimEventRaised += OnEvent;
        }

        public void Detach() { _boot.SimEventRaised -= OnEvent; }

        /// <summary>The human god's view (rebuilt when the seat moves); null in a game without gods.</summary>
        public IDivineView View
        {
            get
            {
                if (!Available) return null;
                int god = _boot.HumanGod;
                if (_view == null || _viewGod != god)
                {
                    try { _view = DivineViewFiller.For(_boot.Sim, god); _viewGod = god; }
                    catch (System.Exception) { _view = null; }
                }
                return _view;
            }
        }

        /// <summary>The sim's Mythic cost table (an estimate source for the ghost: the spend event has the real figure).</summary>
        public MythicCosts Costs => _boot.Sim.State.Tuning.Divinity.Mythic.Costs;

        public int TicksPerDay => _boot.Sim.State.Setup?.TicksPerDay ?? SimCalendar.DefaultTicksPerDay;

        /// <summary>True in a sandbox game (the Mythic mode buttons are offered).</summary>
        public bool Sandbox => Gods.IsSandbox(_boot.Sim.State);

        private void OnEvent(SimEvent e)
        {
            Feed.God = _boot.HumanGod;
            Feed.Accept(e);
            if (AlertAggregator.Map(e, e.Tick, out var input)) Alerts.Add(input);
        }
    }
}
