using Hangfire;
using SMTPServer.Repositories;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMTPServer.Services
{
    public class SMTPServerService : ISMTPServerService
    {
        private readonly int port = 26;
        private readonly IOneSourceRepository _oneSourceRepository;
        private readonly ILogger<SMTPServerService> _logger;
        private readonly IConfiguration _configuration;

        private Queue<string> emailMessageQueue = new Queue<string>();

        public SMTPServerService(IOneSourceRepository oneSourceRepository, ILogger<SMTPServerService> logger, IConfiguration configuration)
        {
            _oneSourceRepository = oneSourceRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task HandleClientAsync(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(IPAddress.Any, port);

            try
            {
                RecurringJob.AddOrUpdate(
                    "emailMessagesHandler",
                    () => HandleEmailMessages(CancellationToken.None),
                    "*/5 * * * * *"
                );

                listener.Start();

                _logger.LogInformation("Started TCP listener to listen for incoming emails from atosonesource.com.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    await EnqueueEmailMessages(client);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Server: Operation failed.");
            }
            finally
            {
                listener.Stop();

                _logger.LogInformation("Stopped TCP listener for listening for incoming emails from atosonesource.com.");
            }
        }

        private async Task EnqueueEmailMessages(TcpClient client)
        {
            _logger.LogInformation("Intercepted an email message. Enqueuing the message started.");

            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                emailMessageQueue.Enqueue(request);

                client.Close();
            }

            _logger.LogInformation("Enqueuing the email message ended.");
        }

        public Task HandleEmailMessages(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Started handling email received messages.");

            foreach (string emailMessage in emailMessageQueue)
            {
                _logger.LogInformation("Handling the following email message: "+emailMessage);
            }

            _logger.LogInformation("Ended handling email received messages.");

            return Task.CompletedTask;
        }
    }
}
