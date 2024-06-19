using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using MimeKit;
using Hangfire;
using OneSourceSMTPServer.Data.Entities;
using OneSourceSMTPServer.Repositories;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Hangfire.MAMQSqlExtension;
using Microsoft.Extensions.Logging;
using Azure.Messaging;
using static System.Collections.Specialized.BitVector32;

namespace OneSourceSMTPServer.Services
{
    public class SMTPServerService : ISMTPServerService
    {
        private bool serviceStarted = false;
        private readonly int port = 25;
        private readonly IOneSourceRepository _oneSourceRepository;
        private readonly ILogger<SMTPServerService> _logger;
        private readonly IConfiguration _configuration;
        private TcpListener listener;
        private static Queue<MimeMessage> emailMessageQueue = new Queue<MimeMessage>();

        public SMTPServerService(IOneSourceRepository oneSourceRepository, ILogger<SMTPServerService> logger, IConfiguration configuration)
        {
            _oneSourceRepository = oneSourceRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task HandleClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                var listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!serviceStarted)
                    {
                        _logger.LogInformation("Starting handling email messages hangfire task.");

                        RecurringJob.AddOrUpdate(
                            "HandleEmailMessages",
                            () => HandleEmailMessages(CancellationToken.None),
                            _configuration["SMTPReceiver:HandleEmailIntervalCronExpression"], new RecurringJobOptions
                            {
                                QueueName= "onesource_main_queue"
                            }
                        );

                        _logger.LogInformation("Started handling email messages hangfire task.");

                        _logger.LogInformation("Starting deleting old email and logs hangfire task.");

                        RecurringJob.AddOrUpdate(
                            "DeleteOldEmailsAndLogs",
                            () => DeleteOldEmailsAndLogs(CancellationToken.None),
                            _configuration["DataRetentionPolicy:DeleteOldLogsAndEmailsCronExpression"], new RecurringJobOptions
                            {
                                QueueName = "onesource_main_queue"
                            }
                        );

                        _logger.LogInformation("Started deleting old email and logs hangfire task.");

                        _logger.LogInformation("Started TCP listener to listen for incoming emails from Test1");

                        _logger.LogInformation("Started listening on TCP.");

                        listener.Start();

                        _logger.LogInformation("Started TCP listener to listen for incoming emails from Test1");

                        _ = EnqueueEmailMessage(listener, cancellationToken);

                        serviceStarted = true;
                    }

                    await Task.Delay(1000 * 60);
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

        public void StopListener()
        {
            listener.Stop();
        }

        private async Task EnqueueEmailMessage(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {

                    _logger.LogInformation("Started listening on TCP.");

                    TcpClient client = await listener.AcceptTcpClientAsync();

                    _logger.LogInformation("Intercepted an email message. Enqueuing the message started.");

                    using (NetworkStream networkStream = client.GetStream())
                    {
                        _logger.LogInformation("Opening the network stream.");

                        using (var reader = new StreamReader(networkStream, Encoding.ASCII))
                        {
                            using (var writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true })
                            {
                                await writer.WriteLineAsync("220 OneSourceSMTPServer");

                                _logger.LogInformation("Opening the stream in the stream reader.");

                                var emailBuilder = new StringBuilder();

                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    string line = await reader.ReadLineAsync();

                                    if (line == null)
                                        break;

                                    _logger.LogInformation($"Line that has been read: {line}");

                                    if (line.StartsWith("EHLO") || line.StartsWith("HELO"))
                                    {
                                        await writer.WriteLineAsync("250 Hello");
                                    }
                                    else if (line.StartsWith("MAIL FROM"))
                                    {
                                        await writer.WriteLineAsync("250 OK");
                                    }
                                    else if (line.StartsWith("RCPT TO"))
                                    {
                                        await writer.WriteLineAsync("250 OK");
                                    }
                                    else if (line.StartsWith("DATA"))
                                    {
                                        await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                                        while (true)
                                        {
                                            line = await reader.ReadLineAsync();
                                            if (line == ".")
                                                break;
                                            emailBuilder.AppendLine(line);
                                        }

                                        _logger.LogInformation($"Email subject to parse: {emailBuilder}");

                                        var email = ParseEmail(emailBuilder.ToString());

                                        _logger.LogInformation($"Parsed email subject: {email.Subject}");

                                        _logger.LogInformation($"Enqueued the following email message: {email}");

                                        emailMessageQueue.Enqueue(email);

                                        _logger.LogInformation("Enqueuing the email message ended.");

                                        emailBuilder.Clear();

                                        await writer.WriteLineAsync("250 OK");
                                    }
                                    else if (line.StartsWith("QUIT"))
                                    {
                                        await writer.WriteLineAsync("221 Bye");
                                        break;
                                    }
                                    else
                                    {
                                        _logger.LogInformation("500 Command not recognized.");

                                        await writer.WriteLineAsync("500 Command not recognized");
                                    }
                                }
                            }

                            _logger.LogInformation("Closing the stream in the stream reader.");
                        }
                    }

