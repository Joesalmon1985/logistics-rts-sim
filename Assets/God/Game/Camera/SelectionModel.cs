using System;
using System.Collections.Generic;

namespace God.Game
{
    /// <summary>What is selected. New kinds are added as enum values plus a registered describer; the core does not change.</summary>
    public enum SelectionKind { None, Person, Group, Household, Institution, Object, Area, Location }

    /// <summary>A circle on the ground in lattice units (CC-4): the sim resolves who and what is inside at the application tick.</summary>
    public readonly struct AreaSpec : IEquatable<AreaSpec>
    {
        public readonly int CentreNode;
        public readonly int RadiusSteps;
        public AreaSpec(int centreNode, int radiusSteps) { CentreNode = centreNode; RadiusSteps = radiusSteps; }
        public bool Equals(AreaSpec o) => CentreNode == o.CentreNode && RadiusSteps == o.RadiusSteps;
        public override bool Equals(object o) => o is AreaSpec a && Equals(a);
        public override int GetHashCode() => CentreNode * 397 ^ RadiusSteps;
        public override string ToString() => "Area(node " + CentreNode + ", " + RadiusSteps + " steps)";
        /// <summary>Metres to lattice steps (a step is 5 m), at least 1.</summary>
        public static int StepsOf(float metres, double step = 5.0) => Math.Max(1, (int)Math.Round(metres / step));
    }

    /// <summary>Object subtypes (the industrial workstream adds vehicle, power station, mine as new values).</summary>
    public enum ObjectKind { None = 0, Building = 1, Source = 2, Lot = 3 }

    /// <summary>An immutable copy of a selection: what the command builder reads (a pure function of the intent and this).</summary>
    public sealed class SelectionSnapshot
    {
        public SelectionKind Kind;
        /// <summary>Person ids (Person, Group: explicit, hand-picked, ascending) or the one id of a household, institution or object.</summary>
        public int[] Ids = Array.Empty<int>();
        public int Primary;
        public AreaSpec Area;
        public int Node = -1;
        public ObjectKind ObjectKind;
    }

    /// <summary>
    /// The player's selection (view only): a person, a hand-picked group, a household, an institution, an object, an area or a
    /// location. It holds ids and the one explicit scope the player gave; it never enumerates the contents of a household or an
    /// area (CC-4). The HUD's SelectedNpc, SelectedBuilding and SelectedNode follow it. No UnityEngine types: pure and testable.
    /// </summary>
    public sealed class SelectionModel
    {
        private readonly List<int> _ids = new List<int>();
        private readonly Dictionary<SelectionKind, Func<SelectionModel, string>> _describers = new Dictionary<SelectionKind, Func<SelectionModel, string>>();

        public SelectionKind Kind { get; private set; }
        public IReadOnlyList<int> Ids => _ids;
        public int Primary { get; private set; }
        public AreaSpec Area { get; private set; }
        public int Node { get; private set; } = -1;
        public ObjectKind ObjectKind { get; private set; }
        /// <summary>Where the selection is anchored on the ground (world metres), for markers and the camera.</summary>
        public float AnchorX { get; private set; }
        public float AnchorZ { get; private set; }
        /// <summary>Bumps on every change.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public bool IsEmpty => Kind == SelectionKind.None;
        public bool HasPersons => Kind == SelectionKind.Person || Kind == SelectionKind.Group;

        public void Clear()
        {
            if (Kind == SelectionKind.None) return;
            Reset(SelectionKind.None);
            Raise();
        }

        private void Reset(SelectionKind k)
        {
            Kind = k; _ids.Clear(); Primary = 0; Area = default; Node = -1; ObjectKind = ObjectKind.None;
        }

        private void Raise() { Version++; Changed?.Invoke(); }

        public void SetPerson(int id)
        {
            if (id == 0) { Clear(); return; }
            if (Kind == SelectionKind.Person && Primary == id) return;
            Reset(SelectionKind.Person); _ids.Add(id); Primary = id;
            Raise();
        }

