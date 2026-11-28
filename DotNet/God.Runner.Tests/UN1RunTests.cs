using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using God.Recording;
using God.Sim;
using God.Sim.Unified;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>UN1 (spec 12, 13): the events of the world serialise, register in 1200-1219 and decode; the <c>unif_world</c> scenario runs and replays.</summary>
    [Parallelizable(ParallelScope.None)]
    public class UN1RunTests
    {
        static SimEvent[] AllEvents() => new SimEvent[]
        {
            new ForageCollected { Actor = 7, Place = 11, Node = 2345, Ware = Ware.Rabbit, Ticks = 90, Origin = (byte)ControlOrigin.Fixture },
            new AllowanceGranted { Hex = 31, Roll = 9, Amount = 6, Pending = 12 },
            new AllowanceWasted { Hex = 31, Roll = 9, Amount = 4 },
            new RawMaterialised { Hex = 31, Node = 3011, Ware = Ware.Stone, Units = 1, Stock = 5 },
            new WaterUsed { Actor = 7, Point = 3, Fresh = true, Slot = 1, Origin = (byte)ControlOrigin.Policy },
            new UnifiedDay { Day = 4, Alive = 70, Pending = 120, Stock = 300, Foraged = new[] { 1, 2, 3, 4, 5, 6, 7, 8 } },
        };

        [Test]
        public void UN1_EventsRoundTrip()
        {
            var codes = new HashSet<int>();
            var writer = new EventJson("run");
            var (lo, hi) = UnifiedEvents.BlockOf("UN1");
            Assert.AreEqual((1200, 1219), (lo, hi));
            foreach (var e in AllEvents())
            {
                e.Tick = 321; e.Seq = 2;
                var info = EventCatalog.Of(e.GetType());
                Assert.IsTrue(info.Code >= lo && info.Code <= hi, info.Name + " lies in the UN1 block");
                Assert.AreEqual(TelemetryDomain.UnifiedWorld, info.Domain);
                Assert.IsTrue(codes.Add(info.Code), "codes are unique");
                var o = JObject.Parse(writer.Line(e));
                Assert.AreEqual((int)info.Code, (int)o["c"]); Assert.AreEqual(info.Name, (string)o["e"]); Assert.AreEqual(321, (long)o["t"]); Assert.AreEqual(1, (int)o["v"]);
                var payload = (JObject)o["p"];
                var fields = e.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.Name != "Tick" && f.Name != "Seq").ToList();
                Assert.Greater(fields.Count, 2);
                foreach (var f in fields) Assert.IsNotNull(payload[f.Name], info.Name + "." + f.Name + " is in the payload");
                // decode: the payload reads back into an event of the same type with every field equal
                var back = (SimEvent)payload.ToObject(e.GetType(), JsonSerializer.Create(SaveFile.Settings));
                foreach (var f in fields)
                {
                    var a = f.GetValue(e); var b = f.GetValue(back);
                    if (a is Array aa) CollectionAssert.AreEqual(aa, (Array)b, info.Name + "." + f.Name); else Assert.AreEqual(a, b, info.Name + "." + f.Name);
                }
            }
            Assert.AreEqual(6, codes.Count);
            foreach (int c in new[] { 1200, 1201, 1202, 1203, 1204, 1205 }) Assert.IsTrue(codes.Contains(c), "code " + c);
            Assert.IsTrue(EventCatalog.Of(typeof(ForageCollected)).HighVolume); Assert.IsTrue(EventCatalog.Of(typeof(RawMaterialised)).HighVolume); Assert.IsTrue(EventCatalog.Of(typeof(WaterUsed)).HighVolume);
        }

        [Test]
        public void UN1_Scenario_unif_world_RunsAndReplays()
        {
            var specs = new List<RunSpec>();
            for (int k = 0; k < 2; k++)
            {
                var sp = TestUtil.Spec("unif_world", 1, 3600);
                sp.Scenario.Checks.Remove("smoke");
                sp.CheckpointEvery = 600;
                specs.Add(sp);
            }
            var r = TestUtil.RunAll(specs);
            Assert.AreEqual(0, r[0].ExitCode, r[0].Error + " " + r[0].StopReason + " " + string.Join(" | ", r[0].Messages));
            CollectionAssert.AreEqual(TestUtil.Seq(r[0]), TestUtil.Seq(r[1]), "identical at every checkpoint");
            Assert.AreEqual(r[0].FinalHash, r[1].FinalHash);
        }
    }
}
