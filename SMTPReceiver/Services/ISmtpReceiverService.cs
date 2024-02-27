using System.Net.Sockets;
using System.Threading.Tasks;

namespace SMTPReceiver.Services
{
    public interface ISmtpReceiverService
    {
        Task<TcpListener> Start();

        Task HandleClientAsync(TcpListener listener, CancellationToken cancellationToken);
    }
}
