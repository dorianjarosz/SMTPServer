using SMTPServer.Services;

namespace SMTPServer
{
    public class Worker : BackgroundService
    {
        private readonly ISMTPServerService _smtpReceiver;

        public Worker(ISMTPServerService smtpReceiver)
        {
            _smtpReceiver = smtpReceiver;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _smtpReceiver.HandleClientAsync(stoppingToken);
        }
    }
}
