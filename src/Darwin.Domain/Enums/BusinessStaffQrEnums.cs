namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Purpose discriminator for staff QR codes.
    /// Extend cautiously; keep semantics stable for mobile clients.
    /// </summary>
    public enum BusinessStaffQrPurpose : short
    {
        StaffSignIn = 1,
        TerminalPairing = 2,
        PrivilegedScan = 3
    }
}
