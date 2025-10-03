using Darwin.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Domain.Entities.Pricing
{
    /// <summary>
    /// Tracks actual uses of a promotion to enforce caps and provide reporting.
    /// </summary>
    public sealed class PromotionRedemption : BaseEntity
    {
        public Guid PromotionId { get; set; }
        /// <summary>Optional user id who redeemed; null for guests.</summary>
        public Guid? UserId { get; set; }
        /// <summary>Order id associated with this redemption.</summary>
        public Guid OrderId { get; set; }
    }
}