        /// <summary>Shift or Ctrl click: adds the person, or takes it away when it is already in; two or more make a group.</summary>
        public void Toggle(int id)
        {
            if (id == 0) return;
            if (!HasPersons) { SetPerson(id); return; }
            if (!_ids.Remove(id)) _ids.Add(id);
            _ids.Sort();
            if (_ids.Count == 0) { Reset(SelectionKind.None); }
            else
            {
                Kind = _ids.Count == 1 ? SelectionKind.Person : SelectionKind.Group;
                if (!_ids.Contains(Primary)) Primary = _ids[0];
                if (_ids.Contains(id)) Primary = id;
            }
            Raise();
        }

        /// <summary>A hand-picked group (a box select): the explicit ids, ascending. Empty clears; one is a person.</summary>
        public void SetGroup(IEnumerable<int> ids)
        {
            var list = new List<int>(ids);
            list.Sort();
            for (int i = list.Count - 1; i > 0; i--) if (list[i] == list[i - 1]) list.RemoveAt(i);
            if (list.Count == 0) { Clear(); return; }
            if (list.Count == 1) { SetPerson(list[0]); return; }
            Reset(SelectionKind.Group); _ids.AddRange(list); Primary = list[0];
            Raise();
        }

        /// <summary>The household id (the sim resolves the members at the application tick); the person it was chosen from stays primary.</summary>
        public void SetHousehold(int householdId, int fromPerson)
        {
            if (householdId == 0) { SetPerson(fromPerson); return; }
            Reset(SelectionKind.Household); _ids.Add(householdId); Primary = fromPerson;
            Raise();
        }

        public void SetInstitution(int orgId)
        {
            if (orgId == 0) { Clear(); return; }
            Reset(SelectionKind.Institution); _ids.Add(orgId); Primary = orgId;
            Raise();
        }

        public void SetObject(ObjectKind kind, int id)
        {
            if (id == 0) { Clear(); return; }
            Reset(SelectionKind.Object); _ids.Add(id); Primary = id; ObjectKind = kind;
            Raise();
        }

        public void SetArea(AreaSpec area, float anchorX = 0f, float anchorZ = 0f)
        {
            if (Kind == SelectionKind.Area && Area.Equals(area)) return;
            Reset(SelectionKind.Area); Area = area; AnchorX = anchorX; AnchorZ = anchorZ;
            Raise();
        }

        public void SetLocation(int node, float anchorX = 0f, float anchorZ = 0f)
        {
            if (node < 0) { Clear(); return; }
            if (Kind == SelectionKind.Location && Node == node) return;
            Reset(SelectionKind.Location); Node = node; AnchorX = anchorX; AnchorZ = anchorZ;
            Raise();
        }

        public bool Contains(int personId) => HasPersons && _ids.Contains(personId);

        public SelectionSnapshot Snapshot() => new SelectionSnapshot
        {
            Kind = Kind, Ids = _ids.ToArray(), Primary = Primary, Area = Area, Node = Node, ObjectKind = ObjectKind,
        };

        /// <summary>Registers how a (possibly new) kind is described to the player.</summary>
        public void RegisterDescriber(SelectionKind kind, Func<SelectionModel, string> describe) { _describers[kind] = describe; }

        public string Describe()
        {
            if (_describers.TryGetValue(Kind, out var d)) return d(this);
            switch (Kind)
            {
                case SelectionKind.Person: return "Person #" + Primary;
                case SelectionKind.Group: return _ids.Count + " people";
                case SelectionKind.Household: return "Household " + (_ids.Count > 0 ? _ids[0] : 0);
                case SelectionKind.Institution: return "Institution " + Primary;
                case SelectionKind.Object: return ObjectKind + " " + Primary;
                case SelectionKind.Area: return Area.ToString();
                case SelectionKind.Location: return "Node " + Node;
                default: return "Nothing selected";
            }
        }
    }
}
