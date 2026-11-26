using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using God.Sim;
using God.Sim.Population;

namespace God.Runner.Demog
{
    /// <summary>One run's figures, derived from its <see cref="DemogRecorder"/> (the per-seed row of the report).</summary>
    public sealed class DemogRunSummary
    {
        public string Arm;
        public ulong Seed;
        public int ExitCode, Years, Start, End, Births, Deaths, Arrivals, Year3, MaxGeneration, ExtinctDay = -1, InvariantFailures;
        public int[] DeathsByCause = new int[9];
        public bool Reconciles;
        public string Outcome;
        // fertility
        public int CompletedWomen, CompletedBirths, CensoredWomen, CensoredBirths, FounderLateWomen, FounderLateBirths, BornWomenDiedFertile, BornWomenDiedFertileBirths;
        public double CompletedFertility => CompletedWomen == 0 ? double.NaN : (double)CompletedBirths / CompletedWomen;
        // child mortality
        public int ChildOutcomeKnown, ChildDied, ChildReachedAdult, ChildCensored;
        public int[] ChildDeathsByCause = new int[9];
        public double ChildMortality => ChildOutcomeKnown == 0 ? double.NaN : (double)ChildDied / ChildOutcomeKnown;
        // over the run
        public long PersonDays, CareTicks, ChildFedUnits, FoodForaged, FoodEaten, FoodSpoiled, Regeneration, ForageTicks, WalkTicks;
        public long[] WomanDays = new long[DemogRecorder.WomanStatusNames.Length];
        public double MeanDependency, MeanHouseholdSize, MeanReachShare, MeanStockShare, MsyPerDay, EatenPerPersonDay, ForageYield, DensityPerHex;
        public int Unions, UnionsOutside, MaxFounderDeathShare30, MaxFounderDeathShare3;
        public int FounderDeaths;
        public DemogRecorder Rec;
    }

    /// <summary>The demographic report (M4.45): per-run summaries and the multi-seed aggregate per arm, as text and CSV.</summary>
    public static class DemogReport
    {
        private static readonly string[] Causes = { "none", "starvation", "wound", "disease", "old age", "childbirth", "exposure", "violence", "accident" };
        private static string F(double v, int d = 2) => double.IsNaN(v) ? "na" : v.ToString("F" + d, CultureInfo.InvariantCulture);

