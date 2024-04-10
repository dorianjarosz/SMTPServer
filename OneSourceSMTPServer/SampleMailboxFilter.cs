using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace OneSourceSMTPServer
{
    public class SampleMailboxFilter : IMailboxFilter, IMailboxFilterFactory
    {
        public Task<bool> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            File.Create("C:\\SMTPReceiver\\ReceivedEmails\\hurray.txt");

            return Task.FromResult(true);
        }

        public Task<bool> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            File.Create("C:\\SMTPReceiver\\ReceivedEmails\\hurray.txt");

            return Task.FromResult(true);
        }

        public IMailboxFilter CreateInstance(ISessionContext context)
        {
            return new SampleMailboxFilter();
        }
    }
}
