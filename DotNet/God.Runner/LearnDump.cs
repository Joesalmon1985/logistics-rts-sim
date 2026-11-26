using System;
using System.IO;
using System.Linq;
using System.Threading;
using God.Recording;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Runner
{
    /// <summary>
    /// <c>learn-dump</c> (HEADLESS_SIMULATION 4.11, NPC_LEARNING 7): one NPC's whole association store as JSON lines, outside
    /// the sim. The game comes from a scenario run to a time (<c>--scenario --seed --days|--ticks</c>) or from a save
    /// (<c>--from-save</c>). Lines: a header (the NPC, its mode and counters), then every entry (key in words, verb, weights,
    /// use, last day, origin), every live conjunction, every open trace and episode. <c>--npc all</c> writes a summary line
    /// per living NPC instead.
    /// </summary>
    public static class LearnDump
    {
        public static int Run(Args a, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            GodSim sim;
            string save = a.Str("from-save");
            string npcArg = a.Str("npc") ?? throw new UsageException("learn-dump needs --npc ID (or --npc all)");
            string outPath = a.Str("out");
            if (save != null)
            {
                a.CheckAllUsed(0);
                sim = SaveFile.FromJson(File.ReadAllText(save)).ToSim();
            }
            else
            {
                var sc = Cli.LoadScenario(a);
                ulong seed = a.ULong("seed") ?? 1;
                a.CheckAllUsed(0);
                if (!sc.Population) throw new ScenarioException("learn-dump needs a scenario with setup.population");
                var built = SimBuilder.Create(sc.ToSetup(seed), sc.ToTuning(), sc.FactionPolicy != PolicyRegistry.None, sc.Policies);
                sim = built.Sim;
                long limit = sc.LimitTicks > 0 ? sc.LimitTicks : sc.TicksPerDay;
                while (sim.State.Tick < limit && !cancel.IsCancellationRequested) { sim.Step(); sim.DrainEvents(); }
            }
            var p = sim.State.Population ?? throw new ScenarioException("the game has no population");
            var w = outPath != null ? new StreamWriter(outPath) : output;
            try
            {
                if (npcArg == "all")
                {
                    foreach (var n in p.Npcs) if (n.Alive) w.WriteLine(Summary(sim.State, n).ToString(Formatting.None));
                }
                else
                {
                    if (!int.TryParse(npcArg, out int id)) throw new UsageException("--npc takes an id or all");
                    var n = p.Find(id) ?? throw new ScenarioException("no NPC " + id);
                    if (n.Mind == null) throw new ScenarioException("NPC " + id + " is dead: the dead keep no mind");
                    Dump(sim.State, n, w);
                }
            }
            finally { if (outPath != null) w.Dispose(); }
            return ExitCodes.Ok;
        }

        public static JObject Summary(GodState s, Npc n)
        {
            var st = n.Mind.Store;
            return new JObject
            {
                ["npc"] = n.Id, ["faction"] = (int)n.Faction, ["tick"] = s.Tick, ["mode"] = st.Mode.ToString(), ["entries"] = st.Count, ["conjunctions"] = st.ConjUsed,
                ["updates"] = st.Updates, ["evictions"] = st.Evictions, ["recruits"] = st.Recruits, ["frees"] = st.Frees, ["bytes"] = st.Bytes,
                ["storeHash"] = st.HashWeights().ToString("x16"), ["learnedSharePermille"] = n.Mind.LearnedSharePrev, ["exploreSharePermille"] = n.Mind.ExploreSharePrev,
            };
        }

        public static void Dump(GodState s, Npc n, TextWriter w)
        {
            var st = n.Mind.Store;
            var head = Summary(s, n);
            head["type"] = "npc";
            w.WriteLine(head.ToString(Formatting.None));
            // the last decision and why: each weighed option with its score terms (need, instinct, learned, personality,
            // cost, explore), the same breakdown as the inspector card; then the recent experiences with their rewards
            var m = n.Mind;
            var l = m.Last;
            w.WriteLine(new JObject
            {
                ["type"] = "decision", ["tick"] = l.Tick, ["verb"] = VerbTable.Name(l.Verb), ["targetClass"] = Feature.ClassName(l.TargetClass), ["need"] = l.Need,
                ["instinct"] = l.Instinct - l.Need, ["learned"] = l.Learned, ["personality"] = l.Personality, ["cost"] = -l.Cost, ["explore"] = l.Explore, ["total"] = l.Total,
                ["candidates"] = l.Candidates, ["learnedChangedWinner"] = l.LearnedChangedWinner, ["exploreWon"] = l.ExploreWon,
            }.ToString(Formatting.None));
            for (int i = 0; i < m.OpportunityCount; i++)
            {
                var o = m.Opportunities[i];
                w.WriteLine(new JObject
                {
                    ["type"] = "option", ["verb"] = VerbTable.Name(o.Verb), ["targetClass"] = Feature.ClassName(o.Class), ["dist"] = o.Dist, ["remembered"] = o.FromBelief, ["chosen"] = o.Chosen,
                    ["need"] = o.Need, ["instinct"] = o.Instinct - o.Need, ["learned"] = o.Learned, ["personality"] = o.Personality, ["cost"] = -o.Cost, ["explore"] = o.Explore, ["total"] = o.Total,
                }.ToString(Formatting.None));
            }
            for (int k = 0; k < m.ExperienceCount; k++)
            {
                var e = m.Recent(k);
                w.WriteLine(new JObject { ["type"] = "experience", ["tick"] = e.Tick, ["verb"] = VerbTable.Name(e.Verb), ["targetClass"] = Feature.ClassName(e.Class), ["reward"] = e.Reward, ["episode"] = e.Episode }.ToString(Formatting.None));
            }
            for (int i = 0; i < st.Count; i++)
            {
                var e = st.Entries[i];
                w.WriteLine(new JObject
                {
                    ["type"] = "entry", ["key"] = e.Key, ["kind"] = Feature.KindOf(e.Key).ToString(), ["value"] = Feature.ValueOf(e.Key),
                    ["words"] = God.Sim.Information.PopulationKnowledge.Describe(st, e.Key), ["verb"] = VerbTable.Name(e.Verb),
                    ["stw"] = e.Stw, ["ltw"] = e.Ltw, ["use"] = e.Use, ["lastDay"] = e.LastDay, ["origin"] = e.Origin.ToString(), ["locked"] = e.Locked,
                }.ToString(Formatting.None));
            }
            for (int c = 0; c < st.Conj.Length; c++)
            {
                var x = st.Conj[c];
                if (x.Strength == 0) continue;
                w.WriteLine(new JObject { ["type"] = "conjunction", ["slot"] = c, ["a"] = Feature.Describe(x.A), ["b"] = Feature.Describe(x.B), ["strength"] = x.Strength, ["generation"] = x.Generation, ["lastDay"] = x.LastDay }.ToString(Formatting.None));
            }
            for (int i = 0; i < st.RingCount; i++)
            {
                var t = st.Ring[i];
                w.WriteLine(new JObject { ["type"] = "trace", ["tick"] = t.Tick, ["verb"] = VerbTable.Name(t.Verb), ["e"] = t.E, ["return"] = t.Return, ["ongoing"] = t.Ongoing, ["keys"] = t.KeyCount, ["episodeKey"] = t.EpisodeKey }.ToString(Formatting.None));
            }
            for (int i = 0; i < st.OpenCount; i++)
            {
                var ep = st.Open[i];
                w.WriteLine(new JObject { ["type"] = "episode", ["verb"] = VerbTable.Name(ep.T.Verb), ["episodeKey"] = ep.T.EpisodeKey, ["openedDay"] = ep.OpenedDay, ["expiresDay"] = ep.ExpiresDay, ["return"] = ep.T.Return }.ToString(Formatting.None));
            }
        }
    }
}
