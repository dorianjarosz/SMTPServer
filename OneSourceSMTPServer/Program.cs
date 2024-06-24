using Hangfire;
using Hangfire.MAMQSqlExtension;
using Hangfire.SqlServer;
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
                .UseSimpleAssemblyNameTypeSerializer()
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseRecommendedSerializerSettings()
                .UseResultsInContinuations()
                .UseMAMQSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true,
                }, new[] { "onesource_main_queue" }));

        services.AddHangfireServer(options =>
        {
            options.Queues = new[] { "onesource_main_queue", "default" };
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
