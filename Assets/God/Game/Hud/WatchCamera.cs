using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Watch-mode camera keys (P10): 1 to 6 fly to a faction's main HQ, Tab cycles through the recent captures and the fights
    /// going on. View only: it moves the camera.
    /// </summary>
    public sealed class WatchCamera : MonoBehaviour
    {
        public const float FocusHeight = 90f;
        private const int MaxSpots = 5;

        private GodBootstrap _boot;
        private readonly List<Vector3> _captures = new List<Vector3>();
        private int _tab = -1;

        public static WatchCamera Create(GodBootstrap boot)
        {
            var go = new GameObject("WatchCamera");
            go.transform.SetParent(boot.transform, false);
            var w = go.AddComponent<WatchCamera>();
            w._boot = boot;
            boot.SimEventRaised += w.OnEvent;
            return w;
        }

        private void OnDestroy() { if (_boot != null) _boot.SimEventRaised -= OnEvent; }

        private bool Active => _boot != null && _boot.Hud != null && _boot.Hud.Watching && (_boot.Menu == null || !_boot.Menu.Blocking);

        private void OnEvent(SimEvent e)
        {
            if (!(e is HqCaptured c)) return;
            var t = _boot.Sim.State.Territory;
            var hq = t.HqOf(c.HqId);
            if (hq == null) return;
            var g = t.Grid;
            _captures.Insert(0, new Vector3((float)g.X(hq.Node), 0f, (float)g.Z(hq.Node)));
            if (_captures.Count > MaxSpots) _captures.RemoveAt(_captures.Count - 1);
        }

        private void Update()
        {
            if (!Active) return;
            for (int i = 0; i < FactionInfo.Max; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) JumpToFaction((Faction)i);
            if (Input.GetKeyDown(KeyCode.Tab)) NextHotSpot();
        }

        /// <summary>The point of a faction's main HQ (its first finished one, else any), or null when it has none left.</summary>
        public Vector3? HqPoint(Faction f)
        {
            var t = _boot.Sim.State.Territory; var g = t.Grid;
            Hq best = null;
            foreach (var h in t.Hqs) if (h.Faction == f && (best == null || (best.Site && !h.Site))) best = h;
            return best == null ? (Vector3?)null : new Vector3((float)g.X(best.Node), 0f, (float)g.Z(best.Node));
        }

        /// <summary>Flies the camera to a faction's main HQ. Returns false when the faction has no HQ.</summary>
        public bool JumpToFaction(Faction f, bool instant = false)
        {
            if (!_boot.Sim.State.Setup.InGame((int)f)) return false;
            var p = HqPoint(f);
            if (p == null) return false;
            _boot.Rig.FocusOn(p.Value, FocusHeight, instant);
            return true;
        }

        /// <summary>Recent captures (newest first), then the places where units are fighting (busiest first).</summary>
        public List<Vector3> HotSpots()
        {
            var l = new List<Vector3>(_captures);
            var cells = new Dictionary<(int, int), (Vector3 sum, int n)>();
            foreach (var u in _boot.Sim.State.Military.Units)
            {
                if (u.Activity != UnitActivity.Fighting) continue;
                var key = (Mathf.FloorToInt((float)u.PosX / 40f), Mathf.FloorToInt((float)u.PosZ / 40f));
                cells.TryGetValue(key, out var c);
                cells[key] = (c.sum + new Vector3((float)u.PosX, 0f, (float)u.PosZ), c.n + 1);
            }
            var fights = new List<(Vector3 p, int n)>();
            foreach (var kv in cells) fights.Add((kv.Value.sum / kv.Value.n, kv.Value.n));
            fights.Sort((a, b) => b.n.CompareTo(a.n));
            foreach (var f in fights) l.Add(f.p);
            return l;
        }

        /// <summary>Tab: the next hot spot. With none, the next faction's HQ.</summary>
        public void NextHotSpot(bool instant = false)
        {
            var spots = HotSpots();
            _tab++;
            if (spots.Count > 0) { _boot.Rig.FocusOn(spots[_tab % spots.Count], FocusHeight, instant); return; }
            for (int k = 0; k < FactionInfo.Max; k++)
                if (JumpToFaction((Faction)((_tab + k) % _boot.Sim.State.FactionCount), instant)) return;
        }
    }
}
