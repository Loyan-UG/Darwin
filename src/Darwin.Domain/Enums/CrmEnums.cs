namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Represents the qualification lifecycle of a CRM lead.
    /// </summary>
    public enum LeadStatus : short
    {
        New = 0,
        Qualified = 1,
        Disqualified = 2,
        Converted = 3
    }

    /// <summary>
    /// Represents the stage of a sales opportunity.
    /// </summary>
    public enum OpportunityStage : short
    {
        Qualification = 0,
        Proposal = 1,
        Negotiation = 2,
        ClosedWon = 3,
        ClosedLost = 4
    }
}
