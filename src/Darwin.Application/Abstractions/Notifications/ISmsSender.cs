using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Notifications
{
    /// <summary>Sends SMS messages. Optional in early phases.</summary>
    public interface ISmsSender
    {
        Task SendAsync(string toPhoneE164, string text, CancellationToken ct = default);
    }
}