        public static DemogRunSummary Summarise(string arm, ulong seed, DemogRecorder r, int exitCode, int invariantFailures)
        {
            var s = new DemogRunSummary { Arm = arm, Seed = seed, Rec = r, ExitCode = exitCode, InvariantFailures = invariantFailures };
            s.Start = r.Start; s.End = r.Final; s.Births = r.Births; s.Deaths = r.Deaths; s.Arrivals = r.Arrivals; s.MaxGeneration = r.MaxGeneration; s.ExtinctDay = r.ExtinctDay;
            Array.Copy(r.DeathsByCause, s.DeathsByCause, 9);
            s.Reconciles = r.Start + r.Births + r.Arrivals - r.Deaths == r.Final;
            s.Years = r.FinalDay / Math.Max(1, r.DaysPerYear);
            s.Year3 = r.Years.Count > 3 ? r.Years[3].PopStart : 0;
            long endTick = (long)r.FinalDay * (r.TicksPerYear / Math.Max(1, r.DaysPerYear));
            long tpy = r.TicksPerYear;
            long FertileFromTicks() => r.AgeTicks(1000);
            foreach (var p in r.People.Values)
            {
                if (p.Sex == 0)
                {
                    long endFertile = p.BornTick + r.AgeTicks(p.FertileEndMilli);
                    bool reachedEnd = endFertile <= endTick && (p.DeathTick < 0 || p.DeathTick >= endFertile);
                    bool diedInSpan = p.DeathTick >= 0 && p.DeathTick < endFertile && p.DeathTick >= p.BornTick + FertileFromTicks();
                    bool lateFounder = p.Founder && p.EntryAgeMilli > 1000;
                    if (lateFounder) { if (reachedEnd || diedInSpan || p.DeathTick < 0) { s.FounderLateWomen++; s.FounderLateBirths += p.Births; } }
                    else if (reachedEnd) { s.CompletedWomen++; s.CompletedBirths += p.Births; }
                    else if (diedInSpan) { s.BornWomenDiedFertile++; s.BornWomenDiedFertileBirths += p.Births; }
                    else if (p.DeathTick < 0 && p.BornTick + FertileFromTicks() <= endTick) { s.CensoredWomen++; s.CensoredBirths += p.Births; }
                }
                if (p.Born)
                {
                    long adultAt = p.BornTick + r.AgeTicks(1000);
                    if (p.DeathTick >= 0 && p.DeathTick < adultAt) { s.ChildDied++; s.ChildOutcomeKnown++; s.ChildDeathsByCause[(int)p.Cause]++; }
                    else if (adultAt <= endTick) { s.ChildReachedAdult++; s.ChildOutcomeKnown++; }
                    else s.ChildCensored++;
                }
            }
            // founder deaths: the largest share in any window of 30 days and of 3 days
            var fd = r.People.Values.Where(p => p.Founder && p.DeathTick >= 0).Select(p => p.DeathTick).OrderBy(t => t).ToList();
            s.FounderDeaths = fd.Count;
            int founders = r.People.Values.Count(p => p.Founder);
            long tpd = tpy / Math.Max(1, r.DaysPerYear);
            s.MaxFounderDeathShare30 = MaxWindowShare(fd, 30 * tpd, founders);
            s.MaxFounderDeathShare3 = MaxWindowShare(fd, 3 * tpd, founders);
            var ys = r.Years.Where(y => y.PopStart > 0 || y.Year == 0).ToList();
            foreach (var y in r.Years)
            {
                s.PersonDays += y.PersonDays; s.CareTicks += y.CareTicks; s.ChildFedUnits += y.ChildFedUnits; s.FoodForaged += y.FoodForaged; s.FoodEaten += y.FoodEaten;
                s.FoodSpoiled += y.FoodSpoiled; s.Regeneration += y.Regeneration; s.ForageTicks += y.ForageTicks; s.WalkTicks += y.WalkTicks;
                for (int k = 0; k < s.WomanDays.Length; k++) s.WomanDays[k] += y.WomanDays[k];
                s.Unions = Math.Max(s.Unions, y.Unions); s.UnionsOutside = Math.Max(s.UnionsOutside, y.UnionsOutsidePartnership);
            }
            s.MeanDependency = ys.Where(y => y.Adults > 0).Select(y => (double)(y.Children + y.Elders) / y.Adults).DefaultIfEmpty(double.NaN).Average();
            s.MeanHouseholdSize = ys.Where(y => y.Households > 0).Select(y => (double)y.HouseholdMembers / y.Households).DefaultIfEmpty(double.NaN).Average();
            s.MeanReachShare = ys.Where(y => y.WildFoodStock > 0 && y.PopStart > 0).Select(y => (double)y.ReachableWildFood / y.WildFoodStock).DefaultIfEmpty(double.NaN).Average();
            s.MeanStockShare = ys.Where(y => y.WildFoodCapacity > 0).Select(y => (double)y.WildFoodStock / y.WildFoodCapacity).DefaultIfEmpty(double.NaN).Average();
            s.MsyPerDay = r.Years.Count > 0 ? r.Years[0].WildFoodMsyPerDay : 0;
            s.EatenPerPersonDay = s.PersonDays == 0 ? double.NaN : (double)s.FoodEaten / s.PersonDays;
            s.ForageYield = s.ForageTicks == 0 ? double.NaN : 1000.0 * s.FoodForaged / s.ForageTicks;
            s.DensityPerHex = r.LandHexes == 0 ? 0 : (double)r.Start / r.LandHexes;
            s.Outcome = Classify(s);
            return s;
        }

        private static int MaxWindowShare(List<long> ticks, long window, int total)
        {
            if (total == 0 || ticks.Count == 0) return 0;
            int best = 0, j = 0;
            for (int i = 0; i < ticks.Count; i++)
            {
                while (ticks[i] - ticks[j] >= window) j++;
                best = Math.Max(best, i - j + 1);
            }
            return best * 1000 / total;
        }

        /// <summary>The outcome class of a run: extinct; collapsed (end below half of year 3); stable (0.5x-2x of year 3); grew (above 2x).</summary>
        public static string Classify(DemogRunSummary s)
        {
            if (s.End == 0) return "extinct";
            if (s.Year3 == 0) return "short";
            if (s.End * 2 < s.Year3) return "collapsed";
            if (s.End > 2 * s.Year3) return "grew";
            return "stable";
        }

