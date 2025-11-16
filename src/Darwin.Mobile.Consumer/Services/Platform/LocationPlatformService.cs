using Darwin.Mobile.Shared.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Platform
{
    public sealed class LocationPlatformService : ILocation
    {
        public Task<(double lat, double lng)?> GetCurrentAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
