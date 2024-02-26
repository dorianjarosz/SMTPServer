using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMTPReceiver.Services
{
    public class SmtpReceiver : ISmtpReceiver
    {
        private readonly ILogger<Worker> _logger;

        public SmtpReceiver(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        private readonly int port = 7777;

        public Task<TcpListener> Start()
        {
            var listener = new TcpListener(IPAddress.Any, port);

            listener.Start();

            _logger.LogInformation($"SMTP Receiver is listening on port {port}");

            return Task.FromResult(listener);
        }

        public async Task HandleClientAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                using (NetworkStream stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                using (var writer = new StreamWriter(stream, Encoding.ASCII))
                {
                    string email;

                    while ((email = await reader.ReadToEndAsync()) != null)
                    {
                        _logger.LogInformation(email);
                    }

                    string response = "250 OK";
                    await writer.WriteAsync(response);
                    await writer.FlushAsync();
                }

                client.Close();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "SMTP Receiver: Operation cancelled.");
                throw;
            }
        }
    }
}
