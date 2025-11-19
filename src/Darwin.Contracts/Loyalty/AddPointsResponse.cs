using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Loyalty
{
    public sealed class AddPointsResponse
    {
        public int NewBalance { get; init; }
        public DateTime AccruedAtUtc { get; init; }
    }
}
