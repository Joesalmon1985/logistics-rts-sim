using God.Sim.Cognition;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>D6's sim-side hooks: the telling act (start: the gesture and its listeners; act: its duration). OWNER: D6.</summary>
    public static partial class DivinityHooks
    {
        static partial void TryStartD6(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled)
        {
            if (handled || c.Verb != Doctrine.TellVerb) return;
            handled = true;
            if (npc.Mind?.Divine == null) return;
            Doctrine.StartTell(s, ev, npc, c.TargetId);
        }

        static partial void TryActD6(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled) return;
            if (Doctrine.ActTell(s, npc)) handled = true;
        }
    }

    /// <summary>D6's install slot: the three commandment handlers, the doctrine system (order 260) and the minds rebound to the tuning.</summary>
    public static partial class DivinityModule
    {
        static partial void InstallD6(GodSim sim)
        {
            sim.RegisterHandler(new RevealCommandmentHandler());
            sim.RegisterHandler(new ReviseCommandmentHandler());
            sim.RegisterHandler(new RevokeCommandmentHandler());
            sim.AddSystem(DoctrineSystem.SystemOrder, new DoctrineSystem());
            Doctrine.BindTuning(sim.State);
        }
    }

    /// <summary>
    /// D6's system (order 260: after the population's thinks and acts, so the choices of this tick and the tick's divine
    /// events are visible). Per tick: Tier 2 founding (once) and the evidence of this tick's events; the choices people just
    /// made that a held rule weighed (866, and salience for acting on it); the staggered day pass of each person
    /// (<c>(tick + id x 13) % TicksPerDay == 0</c>). Does nothing while no commandment exists and Tier 2 is off (pin P2). OWNER: D6.
    /// </summary>
    public sealed class DoctrineSystem : ISimSystem
    {
        public const int SystemOrder = 260;

        public void Tick(GodState s, EventBuffer ev)
        {
            var d = s.Divinity;
            if (d == null || s.Population == null) return;
            var tu = s.Tuning.Divinity.Doctrine;
            if (tu.Beliefs != 0)
            {
                if (d.Doctrine == null || !d.Doctrine.Founded) Beliefs.Found(s, ev);
                Beliefs.Evidence(s, ev);
            }
            if (d.Doctrine == null) return;
            int tpd = SimCalendar.Of(s).TicksPerDay;
            foreach (var n in s.Population.Npcs)
            {
                var m = n.Mind?.Divine;
                if (m == null || !Bodies.IsLivingPerson(n)) continue;
                if (m.MatchTick == s.Tick && m.MatchCount > 0) Choices(s, ev, n, m);
                if ((s.Tick + (long)n.Id * 13) % tpd != 0) continue;
                if (m.KnownCount > 0) Doctrine.Day(s, ev, n, m);
                if (tu.Beliefs != 0 && m.GodCount > 0) Beliefs.Day(s, ev, n, m);
            }
        }

        /// <summary>The action just started was one a held rule weighed: <see cref="CommandmentChoiceMade"/> and salience for acting on it.</summary>
        private static void Choices(GodState s, EventBuffer ev, Npc n, DivineMind m)
        {
            var a = n.Action;
            int count = m.MatchCount;
            if (a.Verb == 0 || a.Since != s.Tick) return;
            int lastC = 0;
            for (int i = 0; i < count; i++)
            {
                var x = m.Matches[i];
                if (x.Verb != a.Verb || x.Kind != a.TargetKind || (x.Kind == Policy.IntentTarget.Node ? x.TargetNode != a.TargetNode : x.TargetId != a.TargetId)) continue;
                if (x.Commandment == lastC) continue;
                lastC = x.Commandment;
                ev.Add(new CommandmentChoiceMade { Npc = n.Id, Commandment = x.Commandment, Modality = x.Modality, Verb = x.Verb, TargetId = x.TargetId, Term = x.Term });
                int k = m.IndexOfKnown(x.Commandment);
                if (k >= 0) m.Known[k].Salience = (short)System.Math.Min(1000, m.Known[k].Salience + s.Tuning.Divinity.Doctrine.SalienceActed);
            }
        }
    }
}
