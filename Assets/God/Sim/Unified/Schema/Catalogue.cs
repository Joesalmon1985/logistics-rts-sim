namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.7, UN0 spec 5.8): the run-time catalogue facade. Empty at UN0; UN3a's generator writes the tables in
    /// <c>Unified/Catalogue/Generated/*.g.cs</c>, which fill the <c>TablesUN3a</c> slot (sorted by <c>CatId</c>). Immutable after the
    /// static initialiser; lookups are binary searches, never a dictionary. No Excel at run time.
    /// </summary>
    public static partial class Catalogue
    {
        /// <summary>The workbook SHA-256 the generated tables come from ("" while empty).</summary>
        public static readonly string WorkbookSha = Sha();

        public static readonly ReadOnlyArray<ObjectDef> Objects = LoadObjects();
        public static readonly ReadOnlyArray<BuildingDef> Buildings = LoadBuildings();
        public static readonly ReadOnlyArray<RecipeDef> Recipes = LoadRecipes();

        public static bool IsEmpty => Objects.Count == 0 && Buildings.Count == 0;

        /// <summary>The object definition of a catalogue id, or null.</summary>
        public static ObjectDef Object(ushort id)
        {
            int lo = 0, hi = Objects.Count - 1;
            while (lo <= hi) { int mid = (lo + hi) >> 1; var v = Objects[mid].CatId; if (v == id) return Objects[mid]; if (v < id) lo = mid + 1; else hi = mid - 1; }
            return null;
        }

        /// <summary>The building definition of a catalogue id, or null.</summary>
        public static BuildingDef Building(ushort id)
        {
            int lo = 0, hi = Buildings.Count - 1;
            while (lo <= hi) { int mid = (lo + hi) >> 1; var v = Buildings[mid].CatId; if (v == id) return Buildings[mid]; if (v < id) lo = mid + 1; else hi = mid - 1; }
            return null;
        }

        /// <summary>The object definition whose ware is <paramref name="w"/>, or null (a linear scan: for tools and tests, not hot paths).</summary>
        public static ObjectDef OfWare(Ware w) { foreach (var o in Objects) if (o.Ware == w) return o; return null; }

        private static string Sha() { string x = ""; ShaUN3a(ref x); return x; }
        private static ReadOnlyArray<ObjectDef> LoadObjects() { ObjectDef[] t = null; ObjectsUN3a(ref t); return new ReadOnlyArray<ObjectDef>(t ?? new ObjectDef[0]); }
        private static ReadOnlyArray<BuildingDef> LoadBuildings() { BuildingDef[] t = null; BuildingsUN3a(ref t); return new ReadOnlyArray<BuildingDef>(t ?? new BuildingDef[0]); }
        private static ReadOnlyArray<RecipeDef> LoadRecipes() { RecipeDef[] t = null; RecipesUN3a(ref t); return new ReadOnlyArray<RecipeDef>(t ?? new RecipeDef[0]); }

        // ---- the generated tables' slots (UN3a) ----
        static partial void ShaUN3a(ref string sha);
        static partial void ObjectsUN3a(ref ObjectDef[] table);
        static partial void BuildingsUN3a(ref BuildingDef[] table);
        static partial void RecipesUN3a(ref RecipeDef[] table);
    }
}
