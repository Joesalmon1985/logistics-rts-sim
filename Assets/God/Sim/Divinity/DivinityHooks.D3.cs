using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Sim.Divinity
{
    /// <summary>D3's slots of the divinity hooks: protection in <c>Bodies.Damage</c>, the person heal of illness (through M4's fields), the shrine in the sense seam. OWNER: D3.</summary>
    public static partial class DivinityHooks
    {
        /// <summary>Protect: every injury through <c>Bodies.Damage</c> is multiplied by <c>ProtectPermille</c>. Never takes the damage over.</summary>
        static partial void DamageD3(GodState s, EventBuffer ev, Npc body, ref int injuryMicro, DamageCause cause, int actor, int intervention, ref bool handled)
        {
            if (handled || !Miracles.Has(s, body.Id, MiracleKind.Protect)) return;
            injuryMicro = (int)((long)injuryMicro * s.Tuning.Divinity.Miracles.ProtectPermille / 1000);
        }

        /// <summary>HealDisease: an infected person recovers (M4 fields): immunity for the normal days, severity 0, health back by <paramref name="amount"/>.</summary>
        static partial void HealD3(GodState s, EventBuffer ev, Npc body, HealKind what, int amount, int intervention, ref bool handled)
        {
            if (handled || what != HealKind.Illness || !Bodies.IsPerson(body)) return;
            var life = body.Life;
            if (life == null || life.Illness != 1) return;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            life.Illness = 2;
            life.ImmuneUntilDay = day + s.Tuning.Households.Disease.ImmuneDays;
            life.Severity = 0;
            life.IllDaysLeft = 0;
            body.Health = System.Math.Min(1000000, body.Health + amount);
            ev.Add(new IllnessEnded { Npc = body.Id, Days = life.IllDays, Recovered = true });
            handled = true;
        }

        /// <summary>A consecrated place in sight is a shrine (<c>ObjClass.Shrine</c>), perceived like any thing at a node.</summary>
        static partial void SenseExtraD3(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n)
        {
            var m = s.Divinity.Miracles;
            if (m == null) return;
            foreach (var pl in m.Places)
            {
                int d = g.Distance(npc.Node, pl.Node);
                if (d > sight || n >= near.Length) continue;
                near[n++] = new Sensed { Class = ObjClass.Shrine, Category = ObjCategory.Ground, Kind = Policy.IntentTarget.Node, Node = pl.Node, Dist = d };
            }
        }
    }
}
