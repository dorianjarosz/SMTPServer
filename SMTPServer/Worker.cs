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
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var tcpListener = await _smtpReceiver.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
               await _smtpReceiver.HandleClientAsync(tcpListener, stoppingToken);
            }
        }
    }
}
