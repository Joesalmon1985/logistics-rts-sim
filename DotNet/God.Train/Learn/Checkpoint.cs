using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using God.Sim.Unified;
using God.Train.Env;
using God.Train.Net;

namespace God.Train.Learn
{
    /// <summary>
    /// A resumable training state (thresholds-independent): the float master weights, the optimiser state and the step reached.
    /// Written as little-endian binary with no time stamp, so the same state gives the same bytes (test <c>Train_Resume_Exact</c>).
    /// </summary>
    public sealed class Checkpoint
    {
        public const string Magic = "UN6TCKPT1";
        public string Kind = "bc";          // "init" | "bc" | "es"
        public string Stage = "";
        public string Shape = "";
        public string World = "stub";
        public int Step;                    // epochs (bc) or iterations (es) completed
        public float[] P = new float[0], M = new float[0], V = new float[0];
        public double B1Pow = 1, B2Pow = 1;
        public string MetricsJson = "{}";
        public string SeedsText = "";

        public NetShape NetShape => Net.NetShape.Parse(Shape);
        public QNet ToQNet() => FloatNet.ToQNet(NetShape, P);

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                w.Write(Magic); w.Write(Kind); w.Write(Stage); w.Write(Shape); w.Write(World); w.Write(Step); w.Write(SeedsText);
                WriteF(w, P); WriteF(w, M); WriteF(w, V);
                w.Write(B1Pow); w.Write(B2Pow); w.Write(MetricsJson);
            }
            return ms.ToArray();
        }

        public static Checkpoint FromBytes(byte[] b)
        {
            using var r = new BinaryReader(new MemoryStream(b), Encoding.UTF8);
            if (r.ReadString() != Magic) throw new InvalidDataException("not a god-train checkpoint");
            var c = new Checkpoint { Kind = r.ReadString(), Stage = r.ReadString(), Shape = r.ReadString(), World = r.ReadString(), Step = r.ReadInt32(), SeedsText = r.ReadString() };
            c.P = ReadF(r); c.M = ReadF(r); c.V = ReadF(r);
            c.B1Pow = r.ReadDouble(); c.B2Pow = r.ReadDouble(); c.MetricsJson = r.ReadString();
            return c;
        }

        public void Save(string path) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllBytes(path, ToBytes()); }
        public static Checkpoint Load(string path) => FromBytes(File.ReadAllBytes(path));

        private static void WriteF(BinaryWriter w, float[] a) { w.Write(a.Length); foreach (var x in a) w.Write(x); }
        private static float[] ReadF(BinaryReader r) { var a = new float[r.ReadInt32()]; for (int i = 0; i < a.Length; i++) a[i] = r.ReadSingle(); return a; }

        public static Checkpoint Untrained(NetShape s, ulong initSeed, string world)
            => new Checkpoint { Kind = "init", Shape = s.Text, World = world, P = FloatNet.Init(s, initSeed), SeedsText = "init:" + initSeed };
    }

    /// <summary>Demonstration files (teacher sequences, training only), little-endian binary in sequence order.</summary>
    public static class DemoFile
    {
        public const string Magic = "UN6TDEMO1";

        public static void Write(string path, List<Sequence> seqs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using var w = new BinaryWriter(File.Create(path));
            w.Write(Magic); w.Write(ObsSchema.SchemaHash); w.Write(ActSchema.SchemaHash); w.Write(seqs.Count);
            foreach (var s in seqs)
            {
                w.Write(s.Seed); w.Write(s.Person); w.Write((byte)s.Origin); w.Write(s.Samples.Count);
                foreach (var x in s.Samples)
                {
                    foreach (var o in x.Obs) w.Write(o);
                    var m = x.Mask;
                    w.Write(m.Control); w.Write(m.Target); w.Write(m.Source); w.Write(m.Slot); w.Write(m.Design); w.Write(m.Gesture); w.Write(m.Referent1); w.Write(m.Referent2); w.Write(m.Quantity);
                    for (int h = 0; h < Heads.Count; h++) w.Write(x.Label.Get(h));
                }
            }
        }

        public static List<Sequence> Read(string path)
        {
            using var r = new BinaryReader(File.OpenRead(path));
            if (r.ReadString() != Magic) throw new InvalidDataException("not a demonstration file");
            if (r.ReadString() != ObsSchema.SchemaHash || r.ReadString() != ActSchema.SchemaHash) throw new InvalidDataException("demonstrations were recorded on another Obs/Act schema");
            int n = r.ReadInt32(); var list = new List<Sequence>(n);
            for (int i = 0; i < n; i++)
            {
                var s = new Sequence { Seed = r.ReadInt64(), Person = r.ReadInt32(), Origin = (ControlOrigin)r.ReadByte() };
                int k = r.ReadInt32();
                for (int j = 0; j < k; j++)
                {
                    var obs = new short[ObsSchema.Length];
                    for (int o = 0; o < obs.Length; o++) obs[o] = r.ReadInt16();
                    var m = new ActMask { Control = r.ReadUInt64(), Target = r.ReadUInt64(), Source = r.ReadUInt64(), Slot = r.ReadUInt64(), Design = r.ReadUInt64(), Gesture = r.ReadUInt64(), Referent1 = r.ReadUInt64(), Referent2 = r.ReadUInt64(), Quantity = r.ReadUInt64() };
                    var a = ActionChoice.Of(Prim.None);
                    for (int h = 0; h < Heads.Count; h++) a.Set(h, r.ReadInt32());
                    s.Samples.Add(new Sample { Obs = obs, Mask = m, Label = a });
                }
                list.Add(s);
            }
            return list;
        }
    }
}
