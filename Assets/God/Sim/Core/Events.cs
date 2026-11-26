using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Something that happened during a tick, for views to animate and recorders to store. Every concrete event is a
    /// <see cref="DomainEvent"/> listed in <see cref="EventCatalog"/> (M0).
    /// </summary>
    public abstract class SimEvent
    {
        /// <summary>Tick during which the event happened. Stamped by <see cref="EventBuffer.Add"/>.</summary>
        public long Tick;

        /// <summary>
        /// Order within the tick (0, 1, 2...), stamped by <see cref="EventBuffer.Add"/> and reset every tick. With the tick it
        /// is the event's identity: commands first in submission order, then systems in ascending order, each in ascending
        /// entity id, which the sim already guarantees.
        /// </summary>
        public int Seq;
    }

    /// <summary>Collects events in the order systems emit them.</summary>
    public sealed class EventBuffer
    {
        private readonly List<SimEvent> _items = new List<SimEvent>();
        private long _seqTick = long.MinValue;
        private int _seq;

        /// <summary>Tick stamped onto added events (set by GodSim at the start of each Step).</summary>
        public long CurrentTick;

        public IReadOnlyList<SimEvent> Items => _items;
        public int Count => _items.Count;

        public void Add(SimEvent e)
        {
            if (CurrentTick != _seqTick) { _seqTick = CurrentTick; _seq = 0; }
            e.Tick = CurrentTick;
            e.Seq = _seq++;
            _items.Add(e);
        }

        public List<SimEvent> Drain()
        {
            var copy = new List<SimEvent>(_items);
            _items.Clear();
            return copy;
        }

        /// <summary>Moves the events since the last drain into <paramref name="into"/> (appended) without allocating a list.</summary>
        public void DrainInto(List<SimEvent> into)
        {
            into.AddRange(_items);
            _items.Clear();
        }
    }

    /// <summary>A queued command failed its re-check at the tick boundary and was not applied.</summary>
    public sealed class CommandRejected : DomainEvent
    {
        public Command Command;
        public CommandResult Result;
    }

    /// <summary>A queued command was applied at the tick boundary.</summary>
    public sealed class CommandApplied : DomainEvent
    {
        public Command Command;
    }
}
