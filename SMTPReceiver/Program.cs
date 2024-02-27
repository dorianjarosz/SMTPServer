using Microsoft.EntityFrameworkCore;
using SMTPReceiver;
using SMTPReceiver.Data;
using SMTPReceiver.Repositories;
using SMTPReceiver.Services;

namespace OneSource
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<Worker>();
                services.AddSingleton<ISmtpReceiverService, SmtpReceiverService>();
                services.AddSingleton<IOneSourceRepository, OneSourceRepository>();
                services.AddDbContext<OneSourceContext>(options =>
                {
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("OneSourceContextConnection")
                    );
                });
            })
            .UseConsoleLifetime();
    }
}
