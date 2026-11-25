using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>Tools a villager can hold.</summary>
    public enum ToolKind { None, Axe, Pick, Shovel, Sickle, Hammer, Spear }

    /// <summary>
    /// P4b tiny villagers, in the style of the WorldMVP collect walkers but original here: a box torso, head, a cap in the
    /// faction colour, legs and arms on pivots (hip at y 0.8, shoulder at y 1.45), feet at y 0, facing +Z, about 2 m tall
    /// before the view scales them up. Ware loads sit on the head; tools hang from the hand. View code only.
    /// </summary>
    public static class PeopleModels
    {
        public const float Hip = 0.8f, Shoulder = 1.42f, ShoulderX = 0.4f, LegX = 0.15f, HeadTop = 2.0f;

        public static Color Tunic(WalkerKind k, Faction f)
        {
            Color c;
            switch (k)
            {
                case WalkerKind.Builder: c = new Color(0.60f, 0.42f, 0.24f); break;
                case WalkerKind.Harvester: c = new Color(0.46f, 0.54f, 0.34f); break;
                default: c = new Color(0.86f, 0.80f, 0.64f); break;
            }
            return FactionLook.Stone(f) ? c * 0.7f : c;
        }

        public static void Torso(MeshBuilder mb, Color tunic, Color cap, bool apron = false)
        {
            mb.Box(new Vector3(0, 1.13f, 0), new Vector3(0.64f, 0.66f, 0.38f), tunic);
            mb.Box(new Vector3(0, 0.86f, 0), new Vector3(0.68f, 0.22f, 0.42f), tunic * 0.8f);
            mb.Box(new Vector3(0, 0.96f, 0), new Vector3(0.66f, 0.07f, 0.4f), new Color(0.30f, 0.20f, 0.12f));
            if (apron) mb.Box(new Vector3(0, 0.95f, 0.2f), new Vector3(0.48f, 0.6f, 0.04f), new Color(0.40f, 0.28f, 0.16f));
            mb.Box(new Vector3(0, 1.66f, 0), new Vector3(0.4f, 0.42f, 0.4f), ModelKit.Skin);
            mb.Box(new Vector3(0, 1.83f, -0.04f), new Vector3(0.43f, 0.14f, 0.42f), ModelKit.Hair);
            // The cap: the owner's colour, readable from above.
            mb.Box(new Vector3(0, 1.92f, 0), new Vector3(0.46f, 0.12f, 0.46f), cap);
            mb.Box(new Vector3(0, 1.88f, 0.22f), new Vector3(0.4f, 0.05f, 0.14f), cap * 0.8f);
        }

        public static void Leg(MeshBuilder mb, Color trousers)
        {
            mb.Box(new Vector3(0, -0.4f, 0), new Vector3(0.22f, 0.8f, 0.24f), trousers);
            mb.Box(new Vector3(0, -0.74f, 0.05f), new Vector3(0.24f, 0.14f, 0.34f), new Color(0.22f, 0.15f, 0.09f));
        }

        public static void Arm(MeshBuilder mb, Color sleeve)
        {
            mb.Box(new Vector3(0, -0.3f, 0), new Vector3(0.17f, 0.6f, 0.19f), sleeve);
            mb.Box(new Vector3(0, -0.64f, 0), new Vector3(0.16f, 0.16f, 0.17f), ModelKit.Skin);
        }

        /// <summary>A tool in arm space: the hand is at (0, -0.64, 0), the tool sticks forward and up out of it.</summary>
        public static void Tool(MeshBuilder mb, ToolKind t)
        {
            var hand = new Vector3(0, -0.64f, 0);
            switch (t)
            {
                case ToolKind.Axe:
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.1f, -0.1f), hand + new Vector3(0, 0.1f, 0.85f), 0.04f, ModelKit.Wood);
                    mb.Box(hand + new Vector3(0, 0.15f, 0.8f), new Vector3(0.06f, 0.32f, 0.26f), ModelKit.Metal);
                    break;
                case ToolKind.Pick:
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.1f, -0.1f), hand + new Vector3(0, 0.1f, 0.85f), 0.04f, ModelKit.Wood);
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.2f, 0.85f), hand + new Vector3(0, 0.35f, 0.75f), 0.045f, ModelKit.Metal);
                    break;
                case ToolKind.Shovel:
                    ModelKit.Beam(mb, hand + new Vector3(0, 0.2f, -0.15f), hand + new Vector3(0, -0.4f, 0.8f), 0.04f, ModelKit.Wood);
                    mb.Box(hand + new Vector3(0, -0.48f, 0.9f), new Vector3(0.28f, 0.34f, 0.05f), ModelKit.Metal, 0f);
                    break;
                case ToolKind.Sickle:
                    ModelKit.Beam(mb, hand, hand + new Vector3(0, 0.05f, 0.3f), 0.035f, ModelKit.Wood);
                    ModelKit.Beam(mb, hand + new Vector3(0, 0.05f, 0.3f), hand + new Vector3(0, 0.3f, 0.5f), 0.025f, ModelKit.Metal);
                    ModelKit.Beam(mb, hand + new Vector3(0, 0.3f, 0.5f), hand + new Vector3(0, 0.38f, 0.3f), 0.025f, ModelKit.Metal);
                    break;
                case ToolKind.Hammer:
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.05f, -0.05f), hand + new Vector3(0, 0.05f, 0.6f), 0.035f, ModelKit.Wood);
                    mb.Box(hand + new Vector3(0, 0.07f, 0.6f), new Vector3(0.14f, 0.3f, 0.16f), ModelKit.Metal * 0.8f);
                    break;
                case ToolKind.Spear:
                    ModelKit.Beam(mb, hand + new Vector3(0, -0.7f, 0), hand + new Vector3(0, 1.9f, 0), 0.035f, ModelKit.Wood);
                    mb.Cone(hand + new Vector3(0, 1.9f, 0), 0.08f, 0.38f, 4, ModelKit.Metal);
                    break;
            }
        }

        /// <summary>A ware load, as carried on the head (body space: its base at y = 2.0).</summary>
        public static void Load(MeshBuilder mb, Ware w)
        {
            var b = new Vector3(0, HeadTop, 0);
            switch (w)
            {
                case Ware.Log:
                    ModelKit.Log(mb, b + new Vector3(-0.15f, 0.2f, -0.9f), b + new Vector3(0.15f, 0.2f, 0.9f), 0.22f, ModelKit.LogBark, ModelKit.LogEnd);
                    break;
                case Ware.Plank:
                    for (int i = 0; i < 3; i++) mb.Box(b + new Vector3((i - 1) * 0.2f, 0.08f + (i % 2) * 0.12f, 0), new Vector3(0.18f, 0.1f, 1.7f), ModelKit.Plank * (0.95f + 0.04f * i));
                    break;
                case Ware.Kindling:
                    for (int i = 0; i < 5; i++) ModelKit.Beam(mb, b + new Vector3((i - 2) * 0.09f, 0.1f, -0.5f), b + new Vector3((i - 2) * 0.12f, 0.16f, 0.5f), 0.04f, ModelKit.Wood * (1f + 0.05f * i));
                    mb.Box(b + new Vector3(0, 0.15f, 0), new Vector3(0.5f, 0.06f, 0.12f), ModelKit.Straw);
                    break;
                case Ware.Grain:
                    ModelKit.Sheaf(mb, b + new Vector3(0, 0.02f, 0), 0.9f);
                    break;
                case Ware.Clay:
                    Basket(mb, b);
                    ModelKit.Lump(mb, b + new Vector3(0, 0.3f, 0), 0.3f, 0.35f, ModelKit.ClayCol);
                    break;
                case Ware.Ore:
                    Basket(mb, b);
                    ModelKit.Lump(mb, b + new Vector3(-0.1f, 0.3f, 0), 0.22f, 0.3f, ModelKit.OreCol);
                    ModelKit.Lump(mb, b + new Vector3(0.12f, 0.28f, 0.05f), 0.2f, 0.26f, ModelKit.OreCol * 1.2f);
                    break;
                case Ware.Stone:
                case Ware.Rock:
                    mb.Box(b + new Vector3(0, 0.22f, 0), new Vector3(0.62f, 0.44f, 0.56f), ModelKit.StoneCol, 8f);
                    break;
                case Ware.Brick:
                    for (int l = 0; l < 3; l++) for (int i = 0; i < 2; i++)
                        mb.Box(b + new Vector3((i - 0.5f) * 0.28f, 0.08f + l * 0.15f, 0), new Vector3(0.26f, 0.13f, 0.5f), ModelKit.Brick * (0.92f + 0.05f * ((i + l) % 2)));
                    break;
                case Ware.Iron:
                    for (int i = 0; i < 3; i++) mb.Box(b + new Vector3((i - 1) * 0.16f, 0.08f + (i % 2) * 0.1f, 0), new Vector3(0.14f, 0.1f, 0.9f), ModelKit.Iron * (1f + 0.1f * i));
                    break;
            }
        }

        private static void Basket(MeshBuilder mb, Vector3 b)
        {
            mb.Cylinder(b, 0.3f, 0.4f, 0.3f, 8, new Color(0.62f, 0.48f, 0.28f));
        }

        /// <summary>A whole villager in one mesh (for the bobbing workers at buildings): arms forward, the tool in front.</summary>
        public static void Worker(MeshBuilder mb, Color tunic, Color cap, ToolKind tool)
        {
            var trousers = new Color(0.36f, 0.30f, 0.24f);
            Torso(mb, tunic, cap);
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Box(new Vector3(side * LegX, 0.4f, 0), new Vector3(0.22f, 0.8f, 0.24f), trousers);
                mb.Box(new Vector3(side * LegX, 0.06f, 0.05f), new Vector3(0.24f, 0.14f, 0.34f), new Color(0.22f, 0.15f, 0.09f));
            }
            // Arms reach forward to hold the tool.
            foreach (int side in new[] { -1, 1 })
            {
                ModelKit.Beam(mb, new Vector3(side * ShoulderX, Shoulder, 0), new Vector3(side * 0.16f, 1.1f, 0.45f), 0.09f, tunic * 0.95f);
                mb.Box(new Vector3(side * 0.14f, 1.08f, 0.48f), new Vector3(0.16f, 0.16f, 0.16f), ModelKit.Skin);
            }
            // The tool held in both hands, head up and forward.
            switch (tool)
            {
                case ToolKind.Axe:
                case ToolKind.Pick:
                case ToolKind.Hammer:
                    ModelKit.Beam(mb, new Vector3(0, 0.95f, 0.4f), new Vector3(0, 1.9f, 0.75f), 0.045f, ModelKit.Wood);
                    if (tool == ToolKind.Pick) ModelKit.Beam(mb, new Vector3(0, 1.75f, 0.45f), new Vector3(0, 2.0f, 1.1f), 0.05f, ModelKit.Metal);
                    else mb.Box(new Vector3(0, 1.9f, 0.82f), new Vector3(0.08f, 0.26f, 0.34f), ModelKit.Metal);
                    break;
                case ToolKind.Shovel:
                    ModelKit.Beam(mb, new Vector3(0, 1.3f, 0.3f), new Vector3(0, 0.3f, 0.9f), 0.045f, ModelKit.Wood);
                    mb.Box(new Vector3(0, 0.22f, 0.95f), new Vector3(0.3f, 0.36f, 0.06f), ModelKit.Metal);
                    break;
                case ToolKind.Sickle:
                    ModelKit.Beam(mb, new Vector3(0.14f, 1.08f, 0.48f), new Vector3(0.14f, 1.0f, 0.8f), 0.04f, ModelKit.Metal);
                    break;
                case ToolKind.Spear:
                    ModelKit.Beam(mb, new Vector3(0.14f, 0.6f, 0.2f), new Vector3(0.14f, 1.5f, 1.4f), 0.04f, ModelKit.Wood);
                    mb.Cone(new Vector3(0.14f, 1.5f, 1.4f), 0.08f, 0.3f, 4, ModelKit.Metal);
                    break;
            }
        }
    }
}
