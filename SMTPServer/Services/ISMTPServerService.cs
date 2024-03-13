namespace SMTPServer.Services
{
    public interface ISMTPServerService
    {
        Task HandleClientAsync(CancellationToken cancellationToken);
    }
}
