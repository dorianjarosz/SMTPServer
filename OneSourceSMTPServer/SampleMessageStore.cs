using System.Buffers;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace OneSourceSMTPServer
{
    public class SampleMessageStore : MessageStore
    {
        public override Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            File.Create("C:\\SMTPReceiver\\ReceivedEmails\\hurray.txt");

            return Task.FromResult(SmtpResponse.Ok);
        }
    }
}
