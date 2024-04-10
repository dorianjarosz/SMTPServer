using Hangfire;
using Microsoft.EntityFrameworkCore;
using OneSourceSMTPServer;
using OneSourceSMTPServer.Data;
using OneSourceSMTPServer.Repositories;
using OneSourceSMTPServer.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<HostOptions>(hostOptions =>
        {
            hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });
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

        services.AddWindowsService(options =>
        {
            options.ServiceName = "SMTP Server";
        });

        services.AddHostedService<Worker>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