                    client.Close();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Operation cancelled");
                throw;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Format exception");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Server: Operation failed.");
                throw;
            }
        }

        [RetryInQueue("onesource_main_queue")]
        public async Task HandleEmailMessages(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string mailDir = _configuration["SMTPReceiver:ReceivedEmailsDirectory"];

            _logger.LogInformation("HandleEmailMessages job: Started handling email received messages.");

            while (emailMessageQueue.TryPeek(out var emailMessage))
            {
                try
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

                                        string apiUrl;

                                        var json = new
                                        {
                                            mode = mapping.Mode,
                                            fromEmail = fromEmail,
                                            toEmail = toEmailAux,
                                            subject = message.Subject,
                                            originalEML = Path.GetFileName(fileName),
                                            MessageContent = contentEncoded,
                                            dataAccess=mapping.DataAccess,
                                            category=mapping.Category,
                                            section=mapping.Section,
                                            MenuEntryName=mapping.MenuEntryName,
                                        };

                                        StringContent fullcontent;

                                        var jsonString = JsonConvert.SerializeObject(json);

                                        fullcontent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                                        if (mapping.DestinationInstanceVersion == "V3")
                                        {
                                            apiUrl = "/api/SMTPReceiver";
                                        }
                                        else
                                        {
                                            apiUrl = "/Admin/SMTPReceiver.aspx?lng=EN&mode=" + mapping.Mode;
                                        }

                                        string url = destInstance + apiUrl;

                                        if (destInstance.Trim() == "52.211.50.245") //dev without https
                                        {
                                            url = destInstance + apiUrl;
                                        }

                                        HttpResponseMessage remoteServerResponse = null;

                                        try
                                        {
                                            _logger.LogInformation("Forwarding the email.");

                                            remoteServerResponse = await httpClient.PostAsync(url, fullcontent);

                                            if (remoteServerResponse.IsSuccessStatusCode)
                                            {
                                                _logger.LogInformation("Successfully forwarded the email.");

                                                string result = remoteServerResponse.Content.ToString();
                                            }
                                            else
                                            {
                                                _logger.LogError(remoteServerResponse.StatusCode+" has been returned. "+ remoteServerResponse.Content?.ToString() ?? null);
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HandleEmailMessages job: Service error occured");
                }
            }

            _logger.LogInformation("HandleEmailMessages job: Ended handling email received messages.");
        }

        [RetryInQueue("onesource_main_queue")]
        public async Task DeleteOldEmailsAndLogs(CancellationToken cancellationToken)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteOldEmailsAndLogs job: Service error occured");
            }
        }

        private MimeMessage ParseEmail(string rawData)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(rawData.Replace("\r\n", "\n").Replace("\n", "\r\n")));
            return MimeMessage.Load(stream);
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
