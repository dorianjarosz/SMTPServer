using System.Net.Sockets;
using System.Threading.Tasks;

namespace SMTPServer.Services
{
    public interface ISMTPServerService
    {
        Task<TcpListener> Start();

        Task HandleClientAsync(TcpListener listener, CancellationToken cancellationToken);
    }
}
