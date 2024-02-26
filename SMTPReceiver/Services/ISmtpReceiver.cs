using System.Net.Sockets;
using System.Threading.Tasks;

namespace SMTPReceiver.Services
{
    public interface ISmtpReceiver
    {
        Task<TcpListener> Start();

        Task HandleClientAsync(TcpListener listener, CancellationToken cancellationToken);
    }
}
