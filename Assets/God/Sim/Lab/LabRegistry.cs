namespace God.Sim.Lab
{
    /// <summary>Every lab experiment, in id order (STRATEGY_BENCHMARKS 4.6). A method, not a static table, so nothing is shared.</summary>
    public static class LabRegistry
    {
        public static LabExperiment[] All() => new LabExperiment[]
        {
            new L01HawkDove(),
            new L02Retaliator(),
            new L03Attrition(),
            new L04ResidentIntruder(),
            new L05PeckingOrder(),
            new L06CoyFast(),
            new L07SuckerCheatGrudger(),
            new L08RoundRobin(),
            new L09Ecological(),
            new L09bTftAllD(),
            new L10DiggerWasps(),
            new L11Hamilton(),
            new L12SelfishHerd(),
            new L13VampireBats(),
        };

        public static LabExperiment Find(string id)
        {
            foreach (var e in All()) if (string.Equals(e.Id, id, System.StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }
    }
}
