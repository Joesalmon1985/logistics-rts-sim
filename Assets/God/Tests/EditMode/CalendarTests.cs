using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>M0.12: <see cref="SimCalendar"/> days, years, life stages and LifeScale (POPULATION <c>LifeStages_Boundaries</c>).</summary>
    public class CalendarTests
    {
        [Test]
        public void Defaults_AreTheConventions()
        {
            var setup = new GameSetup(1UL);
            Assert.AreEqual(1200, setup.TicksPerDay);
            Assert.AreEqual(10, setup.DaysPerYear);
            Assert.AreEqual(100, setup.LifeScale);
            Assert.IsFalse(setup.DecayFollowsLifeScale);
            Assert.AreEqual(DiseaseSetting.On, setup.Disease);
            Assert.AreEqual(DiseaseSetting.Mild, TutorialIsland.Setup().Disease, "Mild in the tutorial (owner question 3 default)");
            Assert.AreEqual(12000, setup.Calendar.TicksPerYear);
        }

        [Test]
        public void DaysAndYears_TurnOverAtTheRightTicks()
        {
            var c = new SimCalendar(1200, 10, 100);
            Assert.AreEqual(0, c.Day(0)); Assert.AreEqual(0, c.Day(1199)); Assert.AreEqual(1, c.Day(1200));
            Assert.IsFalse(c.IsDayEnd(0)); Assert.IsFalse(c.IsDayEnd(1199)); Assert.IsTrue(c.IsDayEnd(1200)); Assert.IsFalse(c.IsDayEnd(1201));
            Assert.AreEqual(0, c.Year(11999)); Assert.AreEqual(1, c.Year(12000));
            Assert.AreEqual(9, c.DayOfYear(11999)); Assert.AreEqual(0, c.DayOfYear(12000));
            Assert.AreEqual(0, c.TickOfDay(1200)); Assert.AreEqual(600, c.TickOfDay(1800)); Assert.AreEqual(500, c.DayPermille(1800));
            Assert.AreEqual("Day 1, year 1", c.Label(0));
            Assert.AreEqual("Day 10, year 1", c.Label(11999));
            Assert.AreEqual("Day 1, year 2", c.Label(12000));
            var short_ = new SimCalendar(600, 4, 100);
            Assert.AreEqual(2400, short_.TicksPerYear); Assert.AreEqual(1, short_.Year(2400)); Assert.IsTrue(short_.IsDayEnd(600));
        }

        [Test]
        public void LifeStages_Boundaries_AtLifeScale100()
        {
            var c = new SimCalendar(1200, 10, 100);
            Assert.AreEqual(LifeStage.Child, c.StageAt(0, 11999), "child before 1.0");
            Assert.AreEqual(999, c.AgeMilli(0, 11999));
            Assert.AreEqual(LifeStage.Adult, c.StageAt(0, 12000), "adult from 1.0");
            Assert.AreEqual(LifeStage.Adult, c.StageAt(0, 35999), "adult until 3.0");
            Assert.AreEqual(LifeStage.Elder, c.StageAt(0, 36000), "elder from 3.0");
            Assert.AreEqual(1000, c.AgeMilli(500, 12500), "age counts from the birth tick");
            Assert.AreEqual(16, SimCalendar.HudAge(1000), "HUD age = sim year x 16");
            Assert.AreEqual(48, SimCalendar.HudAge(3000));
            Assert.AreEqual(0, SimCalendar.HudAge(c.AgeMilli(0, 0)));
        }

        [Test]
        public void LifeStages_AtLifeScale200_TakeHalfTheTicks()
        {
            var c = new SimCalendar(1200, 10, 200);
            Assert.AreEqual(LifeStage.Child, c.StageAt(0, 5999));
            Assert.AreEqual(LifeStage.Adult, c.StageAt(0, 6000), "adult at 1.0 sim years = half a calendar year");
            Assert.AreEqual(LifeStage.Adult, c.StageAt(0, 17999));
            Assert.AreEqual(LifeStage.Elder, c.StageAt(0, 18000));
            var normal = new SimCalendar(1200, 10, 100);
            foreach (long age in new long[] { 1000, 3000, 3500, 4500 })
                Assert.AreEqual(normal.TicksForAgeMilli(age), 2 * c.TicksForAgeMilli(age), "age " + age);
            Assert.AreEqual(1000, c.AgeMilli(0, c.TicksForAgeMilli(1000)));
            Assert.AreEqual(1000, normal.AgeMilli(0, normal.TicksForAgeMilli(1000)));
            Assert.AreEqual(12000, normal.TicksForAgeMilli(1000));
        }

        [Test]
        public void LifeScale_DoesNotChangeDaysOrYears()
        {
            var a = new SimCalendar(1200, 10, 100); var b = new SimCalendar(1200, 10, 250);
            for (long t = 0; t < 50000; t += 997) { Assert.AreEqual(a.Day(t), b.Day(t)); Assert.AreEqual(a.Year(t), b.Year(t)); }
        }

        [Test]
        public void Decay_IsPerDay_AndFollowsLifeScaleOnlyWhenAsked()
        {
            Assert.AreEqual(30, new SimCalendar(1200, 10, 200).DecayPermille(30), "default: decay is per sim day, not scaled");
            Assert.AreEqual(60, new SimCalendar(1200, 10, 200, true).DecayPermille(30), "when asked, forgetting keeps pace with lives");
            Assert.AreEqual(1000, new SimCalendar(1200, 10, 400, true).DecayPermille(600), "clamped");
            var t = new SimTuning();
            Assert.AreEqual(60, t.Decay.GrudgeBasePermille);
            Assert.IsTrue(t.TrySet("Decay.GrudgeBasePermille", 40, out _));
            Assert.AreEqual(40, t.Decay.GrudgeBasePermille);
        }

        [Test]
        public void TheGameCarriesItsCalendar()
        {
            var sim = GodSim.Create(new GameSetup(2UL, GameSetup.NoHuman) { TicksPerDay = 100, DaysPerYear = 3, LifeScale = 50 });
            sim.Step(300);
            var c = SimCalendar.Of(sim.State);
            Assert.AreEqual(3, c.Day(sim.State.Tick)); Assert.AreEqual(1, c.Year(sim.State.Tick)); Assert.AreEqual(50, c.LifeScale);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => GodSim.Create(new GameSetup(2UL) { TicksPerDay = 0 }));
        }
    }
}
