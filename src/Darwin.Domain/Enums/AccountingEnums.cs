namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Represents the classification of a financial account in a lightweight chart of accounts.
    /// </summary>
    public enum AccountType : short
    {
        Asset = 0,
        Liability = 1,
        Equity = 2,
        Revenue = 3,
        Expense = 4
    }
}
