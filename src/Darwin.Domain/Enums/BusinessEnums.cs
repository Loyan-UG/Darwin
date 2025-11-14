using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Role of a user within a business workspace.
    /// Determines operational capabilities in Business app.
    /// </summary>
    public enum BusinessMemberRole : short
    {
        Owner = 1,     // Full control, billing, program settings
        Manager = 2,   // Operational control, reward/points management
        Staff = 3      // Day-to-day scanning, order capture, minimal settings
    }


    /// <summary>
    /// High-level category of a business for discovery, filtering, and analytics.
    /// </summary>
    public enum BusinessCategoryKind : short
    {
        Unknown = 0,
        Cafe = 10,
        Restaurant = 11,
        Bakery = 12,
        Supermarket = 20,
        SalonSpa = 30,
        Fitness = 40,
        OtherRetail = 50,
        Services = 60
    }
}
