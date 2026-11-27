using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Households;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Draws a person's look by life stage and body state on its figure (OWNER: M4, U3b; POPULATION 6, HOUSEHOLDS 6,
    /// VISUAL_WORLD 10). Called by <see cref="NpcLayer"/> once per drawn person per frame, after the figure is placed and
    /// animated: a child is smaller by its growth with a bigger head, an elder a little shorter, stooped and grey; an expecting
    /// mother shows a belly scaled by the pregnancy's progress; an injured person limps and wears a bandage (a sling when
    /// badly hurt); an ill person is tinted and slumps; a tired one droops; a begging child bobs. Reads the sim; never changes it.
    /// </summary>
    public static class LifeStageLayer
    {
        /// <summary>The older entry point (no extra parts): size and stoop only.</summary>
        public static void Apply(Transform figure, Npc n, GodState s)
        {
            float k = NpcLayer.Scale;
            var l = n.Life;
            if (l == null) { figure.localScale = Vector3.one * k; return; }
            if (n.Stage == LifeStage.Child)
            {
                float g = 0.5f + 0.5f * Mathf.Clamp01(l.Growth / 1000f);
                figure.localScale = Vector3.one * (k * g);
                if (l.Display == DisplayKind.Beg && l.DisplayUntil >= s.Tick && !n.Action.Asleep)
                    figure.position += Vector3.up * (0.04f * l.DisplayLevel * Mathf.Abs(Mathf.Sin(Time.time * 9f)));
                return;
            }
            var scale = Vector3.one * k;
            if (n.Stage == LifeStage.Elder) scale.y *= 0.93f;
            figure.localScale = scale;
            if (n.Stage == LifeStage.Elder && !n.Action.Asleep) figure.rotation *= Quaternion.Euler(8f, 0f, 0f);
        }

        /// <summary>Size, stoop and the visible body state (U3b).</summary>
        public static void Apply(FigureParts p, Npc n, GodState s)
        {
            Apply(p.Root, n, s);
            var l = n.Life;
            bool child = n.Stage == LifeStage.Child, elder = n.Stage == LifeStage.Elder;
            // age: head ratio and hair
            int key = (int)n.Faction * 2 + (elder ? 1 : 0);
            if (p.HeadKey != key) { p.HeadKey = key; p.HeadF.sharedMesh = NpcLayer.HeadMesh(n.Faction, elder); }
            p.Head.localScale = Vector3.one * CharacterLook.HeadScale(child, l != null ? l.Growth : 1000);
            // pregnancy: a belly by progress
            bool preg = l != null && l.PregFather != 0;
            p.Belly.gameObject.SetActive(preg);
            if (preg) p.Belly.localScale = Vector3.one * CharacterLook.BellyScale(CharacterLook.Progress(s.Tick, l.PregStart, l.PregDue));
            // injury: bandages, a sling when badly hurt, a limp
            bool hurt = CharacterLook.Bandaged(n.Injury), sling = CharacterLook.Slung(n.Injury);
            p.BandageArm.gameObject.SetActive(hurt); p.BandageLeg.gameObject.SetActive(hurt); p.Sling.gameObject.SetActive(sling);
            if (hurt)
            {
                p.LegR.localRotation = Quaternion.Slerp(Quaternion.identity, p.LegR.localRotation, CharacterLook.LimpKeep(n.Injury));
                if (!n.Action.Asleep) p.Body.localRotation *= Quaternion.Euler(0, 0, 4f);
            }
            if (sling && !n.Action.Asleep) p.ArmL.localRotation = Quaternion.Euler(-70f, 0, 20f);
            // illness: a tint and a slump; fatigue: a droop
            bool ill = l != null && l.Illness == 1;
            if (ill != p.IllShown)
            {
                p.IllShown = ill;
                foreach (var r in p.Skinned) r.sharedMaterial = ill ? NpcLayer.IllMaterial : ModelKit.Lit;
            }
            if (!n.Action.Asleep)
            {
                int fatigue = n.Drives != null && n.Drives.Length > Drive.Fatigue ? n.DriveOf(Drive.Fatigue) : 0;
                float slump = CharacterLook.SlumpDegrees(ill, fatigue);
                if (slump > 0f) p.Body.localRotation *= Quaternion.Euler(slump, 0, 0);
            }
        }
    }
}
