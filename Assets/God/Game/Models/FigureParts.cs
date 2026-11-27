using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The parts of one person's figure (U3b rig). The original parts keep their names (Body, LegL, LegR, ArmL, ArmR, Cargo,
    /// Tool, Torso); the additions are Head (pivot at y 1.5), FingersL/FingersR, Held (+ HeldStack), Symbol, Aura, Back, Hip,
    /// Belly, Sling and the Bandage parts. View only.
    /// </summary>
    public sealed class FigureParts
    {
        public Transform Root, Body, Head, LegL, LegR, ArmL, ArmR, Held, Symbol, Aura, Back, Hip, Belly, Sling, BandageArm, BandageLeg, FingersL, FingersR;
        public Transform[] HeldStack;
        public MeshFilter Cargo, Tool, BodyF, HeadF, HeldF, FingersLF, FingersRF, AuraF;
        public Renderer[] Skinned;
        public int HeadKey = -1;
        public bool IllShown;
        public Material[] Mats;

        public static readonly Vector3 HeldHome = new Vector3(0, -0.85f, 0.55f);

        public static FigureParts Build(Transform root, Mesh leg)
        {
            var f = new FigureParts { Root = root };
            var body = new GameObject("Body"); body.transform.SetParent(root, false); f.Body = body.transform;
            f.BodyF = ModelKit.Part("Torso", f.Body, null).GetComponent<MeshFilter>();
            f.Head = ModelKit.Part("Head", f.Body, null).transform; f.Head.localPosition = new Vector3(0, PeopleModels.HeadPivot, 0);
            f.HeadF = f.Head.GetComponent<MeshFilter>();
            f.LegL = ModelKit.Part("LegL", f.Body, leg).transform; f.LegL.localPosition = new Vector3(-PeopleModels.LegX, PeopleModels.Hip, 0);
            f.LegR = ModelKit.Part("LegR", f.Body, leg).transform; f.LegR.localPosition = new Vector3(PeopleModels.LegX, PeopleModels.Hip, 0);
            f.ArmL = ModelKit.Part("ArmL", f.Body, null).transform; f.ArmL.localPosition = new Vector3(-PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            f.ArmR = ModelKit.Part("ArmR", f.Body, null).transform; f.ArmR.localPosition = new Vector3(PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            f.Cargo = ModelKit.Part("Cargo", f.Body, null).GetComponent<MeshFilter>();
            f.Tool = ModelKit.Part("Tool", f.ArmR, null).GetComponent<MeshFilter>();
            // hands: finger pips (quantity 1-9: 1-5 on the right, 6-9 add the left)
            f.FingersL = ModelKit.Part("FingersL", f.ArmL, null).transform; f.FingersLF = f.FingersL.GetComponent<MeshFilter>();
            f.FingersR = ModelKit.Part("FingersR", f.ArmR, null).transform; f.FingersRF = f.FingersR.GetComponent<MeshFilter>();
            // the real item in the hands, and up to two more of a stack
            f.Held = ModelKit.Part("Held", f.Body, null).transform; f.Held.localPosition = HeldHome; f.HeldF = f.Held.GetComponent<MeshFilter>();
            f.HeldStack = new Transform[2];
            for (int i = 0; i < 2; i++) { f.HeldStack[i] = ModelKit.Part("HeldStack" + i, f.Held, null).transform; f.HeldStack[i].localPosition = new Vector3((i == 0 ? 0.25f : -0.25f), 0.12f * (i + 1), 0); }
            f.Symbol = new GameObject("Symbol").transform; f.Symbol.SetParent(f.Body, false); f.Symbol.localPosition = new Vector3(0, 2.7f, 0);
            f.Aura = ModelKit.Part("Aura", root, null).transform; f.AuraF = f.Aura.GetComponent<MeshFilter>();
            f.Back = new GameObject("Back").transform; f.Back.SetParent(f.Body, false); f.Back.localPosition = new Vector3(0, 1.2f, -0.28f);
            f.Hip = new GameObject("Hip").transform; f.Hip.SetParent(f.Body, false); f.Hip.localPosition = new Vector3(0.36f, 0.9f, 0);
            f.Belly = ModelKit.Part("Belly", f.Body, null).transform; f.Belly.localPosition = new Vector3(0, 1.0f, 0.26f);
            f.Sling = ModelKit.Part("Sling", f.Body, null).transform;
            f.BandageArm = ModelKit.Part("Bandage", f.ArmL, null).transform; f.BandageArm.localPosition = Vector3.zero;
            f.BandageLeg = ModelKit.Part("BandageLeg", f.LegR, null).transform;
            f.Skinned = new[] { f.BodyF.GetComponent<Renderer>(), f.HeadF.GetComponent<Renderer>(), f.ArmL.GetComponent<Renderer>(), f.ArmR.GetComponent<Renderer>(), f.LegL.GetComponent<Renderer>(), f.LegR.GetComponent<Renderer>() };
            f.Belly.gameObject.SetActive(false); f.Sling.gameObject.SetActive(false); f.BandageArm.gameObject.SetActive(false);
            f.BandageLeg.gameObject.SetActive(false); f.Aura.gameObject.SetActive(false); f.Held.gameObject.SetActive(false);
            f.FingersL.gameObject.SetActive(false); f.FingersR.gameObject.SetActive(false);
            return f;
        }
    }
}
