using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Stage 3 soldiers, small and readable in the OpenRA manner, built from the kit they wear: a villager skeleton in a
    /// tunic (warm for timber factions, grey for stone ones) with a belt and tabard in the faction colour; a metal helmet
    /// with a crest in the faction colour (or a cap in the faction colour without one); a grey breastplate or a brown
    /// studded leather jerkin; the weapon in the right hand (sword, spear, axe, bow or sling); a tall painted shield or a
    /// small round buckler on the left forearm. Local space, feet at y = 0, facing +Z; arms hang along -Y from the
    /// shoulder with the hand at (0, -0.64, 0). Meshes are cached per faction and piece. View code only.
    /// </summary>
    public static class UnitModels
    {
        public const float BarWidth = 1.6f, BarHeight = 3.2f;

        private static bool Warm(Faction f) => !FactionLook.Stone(f);
        private static Color Tunic(Faction f) => Warm(f) ? new Color(0.58f, 0.46f, 0.30f) : new Color(0.32f, 0.31f, 0.33f);
        private static Color Team(Faction f) => ModelKit.Accent(ModelKit.FactionColour(f));
        private static readonly Color Steel = new Color(0.66f, 0.68f, 0.72f), Leather = new Color(0.50f, 0.33f, 0.19f), Bone = new Color(0.92f, 0.86f, 0.70f);

        /// <summary>Torso and head with the head and body items worn.</summary>
        public static Mesh Body(Faction f, Ware head, Ware body) => ModelKit.Cached("sol/body/" + (int)f + "/" + (int)head + "/" + (int)body, mb =>
        {
            var team = Team(f);
            bool helm = head == Ware.Helmet;
            PeopleModels.Torso(mb, Tunic(f), helm ? Steel * 0.9f : team);
            switch (body)
            {
                case Ware.Breastplate:
                    // A steel cuirass with shoulder plates and a stripe of the faction colour down the front.
                    mb.Box(new Vector3(0, 1.17f, 0), new Vector3(0.72f, 0.6f, 0.46f), Steel);
                    mb.Box(new Vector3(0, 1.17f, 0.235f), new Vector3(0.14f, 0.56f, 0.02f), team);
                    mb.Box(new Vector3(-0.38f, 1.4f, 0), new Vector3(0.22f, 0.12f, 0.4f), Steel * 0.85f);
                    mb.Box(new Vector3(0.38f, 1.4f, 0), new Vector3(0.22f, 0.12f, 0.4f), Steel * 0.85f);
                    mb.Box(new Vector3(0, 0.86f, 0), new Vector3(0.72f, 0.16f, 0.46f), Steel * 0.75f);
                    break;
                case Ware.LightArmour:
                    // A studded leather jerkin over the tunic, the tabard showing below.
                    mb.Box(new Vector3(0, 1.15f, 0), new Vector3(0.69f, 0.58f, 0.43f), Leather);
                    for (int i = 0; i < 3; i++)
                        for (int j = 0; j < 2; j++)
                            mb.Box(new Vector3(-0.16f + j * 0.32f, 1.0f + i * 0.17f, 0.22f), new Vector3(0.06f, 0.06f, 0.03f), Bone * 0.9f);
                    mb.Box(new Vector3(0, 0.84f, 0.2f), new Vector3(0.3f, 0.26f, 0.05f), team);
                    break;
                default:
                    // A tabard in the faction colour.
                    mb.Box(new Vector3(0, 1.08f, 0.2f), new Vector3(0.36f, 0.6f, 0.03f), team);
                    break;
            }
            mb.Box(new Vector3(0, 0.95f, 0), new Vector3(0.74f, 0.08f, 0.47f), team * 0.8f);
            if (helm)
            {
                // A rounded steel helmet with a nasal, cheek guards and a crest in the faction colour.
                mb.Cylinder(new Vector3(0, 1.8f, 0), 0.27f, 0.26f, 0.16f, 8, Steel);
                mb.Cone(new Vector3(0, 1.96f, 0), 0.26f, 0.18f, 8, Steel * 1.05f);
                mb.Box(new Vector3(0, 1.7f, 0.22f), new Vector3(0.06f, 0.2f, 0.04f), Steel * 0.85f);
                mb.Box(new Vector3(-0.22f, 1.66f, 0.08f), new Vector3(0.04f, 0.2f, 0.16f), Steel * 0.85f);
                mb.Box(new Vector3(0.22f, 1.66f, 0.08f), new Vector3(0.04f, 0.2f, 0.16f), Steel * 0.85f);
                mb.Box(new Vector3(0, 2.18f, -0.03f), new Vector3(0.09f, 0.2f, 0.46f), team);
            }
        });

        public static Mesh Leg(Faction f) => ModelKit.Cached("inf/leg/" + (int)f, mb => PeopleModels.Leg(mb, Warm(f) ? new Color(0.40f, 0.32f, 0.22f) : new Color(0.22f, 0.21f, 0.23f)));

        /// <summary>The right arm with its weapon in the hand.</summary>
        public static Mesh WeaponArm(Faction f, Ware weapon) => ModelKit.Cached("sol/warm/" + (int)f + "/" + (int)weapon, mb =>
        {
            PeopleModels.Arm(mb, Tunic(f));
            var hand = new Vector3(0, -0.64f, 0);
            switch (weapon)
            {
                case Ware.Spear:
                    // A long ash shaft and a leaf blade.
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.7f, -0.05f), hand + new Vector3(0, 2.1f, 0.15f), 0.035f, ModelKit.Wood);
                    mb.Cone(hand + new Vector3(0, 2.1f, 0.15f), 0.08f, 0.45f, 4, Steel);
                    break;
                case Ware.Axe:
                    // A short haft with a broad bearded head.
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.15f, 0), hand + new Vector3(0, 0.95f, 0.05f), 0.04f, ModelKit.DarkWood);
                    mb.Box(hand + new Vector3(0, 0.8f, 0.2f), new Vector3(0.06f, 0.36f, 0.32f), Steel);
                    mb.Box(hand + new Vector3(0, 0.7f, 0.38f), new Vector3(0.05f, 0.5f, 0.08f), Steel * 1.1f);
                    break;
                case Ware.Bow:
                    // A bow held out across the arm: when the arm is raised forward the stave stands upright.
                    ModelKit.Beam(mb, hand + new Vector3(0, 0.05f, -0.78f), hand + new Vector3(0, -0.2f, -0.3f), 0.035f, ModelKit.Wood);
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.2f, -0.3f), hand + new Vector3(0, -0.24f, 0.3f), 0.04f, ModelKit.DarkWood);
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.24f, 0.3f), hand + new Vector3(0, 0.05f, 0.78f), 0.035f, ModelKit.Wood);
                    ModelKit.Beam(mb, hand + new Vector3(0, 0.05f, -0.78f), hand + new Vector3(0, 0.05f, 0.78f), 0.008f, Bone);
                    break;
                case Ware.Sling:
                    // Two cords hanging from the hand with a stone pouch.
                    ModelKit.Beam(mb, hand, hand + new Vector3(0.03f, -0.55f, 0.02f), 0.012f, Leather * 1.2f);
                    ModelKit.Beam(mb, hand, hand + new Vector3(-0.03f, -0.55f, -0.02f), 0.012f, Leather * 1.2f);
                    mb.Box(hand + new Vector3(0, -0.6f, 0), new Vector3(0.12f, 0.1f, 0.14f), Leather);
                    mb.Box(hand + new Vector3(0, -0.62f, 0), new Vector3(0.08f, 0.08f, 0.08f), ModelKit.Rock);
                    break;
                default:
                    // A straight sword: grip, cross-guard, blade.
                    mb.Box(hand + new Vector3(0, 0.02f, 0), new Vector3(0.06f, 0.2f, 0.06f), ModelKit.DarkWood);
                    mb.Box(hand + new Vector3(0, 0.14f, 0), new Vector3(0.06f, 0.05f, 0.3f), Steel * 0.8f);
                    mb.Box(hand + new Vector3(0, 0.55f, 0), new Vector3(0.04f, 0.78f, 0.11f), Steel * 1.1f);
                    mb.Cone(hand + new Vector3(0, 0.94f, 0), 0.06f, 0.14f, 4, Steel * 1.1f);
                    break;
            }
        });

        /// <summary>The left arm with a big painted shield, a small round buckler, or nothing.</summary>
        public static Mesh ShieldArm(Faction f, Ware shield) => ModelKit.Cached("sol/sarm/" + (int)f + "/" + (int)shield, mb =>
        {
            PeopleModels.Arm(mb, Tunic(f));
            var team = Team(f);
            if (shield == Ware.BigShield)
            {
                // A tall curved shield, painted in the faction colour, with a rim, a boss and a pale band.
                var c = new Vector3(-0.17f, -0.35f, 0.08f);
                const float h = 0.68f, w = 0.4f;
                for (int i = -1; i <= 1; i += 2)
                {
                    var a = c + new Vector3(0, -h, i * w * 0.5f); var b = c + new Vector3(0, h, i * w * 0.5f);
                    var a2 = c + new Vector3(0.1f, -h, i * w * 1.1f); var b2 = c + new Vector3(0.1f, h, i * w * 1.1f);
                    ModelKit.Q2(mb, a, b, b2, a2, team * 0.92f);
                    ModelKit.Beam(mb, a2, b2, 0.03f, team * 0.6f);
                }
                ModelKit.Q2(mb, c + new Vector3(0, -h, -w * 0.5f), c + new Vector3(0, h, -w * 0.5f), c + new Vector3(0, h, w * 0.5f), c + new Vector3(0, -h, w * 0.5f), team);
                mb.Box(c + new Vector3(-0.02f, 0, 0), new Vector3(0.04f, 0.12f, w * 2.1f), Bone * 0.9f);
                mb.Box(c + new Vector3(-0.06f, 0, 0), new Vector3(0.1f, 0.18f, 0.18f), Steel);
            }
            else if (shield == Ware.SmallShield)
            {
                // A small round buckler: a steel rim, the faction colour inside, a steel boss.
                var c = new Vector3(-0.15f, -0.48f, 0.05f);
                int n = 10; float r = 0.27f;
                for (int i = 0; i < n; i++)
                {
                    float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                    var p0 = c + new Vector3(0, Mathf.Sin(a0) * r, Mathf.Cos(a0) * r); var p1 = c + new Vector3(0, Mathf.Sin(a1) * r, Mathf.Cos(a1) * r);
                    ModelKit.T2(mb, c + new Vector3(-0.05f, 0, 0), p0, p1, team);
                    ModelKit.Beam(mb, p0, p1, 0.03f, Steel * 0.85f);
                }
                mb.Box(c + new Vector3(-0.07f, 0, 0), new Vector3(0.08f, 0.12f, 0.12f), Steel);
            }
        });
    }
}
