using System;
using System.Collections.Generic;
using God.Sim;
using Newtonsoft.Json;

namespace God.Game
{
    /// <summary>The persisted pieces of the player's view state. Never in GodState, never hashed, never reaching the sim.</summary>
    public sealed class UiState
    {
        public Dictionary<int, string> Aliases = new Dictionary<int, string>();
        public List<int> Favourites = new List<int>();
        public List<int> Follows = new List<int>();
        public Dictionary<int, string> Notes = new Dictionary<int, string>();
        public SelectionSave Selection;
        public CameraPose? Camera;
    }

    /// <summary>The selection as saved (ids only).</summary>
    public sealed class SelectionSave
    {
        public SelectionKind Kind;
        public int[] Ids = Array.Empty<int>();
        public int Primary, CentreNode, RadiusSteps, Node = -1;
        public ObjectKind ObjectKind;
    }

    /// <summary>
    /// The <c>ui</c> save extra (F1.18, U1): player aliases, favourites, follows, notes, the selection and the camera pose, as JSON
    /// under the key <c>ui</c>. It is written into the save file's extras, which the state hash never covers (U1-T9): names never
    /// change the game. Procedural true names come from <see cref="TrueName"/> (F1's Names.Person once F1a is in); the card shows
    /// the alias over the true name.
    /// </summary>
    public sealed class UiExtra : ISaveExtra
    {
        public const string Key = "ui";
        public const int MaxRoster = 12;

        public UiState State { get; private set; } = new UiState();
        /// <summary>The true (procedural) name of a person; set by the bootstrap. Default: "person #id".</summary>
        public Func<int, string> TrueName = id => "person #" + id;
        /// <summary>Where the live camera pose and selection are read from when saving (set by the player controller).</summary>
        public Func<CameraPose?> CaptureCamera;
        public Func<SelectionModel> CaptureSelection;
        /// <summary>Raised after a load put new state in: the player controller applies the camera and the selection.</summary>
        public event Action Loaded;
        /// <summary>Raised when aliases, favourites, follows or notes change.</summary>
        public event Action Changed;

        public string SaveKey => Key;

        // ---------------------------------------------------------------- names, favourites, follows, notes

        /// <summary>The alias when there is one, else the true name.</summary>
        public string NameOf(int id) => State.Aliases.TryGetValue(id, out var a) && !string.IsNullOrEmpty(a) ? a : TrueName(id);

        public bool HasAlias(int id) => State.Aliases.TryGetValue(id, out var a) && !string.IsNullOrEmpty(a);

        public void Rename(int id, string alias)
        {
            alias = (alias ?? "").Trim();
            if (alias.Length > 40) alias = alias.Substring(0, 40);
            if (alias.Length == 0) State.Aliases.Remove(id); else State.Aliases[id] = alias;
            Changed?.Invoke();
        }

        public bool IsFavourite(int id) => State.Favourites.Contains(id);
        public bool IsFollowed(int id) => State.Follows.Contains(id);

        public bool ToggleFavourite(int id) { bool on = Toggle(State.Favourites, id); Changed?.Invoke(); return on; }
        public bool ToggleFollow(int id) { bool on = Toggle(State.Follows, id); Changed?.Invoke(); return on; }

        private static bool Toggle(List<int> l, int id)
        {
            if (l.Remove(id)) return false;
            l.Add(id);
            return true;
        }

        public void SetNote(int id, string note)
        {
            if (string.IsNullOrEmpty(note)) State.Notes.Remove(id); else State.Notes[id] = note;
            Changed?.Invoke();
        }

        /// <summary>The chips along the left edge: favourites first, then followed people, no repeats, at most <see cref="MaxRoster"/>.</summary>
        public List<int> Roster()
        {
            var l = new List<int>();
            foreach (int id in State.Favourites) if (l.Count < MaxRoster && !l.Contains(id)) l.Add(id);
            foreach (int id in State.Follows) if (l.Count < MaxRoster && !l.Contains(id)) l.Add(id);
            return l;
        }

        // ---------------------------------------------------------------- save

        public string SaveState()
        {
            var s = State;
            var cam = CaptureCamera?.Invoke();
            if (cam.HasValue) s.Camera = cam;
            var sel = CaptureSelection?.Invoke();
            if (sel != null)
                s.Selection = new SelectionSave
                {
                    Kind = sel.Kind, Ids = new List<int>(sel.Ids).ToArray(), Primary = sel.Primary, CentreNode = sel.Area.CentreNode,
                    RadiusSteps = sel.Area.RadiusSteps, Node = sel.Node, ObjectKind = sel.ObjectKind,
                };
            return JsonConvert.SerializeObject(s);
        }

        public void LoadState(string data)
        {
            UiState s = null;
            try { s = JsonConvert.DeserializeObject<UiState>(data); } catch (JsonException) { }
            if (s == null) return;
            if (s.Aliases == null) s.Aliases = new Dictionary<int, string>();
            if (s.Favourites == null) s.Favourites = new List<int>();
            if (s.Follows == null) s.Follows = new List<int>();
            if (s.Notes == null) s.Notes = new Dictionary<int, string>();
            State = s;
            Loaded?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Puts a saved selection into a selection model (ids only; the sim has the truth).</summary>
        public static void Restore(SelectionSave s, SelectionModel into)
        {
            if (s == null) return;
            switch (s.Kind)
            {
                case SelectionKind.Person: into.SetPerson(s.Primary); break;
                case SelectionKind.Group: into.SetGroup(s.Ids); break;
                case SelectionKind.Household: into.SetHousehold(s.Ids.Length > 0 ? s.Ids[0] : 0, s.Primary); break;
                case SelectionKind.Institution: into.SetInstitution(s.Primary); break;
                case SelectionKind.Object: into.SetObject(s.ObjectKind, s.Primary); break;
                case SelectionKind.Area: into.SetArea(new AreaSpec(s.CentreNode, s.RadiusSteps)); break;
                case SelectionKind.Location: into.SetLocation(s.Node); break;
                default: into.Clear(); break;
            }
        }
    }
}
