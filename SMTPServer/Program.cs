using Microsoft.EntityFrameworkCore;
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
            })
            .UseWindowsService()
            .UseConsoleLifetime();
    }
}
