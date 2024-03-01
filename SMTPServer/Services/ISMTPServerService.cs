using System.Net.Sockets;

namespace SMTPServer.Services
{
    public interface ISMTPServerService
    {
        Task HandleClientAsync(CancellationToken cancellationToken);
    }
}