        private static (double mean, double ci) Stat(IEnumerable<double> xs)
        {
            var l = xs.Where(x => !double.IsNaN(x)).ToList();
            if (l.Count == 0) return (double.NaN, double.NaN);
            double m = l.Average();
            if (l.Count < 2) return (m, double.NaN);
            double sd = Math.Sqrt(l.Sum(x => (x - m) * (x - m)) / (l.Count - 1));
            return (m, T95(l.Count - 1) * sd / Math.Sqrt(l.Count));
        }

        /// <summary>Student t (two-sided 95%) for small samples.</summary>
        private static double T95(int df)
        {
            double[] t = { 12.71, 4.30, 3.18, 2.78, 2.57, 2.45, 2.36, 2.31, 2.26, 2.23, 2.20, 2.18, 2.16, 2.14, 2.13, 2.12, 2.11, 2.10, 2.09, 2.09, 2.08, 2.07, 2.07, 2.06, 2.06, 2.06, 2.05, 2.05, 2.05, 2.04 };
            return df <= 0 ? double.NaN : df <= t.Length ? t[df - 1] : 1.96;
        }

        private static string MS(IEnumerable<double> xs, int d = 1) { var (m, c) = Stat(xs); return F(m, d) + " +- " + F(c, d); }

        /// <summary>Writes the text report and the CSVs of all arms into <paramref name="dir"/> and returns the text.</summary>
        public static string Write(string dir, string title, List<List<DemogRunSummary>> arms)
        {
            var txt = new StringBuilder();
            txt.AppendLine("# " + title);
            txt.AppendLine("intervals are 95% (Student t) over seeds; 'na' where no case exists. Years are sim years (10 days); ages in sim years.");
            txt.AppendLine();
            var runs = new StringBuilder("arm,seed,exit,years,start,year3,end,outcome,births,deaths,arrivals,reconciles,max_generation,extinct_day,"
                + string.Join(",", Causes.Skip(1).Select(c => "deaths_" + c.Replace(' ', '_')))
                + ",completed_women,completed_births,completed_fertility,censored_women,censored_births,founder_late_women,founder_late_births,died_fertile_women,died_fertile_births,"
                + "child_known,child_died,child_adult,child_censored,child_mortality,person_days,care_ticks,child_fed_units,food_foraged,food_eaten,food_spoiled,regeneration,msy_per_day,eaten_per_person_day,"
                + "forage_yield_per_1000_ticks,walk_ticks,mean_dependency,mean_household_size,reach_share,stock_share,density_per_hex,unions_max,unions_outside_max,"
                + string.Join(",", Enumerable.Range(0, DemogRecorder.WomanStatusNames.Length).Select(i => "woman_days_" + i))
                + ",founder_deaths,founder_death_max_share30_permille,founder_death_max_share3_permille,invariant_failures\n");
            var yearly = new StringBuilder("arm,seed,year,pop_start,pop_end,children,adults,elders,births,deaths,conceptions,"
                + string.Join(",", Causes.Skip(1).Select(c => "deaths_" + c.Replace(' ', '_'))) + ","
                + string.Join(",", DemogRecorder.AgeBandNames.Select((n, i) => "deaths_age" + i)) + ","
                + string.Join(",", DemogRecorder.AgeBandNames.Select((n, i) => "person_days_age" + i)) + ","
                + string.Join(",", Enumerable.Range(0, DemogRecorder.AgeBinCount).Select(i => "age_bin" + i)) + ","
                + "households,household_members,households_with_children,households_with_elders,max_household," + string.Join(",", DemogRecorder.CompositionNames.Select((n, i) => "comp" + i)) + ","
                + "romantic_pairs,mixed_pairs,same_sex_pairs,unions,unions_outside," + string.Join(",", Enumerable.Range(0, DemogRecorder.WomanStatusNames.Length).Select(i => "woman_days_" + i)) + ","
                + "care_ticks,child_fed_acts,child_fed_units,fed_by_parent,fed_by_non_parent,tended,arrangements_made,arrangements_ended,fostered,orphaned,"
                + "food_foraged,food_produced_other,food_eaten,food_spoiled,food_buried,regeneration,forage_ticks,walk_ticks,sources_foraged,food_sources,"
                + "wild_stock,wild_capacity,msy_per_day,reachable_wild,store_food,carried_food,ground_food,factions_alive,max_generation_alive,starve_food_within5,starve_food_within10,starve_capacity_within10,starve_no_food10,nutrition_adults,nutrition_children\n");
            foreach (var arm in arms)
            {
                if (arm.Count == 0) continue;
                string name = arm[0].Arm;
                foreach (var s in arm)
                {
                    runs.Append(string.Join(",", new object[] { name, s.Seed, s.ExitCode, s.Years, s.Start, s.Year3, s.End, s.Outcome, s.Births, s.Deaths, s.Arrivals, s.Reconciles ? 1 : 0, s.MaxGeneration, s.ExtinctDay }
                        .Concat(Enumerable.Range(1, 8).Select(i => (object)s.DeathsByCause[i]))
                        .Concat(new object[] { s.CompletedWomen, s.CompletedBirths, F(s.CompletedFertility, 3), s.CensoredWomen, s.CensoredBirths, s.FounderLateWomen, s.FounderLateBirths, s.BornWomenDiedFertile, s.BornWomenDiedFertileBirths,
                            s.ChildOutcomeKnown, s.ChildDied, s.ChildReachedAdult, s.ChildCensored, F(s.ChildMortality, 3), s.PersonDays, s.CareTicks, s.ChildFedUnits, s.FoodForaged, s.FoodEaten, s.FoodSpoiled, s.Regeneration, F(s.MsyPerDay, 0), F(s.EatenPerPersonDay, 3),
                            F(s.ForageYield, 2), s.WalkTicks, F(s.MeanDependency, 3), F(s.MeanHouseholdSize, 2), F(s.MeanReachShare, 3), F(s.MeanStockShare, 3), F(s.DensityPerHex, 2), s.Unions, s.UnionsOutside })
                        .Concat(s.WomanDays.Select(x => (object)x))
                        .Concat(new object[] { s.FounderDeaths, s.MaxFounderDeathShare30, s.MaxFounderDeathShare3, s.InvariantFailures })) + "\n");
                    foreach (var y in s.Rec.Years)
                        yearly.Append(string.Join(",", new object[] { name, s.Seed, y.Year, y.PopStart, y.PopEnd, y.Children, y.Adults, y.Elders, y.Births, y.Deaths, y.Conceptions }
                            .Concat(Enumerable.Range(1, 8).Select(i => (object)y.DeathsByCause[i])).Concat(y.DeathsByAgeBand.Select(x => (object)x)).Concat(y.PersonDaysByAgeBand.Select(x => (object)x))
                            .Concat(y.AgeBins.Select(x => (object)x))
                            .Concat(new object[] { y.Households, y.HouseholdMembers, y.HouseholdsWithChildren, y.HouseholdsWithElders, y.MaxHouseholdSize }).Concat(y.Composition.Select(x => (object)x))
                            .Concat(new object[] { y.RomanticPairs, y.MixedPairs, y.SameSexPairs, y.Unions, y.UnionsOutsidePartnership }).Concat(y.WomanDays.Select(x => (object)x))
                            .Concat(new object[] { y.CareTicks, y.ChildFedActs, y.ChildFedUnits, y.ChildFedByParent, y.ChildFedByNonParent, y.Tended, y.ArrangementsMade, y.ArrangementsEnded, y.Fostered, y.Orphaned,
                                y.FoodForaged, y.FoodProducedOther, y.FoodEaten, y.FoodSpoiled, y.FoodBuried, y.Regeneration, y.ForageTicks, y.WalkTicks, y.SourcesForaged, y.FoodSources,
                                y.WildFoodStock, y.WildFoodCapacity, y.WildFoodMsyPerDay, y.ReachableWildFood, y.StoreFood, y.CarriedFood, y.GroundFood, y.FactionsAlive, y.MaxGenerationAlive,
                                y.StarveFoodWithin5, y.StarveFoodWithin10, y.StarveCapacityWithin10, y.StarveDeathsNoFoodWithin10, y.MeanNutritionAdults, y.MeanNutritionChildren })) + "\n");
                }
                txt.Append(ArmText(name, arm));
            }
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "runs.csv"), runs.ToString());
            File.WriteAllText(Path.Combine(dir, "yearly.csv"), yearly.ToString());
            File.WriteAllText(Path.Combine(dir, "report.txt"), txt.ToString());
            return txt.ToString();
        }

        public static string ArmText(string name, List<DemogRunSummary> arm)
        {
            var t = new StringBuilder();
            int n = arm.Count;
            t.AppendLine("## " + name + " (" + n + " seeds, " + arm.Max(s => s.Years) + " years max)");
            var outcomes = arm.GroupBy(s => s.Outcome).OrderBy(g => g.Key).Select(g => g.Key + " " + g.Count());
            t.AppendLine("outcomes: " + string.Join(", ", outcomes) + $"; never extinct {arm.Count(s => s.End > 0)}/{n}; reconcile {arm.Count(s => s.Reconciles)}/{n}; invariant failures {arm.Sum(s => s.InvariantFailures)}; exit codes {string.Join(" ", arm.Select(s => s.ExitCode).Distinct())}");
            t.AppendLine($"population: start {MS(arm.Select(s => (double)s.Start), 0)}, year 3 {MS(arm.Select(s => (double)s.Year3))}, end {MS(arm.Select(s => (double)s.End))}; end/year3 {MS(arm.Where(s => s.Year3 > 0).Select(s => (double)s.End / s.Year3), 2)}; max generation {MS(arm.Select(s => (double)s.MaxGeneration))}");
            // population by year (mean over seeds, extinct seeds count 0)
            int years = arm.Max(s => s.Rec.Years.Count);
            t.AppendLine("population at each year start (mean): " + string.Join(" ", Enumerable.Range(0, years).Select(y => F(arm.Average(s => y < s.Rec.Years.Count ? (double)s.Rec.Years[y].PopStart : 0), 0))));
            t.AppendLine($"births {MS(arm.Select(s => (double)s.Births))}; deaths {MS(arm.Select(s => (double)s.Deaths))}: "
                + string.Join(", ", Enumerable.Range(1, 8).Where(i => arm.Sum(s => s.DeathsByCause[i]) > 0).Select(i => Causes[i] + " " + MS(arm.Select(s => (double)s.DeathsByCause[i])))));
            t.AppendLine($"fertility: completed (women who lived through their fertile span, born in the run or founders who started it as children) {MS(arm.Select(s => s.CompletedFertility), 2)} children per woman over {arm.Sum(s => s.CompletedWomen)} women; "
                + $"pooled {F(Ratio(arm.Sum(s => s.CompletedBirths), arm.Sum(s => s.CompletedWomen)), 2)}");
            t.AppendLine($"   censored (alive and fertile at the end) {arm.Sum(s => s.CensoredWomen)} women, {F(Ratio(arm.Sum(s => s.CensoredBirths), arm.Sum(s => s.CensoredWomen)), 2)} so far; died during the span {arm.Sum(s => s.BornWomenDiedFertile)} women, {F(Ratio(arm.Sum(s => s.BornWomenDiedFertileBirths), arm.Sum(s => s.BornWomenDiedFertile)), 2)}; "
                + $"founders who started as adults (left-censored) {arm.Sum(s => s.FounderLateWomen)} women, {F(Ratio(arm.Sum(s => s.FounderLateBirths), arm.Sum(s => s.FounderLateWomen)), 2)}");
            long wd = arm.Sum(s => s.WomanDays.Sum());
            t.AppendLine("   reproductive opportunity (fertile woman-days " + wd + "): " + string.Join(", ", DemogRecorder.WomanStatusNames.Select((nm, i) => nm + " " + F(Ratio(arm.Sum(s => s.WomanDays[i]) * 100.0, wd), 1) + "%")));
            t.AppendLine($"child mortality (died before 1.0 / outcome known) {MS(arm.Select(s => s.ChildMortality), 3)}; pooled {F(Ratio(arm.Sum(s => s.ChildDied), arm.Sum(s => s.ChildOutcomeKnown)), 3)} of {arm.Sum(s => s.ChildOutcomeKnown)}; reached adulthood {arm.Sum(s => s.ChildReachedAdult)}; censored {arm.Sum(s => s.ChildCensored)}; causes: "
                + string.Join(", ", Enumerable.Range(1, 8).Where(i => arm.Sum(s => s.ChildDeathsByCause[i]) > 0).Select(i => Causes[i] + " " + arm.Sum(s => s.ChildDeathsByCause[i]))));
            // age-specific mortality (deaths per person-year), pooled
            var dBand = new long[DemogRecorder.AgeBands.Length]; var pdBand = new long[DemogRecorder.AgeBands.Length];
            foreach (var s in arm) foreach (var y in s.Rec.Years) for (int b = 0; b < dBand.Length; b++) { dBand[b] += y.DeathsByAgeBand[b]; pdBand[b] += y.PersonDaysByAgeBand[b]; }
            int dpy = arm[0].Rec.DaysPerYear;
            t.AppendLine("age-specific mortality (deaths per person-year, pooled): " + string.Join(", ", DemogRecorder.AgeBandNames.Select((nm, b) => nm + " " + F(pdBand[b] == 0 ? double.NaN : dBand[b] * (double)dpy / pdBand[b], 2))));
            // age structure (pooled, year 3 and the last common year)
            foreach (int yy in new[] { 0, 3, 6, 10, 14 })
            {
                var at = arm.Where(s => yy < s.Rec.Years.Count && s.Rec.Years[yy].PopStart > 0).Select(s => s.Rec.Years[yy]).ToList();
                if (at.Count == 0) continue;
                t.AppendLine($"age structure year {yy} ({at.Count} seeds alive; 0.25-year bins, summed): " + string.Join(" ", Enumerable.Range(0, DemogRecorder.AgeBinCount).Select(b => at.Sum(y => y.AgeBins[b])))
                    + $"; dependency (children+elders)/adults {F(Ratio(at.Sum(y => y.Children + y.Elders), at.Sum(y => y.Adults)), 2)}; households {at.Sum(y => y.Households)} mean size {F(Ratio(at.Sum(y => y.HouseholdMembers), at.Sum(y => y.Households)), 2)} max {at.Max(y => y.MaxHouseholdSize)}; "
                    + "composition " + string.Join(", ", DemogRecorder.CompositionNames.Select((nm, i) => nm + " " + at.Sum(y => y.Composition[i])))
                    + $"; romantic pairs {at.Sum(y => y.RomanticPairs)} (same-sex {at.Sum(y => y.SameSexPairs)}), reproductive unions {at.Sum(y => y.Unions)} (outside a partnership {at.Sum(y => y.UnionsOutsidePartnership)})");
            }
            long pd = arm.Sum(s => s.PersonDays);
            var allY = arm.SelectMany(s => s.Rec.Years).ToList();
            t.AppendLine($"care: care ticks per person-day {F(Ratio(arm.Sum(s => s.CareTicks), pd), 1)}; child feedings {allY.Sum(y => y.ChildFedActs)} ({allY.Sum(y => y.ChildFedUnits)} units; by a parent {allY.Sum(y => y.ChildFedByParent)}, by a non-parent {allY.Sum(y => y.ChildFedByNonParent)}); tended {allY.Sum(y => y.Tended)}; "
                + $"arrangements made {allY.Sum(y => y.ArrangementsMade)} ended {allY.Sum(y => y.ArrangementsEnded)}; orphaned {allY.Sum(y => y.Orphaned)} fostered {allY.Sum(y => y.Fostered)}");
            t.AppendLine($"food (units, per run): foraged {MS(arm.Select(s => (double)s.FoodForaged), 0)}, eaten {MS(arm.Select(s => (double)s.FoodEaten), 0)}, spoiled {MS(arm.Select(s => (double)s.FoodSpoiled), 0)} ({F(Ratio(arm.Sum(s => s.FoodSpoiled) * 100.0, arm.Sum(s => s.FoodForaged)), 1)}% of foraged), "
                + $"regenerated {MS(arm.Select(s => (double)s.Regeneration), 0)}; eaten per person-day {MS(arm.Select(s => s.EatenPerPersonDay), 2)}; foraging yield {MS(arm.Select(s => s.ForageYield), 2)} units per 1000 forage ticks; walk ticks per person-day {F(Ratio(arm.Sum(s => s.WalkTicks), pd), 0)}");
            double msy = arm.Average(s => s.MsyPerDay);
            double need = Ratio(arm.Sum(s => s.FoodEaten), pd);
            t.AppendLine($"capacity: wild food sources {arm[0].Rec.Years[0].FoodSources}, capacity {arm[0].Rec.Years[0].WildFoodCapacity} units, logistic MSY {F(msy, 0)} units/day; at the observed {F(need, 2)} units eaten per person-day "
                + $"the island feeds about K = {F(need > 0 ? msy / need : double.NaN, 0)} people at MSY; founders {F(arm.Average(s => (double)s.Start), 0)} ({F(arm.Average(s => s.DensityPerHex), 2)} per hex of {arm[0].Rec.LandHexes}); "
                + $"mean wild stock {MS(arm.Select(s => s.MeanStockShare * 100), 1)}% of capacity; share of standing wild food within {DemogRecorder.ReachNodes} nodes of a living person {MS(arm.Select(s => s.MeanReachShare * 100), 1)}%");
            int sd = allY.Sum(y => y.StarveByAgeBand.Sum());
            t.AppendLine($"starvation context ({sd} deaths): by age " + string.Join(", ", DemogRecorder.AgeBandNames.Select((nm, b) => nm + " " + allY.Sum(y => y.StarveByAgeBand[b])))
                + $"; mean standing wild food within 5 nodes of the death {F(Ratio(allY.Sum(y => y.StarveFoodWithin5), sd), 0)} units, within 10 nodes {F(Ratio(allY.Sum(y => y.StarveFoodWithin10), sd), 0)} units "
                + $"({F(Ratio(allY.Sum(y => y.StarveFoodWithin10) * 100.0, allY.Sum(y => y.StarveCapacityWithin10)), 1)}% of the capacity there); deaths with no wild food within 10 nodes {allY.Sum(y => y.StarveDeathsNoFoodWithin10)}; "
                + $"nearest source holding 5+ units: mean {F(Ratio(allY.Sum(y => y.StarveNearestSum), allY.Sum(y => y.StarveNearestHist.Sum())), 1)} nodes; within 10 {allY.Sum(y => y.StarveNearestHist[0])}, 11-20 {allY.Sum(y => y.StarveNearestHist[1])}, 21-40 {allY.Sum(y => y.StarveNearestHist[2])}, beyond 40 {allY.Sum(y => y.StarveNearestHist[3])}");
            var cens = allY.Where(y => y.PopStart > 0).ToList();
            t.AppendLine($"nutrition at the year censuses (permille, mean of yearly means): adults {F(cens.Where(y => y.Adults + y.Elders > 0).Select(y => (double)y.MeanNutritionAdults).DefaultIfEmpty(double.NaN).Average(), 0)}, children {F(cens.Where(y => y.Children > 0).Select(y => (double)y.MeanNutritionChildren).DefaultIfEmpty(double.NaN).Average(), 0)}");
            t.AppendLine($"founder deaths: largest share in any 30-day window {MS(arm.Select(s => s.MaxFounderDeathShare30 / 10.0), 1)}% (max {F(arm.Max(s => s.MaxFounderDeathShare30) / 10.0, 1)}%), in any 3-day (0.3-year) window {MS(arm.Select(s => s.MaxFounderDeathShare3 / 10.0), 1)}% (max {F(arm.Max(s => s.MaxFounderDeathShare3) / 10.0, 1)}%)");
            t.AppendLine("child feedings by the feeder's believed kin band: " + string.Join(", ", new[] { "none (unrelated)", "parent", "sibling", "other kin (grandparent, aunt...)", "partner" }
                .Select((nm, b) => nm + " " + allY.Sum(y => y.ChildFedByBand[b]))));
            // M4.40: preferences, a paired comparison within each seed (high >= 700 against low <= 300)
            t.AppendLine(Pref(arm, "child desire", p => p.ChildDesire, p => p.AdultDays > 0 ? 10.0 * p.Tries / p.AdultDays : double.NaN, "tries for a child per adult-year")
                + "; " + Pref(arm, "child desire", p => p.ChildDesire, p => p.AdultDays > 0 ? 10.0 * p.Wants / p.AdultDays : double.NaN, "shows of wanting a child per adult-year")
                + "; " + Pref(arm, "child desire", p => p.Sex == 0 && p.AdultDays >= 10 ? p.ChildDesire : -1, p => 10.0 * p.Births / p.AdultDays, "births per woman-year"));
            t.AppendLine(Pref(arm, "companionship", p => p.Companionship, p => p.AdultDays > 0 ? (double)p.PartneredDays / p.AdultDays : double.NaN, "share of adult days partnered")
                + "; " + Pref(arm, "companionship", p => p.Companionship, p => p.AdultDays > 0 ? 10.0 * p.PartnershipsFormed / p.AdultDays : double.NaN, "partnerships formed per adult-year"));
            t.AppendLine($"reproductive relationships formed: {allY.Sum(y => y.UnionsFormed)} ({allY.Sum(y => y.UnionsOutsideFormed)} outside a partnership); births to a father outside the mother's partnership: "
                + arm.Sum(s => s.Rec.People.Values.Count(p => p.Born && p.FatherOutside)));
            // M4.44: shared-care arrangements and free-riding
            var arrs = arm.SelectMany(s => s.Rec.Arrangements.Values).ToList();
            var lopsided = arrs.Where(a => Math.Max(a.Given, a.Returned) >= 400 && Math.Max(a.Given, a.Returned) >= 2 * Math.Max(1, Math.Min(a.Given, a.Returned))).ToList();
            int endedByExploited = lopsided.Count(a => a.Reason == 1 && a.EndedBy == (a.Given >= a.Returned ? a.Carer : a.Asker));
            t.AppendLine($"shared-care arrangements: {arrs.Count} made; one-sided (one side gave at least 400 ticks and at least twice the other's) {lopsided.Count}: ended by the side giving more {endedByExploited}, "
                + $"by the other side {lopsided.Count(a => a.Reason == 1) - endedByExploited}, by a death {lopsided.Count(a => a.Reason == 2)}, by a household change {lopsided.Count(a => a.Reason == 3)}, still open {lopsided.Count(a => a.EndedTick < 0)}");
            var adultsAll = arm.SelectMany(s => s.Rec.People.Values).Where(p => p.AdultDays >= 5).ToList();
            var riders = adultsAll.Where(p => p.Withdrawn >= 10 && p.Withdrawn > 2 * p.Deposited).ToList();
            var givers = adultsAll.Where(p => p.Deposited >= 10 && p.Deposited >= p.Withdrawn).ToList();
            t.AppendLine($"household stores: adults who took at least 10 units and more than twice what they put in (free-riding) {riders.Count} of {adultsAll.Count}; net contributors {givers.Count}. "
                + $"Starved: free-riders {F(Ratio(riders.Count(p => p.Cause == DeathCause.Starvation) * 100.0, riders.Count), 1)}%, contributors {F(Ratio(givers.Count(p => p.Cause == DeathCause.Starvation) * 100.0, givers.Count), 1)}%; "
                + $"births per woman: free-riders {F(Ratio(riders.Where(p => p.Sex == 0).Sum(p => p.Births), riders.Count(p => p.Sex == 0)), 2)}, contributors {F(Ratio(givers.Where(p => p.Sex == 0).Sum(p => p.Births), givers.Count(p => p.Sex == 0)), 2)}");
            // survival across generations
            var gens = arm.SelectMany(s => s.Rec.People.Values.Where(p => p.Born)).GroupBy(p => p.Generation).OrderBy(g => g.Key);
            t.AppendLine("generations (born in the run: births, reached 1.0, still alive at the end): " + string.Join("; ", gens.Select(g => "g" + g.Key + " " + g.Count() + "/" + g.Count(p => p.MaturedTick >= 0) + "/" + g.Count(p => p.DeathTick < 0))));
            t.AppendLine();
            return t.ToString();
        }

        private static double Ratio(double a, double b) => b == 0 ? double.NaN : a / b;

        /// <summary>
        /// A paired comparison (M4.40): in each seed, the mean of <paramref name="measure"/> over people whose preference is
        /// at least 700 against those at most 300 (people with a negative key are left out); the mean difference over seeds
        /// with its interval, and in how many seeds high exceeded low.
        /// </summary>
        private static string Pref(List<DemogRunSummary> arm, string pref, Func<DemogPerson, int> key, Func<DemogPerson, double> measure, string what)
        {
            var diffs = new List<double>(); int higher = 0; double hiSum = 0, loSum = 0; int n = 0;
            foreach (var s in arm)
            {
                var hi = s.Rec.People.Values.Where(p => key(p) >= 700).Select(measure).Where(x => !double.IsNaN(x)).ToList();
                var lo = s.Rec.People.Values.Where(p => key(p) >= 0 && key(p) <= 300).Select(measure).Where(x => !double.IsNaN(x)).ToList();
                if (hi.Count < 2 || lo.Count < 2) continue;
                double d = hi.Average() - lo.Average();
                diffs.Add(d); if (d > 0) higher++;
                hiSum += hi.Average(); loSum += lo.Average(); n++;
            }
            if (n == 0) return what + " by " + pref + ": na";
            return $"{what} by {pref}: high {F(hiSum / n, 3)} vs low {F(loSum / n, 3)}, difference {MS(diffs, 3)}, high above low in {higher}/{n} seeds";
        }
    }
}
