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

        var connectionString = context.Configuration.GetConnectionString("OneSourceContextConnection");

        services.AddDbContextFactory<OneSourceContext>(options =>
                     options.UseSqlServer(connectionString));

        services.AddSingleton<ISMTPServerService, SMTPServerService>();
        services.AddSingleton<IOneSourceRepository, OneSourceRepository>();

        services.AddHostedService<Worker>();

        services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseRecommendedSerializerSettings()
                .UseSimpleAssemblyNameTypeSerializer()
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
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
