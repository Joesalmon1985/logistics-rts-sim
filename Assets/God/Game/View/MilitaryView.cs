using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The military view. Since P4b it is drawn by models: <see cref="UnitLayer"/> (soldiers dressed in their kit that face
    /// their movement, strike or shoot by weapon, show how each blow ended, show a small bar only when damaged and fall
    /// when they die), <see cref="OverlayLayer"/>
    /// (numbered war standards on marked enemy buildings, defend standards with a guard count) and the shared
    /// <see cref="FxLayer"/>. Building damage, rubble and collapse are drawn by <see cref="BuildingLayer"/>. View only:
    /// it reads <see cref="MilitaryState"/> and never changes it.
    /// </summary>
    public sealed class MilitaryView : MonoBehaviour
    {
        private GodSim _sim;
        private UnitLayer _units;
        private OverlayLayer _overlay;

        public GodSim Sim => _sim;
        public UnitLayer Units => _units;
        public OverlayLayer Overlay => _overlay;
        /// <summary>Units drawn now (for tests).</summary>
        public int UnitsDrawn => _units.Count;
        /// <summary>Arrows and stones in flight last frame (for tests).</summary>
        public int TracersDrawn => FxLayer.Instance != null ? FxLayer.Instance.ShotsDrawn : 0;

        public static MilitaryView Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("MilitaryView");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<MilitaryView>();
            v._sim = sim;
            v._units = UnitLayer.Create(sim, go.transform);
            v._overlay = OverlayLayer.Create(sim, go.transform);
            return v;
        }

        public static Color FactionColour(Faction f) => FactionLook.Colour(f);
    }
}
