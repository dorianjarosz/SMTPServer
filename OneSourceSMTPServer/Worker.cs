using OneSourceSMTPServer.Services;

namespace OneSourceSMTPServer
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

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _smtpReceiver.StopListener();
            return base.StopAsync(cancellationToken);
        }
    }
}
