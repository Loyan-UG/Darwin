using System;
using Darwin.Application.Abstractions.Services;

namespace Darwin.Infrastructure.Adapters.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
