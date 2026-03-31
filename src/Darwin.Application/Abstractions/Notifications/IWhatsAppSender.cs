using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Notifications
{
    /// <summary>Sends WhatsApp text messages via the configured provider.</summary>
    public interface IWhatsAppSender
    {
        Task SendTextAsync(
            string toPhoneE164,
            string text,
            CancellationToken ct = default,
            ChannelDispatchContext? context = null);
    }
}
