using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SMTPServer.Data;
using SMTPServer.Repositories;
using SMTPServer.Services;

namespace SMTPServer
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
                services.AddSingleton<ISMTPServerService, SMTPServerService>();
                services.AddSingleton<IOneSourceRepository, OneSourceRepository>();

                var connectionString = context.Configuration.GetConnectionString("OneSourceContextConnection");

                var optionsBuilder = new DbContextOptionsBuilder<OneSourceContext>();

                optionsBuilder.UseSqlServer(connectionString);

                services.AddSingleton(db => new OneSourceContext(optionsBuilder.Options));

                services.AddHostedService<Worker>();

                services.AddHangfire(configuration => configuration
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseSqlServerStorage(connectionString));

                services.AddHangfireServer(options =>
                {
                    options.SchedulePollingInterval = TimeSpan.FromMilliseconds(5000);
                });
            })
            .UseWindowsService()
            .UseConsoleLifetime();
    }
}
