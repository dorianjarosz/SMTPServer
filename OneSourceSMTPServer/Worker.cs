using Microsoft.EntityFrameworkCore;
using OneSourceSMTPServer.Data;
using OneSourceSMTPServer.Services;

namespace OneSourceSMTPServer
{
    public class Worker : BackgroundService
    {
        private readonly ISMTPServerService _smtpReceiver;
        private readonly OneSourceContext _context;

        public Worker(ISMTPServerService smtpReceiver, OneSourceContext context)
        {
            _smtpReceiver = smtpReceiver;
            _context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _context.Database.MigrateAsync();

            await _smtpReceiver.HandleClientAsync(stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _smtpReceiver.StopListener();
            return base.StopAsync(cancellationToken);
        }
    }
}
