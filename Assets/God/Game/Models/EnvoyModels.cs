using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P8 trade envoys: a herald in a pale tunic with a sash and a pennant staff in the faction colour, leading a small
    /// pack donkey with a faction-coloured blanket and bundles of the wares it carries. Local space, feet at y = 0, facing
    /// +Z. Original procedural geometry, built from the same primitives as the other people models. View code only.
    /// </summary>
    public static class EnvoyModels
    {
        public const float Hip = PeopleModels.Hip;
        /// <summary>Height of the donkey's back (where bundles sit) and its leg pivots.</summary>
        public const float BackY = 1.28f, DonkeyHip = 0.72f;
        /// <summary>Distance the donkey trails behind the herald (world metres at scale 1).</summary>
        public const float Trail = 2.3f;

        private static readonly Color Cream = new Color(0.90f, 0.86f, 0.72f);
        private static readonly Color Fur = new Color(0.52f, 0.46f, 0.40f), FurLight = new Color(0.72f, 0.66f, 0.58f), Hoof = new Color(0.22f, 0.17f, 0.13f);

        // ---------------------------------------------------------------- herald

        public static Mesh HeraldBody(Faction f) => ModelKit.Cached("envoy/body/" + (int)f, mb =>
        {
            var acc = ModelKit.Accent(FactionLook.Colour(f));
            var tunic = FactionLook.Stone(f) ? Cream * 0.78f : Cream;
            PeopleModels.Torso(mb, tunic, acc);
            // A sash across the chest and a short cloak in the faction colour, so it reads from above.
            ModelKit.Beam(mb, new Vector3(-0.3f, 1.42f, 0.2f), new Vector3(0.3f, 0.88f, 0.2f), 0.06f, acc);
            mb.Box(new Vector3(0, 1.2f, -0.24f), new Vector3(0.7f, 0.74f, 0.08f), acc * 0.9f);
            // A small satchel on the hip.
            mb.Box(new Vector3(0.38f, 0.95f, 0.05f), new Vector3(0.12f, 0.3f, 0.34f), new Color(0.45f, 0.30f, 0.17f));
        });

        public static Mesh HeraldLeg() => ModelKit.Cached("envoy/leg", mb => PeopleModels.Leg(mb, new Color(0.34f, 0.28f, 0.22f)));

        public static Mesh HeraldArm(Faction f) => ModelKit.Cached("envoy/arm/" + (int)f, mb => PeopleModels.Arm(mb, (FactionLook.Stone(f) ? Cream * 0.78f : Cream) * 0.95f));

        /// <summary>The right arm, holding a staff topped by a pennant in the faction colour (arm space: the hand at (0, -0.64, 0)).</summary>
        public static Mesh StaffArm(Faction f) => ModelKit.Cached("envoy/staffarm/" + (int)f, mb =>
        {
            PeopleModels.Arm(mb, (FactionLook.Stone(f) ? Cream * 0.78f : Cream) * 0.95f);
            var acc = ModelKit.Accent(FactionLook.Colour(f));
            var hand = new Vector3(0, -0.64f, 0.06f);
            ModelKit.Beam(mb, hand + new Vector3(0, -1.05f, 0), hand + new Vector3(0, 1.9f, 0), 0.04f, ModelKit.Wood);
            var top = hand + new Vector3(0, 1.9f, 0);
            mb.Quad(top, top + new Vector3(0, -0.5f, 0), top + new Vector3(0, -0.4f, 0.75f), top + new Vector3(0, -0.05f, 0.7f), acc);
            mb.Quad(top, top + new Vector3(0, -0.05f, 0.7f), top + new Vector3(0, -0.4f, 0.75f), top + new Vector3(0, -0.5f, 0), acc * 0.85f);
            mb.Box(top + new Vector3(0, 0.06f, 0), new Vector3(0.1f, 0.1f, 0.1f), new Color(0.92f, 0.78f, 0.35f));
        });

        // ---------------------------------------------------------------- pack donkey

        public static Mesh DonkeyBody(Faction f) => ModelKit.Cached("envoy/donkey/" + (int)f, mb =>
        {
            var acc = ModelKit.Accent(FactionLook.Colour(f));
            mb.Box(new Vector3(0, 1.0f, 0), new Vector3(0.52f, 0.5f, 1.5f), Fur);
            mb.Box(new Vector3(0, 0.84f, 0), new Vector3(0.5f, 0.2f, 1.4f), FurLight);
            // Neck, head, muzzle and long ears.
            ModelKit.Beam(mb, new Vector3(0, 1.08f, 0.62f), new Vector3(0, 1.52f, 0.98f), 0.14f, Fur);
            mb.Box(new Vector3(0, 1.58f, 1.18f), new Vector3(0.26f, 0.3f, 0.5f), Fur);
            mb.Box(new Vector3(0, 1.48f, 1.46f), new Vector3(0.2f, 0.2f, 0.18f), FurLight);
            mb.Box(new Vector3(-0.08f, 1.84f, 1.06f), new Vector3(0.06f, 0.32f, 0.09f), Fur * 0.9f);
            mb.Box(new Vector3(0.08f, 1.84f, 1.06f), new Vector3(0.06f, 0.32f, 0.09f), Fur * 0.9f);
            mb.Box(new Vector3(0, 1.62f, 1.0f), new Vector3(0.1f, 0.18f, 0.2f), Fur * 0.65f);
            ModelKit.Beam(mb, new Vector3(0, 1.12f, -0.74f), new Vector3(0, 0.62f, -0.88f), 0.04f, Fur * 0.7f);
            // The pack frame with a blanket and a halter in the faction colour.
            mb.Box(new Vector3(0, BackY - 0.02f, 0), new Vector3(0.62f, 0.07f, 1.0f), acc);
            mb.Box(new Vector3(0, BackY - 0.2f, 0.0f), new Vector3(0.66f, 0.3f, 0.12f), acc * 0.85f);
            mb.Box(new Vector3(0, BackY + 0.02f, 0), new Vector3(0.3f, 0.06f, 0.7f), ModelKit.DarkWood);
            mb.Box(new Vector3(0, 1.6f, 1.26f), new Vector3(0.28f, 0.06f, 0.06f), acc);
        });

        public static Mesh DonkeyLeg() => ModelKit.Cached("envoy/donkeyleg", mb =>
        {
            mb.Box(new Vector3(0, -0.34f, 0), new Vector3(0.13f, 0.68f, 0.13f), Fur * 0.9f);
            mb.Box(new Vector3(0, -0.7f, 0.01f), new Vector3(0.15f, 0.1f, 0.17f), Hoof);
        });

        /// <summary>A bundle of a ware on the donkey: the walker's head load, so a log is a log. Origin at the load's base (y = 2 in <see cref="PeopleModels.Load"/>).</summary>
        public static Mesh Bundle(Ware w) => ModelKit.Cached("envoy/bundle/" + (int)w, mb => PeopleModels.Load(mb, w));

        /// <summary>The rope between the herald's hand and the donkey's halter: a thin bar along +Z, scaled to length.</summary>
        public static Mesh Rope() => ModelKit.Cached("envoy/rope", mb => mb.Box(new Vector3(0, 0, 0.5f), new Vector3(0.05f, 0.05f, 1f), new Color(0.62f, 0.50f, 0.32f)));
    }
}
