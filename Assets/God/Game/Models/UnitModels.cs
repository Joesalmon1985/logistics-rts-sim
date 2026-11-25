using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b soldiers and war wagons, small and readable in the OpenRA manner. Infantry: a villager skeleton in a padded
    /// tunic and helmet, a round shield in the faction colour; the player's spearmen throw javelins, the enemy's carry a
    /// crossbow. Vehicles: a four-wheeled war wagon with pavise boards in the faction colour, a mounted bolt thrower that
    /// turns to its target, and a pennant. Local space, feet (or wheels) at y = 0, facing +Z. View code only.
    /// </summary>
    public static class UnitModels
    {
        public static Mesh InfantryBody(Faction f) => ModelKit.Cached("inf/body/" + (int)f, mb =>
        {
            bool p = !FactionLook.Stone(f);
            var tunic = p ? new Color(0.58f, 0.46f, 0.30f) : new Color(0.32f, 0.31f, 0.33f);
            var helm = p ? new Color(0.48f, 0.34f, 0.20f) : ModelKit.Metal * 0.85f;
            PeopleModels.Torso(mb, tunic, helm);
            // Helmet with a crest or a nasal, and a sash in the faction colour.
            if (p) mb.Box(new Vector3(0, 2.06f, -0.02f), new Vector3(0.1f, 0.18f, 0.44f), ModelKit.Accent(ModelKit.FactionColour(f)));
            else { mb.Cone(new Vector3(0, 1.96f, 0), 0.3f, 0.32f, 4, helm); mb.Box(new Vector3(0, 1.7f, 0.21f), new Vector3(0.07f, 0.22f, 0.04f), helm); }
            mb.Box(new Vector3(0, 1.1f, 0.0f), new Vector3(0.68f, 0.1f, 0.42f), ModelKit.Accent(ModelKit.FactionColour(f)), 0f);
        });

        public static Mesh Leg(Faction f) => ModelKit.Cached("inf/leg/" + (int)f, mb => PeopleModels.Leg(mb, !FactionLook.Stone(f) ? new Color(0.40f, 0.32f, 0.22f) : new Color(0.22f, 0.21f, 0.23f)));

        /// <summary>The weapon arm: a spear (player) or a crossbow (enemy) in the hand.</summary>
        public static Mesh WeaponArm(Faction f) => ModelKit.Cached("inf/warm/" + (int)f, mb =>
        {
            PeopleModels.Arm(mb, !FactionLook.Stone(f) ? new Color(0.58f, 0.46f, 0.30f) : new Color(0.32f, 0.31f, 0.33f));
            var hand = new Vector3(0, -0.64f, 0);
            if (!FactionLook.Stone(f))
            {
                ModelKit.Beam(mb, hand + new Vector3(0, -0.6f, -0.05f), hand + new Vector3(0, 1.9f, 0.15f), 0.04f, ModelKit.Wood);
                mb.Cone(hand + new Vector3(0, 1.9f, 0.15f), 0.08f, 0.4f, 4, ModelKit.Metal);
            }
            else
            {
                // A crossbow held forward: stock, prod, string.
                // Modelled along the arm (-Y): the view raises the arm to aim it forward.
                mb.Box(hand + new Vector3(0, -0.3f, 0.08f), new Vector3(0.1f, 0.95f, 0.12f), ModelKit.DarkWood);
                mb.Box(hand + new Vector3(0, -0.72f, 0.08f), new Vector3(0.95f, 0.08f, 0.08f), ModelKit.Metal * 0.8f);
            }
        });

        public static Mesh ShieldArm(Faction f) => ModelKit.Cached("inf/sarm/" + (int)f, mb =>
        {
            PeopleModels.Arm(mb, !FactionLook.Stone(f) ? new Color(0.58f, 0.46f, 0.30f) : new Color(0.32f, 0.31f, 0.33f));
            // A round shield on the outside of the forearm, painted in the faction colour with a pale boss.
            var col = ModelKit.Accent(ModelKit.FactionColour(f));
            var c = new Vector3(-0.16f, -0.4f, 0.05f);
            int n = 10; float r = 0.46f;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                var p0 = c + new Vector3(0, Mathf.Sin(a0) * r, Mathf.Cos(a0) * r); var p1 = c + new Vector3(0, Mathf.Sin(a1) * r, Mathf.Cos(a1) * r);
                ModelKit.T2(mb, c + new Vector3(-0.06f, 0, 0), p0, p1, col);
                ModelKit.Beam(mb, p0, p1, 0.04f, col * 0.65f);
            }
            mb.Box(c + new Vector3(-0.08f, 0, 0), new Vector3(0.1f, 0.16f, 0.16f), new Color(0.92f, 0.86f, 0.70f));
        });

        // ---------------------------------------------------------------- vehicle

        public const float WagonWheelR = 0.62f;

        public static Mesh WagonBody(Faction f) => ModelKit.Cached("veh/body/" + (int)f, mb =>
        {
            var st = ModelStyle.Of(f);
            var wood = !FactionLook.Stone(f) ? ModelKit.Wood : new Color(0.30f, 0.26f, 0.24f);
            // Bed and axles.
            mb.Box(new Vector3(0, 0.95f, 0), new Vector3(1.9f, 0.35f, 3.4f), wood);
            mb.Box(new Vector3(0, 0.62f, 1.1f), new Vector3(2.3f, 0.16f, 0.16f), ModelKit.DarkWood);
            mb.Box(new Vector3(0, 0.62f, -1.1f), new Vector3(2.3f, 0.16f, 0.16f), ModelKit.DarkWood);
            // Pavise boards along the sides in the faction colour, with pale bosses; a plank front.
            var col = st.Accent;
            foreach (int side in new[] { -1, 1 })
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(side * 1.0f, 1.45f, -1.0f + i * 1.0f);
                    mb.Box(p, new Vector3(0.1f, 0.75f, 0.9f), i == 1 ? col : col * 0.8f);
                    mb.Box(p + new Vector3(side * 0.06f, 0, 0), new Vector3(0.06f, 0.18f, 0.18f), new Color(0.92f, 0.86f, 0.70f));
                }
            mb.Box(new Vector3(0, 1.4f, 1.68f), new Vector3(1.9f, 0.6f, 0.12f), wood * 1.1f);
            mb.Box(new Vector3(0, 1.4f, -1.68f), new Vector3(1.9f, 0.6f, 0.12f), wood * 1.1f);
            // A tow pole at the front, a pennant at the back.
            ModelKit.Beam(mb, new Vector3(0, 0.9f, 1.7f), new Vector3(0, 0.6f, 2.6f), 0.07f, ModelKit.DarkWood);
            mb.Cylinder(new Vector3(-0.75f, 1.1f, -1.5f), 0.06f, 0.05f, 2.6f, 5, ModelKit.DarkWood);
            mb.Quad(new Vector3(-0.75f, 3.6f, -1.5f), new Vector3(-0.75f, 3.0f, -1.5f), new Vector3(-0.75f, 3.25f, -2.6f), new Vector3(-0.75f, 3.5f, -2.5f), col);
            mb.Quad(new Vector3(-0.75f, 3.6f, -1.5f), new Vector3(-0.75f, 3.5f, -2.5f), new Vector3(-0.75f, 3.25f, -2.6f), new Vector3(-0.75f, 3.0f, -1.5f), col * 0.85f);
            // The crew: a small figure sitting at the back.
            mb.Box(new Vector3(0.45f, 1.55f, -1.0f), new Vector3(0.5f, 0.6f, 0.4f), !FactionLook.Stone(f) ? new Color(0.58f, 0.46f, 0.30f) : new Color(0.32f, 0.31f, 0.33f));
            mb.Box(new Vector3(0.45f, 2.0f, -1.0f), new Vector3(0.34f, 0.34f, 0.34f), ModelKit.Skin);
            mb.Box(new Vector3(0.45f, 2.2f, -1.0f), new Vector3(0.38f, 0.1f, 0.38f), col);
        });

        public static Mesh WagonWheel() => ModelKit.Cached("veh/wheel", mb => ModelKit.Wheel(mb, WagonWheelR, 0.18f, ModelKit.DarkWood, ModelKit.Wood, 6));

        /// <summary>The bolt thrower on its turntable (pivot at the turntable, facing +Z).</summary>
        public static Mesh Thrower(Faction f) => ModelKit.Cached("veh/thrower/" + (int)f, mb =>
        {
            var wood = !FactionLook.Stone(f) ? ModelKit.Wood * 1.1f : new Color(0.36f, 0.32f, 0.30f);
            mb.Cylinder(Vector3.zero, 0.5f, 0.45f, 0.25f, 8, ModelKit.DarkWood);
            mb.Box(new Vector3(0, 0.45f, 0.2f), new Vector3(0.3f, 0.25f, 1.9f), wood);
            mb.Box(new Vector3(0, 0.5f, 0.95f), new Vector3(1.7f, 0.16f, 0.2f), wood * 0.9f);
            ModelKit.Beam(mb, new Vector3(-0.85f, 0.5f, 0.95f), new Vector3(0, 0.55f, -0.55f), 0.02f, new Color(0.9f, 0.86f, 0.74f));
            ModelKit.Beam(mb, new Vector3(0.85f, 0.5f, 0.95f), new Vector3(0, 0.55f, -0.55f), 0.02f, new Color(0.9f, 0.86f, 0.74f));
            ModelKit.Beam(mb, new Vector3(0, 0.62f, -0.3f), new Vector3(0, 0.62f, 1.4f), 0.04f, ModelKit.Metal);
        });

        /// <summary>A corpse-free death: the body mesh tips over; the view uses the live parts. Health bar sizes per kind.</summary>
        public static float BarWidth(UnitKind k) => k == UnitKind.Vehicle ? 2.6f : 1.6f;
        public static float BarHeight(UnitKind k) => k == UnitKind.Vehicle ? 4.2f : 3.2f;
    }
}
