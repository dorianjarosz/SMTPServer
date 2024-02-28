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
                services.AddSingleton<ISmtpReceiverService, SmtpReceiverService>();
                services.AddSingleton<IOneSourceRepository, OneSourceRepository>();

                var connectionString = context.Configuration.GetConnectionString("OneSourceContextConnection");

                var optionsBuilder = new DbContextOptionsBuilder<OneSourceContext>();

                optionsBuilder.UseSqlServer(connectionString);

                services.AddSingleton(db => new OneSourceContext(optionsBuilder.Options));

                services.AddHostedService<Worker>();
            })
            .UseWindowsService()
            .UseConsoleLifetime();
    }
}
