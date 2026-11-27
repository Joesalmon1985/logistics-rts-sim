namespace God.Sim.Orgs
{
    /// <summary>
    /// F1.1 (contracts 1): the identity rules of organisations and the civilisation ledger. OWNER: F1 (frozen).
    /// <list type="bullet">
    /// <item><c>OrgId</c>: allocated only by <c>Orgs.Found</c> (C6) from <see cref="OrgState.NextOrgId"/>; every organisation of every
    /// kind; dissolved organisations keep their record.</item>
    /// <item><c>RequestId</c> (service request): <c>Services.Open</c> (C6) from <see cref="OrgState.NextRequestId"/>.</item>
    /// <item><c>CaseId</c>: C9, counter in its <c>OrgState.Justice</c> sub-object.</item>
    /// <item><c>ObligationId</c> (<c>LoanId</c> is an alias): C2 <c>Obligations.Open</c> from <c>CivState.NextObligationId</c>.</item>
    /// <item><c>ContractId</c> (employment): C3 <c>Employment.Hire</c> from <c>CivState.NextContractId</c>.</item>
    /// </list>
    /// All ids are <c>int</c>, start at 1, are never reused (also after dissolution, save or load) and are allocated in
    /// deterministic processing order; 0 means none.
    /// </summary>
    public static class OrgIds
    {
        /// <summary>Takes the next id of a counter.</summary>
        public static int Take(ref int counter) => counter++;
    }
}
