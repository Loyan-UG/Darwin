using System;

namespace Darwin.Application.Abstractions.Services
{
    /// <summary> Abstraction for current UTC time to improve testability. </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
