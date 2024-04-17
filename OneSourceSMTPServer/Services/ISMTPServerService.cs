namespace OneSourceSMTPServer.Services
{
    public interface ISMTPServerService
    {
        Task HandleClientAsync(CancellationToken cancellationToken);

        void StopListener();
    }
}
