using SMTPReceiver.Services;

namespace SMTPReceiver
{
    public class Worker : BackgroundService
    {
        private readonly ISmtpReceiver _smtpReceiver;

        public Worker(ISmtpReceiver smtpReceiver)
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