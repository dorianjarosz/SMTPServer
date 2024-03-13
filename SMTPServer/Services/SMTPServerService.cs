using Hangfire;
using MimeKit;
using Newtonsoft.Json;
using SMTPServer.Data.Entities;
using SMTPServer.Repositories;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMTPServer.Services
{
    public class SMTPServerService : ISMTPServerService
    {
        private bool enququeTestEmailMessage = false;
        private string testEmailMessage = "UmVjZWl2ZWQ6IGZyb20gYXBwMTMyMTUwLmFtczEwMS5zZXJ2aWNlLW5vdy5jb20gKHVua25vd24gWzEwLjI0OS4zOS4xNjldKQoJYnkgb3V0Ym91bmQzMy5zZXJ2aWNlLW5vdy5jb20gKFBvc3RmaXgpIHdpdGggRVNNVFAgaWQgQjdFMDYyMTVDOENGCglmb3IgPEdlbWFsdG8tRERDLU1JLWRsQGF0b3NvbmVzb3VyY2UuY29tPjsgU3VuLCAgMSBPY3QgMjAyMyAyMTowODozMSAtMDcwMCAoUERUKQpES0lNLUZpbHRlcjogT3BlbkRLSU0gRmlsdGVyIHYyLjExLjAgb3V0Ym91bmQzMy5zZXJ2aWNlLW5vdy5jb20gQjdFMDYyMTVDOENGCkRLSU0tU2lnbmF0dXJlOiB2PTE7IGE9cnNhLXNoYTI1NjsgYz1yZWxheGVkL3JlbGF4ZWQ7IGQ9c2VydmljZS1ub3cuY29tOwoJcz0yMDIxMDUwNDsgdD0xNjk2MjE5NzExOwoJYmg9eFdFOGtjTUpSVnNDSUo1Q242V0FHa1hBMExjZnZsSXVoRnFWeHBzNGxTST07CgloPURhdGU6RnJvbTpSZXBseS1UbzpUbzpTdWJqZWN0OkZyb207CgliPUJUMGJOeHZEUTFwaHR0OXlZdHRjdGcvU0ltRE9DOVM1S29CSWNuRzBHZ2RFMVdsMlUxUlZOeGpJejlNVVJFNks3CgkgQVNqYU1JRVIrSmJ6ZFVEWVQrTlpiOGc2Tm8vclMvYXN5YWJyVHQrTGI5dTdXZW9EbWc4Q2FPYU52RWVyUndiaFNNCgkgUXF2c0J6REtNTDJ1SFpZR01Fc0ZiUzFHTndIc0E3N1V1dGFMVVB1dTRudDNIekhMSktqYUZudUViamU1b2l5T210CgkgcitqOC9IdE50QXVabkttY2hPT2Via3VUc1FrNzRZY2JvRWRTVVpvZm9CVHd1KzRVbXJiY2x5T0lEbGttblBYOWdzCgkgd3doT3I1YVd5Z3FXSkM1KzhKOEwzdGtGY0N0ZVFpSXlFcE5tcW1LT0RjcHJwN2Z5VlYra0MwVUhOYkY3MlRoM3lTCgkgNUlVbEJyWGpMSFoxdz09CkRhdGU6IFN1biwgMSBPY3QgMjAyMyAyMTowODozMSAtMDcwMCAoUERUKQpGcm9tOiBJVCBTZXJ2aWNlIERlc2sgPGF0b3NnbG9iYWxAc2VydmljZS1ub3cuY29tPgpSZXBseS1UbzogSVQgU2VydmljZSBEZXNrIDxhdG9zZ2xvYmFsQHNlcnZpY2Utbm93LmNvbT4KVG86IEdlbWFsdG8tRERDLU1JLWRsQGF0b3NvbmVzb3VyY2UuY29tCk1lc3NhZ2UtSUQ6IDwxOTI3MjIzMC4xNjE3NTguMTY5NjIxOTcxMTcyNEBhcHAxMzIxNTAuYW1zMTAxLnNlcnZpY2Utbm93LmNvbT4KU3ViamVjdDogSU5DMDM5NTE2NzIyIC0gTm90aWNlICMzIC0gUDEgLSBBTEwgU0VSVklDRVMvU1lTVEVNUyBSRVNUT1JFRAogKENPTkZJUk1BVElPTiBQRU5ESU5HKSAtICEhIVAxIEFsZXJ0IHRvIEF0b3MgT3JhY2xlIEVCUyBtYWluIHBhZ2Ugc2xvd25lc3MKIGlzc3VlCk1JTUUtVmVyc2lvbjogMS4wCkNvbnRlbnQtVHlwZTogdGV4dC9wbGFpbjsgY2hhcnNldD1VVEYtOApDb250ZW50LVRyYW5zZmVyLUVuY29kaW5nOiBxdW90ZWQtcHJpbnRhYmxlClgtU2VydmljZU5vdy1Tb3VyY2U6IE5vdGlmaWNhdGlvbi0zNzRiYmFmNDFiNjFiNDk0NTdmMWM5MTg2ZTRiY2JlNQpYLVNlcnZpY2VOb3ctU3lzRW1haWwtVmVyc2lvbjogMgpQcmVjZWRlbmNlOiBidWxrCkF1dG8tU3VibWl0dGVkOiBhdXRvLWdlbmVyYXRlZApYLVNlcnZpY2VOb3ctR2VuZXJhdGVkOiB0cnVlCgp7IkNvbW11bmljYXRpb25UeXBlIjoiTU9OSVRPUiIsIlN0YXR1cyI6IkFMTCBTRVJWSUNFUy9TWVNURU1TIFJFU1RPUkVEIChDT049CkZJUk1BVElPTiBQRU5ESU5HKSIsIkluY2lkZW50VGl0bGUiOiIhISFQMSBBbGVydCB0byBBdG9zIE9yYWNsZSBFQlMgbWFpbiBwYT0KZ2Ugc2xvd25lc3MgaXNzdWUiLCJUaWNrZXQiOiJJTkMwMzk1MTY3MjIiLCJJbmNpZGVudExldmVsIjoiUDEiLCJOb3RpZmljYXRpPQpvbk51bWJlciI6IjMiLCJTdGFydE9mSW5jaWRlbnQiOiIyMDIzLTEwLTAyIDAzOjA3OjE2IFVUQyArMDI6MDAiLCJDcml0aWNhbEk9Cm1wYWN0U3RhcnQiOiIyMDIzLTEwLTAyIDAzOjA3OjAwIFVUQyArMDI6MDAiLCJTZXJ2aWNlUmVzdG9yYXRpb24iOiIyMDIzLTEwLT0KMDIgMDU6NDU6MDAgVVRDICswMjowMCIsIkJ1c2luZXNzSW1wYWN0ZWQiOiJHZW1hbHRvIEhhdXNzbWFubiIsIkJVU0lORVNTSU1QPQpBQ1RFWEVDVVRJVkVTVU1NQVJZIjoiIiwiSU5DSURFTlRFWEVDVVRJVkVTVU1NQVJZIjoiPGJyIC8+PGJyIC8+PHN0cm9uZz5NT049CklUT1I6IDIwMjMtMTAtMDIgMDY6MDc6MzMgVVRDICYjNDM7MDI6MDA8L3N0cm9uZz48cD5Qb3N0IHJlc3RhcnQgYXBwbGljYXRpbz0KbiB3b3JraW5nIGZpbmU8L3A+XHJcbjxwPlVzZXJzIGFyZSBzdGlsbCBmYWNpbmcgdGhlIGlzc3VlIHdpdGggc2xvd25lc3Mgb2YgPQphcHBsaWNhdGlvbjwvcD5cclxuPHA+Tm8gaXNzdWUgb2JzZXJ2ZWQgb24gYXBwbGljYXRpb24gJmFtcDsgREIgZnJvbSBBdG9zIGU9Cm5kPC9wPlxyXG48cD5PcmFjbGUgY2FzZSBTUiAzLTM0NDM0NzQxOTQxIHJhaXNlZCB0byBpbnZlc3RpZ2F0ZSB0aGUgaXNzdWU8Lz0KcD5cclxuPHA+QXMgcGVyIHRoZSBwcmV2aW91cyBpbmNpZGVudCBJMjIxMTI5XzAwMDE3NCwgQXRvcyBkaXNhYmxlZCB0aGUgcGVyPQpzb25hbGl6YXRpb24gYW5kIHJlc3RhcnRlZCBvYWNvcmVzPC9wPlxyXG48cD5Qb3N0IGRpc2FibGluZyB0aGUgcGVyc29uYWxpemE9CnRpb24sIHVzZXJzIGFyZSBhYmxlIHRvIGxvZyBpbiB3aXRob3V0IGFueSBpc3N1ZS48L3A+XHJcbjxwPjxiciAvPjxzdHJvbmc+VT0KUERBVEU6IDIwMjMtMTAtMDIgMDQ6MjA6MDQgVVRDICYjNDM7MDI6MDA8L3N0cm9uZz48L3A+XHJcbjxwPkFwcGxpY2F0aW9uIGlzPQogYWNjZXNzaWJsZSBmcm9tIEF0b3MgZW5kPC9wPlxyXG48cD5NdWx0aXBsZSBvYWNvcmVfc2VydmVycyBhcmUgaW4gd2FybmluZyA9CnN0YXRlPC9wPlxyXG48cD5BcyByZXF1ZXN0ZWQgYnkgVGhhbGVzIElNIFBldGVyIEdVSSwgQXRvcyB0ZWFtIGlzIHBlcmZvcm1pbj0KZyB0aGUgcmVzdGFydCBvZiB0aGUgYXBwbGljYXRpb248L3A+XHJcbjxwPjxiciAvPjxzdHJvbmc+SU5JVElBTDogMjAyMy0xMC0wPQoyIDAzOjQ1OjI0IFVUQyAmIzQzOzAyOjAwPC9zdHJvbmc+PC9wPlxyXG48cD5QZXJmb3JtYW5jZSBpc3N1ZSB3aXRoIE9yYWNsZSA9CkVCUyBtYWluIHBhZ2UgcmVwb3J0ZWQgYnkgU2hhbmdoYWkgdXNlcnM8L3A+XHJcbjxwPkludmVzdGlnYXRpb24gb25nb2luZzwvcD0KPiIsIlNpdGVzTG9jYXRpb25zQWZmZWN0ZWQiOiJTaGFuZ2hhaSwgU2luZ2Fwb3JlIiwiQ3VycmVudEJ1c2luZXNzSW1wYWN0IjoiPQpQZXJmb3JtYW5jZSBpc3N1ZSB3aXRoIE9yYWNsZSBFQlMgbWFpbiBwYWdlIiwiQ2FzZVN1bW1hcnkiOiI8YnIgLz48YnIgLz48c3Q9CnJvbmc+TU9OSVRPUjogMjAyMy0xMC0wMiAwNjowNzozMyBVVEMgJiM0MzswMjowMDwvc3Ryb25nPjxwPlBvc3QgcmVzdGFydCBhcD0KcGxpY2F0aW9uIHdvcmtpbmcgZmluZTwvcD5cclxuPHA+VXNlcnMgYXJlIHN0aWxsIGZhY2luZyB0aGUgaXNzdWUgd2l0aCBzbG93PQpuZXNzIG9mIGFwcGxpY2F0aW9uPC9wPlxyXG48cD5ObyBpc3N1ZSBvYnNlcnZlZCBvbiBhcHBsaWNhdGlvbiAmYW1wOyBEQiBmcm89Cm0gQXRvcyBlbmQ8L3A+XHJcbjxwPk9yYWNsZSBjYXNlIFNSIDMtMzQ0MzQ3NDE5NDEgcmFpc2VkIHRvIGludmVzdGlnYXRlIHRoZT0KIGlzc3VlPC9wPlxyXG48cD5BcyBwZXIgdGhlIHByZXZpb3VzIGluY2lkZW50IEkyMjExMjlfMDAwMTc0LCBBdG9zIGRpc2FibGVkPQogdGhlIHBlcnNvbmFsaXphdGlvbiBhbmQgcmVzdGFydGVkIG9hY29yZXM8L3A+XHJcbjxwPlBvc3QgZGlzYWJsaW5nIHRoZSBwZXI9CnNvbmFsaXphdGlvbiwgdXNlcnMgYXJlIGFibGUgdG8gbG9nIGluIHdpdGhvdXQgYW55IGlzc3VlLjwvcD5cclxuPHA+PGJyIC8+PD0Kc3Ryb25nPlVQREFURTogMjAyMy0xMC0wMiAwNDoyMDowNCBVVEMgJiM0MzswMjowMDwvc3Ryb25nPjwvcD5cclxuPHA+QXBwbGljPQphdGlvbiBpcyBhY2Nlc3NpYmxlIGZyb20gQXRvcyBlbmQ8L3A+XHJcbjxwPk11bHRpcGxlIG9hY29yZV9zZXJ2ZXJzIGFyZSBpbiA9Cndhcm5pbmcgc3RhdGU8L3A+XHJcbjxwPkFzIHJlcXVlc3RlZCBieSBUaGFsZXMgSU0gUGV0ZXIgR1VJLCBBdG9zIHRlYW0gaXMgcD0KZXJmb3JtaW5nIHRoZSByZXN0YXJ0IG9mIHRoZSBhcHBsaWNhdGlvbjwvcD5cclxuPHA+PGJyIC8+PHN0cm9uZz5JTklUSUFMOiAyPQowMjMtMTAtMDIgMDM6NDU6MjQgVVRDICYjNDM7MDI6MDA8L3N0cm9uZz48L3A+XHJcbjxwPlBlcmZvcm1hbmNlIGlzc3VlIHdpdGg9CiBPcmFjbGUgRUJTIG1haW4gcGFnZSByZXBvcnRlZCBieSBTaGFuZ2hhaSB1c2VyczwvcD5cclxuPHA+SW52ZXN0aWdhdGlvbiBvbj0KZ29pbmc8L3A+IiwiR0RQUnN0YXRlbWVudDEiOiJUbyBtb2RpZnkgeW91ciBzdWJzY3JpcHRpb25zIGZvciBNSSBub3RpZmljYXRpPQpvbnMgb3IgaWYgeW91IGRvIG5vdCB3YW50IHRvIHJlY2VpdmUgdGhlbSBhbnkgbW9yZSBwbGVhc2UgY29udGFjdCB5b3VyIEF0b3M9CiByZXByZXNlbnRhdGl2ZS5UaGlzIGUtbWFpbCBhbmQgdGhlIGRvY3VtZW50cyBhdHRhY2hlZCBhcmUgY29uZmlkZW50aWFsIGFuZD0KIGludGVuZGVkIHNvbGVseSBmb3IgdGhlIGFkZHJlc3NlZTsgaXQgbWF5IGFsc28gYmUgcHJpdmlsZWdlZC4gSWYgeW91IHJlY2VpPQp2ZSB0aGlzIGUtbWFpbCBpbiBlcnJvciwgcGxlYXNlIG5vdGlmeSB0aGUgc2VuZGVyIGltbWVkaWF0ZWx5IGFuZCBkZXN0cm95IGk9CnQuIEFzIGl0cyBpbnRlZ3JpdHkgY2Fubm90IGJlIHNlY3VyZWQgb24gdGhlIEludGVybmV0LCB0aGUgQXRvcyBncm91cCBsaWFiaT0KbGl0eSBjYW5ub3QgYmUgdHJpZ2dlcmVkIGZvciB0aGUgbWVzc2FnZSBjb250ZW50LiBBbHRob3VnaCB0aGUgc2VuZGVyIGVuZGVhPQp2b3JzIHRvIG1haW50YWluIGEgY29tcHV0ZXIgdmlydXMtZnJlZSBuZXR3b3JrLCB0aGUgc2VuZGVyIGRvZXMgbm90IHdhcnJhbnQ9CiB0aGF0IHRoaXMgdHJhbnNtaXNzaW9uIGlzIHZpcnVzLWZyZWUgYW5kIHdpbGwgbm90IGJlIGxpYWJsZSBmb3IgYW55IGRhbWFnZT0KcyByZXN1bHRpbmcgZnJvbSBhbnkgdmlydXMgdHJhbnNtaXR0ZWQuIFRoaXMgZS1tYWlsIHdhcyBzZW50IGF1dG9tYXRpY2FsbHkuPQogUGxlYXNlIGRvbid0IHNlbmQgYW5zd2VycyB0byB0aGlzIGVtYWlsIGFkZHJlc3MuIn0KClJlZjpNU0c1NjYwNjg0NTg=";
        private readonly int port = 25;
        private readonly IOneSourceRepository _oneSourceRepository;
        private readonly ILogger<SMTPServerService> _logger;
        private readonly IConfiguration _configuration;

        private static Queue<MimeMessage> emailMessageQueue = new Queue<MimeMessage>();

        public SMTPServerService(IOneSourceRepository oneSourceRepository, ILogger<SMTPServerService> logger, IConfiguration configuration)
        {
            _oneSourceRepository = oneSourceRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task HandleClientAsync(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(IPAddress.Any, port);

            try
            {
                RecurringJob.AddOrUpdate(
                    "HandleEmailMessages",
                    () => HandleEmailMessages(CancellationToken.None),
                    "*/5 * * * * *"
                );

                RecurringJob.AddOrUpdate(
                    "DeleteOldEmailsAndLogs",
                    () => DeleteOldEmailsAndLogs(CancellationToken.None),
                    "0 * * * *"
                );

                listener.Start();

                _logger.LogInformation("Started TCP listener to listen for incoming emails from atosonesource.com.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (enququeTestEmailMessage)
                    {
                        EnqueueTestEmailMessage();
                    }

                    TcpClient client = await listener.AcceptTcpClientAsync();

                    await EnqueueEmailMessage(client);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Server: Operation failed.");
            }
            finally
            {
                listener.Stop();

                _logger.LogInformation("Stopped TCP listener for listening for incoming emails from atosonesource.com.");
            }
        }

        private void EnqueueTestEmailMessage()
        {
            _logger.LogInformation("Enqueuing the test email message started.");

            byte[] buff = Convert.FromBase64String(testEmailMessage);

            MimeMessage message;

            using (Stream stream = new MemoryStream(buff))
            {
                message = MimeMessage.Load(stream);
            }

            emailMessageQueue.Enqueue(message);

            _logger.LogInformation("Enqueuing the test email message ended.");
        }

        private async Task EnqueueEmailMessage(TcpClient client)
        {
            _logger.LogInformation("Intercepted an email message. Enqueuing the message started.");

            using (NetworkStream stream = client.GetStream())
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    string emailContent;

                    while ((emailContent = await reader.ReadToEndAsync()) != null)
                    {
                        MimeMessage message;

                        using (Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(emailContent)))
                        {
                            message = MimeMessage.Load(memoryStream);
                        }

                        emailMessageQueue.Enqueue(message);
                    }
                }

                client.Close();
            }

            _logger.LogInformation("Enqueuing the email message ended.");
        }

        public async Task HandleEmailMessages(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string mailDir = _configuration["MailDirectory"];

                _logger.LogInformation("HandleEmailMessages job: Started handling email received messages.");

                while (emailMessageQueue.TryPeek(out var emailMessage))
                {
                    emailMessageQueue.Dequeue();

                    var message = emailMessage;

                    string createdTimestamp = GetHeaderValue(message.Headers, "CreatedTimestamp");

                    var createdTimestampDate = !string.IsNullOrWhiteSpace(createdTimestamp) ? DateTime.Parse(createdTimestamp) : DateTime.Now;

                    string fileName = GetFileNameFromSessionInfo(mailDir, createdTimestampDate);

                    _logger.LogInformation("HandleEmailMessages job: Start receiving mail: {0}", fileName);

                    await message.WriteToAsync(fileName);

                    _logger.LogInformation("HandleEmailMessages job: Saved mail to '{0}'.", fileName);

                    string toEmailSingle = "";

                    MailboxAddress from = (MailboxAddress)message.From[0];
                    string fromEmail = from.Address;

                    List<string> toEmail = new List<string>();

                    _logger.LogInformation("HandleEmailMessages job: Saving emails for recipients.");

                    foreach (InternetAddress mailAux in message.To)
                    {
                        if (mailAux.GetType() == new GroupAddress("test").GetType())
                        {
                            GroupAddress grp = (GroupAddress)mailAux;
                            try
                            {
                                if (grp != null && grp.Members != null && grp.Members.Count > 0)
                                {
                                    foreach (MailboxAddress mlAux in grp.Members)
                                    {
                                        MailboxAddress frm = mlAux;
                                        toEmailSingle = frm.Address; //For use in exception if it's necessary
                                        if (!toEmail.Contains(frm.Address))
                                        {
                                            toEmail.Add(frm.Address);
                                        }

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var smtpLog = new SMTPLog
                                {
                                    EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                    From = fromEmail,
                                    To = toEmailSingle,
                                    Subject = message.Subject.Replace("'", "''"),
                                    Mode = "SMTP IN - CRASH",
                                    RuleName = ex.Message,
                                    IsEnabled = false
                                };

                                await _oneSourceRepository.AddAsync(smtpLog);
                                throw;
                            }
                        }
                        else
                        {
                            try
                            {
                                MailboxAddress frm = (MailboxAddress)mailAux;
                                toEmailSingle = frm.Address; //For use in exception if it's necessary
                                if (!toEmail.Contains(frm.Address))
                                {
                                    toEmail.Add(frm.Address);
                                }
                            }
                            catch (Exception ex)
                            {
                                var smtpLog = new SMTPLog
                                {
                                    EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                    From = fromEmail,
                                    To = toEmailSingle,
                                    Subject = message.Subject.Replace("'", "''"),
                                    Mode = "SMTP IN - CRASH",
                                    RuleName = ex.Message,
                                    IsEnabled = false
                                };

                                await _oneSourceRepository.AddAsync(smtpLog);
                                throw;
                            }
                        }
                    }

                    _logger.LogInformation("HandleEmailMessages job: Saved emails for recipients successfully.");

                    _logger.LogInformation("HandleEmailMessages job: Saving emails for CC recipients.");

                    foreach (InternetAddress iaAux in message.Cc)
                    {
                        if (iaAux.GetType() == new GroupAddress("test").GetType())
                        {
                            GroupAddress grp = (GroupAddress)iaAux;
                            try
                            {
                                if (grp != null && grp.Members != null && grp.Members.Count > 0)
                                {
                                    foreach (MailboxAddress mlAux in grp.Members)
                                    {
                                        MailboxAddress frm = mlAux;
                                        toEmailSingle = frm.Address; //For use in exception if it's necessary
                                        if (!toEmail.Contains(frm.Address))
                                        {
                                            toEmail.Add(frm.Address);
                                        }

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var smtpLog = new SMTPLog
                                {
                                    EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                    From = fromEmail,
                                    To = toEmailSingle,
                                    Subject = message.Subject.Replace("'", "''"),
                                    Mode = "SMTP IN - CRASH",
                                    RuleName = ex.Message,
                                    IsEnabled = false
                                };

                                await _oneSourceRepository.AddAsync(smtpLog);
                                throw;
                            }
                        }
                        else
                        {
                            try
                            {
                                MailboxAddress frm = (MailboxAddress)iaAux;
                                toEmailSingle = frm.Address; //For use in exception if it's necessary
                                if (!toEmail.Contains(frm.Address))
                                {
                                    toEmail.Add(frm.Address);
                                }
                            }
                            catch (Exception ex)
                            {
                                var smtpLog = new SMTPLog
                                {
                                    EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                    From = fromEmail.Replace("'", "''"),
                                    To = toEmailSingle.Replace("'", "''"),
                                    Subject = message.Subject.Replace("'", "''"),
                                    Mode = "SMTP IN - CRASH",
                                    RuleName = ex.Message,
                                    IsEnabled = false
                                };

                                await _oneSourceRepository.AddAsync(smtpLog);
                                throw;
                            }
                        }
                    }

                    _logger.LogInformation("HandleEmailMessages job: Saved emails for CC recipients successfully.");

                    _logger.LogInformation("HandleEmailMessages job: Saving emails for senders.");

                    string htmlBody = message.HtmlBody;

                    if (htmlBody == null)
                    {
                        htmlBody = message.TextBody;
                    }

                    if (toEmail.Count() == 0)
                    {
                        toEmail.Add("");
                    }

                    byte[] buff = await File.ReadAllBytesAsync(fileName);
                    string contentEncoded = Convert.ToBase64String(buff);

                    foreach (string toEmailAux in toEmail)
                    {
                        var smtpLog = new SMTPLog
                        {
                            EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                            From = fromEmail.Replace("'", "''"),
                            To = toEmailAux.Replace("'", "''"),
                            Subject = message.Subject.Replace("'", "''"),
                            Mode = "SMTP RECEIVED - PRE",
                            RuleName = "",
                            IsEnabled = false
                        };

                        await _oneSourceRepository.AddAsync(smtpLog);

                        var mappings = await _oneSourceRepository.GetAllAsync<MappingSMTPReceiver>();

                        foreach (var mapping in mappings)
                        {
                            var json = new
                            {
                                fromEmail= fromEmail,
                                toEmail= toEmailAux,
                                subject= message.Subject,
                                originalEML= Path.GetFileName(fileName),
                                messageContent= contentEncoded
                            };

                            var jsonString = JsonConvert.SerializeObject(json);

                            var fullcontent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                            string destInstance = "";

                            //if toEmail condition match
                            if (!string.IsNullOrWhiteSpace(mapping.ToEmail) && mapping.ToEmail == toEmailAux)
                            {
                                destInstance = mapping.DestinationInstance;

                                //if containsString condition match
                                if (!string.IsNullOrEmpty(mapping.ContainsString) && htmlBody != null && htmlBody.ToLower().Contains(mapping.ContainsString))
                                {
                                    destInstance = mapping.DestinationInstance;
                                }
                                else if (mapping.ContainsString != null && mapping.ContainsString == "")
                                {
                                    destInstance = mapping.DestinationInstance;
                                }
                                else if (mapping.ContainsString == null)
                                {
                                    destInstance = mapping.DestinationInstance;
                                }
                                else
                                {
                                    destInstance = "";
                                }

                                if (mapping.DiscardInternal != null && mapping.DiscardInternal == true && htmlBody != null && (htmlBody.Contains("Owning GBU") || htmlBody.Contains("Leading GBU")))
                                {
                                    destInstance = "";
                                }

                            }

                            if (!string.IsNullOrWhiteSpace(destInstance))
                            {
                                //Indicate the process mode (MIP1, BODYSAVER, FILESAVER, BACKUPS, ...)

                                try
                                {
                                    bool isEnabled = mapping.IsEnabled;


                                    if (isEnabled)
                                    {
                                        var smtpLog2 = new SMTPLog
                                        {
                                            EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                            From = fromEmail.Replace("'", "''"),
                                            To = toEmailAux.Replace("'", "''"),
                                            Subject = message.Subject.Replace("'", "''"),
                                            Mode = mapping.Mode,
                                            RuleName = mapping.Id + ": " + mapping.Description.Replace("'", "''") + " > " + mapping.DestinationInstance.Replace("'", "''"),
                                            IsEnabled = true
                                        };

                                        await _oneSourceRepository.AddAsync(smtpLog2);

                                        var httpClient = new HttpClient();

                                        string url = destInstance + "/api/SMTPReceiver";

                                        if (destInstance.Trim() == "52.211.50.245") //dev without https
                                        {
                                            url = destInstance + "/api/SMTPReceiver";
                                        }

                                        HttpResponseMessage remoteServerResponse = null;

                                        try
                                        {

                                            remoteServerResponse = await httpClient.PostAsync(url, fullcontent);
                                            if (remoteServerResponse.IsSuccessStatusCode)
                                            {
                                                string result = remoteServerResponse.Content.ToString();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, $"HandleEmailMessages job: An error occured. Sender email: {fromEmail}, recipient email: {toEmailAux}, email file: {Path.GetFileName(fileName)}");
                                        }
                                    }
                                    else
                                    {
                                        var smtpLog3 = new SMTPLog
                                        {
                                            EmlPath = Path.GetFileName(fileName).Replace("'", "''"),
                                            From = fromEmail.Replace("'", "''"),
                                            To = toEmailAux.Replace("'", "''"),
                                            Subject = message.Subject.Replace("'", "''"),
                                            Mode = mapping.Mode,
                                            RuleName = "*** " + mapping.Id + ": " + mapping.Description.Replace("'", "''") + " > " + mapping.DestinationInstance.Replace("'", "''"),
                                            IsEnabled = false
                                        };

                                        await _oneSourceRepository.AddAsync(smtpLog3);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"HandleEmailMessages job: An error occured. Sender email: {fromEmail}, recipient email: {toEmailAux}, email file: {Path.GetFileName(fileName)}");
                                    throw;
                                }

                            }
                        }
                    }

                    _logger.LogInformation("HandleEmailMessages job: Handling the following email message: " + emailMessage.Subject);
                }

                _logger.LogInformation("HandleEmailMessages job: Ended handling email received messages.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandleEmailMessages job: Service error occured");
            }
            
        }

        public async Task DeleteOldEmailsAndLogs(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DeleteOldEmailsAndLogs job: Started deleting old emails and logs.");

            string mailDir = _configuration["MailDirectory"];

            string timeForPersistingDataInMonthsValue = _configuration["DataRetentionPolicy:TimeForPersistingDataInMonths"];

            int timeForPersistingDataInMonths = int.Parse(timeForPersistingDataInMonthsValue);

            var latestDateToPersistData = DateTime.Now.AddMonths(-timeForPersistingDataInMonths);

            var logsToDeltete = await _oneSourceRepository.GetAsync<SMTPLog>(l => l.LastUpdate < latestDateToPersistData);

            _logger.LogInformation("DeleteOldEmailsAndLogs job: Started deleting old SMTP logs.");

            if (logsToDeltete.Count > 0)
            {
                await _oneSourceRepository.RemoveRangeAsync(logsToDeltete);
            }

            _logger.LogInformation("DeleteOldEmailsAndLogs job: Finished deleting old SMTP logs.");

            _logger.LogInformation("DeleteOldEmailsAndLogs job: Started deleting old EML email files.");

            string[] files = Directory.GetFiles(mailDir);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);

                if (fi.CreationTime < latestDateToPersistData)
                {
                    fi.Delete();
                }
            }

            _logger.LogInformation("DeleteOldEmailsAndLogs job: Finished deleting old EML email files.");
        }

        private string GetHeaderValue(IEnumerable<Header> headers, string headerName)
        {
            foreach (var header in headers)
            {
                if (header.Field.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Value;
                }
            }
            return null;
        }
        private string GetFileNameFromSessionInfo(string mailDir, DateTime createdTimestampDate)
        {
            var fileName = createdTimestampDate.ToString("yyyy-MM-dd_HHmmss_fff") + ".eml";
            var fullName = Path.Combine(mailDir, fileName);
            return fullName;
        }
    }
}
