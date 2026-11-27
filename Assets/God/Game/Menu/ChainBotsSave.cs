using System.Collections.Generic;
using God.Sim;
using Newtonsoft.Json;

namespace God.Game
{
    /// <summary>
    /// Saves what the scripted builders (<see cref="GodBootstrap.Chains"/>, the bots of the game until the faction AI
    /// takes over) have already placed, so a loaded game goes on with the next item instead of building again.
    /// It is the first <see cref="ISaveExtra"/>; the faction AI registers its own the same way.
    /// </summary>
    public sealed class ChainBotsSave : ISaveExtra
    {
        private sealed class Entry
        {
            public int Faction;
            public Dictionary<string, int> Placed = new Dictionary<string, int>();
            public List<string> Skipped = new List<string>();
            public int HqsPlaced;
        }

        private readonly GodBootstrap _boot;
        public ChainBotsSave(GodBootstrap boot) { _boot = boot; }

        public string SaveKey => "chainbots";

        public string SaveState()
        {
            var list = new List<Entry>();
            foreach (var c in _boot.Chains)
                list.Add(new Entry { Faction = (int)c.Side, Placed = new Dictionary<string, int>(c.Placed), Skipped = new List<string>(c.Skipped), HqsPlaced = c.HqsPlaced });
            return JsonConvert.SerializeObject(list);
        }

        public void LoadState(string data)
        {
            List<Entry> list;
            try { list = JsonConvert.DeserializeObject<List<Entry>>(data); } catch (JsonException) { return; }
            if (list == null) return;
            foreach (var e in list)
            {
                var bot = _boot.Chains.Find(c => (int)c.Side == e.Faction);
                if (bot == null) continue;
                bot.Placed.Clear(); foreach (var kv in e.Placed) bot.Placed[kv.Key] = kv.Value;
                bot.Skipped.Clear(); foreach (var k in e.Skipped) bot.Skipped.Add(k);
                bot.HqsPlaced = e.HqsPlaced;
            }
        }
    }
}
