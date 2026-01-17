namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Lifecycle status for business invitations.
    /// </summary>
    public enum BusinessInvitationStatus : short
    {
        Pending = 0,
        Accepted = 1,
        Revoked = 2,
        Expired = 3
    }
}
