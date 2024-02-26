using SMTPReceiver;
using SMTPReceiver.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<ISmtpReceiver, SmtpReceiver>();
    })
    .Build();

host.Run();
