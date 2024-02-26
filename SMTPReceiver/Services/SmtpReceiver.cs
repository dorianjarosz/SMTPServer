using Microsoft.Extensions.Logging;
using MimeKit;
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

                using (NetworkStream stream = client.Available.())
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                using (var writer = new StreamWriter(stream, Encoding.ASCII))
                {
                    string emailContent;

                    while ((emailContent = await reader.ReadToEndAsync()) != null)
                    {
                        var message = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(emailContent)));

                        var address = ((MailboxAddress)message.From[0]).Address;

                        var toRecipients = message.To;
                        var ccRecipients = message.Cc;
                        var bccRecipients = message.Bcc;


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

        private static string GetHeaderValue(IEnumerable<Header> headers, string headerName)
        {
            foreach (var header in headers)
            {
                if (header.Field.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Value;
                }
            }
            return null;
        }
    }
}
