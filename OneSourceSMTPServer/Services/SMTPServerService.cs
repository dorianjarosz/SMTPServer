using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using MimeKit;
using Hangfire;
using OneSourceSMTPServer.Data.Entities;
using OneSourceSMTPServer.Repositories;
using System.Net;

namespace OneSourceSMTPServer.Services
{
    public class SMTPServerService : ISMTPServerService
    {
        private bool serviceStarted = false;
        private bool enququeTestEmailMessages = false;
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
            var listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!serviceStarted)
                    {
                        _logger.LogInformation("Starting handling email messages hangfire task.");

                        RecurringJob.AddOrUpdate(
                            "HandleEmailMessages",
                            () => HandleEmailMessages(CancellationToken.None),
                            "*/5 * * * * *"
                        );

                        _logger.LogInformation("Started handling email messages hangfire task.");

                        _logger.LogInformation("Starting deleting old email and logs hangfire task.");

                        RecurringJob.AddOrUpdate(
                            "DeleteOldEmailsAndLogs",
                            () => DeleteOldEmailsAndLogs(CancellationToken.None),
                            _configuration["DataRetentionPolicy:DeleteOldLogsAndEmailsCronExpression"]
                        );

                        _logger.LogInformation("Started deleting old email and logs hangfire task.");

                        listener.Start();

                        _logger.LogInformation("Started TCP listener to listen for incoming emails from Test1");

                        _ = EnqueueEmailMessage(listener);

                        serviceStarted = true;
                    }

                    await Task.Delay(60 * 1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Server: Operation failed.");
                throw;
            }
            finally
            {
                _logger.LogInformation("Stopped TCP listener for listening for incoming emails from atosonesource.com.");
            }
        }

        private async Task EnqueueTestEmailMessage()
        {
            _logger.LogInformation("Enqueuing the test email message started.");

            foreach (var filePath in Directory.GetFiles("C:\\TestEmails"))
            {
                using (var streamReader = new StreamReader(filePath))
                {
                    string emailContent = emailContent = await streamReader.ReadToEndAsync();

                    MimeMessage message;

                    using (Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(emailContent)))
                    {
                        message = MimeMessage.Load(memoryStream);
                    }

                    emailMessageQueue.Enqueue(message);
                }
            }

            _logger.LogInformation("Enqueuing the test email message ended.");
        }

        private async Task EnqueueEmailMessage(TcpListener listener)
        {
            while (true)
            {
                _logger.LogInformation("Started listening on TCP.");

                TcpClient client = await listener.AcceptTcpClientAsync();

                _logger.LogInformation("Intercepted an email message. Enqueuing the message started.");

                File.Create("C:\\SMTPReceiver\\ReceivedEmails\\hurray.txt");

                //using (NetworkStream stream = client.GetStream())
                //{
                //    using (var reader = new StreamReader(stream, Encoding.ASCII))
                //    {
                //        string emailContent;

                //        while ((emailContent = await reader.ReadToEndAsync()) != null)
                //        {
                //            MimeMessage message;

                //            using (Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(emailContent)))
                //            {
                //                message = MimeMessage.Load(memoryStream);
                //            }

                //            emailMessageQueue.Enqueue(message);
                //        }
                //    }

                    client.Close();
                //}

                _logger.LogInformation("Enqueuing the email message ended.");
            }
        }

        public async Task HandleEmailMessages(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string mailDir = _configuration["SMTPReceiver:ReceivedEmailsDirectory"];

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
                                mode = mapping.Mode,
                                fromEmail = fromEmail,
                                toEmail = toEmailAux,
                                subject = message.Subject,
                                originalEML = Path.GetFileName(fileName),
                                messageContent = contentEncoded
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

            string mailDir = _configuration["SMTPReceiver:ReceivedEmailsDirectory"];

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
